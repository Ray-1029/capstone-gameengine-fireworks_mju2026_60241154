using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LanternFestival;

public static class SpatialUpgrade
{
    const string Root="Assets/Festival";
    [Serializable] class Features { public Onset[] onsets; }
    [Serializable] class Onset { public double t; public float flux,rms; }
    [MenuItem("Festival/Apply wide and natural fireworks revision")]
    public static void Upgrade()
    {
        Directory.CreateDirectory("Verification/Wide");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!=Root+"/Scenes/LanternFestival.unity") scene=EditorSceneManager.OpenScene(Root+"/Scenes/LanternFestival.unity",OpenSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);
        var show=AssetDatabase.LoadAssetAtPath<FireworkShowData>(Root+"/Data/ShowTimeline.asset");
        var camera=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
        // Batch mode may start with a 4:3 Game view; compose against the delivered 16:9 frame.
        camera.aspect=16f/9;
        if(show.paradeVersion<2)
        {
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/LanternFestival_BeforeWide.unity",true);
            AssetDatabase.CopyAsset(Root+"/Data/ShowTimeline.asset",Root+"/Data/ShowTimeline_BeforeWide.asset");
        }
        var prefabs=new Dictionary<string,FireworkEffect>();ParadeUpgrade.ConfigureEffects(prefabs);
            int gold=0,willow=0,pattern=0,glitter=0,comet=0;
            foreach(var cue in show.cues.Where(c=>!c.label.StartsWith("CALM")&&!c.label.StartsWith("WIDE")))
            {
                if(cue.brightness<=0) cue.brightness=1;
                if(cue.label.StartsWith("Opening •")||cue.label.StartsWith("FIRST GRAND")||cue.label=="FINAL • central gold") continue;
                float u=.54f;
                switch(cue.prefab.shape)
                {
                    case FireworkShape.Chrysanthemum: u=gold++%2==0?(cue.scale>=.95f?.46f:.39f):.70f;break;
                    case FireworkShape.Willow: u=willow++%2==0?.44f:.76f;break;
                    case FireworkShape.RedBurst:
                        u=cue.label.Contains("larger centre")?.54f:cue.launchPosition.x<0?.09f:.88f;
                        if(cue.label.StartsWith("FINAL")) u=cue.launchPosition.x<0?.33f:.84f;
                        break;
                    case FireworkShape.Fan:u=cue.launchPosition.x<0?.12f:.86f;break;
                    case FireworkShape.Comet:u=new[]{.10f,.88f,.51f}[comet++%3];break;
                    case FireworkShape.Glitter:u=new[]{.09f,.88f,.34f,.91f}[glitter++%4];break;
                    default:u=pattern++%2==0?.33f:.84f;break;
                }
                Place(camera,cue,u);
                if(show.paradeVersion<2) cue.musicalEvidence+=$" | Wide revision: projected burst centre at screen X={u:F2}.";
            }
        if(show.paradeVersion<2)
        {
            var onsets=JsonUtility.FromJson<Features>(File.ReadAllText("Analysis/features.json")).onsets;
            int serial=0;
            Action<double,FireworkShape,float,float,float,float,bool> add=(target,kind,u,height,scale,brightness,calm)=>
            {
                var onset=onsets.OrderBy(o=>Math.Abs(o.t-target)).First();
                if(Math.Abs(onset.t-target)>.15) throw new Exception("No source-file onset near "+target);
                var cue=new FireworkCue {label=$"{(calm?"CALM":"WIDE")} • {kind} {++serial:00}",referenceTime=onset.t,anchor=CueAnchor.Burst,riseTime=calm?1.6f:2.1f,launchPosition=new Vector3(0,0,78),height=height,scale=scale,brightness=brightness,prefab=prefabs[kind.ToString()],seed=19001+serial*157,musicalEvidence=$"Measured source attack {onset.t:F4}s, spectral flux {onset.flux:F4}; "+(calm?"restrained small embers across a lower-energy phrase, keeping the large returns distinct.":"outer-sky accent supporting the strong phrase.")};
                Place(camera,cue,u);show.cues.Add(cue);
            };
            double[] calm={8.9113,15.7413,17.0813,53.4513,56.3813,59.8213,62.5513,80.7213,82.5513,86.1913,90.7313,110.6013,112.4513,114.8013,126.1813,129.0413,131.6413,135.2813,138.4313,142.0913,150.5213};
            for(int i=0;i<calm.Length;i++)
            {
                var kind=i%3==0?FireworkShape.FallingLeaves:i%3==1?FireworkShape.Palm:FireworkShape.Peony;
                float u=new[]{.095f,.88f,.35f,.90f,.10f,.79f}[i%6];
                add(calm[i],kind,u,32+(i%3)*4,kind==FireworkShape.FallingLeaves?.70f:kind==FireworkShape.Palm?.57f:.42f,kind==FireworkShape.FallingLeaves?.65f:.48f,true);
            }
            add(69.8213,FireworkShape.Palm,.86f,48,.95f,1,false);
            add(104.3713,FireworkShape.Palm,.34f,47,.95f,1,false);
            add(157.0913,FireworkShape.Palm,.85f,48,1,1,false);
            add(181.1413,FireworkShape.Palm,.33f,47,.90f,1,false);
            add(68.0013,FireworkShape.Peony,.90f,38,.75f,1,false);
            add(97.1013,FireworkShape.Peony,.10f,39,.65f,1,false);
            add(155.2713,FireworkShape.Peony,.89f,40,.82f,1,false);
            add(179.3713,FireworkShape.Peony,.10f,41,.68f,1,false);
            add(75.2713,FireworkShape.Crossette,.09f,36,.68f,1,false);
            add(105.2713,FireworkShape.Crossette,.88f,45,.83f,1,false);
            add(121.6413,FireworkShape.Crossette,.09f,36,.65f,1,false);
            add(164.3613,FireworkShape.Crossette,.88f,43,.90f,1,false);
            show.cues=show.cues.OrderBy(c=>c.referenceTime).ThenBy(c=>c.label).ToList();
            show.paradeVersion=2;
            show.analysisNotes+=" Wide/natural revision: 163 cues, 13 types; 21 low-intensity calm cues and 12 outer-sky accents. Pattern shells use jittered ignition, individual drag/burnout and broken short trails. Main source-file anchors unchanged.";
        }
        foreach(var cue in show.cues.Where(c=>c.label.StartsWith("CALM")||c.label.StartsWith("WIDE")))
        {
            int index=int.Parse(cue.label.Substring(cue.label.Length-2))-1;
            float u=cue.label.StartsWith("CALM")?new[]{.095f,.88f,.35f,.90f,.10f,.79f}[index%6]:new[]{.86f,.34f,.85f,.33f,.90f,.10f,.89f,.10f,.09f,.88f,.09f,.88f}[index-21];
            Place(camera,cue,u);
        }
        // Keep the largest shells wholly in the open sky to the right of the pillar.
        foreach(var cue in show.cues.Where(c=>c.prefab.shape==FireworkShape.Chrysanthemum&&c.scale>=.95f))
            if(camera.WorldToViewportPoint(cue.launchPosition+Vector3.up*cue.height).x<.45f) Place(camera,cue,.46f);
        var look=camera.GetComponent<FestivalLookCamera>();if(!look)look=camera.gameObject.AddComponent<FestivalLookCamera>();look.showHint=true;
        EditorUtility.SetDirty(show);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        ParadeUpgrade.ExportTimeline(show);
        FestivalBuilder.Validate();ParadeUpgrade.VerifyShapesAndCamera();Validate();Capture();
        EditorSceneManager.SaveScene(scene);Selection.activeObject=show;
        Debug.Log("WIDE_UPGRADE_COMPLETE cues="+show.cues.Count);
        camera.ResetAspect();
    }
    static void Place(Camera camera,FireworkCue cue,float screenX)
    {
        Vector3 centre=cue.launchPosition+Vector3.up*cue.height;
        Vector3 screen=camera.WorldToViewportPoint(centre);screen.x=screenX;
        cue.launchPosition.x=camera.ViewportToWorldPoint(screen).x;
    }
    public static void Validate()
    {
        var show=AssetDatabase.LoadAssetAtPath<FireworkShowData>(Root+"/Data/ShowTimeline.asset");var camera=Camera.main;camera.aspect=16f/9;
        var lines=new List<string>();int errors=0;
        Action<bool,string> check=(ok,text)=>{lines.Add((ok?"PASS ":"FAIL ")+text);if(!ok)errors++;};
        check(show.cues.Count==163,"130 existing + 21 calm + 12 outer-sky cues");
        check(show.cues.Select(c=>c.prefab.shape).Distinct().Count()==13,"All thirteen types present in the show");
        var bursts=show.cues.Where(c=>c.HasBurst).ToArray();
        int left=bursts.Count(c=>camera.WorldToViewportPoint(c.launchPosition+Vector3.up*c.height).x<.18f);
        int right=bursts.Count(c=>camera.WorldToViewportPoint(c.launchPosition+Vector3.up*c.height).x>.80f);
        check(left>=18&&right>=30,$"Distributed burst centres: left outer sky {left}, right outer sky {right}");
        foreach(var c in bursts)
        {
            var p=camera.WorldToViewportPoint(c.launchPosition+Vector3.up*c.height);
            check(p.x>.04f&&p.x<.96f&&p.y>.23f&&p.y<.90f,"Visible centre: "+c.label);
        }
        var quiet=show.cues.Where(c=>c.label.StartsWith("CALM")).ToArray();
        check(quiet.All(c=>c.brightness<=.65f&&c.scale<=.71f),"Calm cues are small and dimmer than highlights");
        check(quiet.All(c=>!(c.referenceTime>64&&c.referenceTime<66.1813)),"Pre-grand pause retained");
        double[,] rests={{13.86,13.983},{14.31,14.443},{35.43,35.613},{52.88,53.003},{65.13,65.263},{65.36,65.483},{188.22,189.64}};
        foreach(var c in show.cues.Where(c=>c.label.StartsWith("CALM")||c.label.StartsWith("WIDE")))
        {
            bool clear=true;for(int i=0;i<rests.GetLength(0);i++)clear&=!(c.referenceTime>=rests[i,0]&&c.referenceTime<=rests[i,1])&&!(c.LaunchTime>=rests[i,0]&&c.LaunchTime<=rests[i,1]);
            check(clear,"No added launch/flash inside measured rest: "+c.label);
        }
        lines.Add("SUMMARY errors="+errors);Directory.CreateDirectory("Verification/Wide");File.WriteAllLines("Verification/Wide/wide-validation.txt",lines);
        if(errors>0)throw new Exception("Wide verification failed: "+errors);
    }
    public static void Capture()
    {
        Directory.CreateDirectory("Verification/Wide");var player=UnityEngine.Object.FindFirstObjectByType<FireworkShowPlayer>();var camera=Camera.main;
        var show=player.show;player.ClearEffects();Render(camera,"00_background");
        foreach(var shot in new[]{("01_wide_phrase",76.25),("02_quiet",130.10),("03_highlight",160.6),("04_final",189.3),("05_early_sides",20.0)})
        {player.Seek(shot.Item2,false);Render(camera,shot.Item1);}
        player.ClearEffects();
        foreach(var kind in new[]{FireworkShape.Star,FireworkShape.DoubleRing,FireworkShape.Palm,FireworkShape.Peony,FireworkShape.Crossette,FireworkShape.FallingLeaves})
        {
            var prefab=show.cues.First(c=>c.prefab.shape==kind).prefab;
            var cue=new FireworkCue {prefab=prefab,referenceTime=5,riseTime=2,launchPosition=new Vector3(8,0,78),height=51,scale=1.05f,seed=92};
            var fx=UnityEngine.Object.Instantiate(prefab);fx.Prepare(cue);
            fx.Evaluate(5+(kind==FireworkShape.Palm?2.4:kind==FireworkShape.Crossette?1.9:kind==FireworkShape.FallingLeaves?2.1:1.2));
            Render(camera,"type_"+kind);UnityEngine.Object.DestroyImmediate(fx.gameObject);
        }
        player.ClearEffects();
    }
    static void Render(Camera camera,string name)
    {
        var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB) {antiAliasing=4};
        var previous=camera.targetTexture;var active=RenderTexture.active;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes("Verification/Wide/"+name+".png",texture.EncodeToPNG());}
        finally{camera.targetTexture=previous;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);}
    }
}

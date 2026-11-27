using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using LanternFestival;

public static class ParadeUpgrade
{
    const string Root="Assets/Festival";
    [Serializable] class Features { public Onset[] onsets; }
    [Serializable] class Onset { public double t; public float flux,rms; }
    [Serializable] class Export { public int paradeVersion=1; public string notes; public Row[] cues; }
    [Serializable] class Row { public string label,evidence,kind,anchor; public double time; public float rise,x,y,z,height,scale,brightness=1; public int seed; }

    public static void ConfigureEffects(Dictionary<string,FireworkEffect> effects)
    {
        Configure("Gold_Chrysanthemum",FireworkShape.Chrysanthemum,360,15.5f,5.2f,.58f,.28f,.95f,.12f);
        Configure("Gold_Willow",FireworkShape.Willow,280,12.5f,8.7f,1.8f,.42f,2.1f,.105f);
        Configure("Red_ShortBurst",FireworkShape.RedBurst,180,11.5f,2.4f,.75f,.50f,.45f,.12f);
        Configure("Red_LowFan",FireworkShape.Fan,60,25,1.8f,8,.18f,.8f,.105f);
        Configure("Gold_Star",FireworkShape.Star,180,16,3.9f,1.25f,.24f,.24f,.10f);
        Configure("Gold_Red_MultiBreak",FireworkShape.MultiBreak,180,15.5f,5.7f,.75f,.30f,.65f,.12f);
        Configure("Gold_Strobe",FireworkShape.Glitter,240,13.5f,1.25f,.50f,.20f,.08f,.11f);
        Configure("Gold_Red_DoubleRing",FireworkShape.DoubleRing,156,15,3.8f,1.35f,.25f,.21f,.10f);
        Configure("Gold_Palm",FireworkShape.Palm,60,9.5f,5.6f,3.0f,.25f,1.6f,.13f);
        Configure("Red_Gold_Peony",FireworkShape.Peony,240,12.0f,2.9f,1.1f,.55f,.10f,.12f);
        Configure("Gold_Crossette",FireworkShape.Crossette,60,12.5f,4.8f,.95f,.30f,.48f,.12f);
        Configure("Gold_FallingLeaves",FireworkShape.FallingLeaves,64,5.2f,6.5f,.38f,.48f,.30f,.06f);
        foreach(var shape in Enum.GetValues(typeof(FireworkShape)).Cast<FireworkShape>())
        {
            string name=PrefabName(shape);
            effects[shape.ToString()]=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/"+name+".prefab").GetComponent<FireworkEffect>();
        }
    }
    static string PrefabName(FireworkShape shape)
    {
        switch(shape)
        {
            case FireworkShape.Chrysanthemum:return "Gold_Chrysanthemum";
            case FireworkShape.Willow:return "Gold_Willow";
            case FireworkShape.RedBurst:return "Red_ShortBurst";
            case FireworkShape.Fan:return "Red_LowFan";
            case FireworkShape.Comet:return "Blue_ThinComets";
            case FireworkShape.Star:return "Gold_Star";
            case FireworkShape.MultiBreak:return "Gold_Red_MultiBreak";
            case FireworkShape.Glitter:return "Gold_Strobe";
            case FireworkShape.DoubleRing:return "Gold_Red_DoubleRing";
            case FireworkShape.Palm:return "Gold_Palm";
            case FireworkShape.Peony:return "Red_Gold_Peony";
            case FireworkShape.Crossette:return "Gold_Crossette";
            case FireworkShape.FallingLeaves:return "Gold_FallingLeaves";
            default:throw new ArgumentOutOfRangeException(nameof(shape));
        }
    }
    static void Configure(string name,FireworkShape shape,int rays,float speed,float life,float gravity,float drag,float trail,float width)
    {
        string path=Root+"/Prefabs/"+name+".prefab";
        string source=File.Exists(path)?path:Root+"/Prefabs/Gold_Chrysanthemum.prefab";
        var go=PrefabUtility.LoadPrefabContents(source);
        try
        {
            go.name=name; var fx=go.GetComponent<FireworkEffect>();
            fx.shape=shape; fx.rayCount=rays; fx.speed=speed; fx.lifetime=life;
            fx.gravity=gravity; fx.drag=drag; fx.trailLength=trail; fx.particleWidth=width;
            fx.secondaryDelay=1.15f; fx.satelliteCount=12;
            ColorUtility.TryParseHtmlString("#E63227",out var red); fx.secondaryColor=red;
            var main=fx.cores.main; main.maxParticles=2048;
            main=fx.tails.main; main.maxParticles=32000;
            main=fx.sparks.main; main.maxParticles=8192;
            if(shape==FireworkShape.Star || shape==FireworkShape.DoubleRing)
            {
                var renderer=fx.tails.GetComponent<ParticleSystemRenderer>();
                renderer.velocityScale=.012f;renderer.lengthScale=1.6f;
            }
            if(shape==FireworkShape.Crossette) fx.secondaryDelay=.88f;
            PrefabUtility.SaveAsPrefabAsset(go,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(go); }
    }

    [MenuItem("Festival/Apply parade upgrade (keeps existing scenery)")]
    public static void Upgrade()
    {
        Directory.CreateDirectory("Verification/Parade");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!=Root+"/Scenes/LanternFestival.unity") scene=EditorSceneManager.OpenScene(Root+"/Scenes/LanternFestival.unity",OpenSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);
        var show=AssetDatabase.LoadAssetAtPath<FireworkShowData>(Root+"/Data/ShowTimeline.asset");
        if(!show) throw new Exception("Existing timeline not found");
        if(show.paradeVersion==0)
        {
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/LanternFestival_BeforeParade.unity",true);
            AssetDatabase.CopyAsset(Root+"/Data/ShowTimeline.asset",Root+"/Data/ShowTimeline_BeforeParade.asset");
        }
        var effects=new Dictionary<string,FireworkEffect>(); ConfigureEffects(effects);
        if(show.paradeVersion==0)
        {
            foreach(var cue in show.cues)
            {
                if(cue.prefab.shape==FireworkShape.Comet) continue;
                if(cue.referenceTime>=66)
                {
                    cue.scale*=cue.prefab.shape==FireworkShape.Willow?1.08f:1.12f;
                    if(cue.HasBurst) cue.height+=cue.prefab.shape==FireworkShape.RedBurst?3:8;
                }
                if(cue.label.StartsWith("FINAL") && cue.prefab.shape==FireworkShape.Chrysanthemum) { cue.scale=1.40f; cue.height=58; }
            }
            var measured=JsonUtility.FromJson<Features>(File.ReadAllText("Analysis/features.json")).onsets;
            int index=0;
            Action<double,FireworkShape,float,float,float> add=(target,kind,x,height,scale)=>
            {
                var onset=measured.OrderBy(o=>Math.Abs(o.t-target)).First();
                if(Math.Abs(onset.t-target)>.12) throw new Exception("No measured audio attack near "+target);
                float rise=kind==FireworkShape.Glitter?1.7f:2.3f;
                show.cues.Add(new FireworkCue { label=$"PARADE • {kind} {++index:00}",musicalEvidence=$"Extra parade layer on measured source-file attack {onset.t:F4}s; spectral flux {onset.flux:F4}. "+(kind==FireworkShape.MultiBreak?"Secondary flashes are decorative offsets 1.15–1.54 s after the first flash.":""),referenceTime=onset.t,anchor=CueAnchor.Burst,riseTime=rise,launchPosition=new Vector3(x,0,78),height=height,scale=scale,prefab=effects[kind.ToString()],seed=7100+index*173 });
            };
            double[] stars={29.8213,44.3713,73.4513,98.9113,118.9113,144.3713,162.5413,179.3713};
            for(int i=0;i<stars.Length;i++) add(stars[i],FireworkShape.Star,i%2==0?27:-19,stars[i]<66?32:48,stars[i]<66?.48f:.85f);
            double[] multi={71.6113,104.3713,120.7313,158.9113,166.1813,183.0013};
            for(int i=0;i<multi.Length;i++) add(multi[i],FireworkShape.MultiBreak,i%2==0?24:-14,48+i%2*5,.92f);
            double[] glitter={23.2313,26.1913,33.4613,40.7313,48.0013,68.0013,75.2713,95.2713,108.0013,117.0913,122.5513,155.2713,164.3613,170.1613,177.5913,185.5113};
            for(int i=0;i<glitter.Length;i++) add(glitter[i],FireworkShape.Glitter,i%2==0?-23:31,glitter[i]<66?29:35,glitter[i]<66?.55f:.85f);
            double[] rings={77.0513,100.7313,148.0013,160.7113,174.3713,181.1413};
            for(int i=0;i<rings.Length;i++) add(rings[i],FireworkShape.DoubleRing,i%2==0?-17:27,47,.90f);
            add(188.0213,FireworkShape.Glitter,-25,36,.9f); add(188.0213,FireworkShape.Glitter,35,36,.9f);
            show.cues=show.cues.OrderBy(c=>c.referenceTime).ThenBy(c=>c.label).ToList();
            show.paradeVersion=1;
            show.analysisNotes+=" Parade revision: 130 cues, 9 prefab types. Measured main cue anchors preserved; multibreak child flashes use an explicit relative delay. No extra blue outside the finale.";
        }
        var camera=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
        camera.transform.rotation=Quaternion.Euler(-18,0,0); camera.fieldOfView=58;
        if(!camera.GetComponent<FestivalLookCamera>()) camera.gameObject.AddComponent<FestivalLookCamera>();
        EditorUtility.SetDirty(show); EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        ExportTimeline(show);
        FestivalBuilder.Validate(); VerifyShapesAndCamera(); Capture();
        Selection.activeObject=show;
        Debug.Log("PARADE_UPGRADE_COMPLETE cues="+show.cues.Count);
    }

    public static void ExportTimeline(FireworkShowData show)
    {
        var rows=show.cues.Select(c=>new Row {label=c.label,evidence=c.musicalEvidence,kind=c.prefab.shape.ToString(),anchor=c.anchor.ToString(),time=c.referenceTime,rise=c.riseTime,x=c.launchPosition.x,y=c.launchPosition.y,z=c.launchPosition.z,height=c.height,scale=c.scale,brightness=c.brightness,seed=c.seed}).ToArray();
        File.WriteAllText("Analysis/cues.json",JsonUtility.ToJson(new Export {paradeVersion=show.paradeVersion,notes=show.analysisNotes,cues=rows},true));
        var csv=new StringBuilder("Label,File time seconds,Anchor,Kind,Launch time,Burst time,X,Y,Z,Height,Scale,Brightness,Evidence\n");
        foreach(var c in show.cues) csv.AppendLine(string.Join(",",new[]{Q(c.label),c.referenceTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),c.anchor.ToString(),c.prefab.shape.ToString(),c.LaunchTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),c.HasBurst?c.BurstTime.ToString("F4",System.Globalization.CultureInfo.InvariantCulture):"N/A",c.launchPosition.x.ToString(),c.launchPosition.y.ToString(),c.launchPosition.z.ToString(),c.height.ToString(),c.scale.ToString(),c.brightness.ToString(),Q(c.musicalEvidence)}));
        File.WriteAllText("Analysis/all-cues.csv",csv.ToString(),new UTF8Encoding(true));
    }
    static string Q(string value) { return "\""+value.Replace("\"","\"\"")+"\""; }

    public static void VerifyShapesAndCamera()
    {
        var show=AssetDatabase.LoadAssetAtPath<FireworkShowData>(Root+"/Data/ShowTimeline.asset");
        var lines=new List<string>(); int errors=0;
        Action<bool,string> check=(ok,text)=>{lines.Add((ok?"PASS ":"FAIL ")+text); if(!ok) errors++;};
        check(show.cues.Count>=130,"Original parade cues retained");
        check(show.cues.Select(c=>c.prefab.shape).Distinct().Count()>=9,"At least nine firework types used in the actual show");
        check(show.cues.Where(c=>c.prefab.shape==FireworkShape.Comet).All(c=>c.referenceTime>173.9),"Blue remains finale-only");
        var cam=Camera.main; var look=cam.GetComponent<FestivalLookCamera>();
        var rotation=cam.transform.rotation; float fov=cam.fieldOfView;
        look.Initialize(); look.RotateView(83,24);
        check(Quaternion.Angle(rotation,cam.transform.rotation)>75,"Free look rotates from spectator eye point");
        look.Zoom(1000);check(Math.Abs(cam.fieldOfView-look.minimumFov)<.01,"Zoom minimum clamp");
        look.Zoom(-1000);check(Math.Abs(cam.fieldOfView-look.maximumFov)<.01,"Zoom maximum clamp");
        look.RotateView(0,1000);check(Math.Abs(Mathf.DeltaAngle(0,cam.transform.eulerAngles.x)+look.maximumPitch)<.01,"Pitch clamp");
        look.ResetView();check(Quaternion.Angle(rotation,cam.transform.rotation)<.01&&Math.Abs(fov-cam.fieldOfView)<.01,"C/reset restores original view and field of view");
        foreach(var prefab in show.cues.Select(c=>c.prefab).Distinct())
        {
            var cue=new FireworkCue { prefab=prefab,referenceTime=5,anchor=CueAnchor.Burst,riseTime=2,launchPosition=new Vector3(8,0,78),height=48,scale=1,seed=55 };
            if(!prefab.HasBurst) { cue.anchor=CueAnchor.Launch; cue.referenceTime=5; cue.riseTime=0; }
            var a=UnityEngine.Object.Instantiate(prefab); var b=UnityEngine.Object.Instantiate(prefab); a.Prepare(cue);b.Prepare(cue);
            double sample=5+(prefab.shape==FireworkShape.Glitter?.42:prefab.HasBurst?1.8:.75);
            for(double t=3;t<sample;t+=.023) a.Evaluate(t);
            a.Evaluate(sample); b.Evaluate(sample);
            var pa=new ParticleSystem.Particle[32000];var pb=new ParticleSystem.Particle[32000];
            bool equal=true; int total=0;
            foreach(var pair in new[]{(a.cores,b.cores),(a.tails,b.tails),(a.sparks,b.sparks)})
            {
                int na=pair.Item1.GetParticles(pa),nb=pair.Item2.GetParticles(pb); total+=na;equal&=na==nb;
                for(int i=0;i<Math.Min(na,nb);i++) equal&=(pa[i].position-pb[i].position).sqrMagnitude<1e-9 && float.IsFinite(pa[i].position.x);
            }
            check(equal && total>0,"Visible, frame-independent absolute-time geometry: "+prefab.name);
            if(prefab.shape==FireworkShape.MultiBreak)
            {
                a.Evaluate(5.8); int before=a.cores.particleCount;
                a.Evaluate(6.8); int after=a.cores.particleCount;
                check(before<=18 && after>300,"Carriers separate first, then 12 local secondary bursts");
            }
            UnityEngine.Object.DestroyImmediate(a.gameObject); UnityEngine.Object.DestroyImmediate(b.gameObject);
        }
        lines.Add("SUMMARY errors="+errors); Directory.CreateDirectory("Verification/Parade");
        File.WriteAllLines("Verification/Parade/parade-validation.txt",lines);
        if(errors>0) throw new Exception("Parade checks failed: "+errors);
    }

    public static void Capture()
    {
        Directory.CreateDirectory("Verification/Parade");
        var player=UnityEngine.Object.FindFirstObjectByType<FireworkShowPlayer>(); var camera=Camera.main;
        foreach(var item in new[]{("01_grand",67.9),("02_star",74.9),("03_multibreak",160.95),("04_parade",166.95),("05_finale",189.55)})
        {
            player.Seek(item.Item2,false); Render(camera,"Verification/Parade/"+item.Item1+".png");
        }
        player.ClearEffects();
        var show=player.show;
        foreach(var kind in new[]{FireworkShape.Star,FireworkShape.MultiBreak,FireworkShape.Glitter,FireworkShape.DoubleRing})
        {
            var prefab=show.cues.First(c=>c.prefab.shape==kind).prefab;
            var cue=new FireworkCue {prefab=prefab,referenceTime=5,riseTime=2,launchPosition=new Vector3(8,0,78),height=51,scale=1.15f,seed=71};
            var fx=UnityEngine.Object.Instantiate(prefab);fx.Prepare(cue);
            fx.Evaluate(5+(kind==FireworkShape.MultiBreak?2.05:kind==FireworkShape.Glitter?.42:1.5));
            Render(camera,"Verification/Parade/type_"+kind+".png"); UnityEngine.Object.DestroyImmediate(fx.gameObject);
        }
        var look=camera.GetComponent<FestivalLookCamera>();look.Initialize();player.Seek(166.95,false);look.RotateView(20,10);
        Render(camera,"Verification/Parade/06_look_around.png");look.ResetView();player.ClearEffects();
    }
    static void Render(Camera camera,string path)
    {
        var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB) {antiAliasing=4};
        var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;
        var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
        try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
        finally {camera.targetTexture=oldTarget;RenderTexture.active=oldActive;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
}

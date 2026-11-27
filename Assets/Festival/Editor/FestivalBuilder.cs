using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using LanternFestival;

public static class FestivalBuilder
{
    const string Root="Assets/Festival";
    static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static Transform scenery;
    [Serializable] class CueFile { public CueRow[] cues; public string notes; public int paradeVersion; }
    [Serializable] class CueRow { public string label,evidence,kind,anchor; public double time; public float rise,x,y,z,height,scale,brightness=1; public int seed; }

    [MenuItem("Festival/Build or rebuild festival assets")]
    public static void Build()
    {
        foreach(var d in new[]{"Scenes","Prefabs","Materials","Meshes","Data","Audio"}) Directory.CreateDirectory(Root+"/"+d);
        Directory.CreateDirectory("Verification");
        AssetDatabase.Refresh();
        mats.Clear();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        PlayerSettings.companyName="Lantern Atelier";
        PlayerSettings.productName="Lantern Festival | Show";
        PlayerSettings.defaultScreenWidth=1920; PlayerSettings.defaultScreenHeight=1080;
        PlayerSettings.runInBackground=true; PlayerSettings.colorSpace=ColorSpace.Linear;
        QualitySettings.antiAliasing=4; QualitySettings.pixelLightCount=8;
        QualitySettings.vSyncCount=1;
        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.035f,.045f,.065f);
        RenderSettings.fog=false;
        var camera=new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag="MainCamera"; camera.transform.position=new Vector3(0,1.7f,-28);
        camera.transform.rotation=Quaternion.Euler(-18,0,0);
        camera.fieldOfView=58; camera.nearClipPlane=.1f; camera.farClipPlane=300;
        camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.0045f,.007f,.014f);
        camera.allowHDR=true; camera.allowMSAA=true;
        camera.gameObject.AddComponent<AudioListener>();
        camera.gameObject.AddComponent<FestivalLookCamera>();
        var bloom=camera.gameObject.AddComponent<FestivalBloom>(); bloom.shader=Shader.Find("Hidden/Festival/Bloom");
        var moon=new GameObject("Moonlight").AddComponent<Light>(); moon.type=LightType.Directional;
        moon.color=new Color(.45f,.58f,.85f); moon.intensity=.30f; moon.transform.rotation=Quaternion.Euler(32,-35,0); moon.shadows=LightShadows.Soft;

        PrepareMaterials();
        scenery=new GameObject("Festival Set | editable 3D meshes").transform;
        BuildGround(); BuildGate(); BuildLanterns(); BuildTemple(); BuildTrees(); BuildAudience(); BuildStars();

        var effects=new Dictionary<string,FireworkEffect>();
        effects["Chrysanthemum"]=MakeEffect("Gold_Chrysanthemum",FireworkShape.Chrysanthemum,Html("FFD36A"),230,10.5f,4.8f,.58f,.25f,.62f,.080f);
        effects["Willow"]=MakeEffect("Gold_Willow",FireworkShape.Willow,Html("FFD36A"),180,8.7f,8.5f,1.6f,.48f,1.75f,.073f);
        effects["RedBurst"]=MakeEffect("Red_ShortBurst",FireworkShape.RedBurst,Html("E63227"),110,7.5f,2.1f,.75f,.55f,.26f,.085f);
        effects["Fan"]=MakeEffect("Red_LowFan",FireworkShape.Fan,Html("E63227"),60,20,1.65f,8,.18f,.7f,.080f);
        effects["Comet"]=MakeEffect("Blue_ThinComets",FireworkShape.Comet,Html("548CFF"),60,23,2.8f,3,.18f,1.1f,.060f);
        ParadeUpgrade.ConfigureEffects(effects);

        var audioPath=Root+"/Audio/Show.wav";
        var importer=(AudioImporter)AssetImporter.GetAtPath(audioPath);
        if(importer!=null)
        {
            var settings=importer.defaultSampleSettings; settings.loadType=AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat=AudioCompressionFormat.PCM; settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData=true; importer.defaultSampleSettings=settings; importer.forceToMono=false; importer.SaveAndReimport();
        }
        var data=AssetDatabase.LoadAssetAtPath<FireworkShowData>(Root+"/Data/ShowTimeline.asset");
        if(!data) { data=ScriptableObject.CreateInstance<FireworkShowData>(); AssetDatabase.CreateAsset(data,Root+"/Data/ShowTimeline.asset"); }
        data.music=AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath); data.preRoll=3.2f; data.tailAfterAudio=9;
        var json=JsonUtility.FromJson<CueFile>(File.ReadAllText("Analysis/cues.json"));
        data.analysisNotes=json.notes; data.paradeVersion=json.paradeVersion; data.cues.Clear();
        foreach(var row in json.cues)
            data.cues.Add(new FireworkCue { label=row.label,musicalEvidence=row.evidence,referenceTime=row.time,
                anchor=row.anchor=="Launch"?CueAnchor.Launch:CueAnchor.Burst,riseTime=row.rise,
                launchPosition=new Vector3(row.x,row.y,row.z),height=row.height,scale=row.scale,brightness=row.brightness,prefab=effects[row.kind],seed=row.seed });
        EditorUtility.SetDirty(data);
        var player=new GameObject("SHOW • select timeline asset to edit cues").AddComponent<FireworkShowPlayer>();
        player.show=data; player.volume=.8f; player.showControls=false;
        var audio=player.GetComponent<AudioSource>(); audio.clip=data.music; audio.playOnAwake=false;
        audio.spatialBlend=0; audio.volume=.8f;
        var settingsObj=new GameObject("READ ME • R restart / Space pause / H controls");
        settingsObj.transform.SetParent(scenery);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),Root+"/Scenes/LanternFestival.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Root+"/Scenes/LanternFestival.unity",true)};
        AssetDatabase.SaveAssets();
        Validate();
        Capture();
        Debug.Log("FESTIVAL_BUILD_COMPLETE");
    }

    static Color Html(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var c); return c; }
    static Material Material(string name,Color c,float metallic=0,float smooth=.3f,Color? emission=null)
    {
        string path=Root+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path); if(!m) { m=new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(m,path); }
        m.color=c; m.SetFloat("_Metallic",metallic); m.SetFloat("_Glossiness",smooth);
        if(emission.HasValue) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor",emission.Value); }
        EditorUtility.SetDirty(m); mats[name]=m; return m;
    }
    static void PrepareMaterials()
    {
        Material("VermilionWood",Html("6F1817"),.08f,.28f,new Color(.075f,.002f,.0005f));
        Material("BeamRed",Html("A52820"),.04f,.3f,new Color(.14f,.003f,.001f));
        Material("WoodDark",Html("1D1113"),0,.25f);
        Material("RoofBlue",Html("1B3044"),.22f,.38f);
        Material("TileEdge",Html("2C4154"),.15f,.4f);
        Material("Plaster",Html("4A3430"),0,.18f);
        Material("GoldMetal",Html("B27B37"),.68f,.42f);
        Material("Ground",Html("020305"),0,.05f);
        Material("Tree",Html("010403"),0,0);
        Material("Trunk",Html("030405"),0,0);
        Material("Audience",Html("010203"),0,0);
        Material("AudienceAlt",Html("030507"),0,0);
        foreach(var key in new[]{"Tree","Trunk","Audience","AudienceAlt","Ground"}) { mats[key].SetFloat("_SpecularHighlights",0); mats[key].SetFloat("_GlossyReflections",0); mats[key].EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); mats[key].EnableKeyword("_GLOSSYREFLECTIONS_OFF"); }
        Material("Window",Html("D77C30"),0,.25f,new Color(.7f,.25f,.035f));
        var silk=new Material(Shader.Find("Festival/LanternSilk")); silk.name="LanternSilk";
        string sp=Root+"/Materials/LanternSilk.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(sp)) { UnityEngine.Object.DestroyImmediate(silk); silk=AssetDatabase.LoadAssetAtPath<Material>(sp); }
        else AssetDatabase.CreateAsset(silk,sp);
        mats["LanternSilk"]=silk;
        foreach(var item in new[]{("Core",9f),("Tail",5.5f),("Spark",5f)})
        {
            string path=Root+"/Materials/"+item.Item1+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m) { m=new Material(Shader.Find("Festival/FireworkParticle")); AssetDatabase.CreateAsset(m,path); }
            m.SetFloat("_Intensity",item.Item2); mats[item.Item1]=m;
        }
    }
    static GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 scale,string mat,Transform parent=null)
    {
        var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent?parent:scenery);
        go.transform.position=p; go.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial=mats[mat]; return go;
    }
    static GameObject Beam(string name,Vector3 a,Vector3 b,float width,float depth,string mat,Transform parent=null)
    {
        var go=Primitive(name,PrimitiveType.Cube,(a+b)/2,new Vector3(width,Vector3.Distance(a,b),depth),mat,parent);
        go.transform.up=(b-a).normalized; return go;
    }
    static GameObject CylinderBetween(string name,Vector3 a,Vector3 b,float radius,string mat,Transform parent=null)
    {
        var go=Primitive(name,PrimitiveType.Cylinder,(a+b)/2,new Vector3(radius*2,Vector3.Distance(a,b)/2,radius*2),mat,parent);
        go.transform.up=(b-a).normalized; return go;
    }
    static void BuildGround()
    {
        Primitive("Festival stone court",PrimitiveType.Cube,new Vector3(0,-.22f,27),new Vector3(150,.4f,150),"Ground");
        for(int i=0;i<9;i++) Beam("Distant stall frame",new Vector3(-37+i*5,0,37),new Vector3(-37+i*5,4,37),.18f,.18f,"VermilionWood");
    }
    static void BuildGate()
    {
        Primitive("Foreground red pillar",PrimitiveType.Cube,new Vector3(-13,10,-6),new Vector3(1.8f,20,1.6f),"VermilionWood");
        Primitive("Pillar lit face",PrimitiveType.Cube,new Vector3(-13.1f,10,-6.83f),new Vector3(1.6f,20,.075f),"BeamRed");
        Primitive("Stone foot",PrimitiveType.Cube,new Vector3(-13,.35f,-6),new Vector3(1.9f,.7f,2.0f),"WoodDark");
        Beam("Diagonal overhead crossbeam",new Vector3(-30,24,-3),new Vector3(-6,12.5f,-5),.9f,1.25f,"BeamRed");
        Beam("Lower inset on beam",new Vector3(-30,23.55f,-3.1f),new Vector3(-6,12.05f,-5.1f),.12f,1.36f,"VermilionWood");
        Beam("Receding courtyard beam",new Vector3(-13,9.6f,-5),new Vector3(-25,5.6f,38),.5f,.7f,"VermilionWood");
        for(int i=1;i<5;i++)
        {
            float u=i/4f; Vector3 p=Vector3.Lerp(new Vector3(-13,0,-5),new Vector3(-25,0,38),u);
            Primitive("Receding red post",PrimitiveType.Cube,p+Vector3.up*(9.6f-4*u)/2,new Vector3(.5f,9.6f-4*u,.65f),"VermilionWood");
        }
    }
    static Mesh LatheMesh()
    {
        var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
        int rings=24,slices=48;
        for(int y=0;y<=rings;y++)
        {
            float t=y/(float)rings; float radius=.24f+.43f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.53f);
            for(int s=0;s<=slices;s++)
            {
                float a=s/(float)slices*Mathf.PI*2;
                vertices.Add(new Vector3(Mathf.Cos(a)*radius,(t-.5f)*1.75f,Mathf.Sin(a)*radius)); uv.Add(new Vector2(s/(float)slices,t));
                if(y<rings && s<slices) { int n=y*(slices+1)+s; triangles.AddRange(new[]{n,n+slices+1,n+1,n+1,n+slices+1,n+slices+2}); }
            }
        }
        var mesh=new Mesh { name="Ribbed lantern silk" }; mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string p=Root+"/Meshes/Lantern.mesh"; var existing=AssetDatabase.LoadAssetAtPath<Mesh>(p);
        if(existing) { existing.Clear(); existing.vertices=mesh.vertices; existing.uv=mesh.uv; existing.triangles=mesh.triangles; existing.RecalculateNormals(); existing.RecalculateBounds(); EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(mesh); return existing; }
        AssetDatabase.CreateAsset(mesh,p); return mesh;
    }
    static void Lantern(string name,Vector3 pos,float scale,Mesh mesh,bool light)
    {
        var root=new GameObject(name).transform; root.SetParent(scenery); root.position=pos;
        var body=new GameObject("Silk shell with amber inner glow"); body.transform.SetParent(root); body.transform.localPosition=Vector3.zero;
        body.AddComponent<MeshFilter>().sharedMesh=mesh; body.AddComponent<MeshRenderer>().sharedMaterial=mats["LanternSilk"];
        Primitive("Upper brass cap",PrimitiveType.Cylinder,pos+Vector3.up*.87f,new Vector3(.54f,.065f,.54f),"GoldMetal",root);
        Primitive("Lower dark cap",PrimitiveType.Cylinder,pos-Vector3.up*.87f,new Vector3(.52f,.055f,.52f),"WoodDark",root);
        CylinderBetween("Hanging cord",pos+Vector3.up*.94f,pos+Vector3.up*1.6f,.015f,"WoodDark",root);
        CylinderBetween("Gold tassel",pos-Vector3.up*.94f,pos-Vector3.up*1.25f,.025f,"GoldMetal",root);
        root.localScale=Vector3.one*scale;
        if(light)
        {
            var l=new GameObject("Amber inner light").AddComponent<Light>(); l.transform.SetParent(root); l.transform.localPosition=Vector3.zero;
            l.type=LightType.Point; l.color=new Color(1,.25f,.045f); l.intensity=2.5f; l.range=5.5f*scale;
        }
    }
    static void BuildLanterns()
    {
        var mesh=LatheMesh();
        for(int i=0;i<6;i++)
        {
            float t=i/5f;
            Vector3 p=Vector3.Lerp(new Vector3(-25,19.5f,-3.4f),new Vector3(-7.3f,10.9f,-5),t);
            Lantern("Near overhead lantern "+(i+1),p,1.05f,mesh,i>=3);
        }
        for(int i=0;i<14;i++)
        {
            float t=i/13f; Vector3 p=Vector3.Lerp(new Vector3(-14,7.8f,-1),new Vector3(-25,4.2f,40),t);
            Lantern("Lanterns receding into festival "+(i+1),p,.78f,mesh,false);
        }
        Vector3 left=new Vector3(-12,9.2f,9),right=new Vector3(28,8.5f,30);
        for(int i=0;i<13;i++)
        {
            float t=i/12f; Vector3 p=Vector3.Lerp(left,right,t); p.y-=Mathf.Sin(t*Mathf.PI)*1.2f;
            Lantern("Cross courtyard lantern "+(i+1),p,.72f,mesh,false);
            if(i<12)
            {
                float u=(i+1)/12f; Vector3 q=Vector3.Lerp(left,right,u); q.y-=Mathf.Sin(u*Mathf.PI)*1.2f;
                CylinderBetween("Catenary cable",p+Vector3.up*1.1f,q+Vector3.up*1.1f,.018f,"WoodDark");
            }
        }
    }
    static void BuildTemple()
    {
        var root=new GameObject("Low tiled temple").transform; root.SetParent(scenery);
        Primitive("Temple body",PrimitiveType.Cube,new Vector3(22,3.2f,30),new Vector3(25,6.4f,11),"VermilionWood",root);
        Primitive("Stone plinth",PrimitiveType.Cube,new Vector3(22,.4f,29),new Vector3(27,.8f,13),"WoodDark",root);
        for(int i=0;i<8;i++)
        {
            float x=10.8f+i*3.2f;
            Primitive("Facade post",PrimitiveType.Cube,new Vector3(x,3.3f,24.3f),new Vector3(.3f,6.5f,.35f),"BeamRed",root);
            if(i<7) Primitive("Shadowed wall bay",PrimitiveType.Cube,new Vector3(x+1.6f,3.1f,24.42f),new Vector3(2.8f,5.1f,.18f),"Plaster",root);
        }
        for(int i=0;i<3;i++)
        {
            float x=18+i*3.0f;
            Primitive("Warm window",PrimitiveType.Cube,new Vector3(x,3.1f,24.25f),new Vector3(1.4f,2.3f,.08f),"Window",root);
            for(int j=-1;j<=1;j++) Primitive("Window lattice",PrimitiveType.Cube,new Vector3(x+j*.38f,3.1f,24.17f),new Vector3(.055f,2.4f,.08f),"WoodDark",root);
            Primitive("Window cross rail",PrimitiveType.Cube,new Vector3(x,3.2f,24.15f),new Vector3(1.5f,.08f,.1f),"WoodDark",root);
        }
        Beam("Gold lintel",new Vector3(9,6.3f,23.9f),new Vector3(35,6.3f,23.9f),.18f,.2f,"GoldMetal",root);
        // Each tile row follows the roof pitch and flared eave in three segments.
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<79;i++)
            {
                float x=8.5f+i*.35f;
                Vector3 a=new Vector3(x,10.1f,30),b=new Vector3(x,7.05f,30+side*6.5f),c=new Vector3(x,6.95f,30+side*8.0f);
                CylinderBetween("Blue tile upper row",a,b,.19f,"RoofBlue",root);
                CylinderBetween("Upturned eave tile",b,c,.2f,"TileEdge",root);
            }
            Beam("Eave fascia",new Vector3(8.3f,6.78f,30+side*8),new Vector3(36.2f,6.78f,30+side*8),.3f,.35f,"VermilionWood",root);
        }
        CylinderBetween("Roof ridge",new Vector3(8,10.13f,30),new Vector3(36.5f,10.13f,30),.3f,"TileEdge",root);
        var fill=new GameObject("Temple warm wash").AddComponent<Light>(); fill.transform.position=new Vector3(22,5,19); fill.type=LightType.Point; fill.color=new Color(1,.23f,.05f); fill.range=18; fill.intensity=3.5f;
    }
    static void BuildTrees()
    {
        var rng=new System.Random(471);
        foreach(var location in new[]{new Vector3(39,0,27),new Vector3(44,0,36),new Vector3(6,0,36),new Vector3(-8,0,42),new Vector3(-32,0,35)})
        {
            var root=new GameObject("Tree silhouette").transform; root.SetParent(scenery);
            CylinderBetween("Trunk",location,location+new Vector3(.3f,9,0),.35f,"Trunk",root);
            for(int i=0;i<23;i++)
            {
                float x=(float)(rng.NextDouble()-.5)*9,y=6+(float)rng.NextDouble()*7,z=(float)(rng.NextDouble()-.5)*6;
                float size=2+(float)rng.NextDouble()*2.2f;
                Primitive("Leaf cluster",PrimitiveType.Sphere,location+new Vector3(x,y,z),new Vector3(size,size*.85f,size),"Tree",root);
                if(i<6) CylinderBetween("Branch",location+Vector3.up*5,location+new Vector3(x,y,z),.12f,"Trunk",root);
            }
        }
    }
    static void BuildAudience()
    {
        var rng=new System.Random(812);
        for(int i=0;i<15;i++)
        {
            float x=-11+i*1.6f+(float)rng.NextDouble()*.5f;
            if(i==5 || i==10) continue;
            float z=-21+(float)rng.NextDouble()*5,h=1.45f+(float)rng.NextDouble()*.3f;
            var root=new GameObject("Audience silhouette "+i).transform; root.SetParent(scenery);
            string material=i%4==0?"AudienceAlt":"Audience";
            Primitive("Head",PrimitiveType.Sphere,new Vector3(x,h,z),new Vector3(.32f,.39f,.33f),material,root);
            Primitive("Shoulders and body",PrimitiveType.Capsule,new Vector3(x,h-.65f,z),new Vector3(.58f,.57f,.35f),material,root);
            Primitive("Left trouser leg",PrimitiveType.Capsule,new Vector3(x-.14f,.42f,z),new Vector3(.19f,.4f,.22f),material,root);
            Primitive("Right trouser leg",PrimitiveType.Capsule,new Vector3(x+.14f,.42f,z+.025f),new Vector3(.19f,.4f,.22f),material,root);
            Beam("Relaxed arm",new Vector3(x-.25f,h-.27f,z),new Vector3(x-.34f,h-.91f,z),.13f,.15f,material,root);
            if(i%9==0)
            {
                Beam("Raised arm",new Vector3(x+.2f,h-.4f,z),new Vector3(x+.45f,h+.15f,z+.1f),.11f,.12f,material,root);
                Primitive("Phone",PrimitiveType.Cube,new Vector3(x+.44f,h+.24f,z+.1f),new Vector3(.13f,.22f,.045f),"WoodDark",root);
            }
        }
        Beam("Crowd rail",new Vector3(-34,.95f,8),new Vector3(46,.95f,8),.055f,.06f,"WoodDark");
    }
    static void BuildStars()
    {
        // Subtle atmospheric points; kept dimmer than every firework.
        var ps=NewParticles("Sparse distant stars",scenery,"Spark",512);
        var main=ps.main; main.playOnAwake=true; main.loop=true; main.startLifetime=100000; main.startSpeed=0;
        var emission=ps.emission; emission.enabled=true; emission.rateOverTime=0; emission.SetBursts(new[]{new ParticleSystem.Burst(0,90)});
        var shape=ps.shape; shape.enabled=true; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=new Vector3(200,50,10);
        ps.transform.position=new Vector3(0,75,165); main.startSize=.08f; main.startColor=new Color(.22f,.27f,.35f,.15f);
    }
    static ParticleSystem NewParticles(string name,Transform parent,string material,int maximum)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false); var ps=go.AddComponent<ParticleSystem>();
        ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main=ps.main; main.playOnAwake=false; main.loop=false; main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.maxParticles=maximum; main.startLifetime=100; main.startSpeed=0; main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
        var emission=ps.emission; emission.enabled=false; var shape=ps.shape; shape.enabled=false;
        var renderer=ps.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=mats[material];
        renderer.renderMode=ParticleSystemRenderMode.Billboard; renderer.maxParticleSize=.5f;
        if(material=="Tail") { renderer.renderMode=ParticleSystemRenderMode.Stretch; renderer.velocityScale=.035f; renderer.lengthScale=4; renderer.cameraVelocityScale=0; }
        renderer.localBounds=new Bounds(Vector3.zero,Vector3.one*600);
        renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
        return ps;
    }
    static FireworkEffect MakeEffect(string name,FireworkShape shape,Color color,int rays,float speed,float life,float gravity,float drag,float trail,float width)
    {
        var go=new GameObject(name); var fx=go.AddComponent<FireworkEffect>(); fx.shape=shape; fx.color=color;
        fx.rayCount=rays; fx.speed=speed; fx.lifetime=life; fx.gravity=gravity; fx.drag=drag; fx.trailLength=trail; fx.particleWidth=width;
        fx.cores=NewParticles("Bright cores",go.transform,"Core",1024); fx.tails=NewParticles("Fine colored tails",go.transform,"Tail",18000); fx.sparks=NewParticles("Flash and embers",go.transform,"Spark",4000);
        string path=Root+"/Prefabs/"+name+".prefab"; var prefab=PrefabUtility.SaveAsPrefabAsset(go,path); UnityEngine.Object.DestroyImmediate(go);
        return prefab.GetComponent<FireworkEffect>();
    }

    [MenuItem("Festival/Validate show")]
    public static void Validate()
    {
        var data=AssetDatabase.LoadAssetAtPath<FireworkShowData>(Root+"/Data/ShowTimeline.asset");
        var checks=new List<string>(); int errors=0;
        Action<bool,string> check=(ok,text)=>{checks.Add((ok?"PASS ":"FAIL ")+text); if(!ok) errors++;};
        check(data && data.music,"Show and music references");
        check(Math.Abs(data.music.samples/48000.0-189.6398333)<.002,"Decoded audio length and preserved origin");
        check(data.cues.Count>60,"Full-song cue coverage");
        foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            check(filter.sharedMesh && filter.sharedMesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z)),"Finite mesh vertices: "+filter.name);
        foreach(var c in data.cues)
        {
            check(c.prefab && c.referenceTime>=0 && c.referenceTime<=data.music.length,"Valid cue "+c.label);
            if(c.anchor==CueAnchor.Burst) check(Math.Abs(c.LaunchTime+c.riseTime-c.referenceTime)<.000001,"Burst = launch + rise: "+c.label);
            if(c.prefab.shape==FireworkShape.Comet) check(c.referenceTime>=173.9 && c.referenceTime<186,"Blue only in final density lift");
        }
        var big=data.cues.Where(c=>c.prefab.shape==FireworkShape.Chrysanthemum && c.scale>=.95f).OrderBy(c=>c.referenceTime).First();
        check(Math.Abs(big.referenceTime-66.1813)<.04,"First large explosion at measured full-band return");
        check(data.preRoll+data.cues.Min(c=>c.LaunchTime)>.2,"Preroll covers earliest launch");
        var test=new FireworkCue { referenceTime=66.1813,riseTime=2.6f,anchor=CueAnchor.Burst };
        var old=test.LaunchTime; test.referenceTime-=.1; check(Math.Abs(test.LaunchTime-old+.1)<.000001,"Moving a burst -0.1 s moves launch -0.1 s");
        var camera=Camera.main;
        foreach(var c in data.cues.Where(c=>c.prefab.shape==FireworkShape.Chrysanthemum && c.scale>=.95f))
        {
            var center=c.launchPosition+Vector3.up*c.height;
            float radius=c.prefab.speed*c.scale*(1-Mathf.Exp(-c.prefab.drag*2.2f))/c.prefab.drag;
            bool visible=true;
            foreach(var offset in new[]{Vector3.left,Vector3.right,Vector3.up,Vector3.down})
            {
                var p=camera.WorldToViewportPoint(center+offset*radius);
                visible &= p.x>.23f && p.x<.97f && p.y>.24f && p.y<.98f;
            }
            check(visible,"Large chrysanthemum inside open-sky framing: "+c.label);
        }
        checks.Add("SUMMARY errors="+errors+", cues="+data.cues.Count+", duration="+data.music.length);
        File.WriteAllLines("Verification/validation.txt",checks);
        if(errors>0) Debug.LogError("Festival validation failures: "+errors); else Debug.Log("FESTIVAL_VALIDATION_PASS");
    }

    [MenuItem("Festival/Render verification frames")]
    public static void Capture()
    {
        Directory.CreateDirectory("Verification");
        var player=UnityEngine.Object.FindFirstObjectByType<FireworkShowPlayer>(); var camera=Camera.main;
        foreach(var item in new[]{("01_intro",-2.5),("02_first_gold",1.55),("03_first_grand",67.45),("04_willow",76.5),("05_finale",182.3),("06_last_accent",189.35)})
        {
            player.Seek(item.Item2,false);
            var rt=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB) { antiAliasing=4 };
            camera.targetTexture=rt; camera.Render(); RenderTexture.active=rt;
            var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,1600,900),0,0); texture.Apply();
            File.WriteAllBytes("Verification/"+item.Item1+".png",texture.EncodeToPNG());
            camera.targetTexture=null; RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture);
        }
        player.ClearEffects();
        Debug.Log("FESTIVAL_CAPTURES_COMPLETE");
    }
}

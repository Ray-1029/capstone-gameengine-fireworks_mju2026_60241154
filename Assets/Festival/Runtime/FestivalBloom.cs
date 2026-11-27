using UnityEngine;
namespace LanternFestival
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class FestivalBloom : MonoBehaviour
    {
        public Shader shader;
        [Range(0,2)] public float strength=.48f;
        Material material;
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if(!shader || !shader.isSupported) { Graphics.Blit(source,destination); return; }
            if(!material) material=new Material(shader) { hideFlags=HideFlags.HideAndDontSave };
            int w=Mathf.Max(1,source.width/2), h=Mathf.Max(1,source.height/2);
            var a=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.DefaultHDR);
            var b=RenderTexture.GetTemporary(w,h,0,RenderTextureFormat.DefaultHDR);
            Graphics.Blit(source,a,material,0);
            for(int i=0;i<3;i++)
            {
                material.SetVector("_Direction",new Vector4((1.5f+i)/w,0,0,0)); Graphics.Blit(a,b,material,1);
                material.SetVector("_Direction",new Vector4(0,(1.5f+i)/h,0,0)); Graphics.Blit(b,a,material,1);
            }
            material.SetTexture("_Bloom",a); material.SetFloat("_Strength",strength);
            Graphics.Blit(source,destination,material,2);
            RenderTexture.ReleaseTemporary(a); RenderTexture.ReleaseTemporary(b);
        }
        void OnDisable() { if(material) { if(Application.isPlaying) Destroy(material); else DestroyImmediate(material); } }
    }
}

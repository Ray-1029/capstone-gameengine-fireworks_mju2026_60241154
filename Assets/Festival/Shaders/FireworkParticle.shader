Shader "Festival/FireworkParticle"
{
    Properties { _Tint("Tint", Color) = (1,1,1,1) _Intensity("Intensity", Float) = 2 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            float4 _Tint; float _Intensity;
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.color=v.color*_Tint; o.uv=v.uv; return o; }
            float4 frag(v2f i):SV_Target
            {
                float r=length(i.uv*2-1);
                float a=pow(saturate(1-r),1.4);
                return float4(i.color.rgb*_Intensity,i.color.a*a);
            }
            ENDCG
        }
    }
}

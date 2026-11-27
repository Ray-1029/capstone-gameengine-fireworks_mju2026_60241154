Shader "Festival/LanternSilk"
{
    Properties { _Color("Vermilion",Color)=(.9,.09,.035,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float3 view:TEXCOORD2; };
            float4 _Color;
            v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.normal=UnityObjectToWorldNormal(v.normal); o.view=WorldSpaceViewDir(v.vertex); return o; }
            float4 frag(v2f i):SV_Target
            {
                float facing=saturate(dot(normalize(i.normal),normalize(i.view)));
                float glow=pow(facing,3)*pow(saturate(1-abs(i.uv.y-.57)*2),2);
                float ribs=1-.11*pow(.5+.5*cos(i.uv.x*6.283185*24),10);
                float rings=1-.055*pow(.5+.5*cos(i.uv.y*6.283185*19),6);
                float3 c=lerp(_Color.rgb*.45,_Color.rgb*1.2,facing);
                c=lerp(c,float3(2.4,.82,.055),glow*.85)*ribs*rings;
                return float4(c,1);
            }
            ENDCG
        }
    }
}

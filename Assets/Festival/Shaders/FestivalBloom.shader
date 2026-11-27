Shader "Hidden/Festival/Bloom"
{
    Properties { _MainTex("Source",2D)="black"{} _Bloom("Bloom",2D)="black"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _Bloom; float4 _Direction; float _Strength;
        half4 threshold(v2f_img i):SV_Target { half3 c=tex2D(_MainTex,i.uv).rgb; return half4(c*saturate((max(c.r,max(c.g,c.b))-.65)*1.3),1); }
        half4 blur(v2f_img i):SV_Target
        {
            half4 c=tex2D(_MainTex,i.uv)*.227027;
            c+=tex2D(_MainTex,i.uv+_Direction.xy*1.384615)*.316216;
            c+=tex2D(_MainTex,i.uv-_Direction.xy*1.384615)*.316216;
            c+=tex2D(_MainTex,i.uv+_Direction.xy*3.230769)*.070270;
            c+=tex2D(_MainTex,i.uv-_Direction.xy*3.230769)*.070270;
            return c;
        }
        half4 compose(v2f_img i):SV_Target
        {
            half3 c=tex2D(_MainTex,i.uv).rgb + tex2D(_Bloom,i.uv).rgb*_Strength;
            c=c/(1+c*.26);
            float2 uv=i.uv*2-1; c*=1-.10*dot(uv,uv);
            return half4(c,1);
        }
        ENDCG
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment threshold
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment blur
            ENDCG }
        Pass { CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment compose
            ENDCG }
    }
}

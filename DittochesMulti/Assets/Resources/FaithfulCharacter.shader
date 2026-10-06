Shader "Dittoches/FaithfulCharacter"
{
    Properties {
        _Color("Tint",Color)=(1,1,1,1)
        _MainTex("Original texture",2D)="white" {}
        _Metallic("Metal",Range(0,1))=0
        _Glossiness("Smoothness",Range(0,1))=.35
        _Cutoff("Alpha cutoff",Range(0,1))=0
        _Cull("Cull",Float)=2
    }
    SubShader {
        Tags {"RenderType"="Opaque"} Cull [_Cull]
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex; fixed4 _Color; half _Metallic,_Glossiness,_Cutoff;
        struct Input {float2 uv_MainTex;float4 color:COLOR;};
        void surf(Input i,inout SurfaceOutputStandard o) {
            fixed4 c=tex2D(_MainTex,i.uv_MainTex)*i.color*_Color;
            clip(c.a-_Cutoff);o.Albedo=c.rgb;o.Alpha=c.a;
            o.Metallic=_Metallic;o.Smoothness=_Glossiness;
        }
        ENDCG
    }
    Fallback "Diffuse"
}

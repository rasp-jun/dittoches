Shader "Dittoches/ArenaSurface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Unlit ("Unlit", Float) = 0
        _OutlineColor ("Outline", Color) = (0.5,1,1,1)
        _OutlineWidth ("Outline width", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Color;
            float _Unlit;
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float shade:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv;
                float diffuse=saturate(dot(UnityObjectToWorldNormal(v.normal),normalize(float3(-.4,1,-.3))));
                o.shade=lerp(.50+.50*diffuse,1,_Unlit);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 color=tex2D(_MainTex,i.uv)*_Color;
                if(_OutlineWidth>0 && color.a<.12)
                {
                    float2 d=_MainTex_TexelSize.xy*_OutlineWidth;
                    float edge=max(max(tex2D(_MainTex,i.uv+float2(d.x,0)).a,tex2D(_MainTex,i.uv-float2(d.x,0)).a),
                                   max(tex2D(_MainTex,i.uv+float2(0,d.y)).a,tex2D(_MainTex,i.uv-float2(0,d.y)).a));
                    color=fixed4(_OutlineColor.rgb,edge*_OutlineColor.a);
                }
                clip(color.a-.12);
                color.rgb*=i.shade;return color;
            }
            ENDCG
        }
    }
}

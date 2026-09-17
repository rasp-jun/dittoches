Shader "Dittoches/ArenaSurface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Unlit ("Unlit", Float) = 0
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
                fixed4 color=tex2D(_MainTex,i.uv)*_Color;clip(color.a-.12);
                color.rgb*=i.shade;return color;
            }
            ENDCG
        }
    }
}

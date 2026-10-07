Shader "Dittoches/CharacterToon"
{
    Properties { _Color ("Tint",Color)=(1,1,1,1) _OutlineWidth ("Ink width",Float)=0.0035 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            Cull Front
            // The expanded hull must not occlude the thin eye and mouth layers.
            ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _OutlineWidth;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
            float4 vert(appdata v):SV_POSITION {v.vertex.xyz+=v.normal*_OutlineWidth;return UnityObjectToClipPos(v.vertex);}
            fixed4 frag():SV_Target {return fixed4(.028,.013,.009,1);}
            ENDCG
        }
        Pass
        {
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;fixed4 color:COLOR;};
            struct v2f {float4 pos:SV_POSITION;float3 normal:TEXCOORD0;fixed4 color:COLOR;};
            v2f vert(appdata v)
            {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                float d=saturate(dot(normalize(i.normal),normalize(float3(-.38,.81,.45))));
                float shade=d<.42?lerp(.40,.80,d/.42):lerp(.80,1,saturate((d-.42)/.48));
                half3 color=i.color.rgb*shade;
                // Blender's authored palette is linear, including the baked vertex colors.
                #ifdef UNITY_COLORSPACE_GAMMA
                color=LinearToGammaSpace(color);
                #endif
                return fixed4(color*_Color.rgb,1);
            }
            ENDCG
        }
    }
}

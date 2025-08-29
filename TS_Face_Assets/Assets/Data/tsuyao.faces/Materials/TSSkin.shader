Shader "Unlit/TSSkin"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _ShadowColor ("Skin Shadow Color", Color) = (1, .5, .5, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _Color;
            float4 _ShadowColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // then, if alpha less than 0.01, discard (pixel is invisible)
                if (col.a < 0.01) {
                  discard;
                }
                // tmp1.x = col.r * (1 - col.r)
                // this bit creates a parabolic curve based on the red channel of the main texture (not where our red tint comes from yet)
                float innerShadowyOutlineFactor = col.r * (1.0 - col.r);

                // clamp shadow factor so it never exceeds 0.11
                innerShadowyOutlineFactor = (innerShadowyOutlineFactor > 0.11) ? 0.11 : innerShadowyOutlineFactor;

                // multiply that shadow factor into _ShadowColor (where our red tint actually comes from)
                float3 shadow = innerShadowyOutlineFactor * _ShadowColor.rgb;

                // final tint color (excluding the shadows, think about what that would look like if we also multiplied by `shadow` here. lol
                float4 tinted = col * _Color;

                // finally, we multiply that tint color by the vertex color and then add our shadow contribution
                tinted.rgb = (tinted.rgb + shadow);
                tinted.a = col.a;

                // unfuck grey borders
                tinted.rgb *= col.a;
                return tinted;
            }
            ENDCG
        }
    }
}

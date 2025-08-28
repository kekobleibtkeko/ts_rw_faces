Shader "Unlit/TSSkin"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _SkinTint ("Skin Tint", Color) = (1, 0.5, 0.5, 1)
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
            float4 _SkinTint;

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
                fixed4 tinted = saturate(col * _Color);

                float sum = tinted.r + tinted.g + tinted.b;
                float tint_power = max(0.000001, 1 - (sum / 3.0));
                tint_power = pow(tint_power, 4.5);
                tinted.rgb = lerp(tinted.rgb, _SkinTint.rgb, tint_power);

                // unfuck grey borders
                tinted.rgb *= col.a;
                return tinted;
            }
            ENDCG
        }
    }
}

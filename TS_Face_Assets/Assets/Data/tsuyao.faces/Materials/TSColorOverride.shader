Shader "Unlit/TSColorOverride"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Cutout" }

        // Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
        Blend One OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work

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

                col.rgb = lerp(
                    dot(
                        col.rgb,
                        float3(0.299, 0.587, 0.114)
                    ),
                    col.rgb,
                    0
                );

                fixed4 tinted = saturate(col * _Color);

                // unfuck grey borders
                tinted.rgb *= col.a;
                return tinted;
            }
            ENDCG
        }
    }
}

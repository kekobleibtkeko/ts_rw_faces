Shader "Unlit/TSEye"
{
    Properties
    {
        _MainTex ("Internal Main Tex", 2D) = "white" {}

        _EyeTex ("Eye Texture", 2D) = "white" {}
        _IrisTex ("Iris Texture", 2D) = "black" {}
        _HLTex ("Iris Texture", 2D) = "black" {}

        _EyeOffset ("Eye Offset", Vector) = (0, 0, 0, 0)
        _IrisOffset ("Iris Offset", Vector) = (0, 0, 0, 0)

        _SkinColor ("Skin Color", Color) = (1, 1, 1, 1)
        _LashColor ("Lash Color", Color) = (0, 0, 0, 1)
        _ScleraColor ("Sclera Color", Color) = (1, 1, 1, 1)
        _IrisColor ("Iris Color", Color) = (0.2, 0.2, 0.7, 1)

        _Flipped ("Flipped", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Cutout" }

        Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                float2 eye_uv : TEXCOORD0;
                float2 iris_uv : TEXCOORD1;
                float2 hl_uv : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _EyeTex;
            float4 _EyeOffset;
            float4 _ScleraColor;

            sampler2D _IrisTex;
            float4 _IrisOffset;
            float4 _IrisColor;

            sampler2D _HLTex;

            float4 _SkinColor;
            float4 _LashColor;

            // sampler states?
            float4 _EyeTex_ST;
            float4 _IrisTex_ST;
            float4 _HLTex_ST;

            float _Flipped;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.eye_uv = TRANSFORM_TEX(v.uv, _EyeTex);
                o.iris_uv = TRANSFORM_TEX(v.uv, _IrisTex);
                o.hl_uv = TRANSFORM_TEX(v.uv, _HLTex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 eye_uv = i.eye_uv;
                float2 eye_offset = _EyeOffset.xy;

                float2 iris_uv = i.iris_uv;
                float2 iris_offset = _IrisOffset.xy;

                if (_Flipped > 0.5)
                {
                    eye_uv.x = -eye_uv.x + 1;
                    eye_offset = -eye_offset;

                    iris_uv.x = -iris_uv.x + 1;
                    iris_offset = -iris_offset;
                }

                // sample textures
                fixed4 eye_col  = tex2D(_EyeTex,  eye_uv - eye_offset);
                fixed4 iris_col = tex2D(_IrisTex, iris_uv - iris_offset);
                fixed4 hl_col = tex2D(_HLTex, i.hl_uv - iris_offset);

                // color targets
                float3 scleraColor = _ScleraColor.rgb;
                float3 lashColor   = _LashColor.rgb;
                float3 skinColor   = _SkinColor.rgb;
                float3 irisRecolored = saturate(iris_col.rgb * _IrisColor.rgb);

                // recolor each region while preserving luminance / shading
                float3 iris_part  = saturate(irisRecolored * eye_col.r);
                float3 scleraPart = saturate(scleraColor * eye_col.r);
                float3 lashPart   = saturate(lashColor * eye_col.g);
                float3 skinPart   = saturate(skinColor * eye_col.b);

                float3 combinedRgb = iris_part + lashPart + skinPart;

                // blend sclera on
                combinedRgb = lerp(combinedRgb, scleraColor, min(1 - iris_col.a, eye_col.r));
                float finalAlpha = eye_col.a;
                // clip(finalAlpha - 0.4);

                return
                    // fixed4(eye_col.rgb, 1); 
                    fixed4(combinedRgb, finalAlpha);
                   
                // return eye_col;
            }
            ENDCG
        }
    }
}

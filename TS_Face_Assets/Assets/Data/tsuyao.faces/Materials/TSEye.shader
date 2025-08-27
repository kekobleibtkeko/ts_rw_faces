Shader "Unlit/TSEye"
{
    Properties
    {
        _EyeTex ("Eye Texture", 2D) = "white" {}
        _IrisTex ("Iris Texture", 2D) = "" {}
        _HLTex ("Iris Texture", 2D) = "" {}

        _EyeOffset ("Eye Offset", Vector) = (0, 0, 0, 0)
        _IrisOffset ("Iris Offset", Vector) = (0, 0, 0, 0)

        _SkinColor ("Skin Color", Color) = (1, 1, 1, 1)
        _LashColor ("Lash Color", Color) = (0, 0, 0, 1)
        _ScleraColor ("Sclera Color", Color) = (1, 1, 1, 1)
        _IrisColor ("Iris Color", Color) = (0.2, 0.2, 0.7, 1)

        _MaskPower ("Mask Power", Float) = 1.2
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
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
                float2 uv : TEXCOORD0;
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

            float _MaskPower;

            // Convert RGB -> HSL
            float3 RgbToHsl(float3 c)
            {
                float maxc = max(c.r, max(c.g, c.b));
                float minc = min(c.r, min(c.g, c.b));
                float l = (maxc + minc) * 0.5;

                float h = 0;
                float s = 0;

                if (maxc != minc)
                {
                    float d = maxc - minc;
                    s = (l > 0.5) ? d / (2 - maxc - minc) : d / (maxc + minc);

                    if (maxc == c.r)
                        h = (c.g - c.b) / d + (c.g < c.b ? 6 : 0);
                    else if (maxc == c.g)
                        h = (c.b - c.r) / d + 2;
                    else
                        h = (c.r - c.g) / d + 4;

                    h /= 6;
                }
                return float3(h, s, l);
            }

            // Helper for HSL -> RGB
            float HueToRgb(float p, float q, float t)
            {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                if (t < 1.0/6.0) return p + (q - p) * 6 * t;
                if (t < 1.0/2.0) return q;
                if (t < 2.0/3.0) return p + (q - p) * (2.0/3.0 - t) * 6;
                return p;
            }

            // Convert HSL -> RGB
            float3 HslToRgb(float3 hsl)
            {
                float r, g, b;
                if (hsl.y == 0)
                {
                    r = g = b = hsl.z; // grey
                }
                else
                {
                    float q = hsl.z < 0.5 ? hsl.z * (1 + hsl.y) : hsl.z + hsl.y - hsl.z * hsl.y;
                    float p = 2 * hsl.z - q;
                    r = HueToRgb(p, q, hsl.x + 1.0/3.0);
                    g = HueToRgb(p, q, hsl.x);
                    b = HueToRgb(p, q, hsl.x - 1.0/3.0);
                }
                return float3(r, g, b);
            }

            // Tinting function: replace hue/sat with tint, keep luminance from src
            float3 HueReplaceTint(float3 srcRgb, float3 tintRgb)
            {
                float3 srcHsl = RgbToHsl(srcRgb);
                float3 tintHsl = RgbToHsl(tintRgb);

                // Replace hue + saturation, keep source luminance
                float3 outHsl = float3(tintHsl.x, tintHsl.y, srcHsl.z);

                return HslToRgb(outHsl);
            }

            float3 HueReplaceTintScaled(float3 srcRgb, float3 tintRgb)
            {
                float3 srcHsl = RgbToHsl(srcRgb);
                float3 tintHsl = RgbToHsl(tintRgb);

                // Keep source luminance, but modulate by tint luminance
                float scaledL = srcHsl.z * (tintHsl.z * 2.0); 
                // '2.0' here is just a tuning factor; tweak to taste

                float3 outHsl = float3(tintHsl.x, tintHsl.y, saturate(scaledL));

                return HslToRgb(outHsl);
            }

            void FixTexAlpha(inout fixed4 clr)
            {
                if (clr.a <= 0.01)
                {
                    clr.rgb = float3(0, 0, 0);
                }
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _EyeTex);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample textures
                fixed4 eye_col  = tex2D(_EyeTex,  i.uv - _EyeOffset.xy);
                fixed4 iris_col = tex2D(_IrisTex, i.uv - _IrisOffset.xy);
                fixed4 hl_col = tex2D(_HLTex, i.uv - _IrisOffset.xy);

                FixTexAlpha(eye_col);
                FixTexAlpha(iris_col);
                FixTexAlpha(hl_col);

                // unpack masks: R = sclera, G = lashes, B = skin
                float3 masks = eye_col.rgb;

                // optional non-linear falloff to make masks crisper (1 = no change)
                masks = pow(masks, float3(_MaskPower, _MaskPower, _MaskPower));

                // sum and normalize so overlapping channels don't "stack" improperly
                float maskSum = masks.r + masks.g + masks.b;
                float3 masksNorm = (maskSum > 0.0001) ? (masks / maskSum) : float3(0,0,0);

                // preserve original detail (shading) by using the eye texture as the source
                float3 srcDetail = eye_col.rgb;

                // color targets
                float3 scleraColor = _ScleraColor.rgb;
                float3 lashColor   = _LashColor.rgb;
                float3 skinColor   = _SkinColor.rgb;

                // recolor each region while preserving luminance / shading
                float3 scleraPart = HueReplaceTintScaled(srcDetail, scleraColor) * masksNorm.r;
                float3 lashPart   = HueReplaceTintScaled(srcDetail, lashColor)   * masksNorm.g;
                float3 skinPart   = HueReplaceTintScaled(srcDetail, skinColor)   * masksNorm.b;

                // background where no mask is present — keep original detail
                float bgMask = saturate(1.0 - maskSum);
                float3 basePart = srcDetail * bgMask;

                // combine the eye-region colors
                float3 combinedRgb = scleraPart + lashPart + skinPart + basePart;

                // handle iris: only visible where iris texture has alpha AND sclera mask permits (R channel)
                float irisMask = iris_col.a * eye_col.r; // iris alpha clipped by sclera presence
                float3 irisRecolored = HueReplaceTintScaled(iris_col.rgb, _IrisColor.rgb);

                // place iris on top, lerp so it blends softly
                combinedRgb = lerp(combinedRgb, irisRecolored, irisMask);
                float finalAlpha = eye_col.a;
                // clip(finalAlpha - 0.4);

                return
                // fixed4(eye_col.rgb, 1); 
                fixed4(combinedRgb, finalAlpha);
            }
            ENDCG
        }
    }
}

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

		_Rotations ("Rotations", Vector) = (0, 0, 0, 0)
		_Scales ("Eye Scale", Vector) = (1, 1, 1, 1)

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

			float4 _Rotations;
			float4 _Scales;

			// sampler states?
			float4 _EyeTex_ST;
			float4 _IrisTex_ST;
			float4 _HLTex_ST;

			float _Flipped;

			float2 TransformUV(float2 uv, float2 offset, float2 scale, float angle, float2 pivot)
			{
				// Apply offset
				uv += offset;

				// Move to pivot space
				uv -= pivot;

				// Apply scale
				uv *= scale;

				// Apply rotation
				float s = sin(angle);
				float c = cos(angle);

				uv = float2(
					uv.x * c - uv.y * s,
					uv.x * s + uv.y * c
				);

				// Move back from pivot space
				uv += pivot;

				return uv;
			}


			float2 RotateUVWithOffset(float2 uv, float2 offset, float angle, float2 pivot)
			{
				// Apply offset first
				uv += offset;

				// Rotate around pivot (which could still be 0.5,0.5, 
				// but now it's "relative to the offset")
				uv -= pivot;

				float s = sin(angle);
				float c = cos(angle);

				float2 rotatedUV = float2(
					uv.x * c - uv.y * s,
					uv.x * s + uv.y * c
				);

				return rotatedUV + pivot;
			}

			float2 RotateUV(float2 uv, float angle, float2 pivot)
			{
				uv -= pivot;

				float s = sin(angle);
				float c = cos(angle);

				float2 rotatedUV = float2(
					uv.x * c - uv.y * s,
					uv.x * s + uv.y * c
				);

				return rotatedUV + pivot;
			}

			v2f vert (appdata v)
			{
				float2 pivot = float2(0.5, 0.5);
				float2 eye_uv = v.uv;
				float2 iris_uv = v.uv;
				float2 hl_uv = v.uv;

				float2 eye_offset = _EyeOffset.xy;
				float2 iris_offset = _IrisOffset.xy;
				float2 hl_offset = _IrisOffset.zw + iris_offset;
				float flip_scalar = 1;
				if (_Flipped > 0.5)
				{
					flip_scalar = -1;
					eye_uv.x = -eye_uv.x + 1;
					eye_offset.x = -eye_offset.x;

					iris_uv.x = -iris_uv.x + 1;
					iris_offset.x = -iris_offset.x;
				}

				float2 eye_scale = _Scales.xy;
				float2 iris_scale = _Scales.zw;

				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				eye_uv = TRANSFORM_TEX(
					TransformUV(eye_uv, -eye_offset, eye_scale, radians(_Rotations.x), pivot),
					_EyeTex
				);
				iris_uv = TRANSFORM_TEX(
					TransformUV(iris_uv, -iris_offset, iris_scale, radians(_Rotations.y), pivot),
					_IrisTex
				);
				hl_uv = TRANSFORM_TEX(
					TransformUV(hl_uv, -hl_offset, iris_scale, radians(_Rotations.z), pivot),
					_HLTex
				);

				o.eye_uv = eye_uv;
				o.iris_uv = iris_uv;
				o.hl_uv = hl_uv;
				
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// sample textures
				fixed4 eye_col  = tex2D(_EyeTex,  i.eye_uv);
				fixed4 iris_col = tex2D(_IrisTex, i.iris_uv);
				fixed4 hl_col = tex2D(_HLTex, i.hl_uv);

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
				combinedRgb = lerp(combinedRgb, hl_col, min(eye_col.r, hl_col.a));
				float finalAlpha = eye_col.a;
				// combinedRgb.rgb *= eye_col.a;
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

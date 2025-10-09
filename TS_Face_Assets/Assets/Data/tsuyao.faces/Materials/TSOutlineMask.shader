Shader "Unlit/TSOutlineMask"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
		_MaskTex ("Mask Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
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
                float2 main_uv : TEXCOORD0;
				float2 mask_uv : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
			sampler2D _MaskTex;

            float4 _MainTex_ST;
			float4 _MaskTex_ST;

            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.main_uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.mask_uv = TRANSFORM_TEX(v.uv, _MaskTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.main_uv);
				fixed4 mask = tex2D(_MaskTex, i.mask_uv);

				col.rgb *= col.a;
				col.rgb *= mask.r;

                col.rgba *= mask.a;
                return col;
            }
            ENDCG
        }
    }
}

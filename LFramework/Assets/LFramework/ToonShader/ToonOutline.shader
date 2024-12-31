Shader "Unlit/ToonOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineWidth ("Outline Width", Range(0.01, 2)) = 0.24
        _OutLineColor ("OutLine Color", Color) = (0.5,0.5,0.5,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Pass
        {
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 lerpColor: TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.lerpColor = v.color;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            half4 frag(v2f i) :SV_Target
            {
                return float4(i.lerpColor, 1.0f);
            }
            ENDCG
        }

        Pass
        {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _OutlineWidth;
            half4 _OutLineColor;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                // float4 pos = UnityObjectToClipPos(v.vertex + float3((v.color.xyz - 0.5f) * 2.0f * _OutlineWidth));
                float4 pos = UnityObjectToClipPos(v.vertex);
                // 变换到齐次裁剪坐标系中
                float3 clipN = normalize(mul((float3x3)unity_MatrixMVP, (v.color.xyz - 0.5f) * 2.0f));

                // 乘上w 当进行NDC时，可以保持描边宽度不变
                float3 ndcN = clipN * pos.w;
                pos.xy += 0.01 * _OutlineWidth * ndcN.xy;

                o.vertex = pos;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutLineColor;
            }
            ENDCG
        }
    }
}
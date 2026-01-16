Shader "Hidden/Lith/Blur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Offset ("Blur Offset", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        
        Pass
        {
            Name "HorizontalBlur"
            ZTest Always Cull Off ZWrite Off
            
            HLSLPROGRAM

            #pragma shader_feature __ LLG_USE_URP            
            #pragma vertex vert
            #pragma fragment fragHorizontal
#if defined(LLG_USE_URP)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#else
            #include "UnityCG.cginc"
#endif

            struct appdata
            {
#if defined(LLG_USE_URP)
                uint vertexID : SV_VertexID;
#else
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
#endif
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

#if defined(LLG_USE_URP)
            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;
            #define BLUR_SAMPLE(uv) SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv)
            #define BLUR_TEXEL_SIZE _BlitTexture_TexelSize
#else
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            #define BLUR_SAMPLE(uv) tex2D(_MainTex, uv)
            #define BLUR_TEXEL_SIZE _MainTex_TexelSize
#endif
            
            half _Offset;

            v2f vert (appdata v)
            {
                v2f o;
#if defined(LLG_USE_URP)
                o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv     = GetFullScreenTriangleTexCoord(v.vertexID);
#else
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
#endif
                return o;
            }

            half4 fragHorizontal (v2f i) : SV_Target
            {
                half2 texelSize = BLUR_TEXEL_SIZE.xy;                
                half4 color = half4(0, 0, 0, 0);
                
                color += BLUR_SAMPLE(i.uv) * 0.28h;
                color += BLUR_SAMPLE(i.uv + half2(-texelSize.x * _Offset, 0)) * 0.18h;
                color += BLUR_SAMPLE(i.uv + half2(texelSize.x * _Offset, 0)) * 0.18h;
                color += BLUR_SAMPLE(i.uv + half2(-texelSize.x * _Offset * 2.0h, 0)) * 0.12h;
                color += BLUR_SAMPLE(i.uv + half2(texelSize.x * _Offset * 2.0h, 0)) * 0.12h;
                color += BLUR_SAMPLE(i.uv + half2(-texelSize.x * _Offset * 3.0h, 0)) * 0.06h;
                color += BLUR_SAMPLE(i.uv + half2(texelSize.x * _Offset * 3.0h, 0)) * 0.06h;
                
                return color;
            }
            ENDHLSL
        }
        
        // Pass 1 - Vertical
        Pass
        {
            Name "VerticalBlur"
            ZTest Always Cull Off ZWrite Off
            
            HLSLPROGRAM

            #pragma shader_feature __ LLG_USE_URP
            #pragma vertex vert
            #pragma fragment fragVertical
#if defined(LLG_USE_URP)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#else
            #include "UnityCG.cginc"
#endif

            struct appdata
            {
#if defined(LLG_USE_URP)
                uint vertexID : SV_VertexID;
#else
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
#endif
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

#if defined(LLG_USE_URP)
            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;
            #define BLUR_SAMPLE(uv) SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv)
            #define BLUR_TEXEL_SIZE _BlitTexture_TexelSize
#else
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            #define BLUR_SAMPLE(uv) tex2D(_MainTex, uv)
            #define BLUR_TEXEL_SIZE _MainTex_TexelSize
#endif

            half _Offset;

            v2f vert (appdata v)
            {
                v2f o;
#if defined(LLG_USE_URP)
                o.vertex = GetFullScreenTriangleVertexPosition(v.vertexID);
                o.uv     = GetFullScreenTriangleTexCoord(v.vertexID);
#else
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
#endif
                return o;
            }

            half4 fragVertical (v2f i) : SV_Target
            {
                half2 texelSize = BLUR_TEXEL_SIZE.xy;                
                half4 color = half4(0, 0, 0, 0);
                
                color += BLUR_SAMPLE(i.uv) * 0.28h;
                color += BLUR_SAMPLE(i.uv + half2(0, -texelSize.y * _Offset)) * 0.18h;
                color += BLUR_SAMPLE(i.uv + half2(0, texelSize.y * _Offset)) * 0.18h;
                color += BLUR_SAMPLE(i.uv + half2(0, -texelSize.y * _Offset * 2.0h)) * 0.12h;
                color += BLUR_SAMPLE(i.uv + half2(0, texelSize.y * _Offset * 2.0h)) * 0.12h;
                color += BLUR_SAMPLE(i.uv + half2(0, -texelSize.y * _Offset * 3.0h)) * 0.06h;
                color += BLUR_SAMPLE(i.uv + half2(0, texelSize.y * _Offset * 3.0h)) * 0.06h;
                
                return color;
            }
            ENDHLSL
        }
    }
}
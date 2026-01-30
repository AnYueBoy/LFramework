Shader "Hidden/SoftMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                // just invert the colors
                col = 1 - col;
                return col;
            }
            ENDCG
        }
    }


    SubShader
    {
        Pass
        {
            CGPROGRAM
            #define SOFTMASK_SIMPLE
            #define SOFTMASK_SLICED
            #define SOFTMASK_TILED
            // -----------------------------------------------------------------------------------------------------

            #ifndef SOFTMASK_INCLUDE

            #define SOFTMASK_INCLUDE

            #include "UnityUI.cginc"

            #if defined(SOFTMASK_SIMPLE) || define(SOFTMASK_SLICED) || define(SOFTMASK_TILED)
            # define __SOFTMASK_ENABLE
            # if defined(SOFTMASK_SLICED) || define(SOFTMASK_TILED)
            # define __SOFTMASK_USE_BORDER

            #endif

            #endif

            #ifdef __SOFTMASK_ENABLE

            #define SOFTMASK_COORDS(idx) float maskPosition: TEXCOORD ## idx;
            #define SOFTMASK_CALCULATE_COORDS(OUT,pos) (OUT).maskPosition = mul(_SoftMask_WorldToMask,pos);
            #define SOFTMASK_GET_MASK(IN) SoftMask_GetMask((IN).maskPosition.xy)

            sampler2D _SoftMask;
            float4 _SoftMask_Rect;
            float4 _SoftMask_UVRect;
            float4x4 _SoftMask_WorldToMask;
            float4 _SoftMask_ChannelWeights;

            #ifdef __SOFTMASK_USE_BORDER

            float4 _SoftMask_BorderRect;
            float4 _SoftMask_UVBoarderRect;
            #endif

            #ifdef SOFTMASK_TILED
            float2 _SoftMask_TileRepeat;
            #endif

            bool _SoftMask_InvertMask;
            bool _SoftMask_InvertOutsides;

            inline float2 __SoftMask_Inset(float2 a, float2 a1, float2 a2, float2 u1, float2 u2, float2 repeat)
            {
                float2 w = a2 - a1;
                float2 d = (a - a1) / w;
                return lerp(u1, u2, (w * repeat != 0.0f) ? frac(d * repeat) : d);
            }

            inline float2 __SoftMask_Inset(float2 a, float2 a1, float2 a2, float2 u1, float2 u2)
            {
                float2 w = a2 - a1;
                return lerp(u1, u2, (w != 0.0f) ? (a - a1) / w : 0.0f);
            }

            #ifdef __SOFTMASK_USE_BORDER
            inline float2 __SoftMask_XY2UV(
                float2 a,
                float2 a1, float2 a2, float2 a3, float2 a4,
                float2 u1, float2 u2, float2 u3, float2 u4)
            {
                float2 s1 = step(a2, a);
                float2 s2 = step(a3, a);
                float2 s1i = 1 - s1;
                float2 s2i = 1 - s2;
                float2 s12 = s1 * s2;
                float2 s12i = s1 * s2i;
                float2 s1i2i = s1i * s2i;
                float2 aa1 = a1 * s1i2i + a2 * s12i + a3 * s12;
                float2 aa2 = a2 * s1i2i + a3 * s12i + a4 * s12;
                float2 uu1 = u1 * s1i2i + u2 * s12i + u3 * s12;
                float2 uu2 = u2 * s1i2i + u3 * s12i + u4 * s12;
                return __SoftMask_Inset(a, aa1, aa2, uu1, uu2
                                        #if  SOFTMASK_TILED
                                        , s12i*_SoftMask_TileRepeat
                                        #endif

                );
            }

            inline float2 xSoftMask_GetMaskUV(float2 maskPosition)
            {
                return
                    __SoftMask_XY2UV(
                        maskPosition,
                        _SoftMask_Rect.xy, _SoftMask_BorderRect.xy, _SoftMask_BorderRect.zw, _SoftMask_Rect.zw,
                        _SoftMask_UVRect.xy, _SoftMask_UVBoarderRect.xy, _SoftMask_UVBoarderRect.zw, _SoftMask_UVRect.zw
                    );
            }

            inline float2 SoftMask_GetMaskUV(float2 maskPosition)
            {
                return
                    __SoftMask_Inset(
                        maskPosition,
                        _SoftMask_Rect.xy, _SoftMask_Rect.zw,
                        _SoftMask_UVRect.xy, _SoftMask_UVRect.zw
                    );
            }
            #else
            inline float2 SoftMask_GetMaskUV(float2 maskPosition)
            {
                return
                    __SoftMask_Inset(
                        maskPosition,
                        _SoftMask_Rect.xy, _SoftMask_Rect.zw,
                        _SoftMask_UVRect.xy, _SoftMask_UVRect.zw
                    );
            }

            #endif

            inline float4 SoftMask_GetMaskTexture(float2 maskPosition)
            {
                return tex2D(_SoftMask, SoftMask_GetMaskUV(maskPosition));
            }

            inline float SoftMask_GetMask(float maskPosition)
            {
                float2 uv = SoftMask_GetMaskUV(maskPosition);
                float4 sampledMask = tex2D(_SoftMask, uv);
                float weightedMask = dot(sampledMask * _SoftMask_ChannelWeights, 1);
                float maskInsideRect = _SoftMask_InvertMask ? 1 - weightedMask : weightedMask;
                float maskOutsideRect = _SoftMask_InvertOutsides;
                float isInsideRect = UnityGet2DClipping(maskPosition, _SoftMask_Rect);
                return lerp(maskOutsideRect, maskInsideRect, isInsideRect);
            }

            #else

            #define SOFTMASK_COORDS(idx)
            #define SOFTMASK_CALCULATE_COORDS(OUT,pos)
            #define SOFTMASK_GET_MASK(IN)   (1.0)

            inline  float4 SoftMask_GetMaskTexture(float2 maskPosition){return 1.0f;}
            inline float SoftMask_GetMask(float2 maskPosition){return 1.0f;}
            #endif

            #endif
            ENDCG
        }
    }
}
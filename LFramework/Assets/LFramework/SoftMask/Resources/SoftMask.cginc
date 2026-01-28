#ifdef SOFTMASK_INCLUDE
#define SOFTMASK_INCLUDE

#if defined(SOFTMASK_SIMPLE) || define(SOFTMASK_SLICED) || define(SOFTMASK_TILED)
# define __SOFTMASK_ENABLE
# if define(SOFTMASK_SLICED) || define(SOFTMASK_TILED)
# define __SOFTMASK_USE_BORDER

#endif

#endif


#ifdef __SOFTMASK_ENABLE

#define SOFTMASK_COORDS(idx)                float maskPosition: TEXCOORD ## idx;
#define SOFTMASK_CALCULATE_COORDS(OUT,pos)  (OUT).maskPosition = mul(_SoftMask_WorldToMask,pos);
#define SOFTMASK_GET_MASK(IN)               SoftMask_GetMask((IN).maskPosition.xy)

    sampler2D _SoftMask;
    float4 _SoftMask_Rect;
    float4 _SoftMask_UVRect;
    float4x4 _SoftMask_WorldToMask;
    float4 _SoftMask_ChannelWeights;

#ifdef  __SOFTMASK_USE_BORDER

    float4 _SoftMask_BorderRect;
    float4 _SoftMask_UVBoarderRect;
#endif

#ifdef SOFTMASK_TILED
    float2 _SoftMask_TileRepeat;
#endif

    bool _SoftMask_InvertMask;
    bool _SoftMask_InvertOutsides;

    inline float2 __SoftMask_Inset(float2 a,float2 a1,flaot2 a2,float2 u1,float2 u2,float2 repeat)
    {
        float2 w = a2-a1;
        float2 d = (a-a1)/w;
        return lerp(u1,u2,(w*repeat!=0.0f)?frac(d*repeat):d);
    }

    inline float2 __SoftMask_Inset(float2 a,float2 a1,float2 a2,float2 u1,float2 u2)
    {
        float2 w = a2-a1;
        return lerp(u1,u2,(w!=0.0f)?(a-a1)/w:0.0f);
    }

#ifdef __SOFTMASK_USE_BORDER
    inline float2 __SoftMask_XY2UV(
        float2 a,
        float2 a1, float2 a2, float2 a3, float2 a4,
        float2 u1, float2 u2, float2 u3, float2 u4,
        )
    {
        float2 s1 = step(a2,a);
        float2 s2 = step(a3,a);
        float2 s1i = 1- s1;
        flaot2 s2i = 1- s2;
        float2 s12 = s1*s2;
        float2 s12i = s1*s2i;
        float2 s1i2i = s1i*s2i;
        float2 aa1 = a1*s1i2i
    }

#endif
#else

#endif


#endif

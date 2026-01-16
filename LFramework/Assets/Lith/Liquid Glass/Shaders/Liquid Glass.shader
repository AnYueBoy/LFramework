Shader "UI/Lith/Liquid Glass"
{
    Properties
    {
        [HideInInspector][PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector][NoScaleOffset] _ExternalTex("External Texture", 2D) = "white" {}
        [HideInInspector][NoScaleOffset] _ExternalBlurTex("External Blur Texture", 2D) = "white" {}
        [NoScaleOffset] _NormalMap ("Normal Map", 2D) = "bump" {}

        _EdgeWidthPx ("Edge Width (px)", Range(0,512)) = 40
        _EdgeCurve ("Edge Curve" , Range(0.25,4)) = 2.4
        _DistortionInnerPx ("Distortion Inner (px)", Range(0, 256)) = 98
        _DistortionEdgePx ("Distortion Edge  (px)", Range(0, 256)) = 80
        _ChromaticAmount ("Chromatic", Range(0,1)) = 0.2
        _GlobalBlurPercent ("Global Blur Percent", Range(0,1)) = 0

        _Saturation ("Saturation", Range(0,10)) = 1.0

        _TintIntensity ("Tint Intensity", Range(0,1)) = 0

        _GlossIntensity ("Gloss Intensity", Range(0,2)) = 0.6
        _GlossWidth ("Gloss Width", Range(0.01,24)) = 1.2

        // UGUI
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    CustomEditor "Lith.LiquidGlass.LiquidGlassShaderGUI"

    SubShader
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma fragmentoption ARB_precision_hint_fastest
            #pragma shader_feature __ LLG_USE_URP
            #pragma multi_compile_local __ USE_TEXTURE_PROPERTY
            #pragma multi_compile_local __ USE_GLOSS
            #pragma multi_compile_local __ USE_SATURATION
            #pragma multi_compile_local __ USE_DISTORTION

            #if defined(LLG_USE_URP)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #define LLG_SRP 1
            #else
            #include "UnityCG.cginc"
            #endif


            #if LLG_SRP
            #define UNITY_PROJ_COORD(a) (a)
            #endif

            struct appdata
            {
                half4 vertex : POSITION;
                half2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct v2f
            {
                half4 pos : SV_POSITION;
                half4 uvproj : TEXCOORD0;
                half2 uv : TEXCOORD1;
                half4 col : COLOR;
            };

            // Textures & texel sizes
            #if defined(USE_TEXTURE_PROPERTY)
            #if LLG_SRP
            TEXTURE2D(_ExternalTex);           SAMPLER(sampler_ExternalTex);
            TEXTURE2D(_ExternalBlurTex);       SAMPLER(sampler_ExternalBlurTex);
            #define LLG_SAMPLE_PROJ(tex, prj) SAMPLE_TEXTURE2D(tex, sampler##tex, (prj).xy / max((prj).w, 1e-6))
            #else
            sampler2D _ExternalTex;
            sampler2D _ExternalBlurTex;
            #define LLG_SAMPLE_PROJ(tex, prj) tex2Dproj(tex, prj)
            #endif
            #else
            #if LLG_SRP
            TEXTURE2D(_LithLiquidGlassExternalTex);     SAMPLER(sampler_LithLiquidGlassExternalTex);
            TEXTURE2D(_LithLiquidGlassExternalBlurTex); SAMPLER(sampler_LithLiquidGlassExternalBlurTex);
            #define LLG_SAMPLE_PROJ(tex, prj) SAMPLE_TEXTURE2D(tex, sampler##tex, (prj).xy / max((prj).w, 1e-6))
            #else
            sampler2D _LithLiquidGlassExternalTex;
            sampler2D _LithLiquidGlassExternalBlurTex;
            #define LLG_SAMPLE_PROJ(tex, prj) tex2Dproj(tex, prj)
            #endif
            #endif

            sampler2D _MainTex;
            half4 _MainTex_TexelSize;
            sampler2D _NormalMap;
            half4 _NormalMap_TexelSize;
            half4 _MainTex_ST;

            // Params
            half _GlobalBlurPercent;
            half _TintIntensity;

            #if defined(USE_DISTORTION)
            half _EdgeWidthPx;
            half _EdgeCurve;
            half _DistortionInnerPx, _DistortionEdgePx;
            half _ChromaticAmount;
            #endif

            #if defined(USE_SATURATION)
            half _Saturation;
            #endif

            #if defined(USE_GLOSS)
            half _GlossIntensity;
            half _GlossWidth;
            #endif

            v2f vert(appdata v)
            {
                v2f o;
                #if LLG_SRP
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                #else
                o.pos = UnityObjectToClipPos(v.vertex);
                #endif
                o.uv = v.uv;
                o.col = v.color;

                o.uvproj.xy = (o.pos.xy + o.pos.w) * 0.5;
                o.uvproj.zw = o.pos.zw;
                return o;
            }

            half2 GetSpriteSizePx()
            {
                if (_NormalMap_TexelSize.z > 0 && _NormalMap_TexelSize.w > 0)
                    return _NormalMap_TexelSize.zw;

                half2 atlas = _MainTex_TexelSize.zw;
                half2 st = _MainTex_ST.xy;
                half2 viaST = atlas * st;
                if (viaST.x > 0.0 && viaST.y > 0.0)
                    return viaST;

                return max(atlas, half2(1.0, 1.0));
            }

            half2 ToSpriteLocal01(half2 uv)
            {
                half2 st = _MainTex_ST.xy;
                half2 ot = _MainTex_ST.zw;
                half2 invSt = 1.0 / max(st, half2(1e-6, 1e-6));
                half2 uvl = (uv - ot) * invSt;
                return saturate(uvl);
            }

            half SpriteEdgeDMinPx(half2 uv)
            {
                half2 sizePx = GetSpriteSizePx();
                half2 uvl = ToSpriteLocal01(uv);
                half2 uvPx = uvl * sizePx;
                half2 dist2Ed = min(uvPx, sizePx - uvPx);

                half scale = max(sizePx.x, sizePx.y);
                half2 dist2EdNorm = dist2Ed * (scale / sizePx);
                return min(dist2EdNorm.x, dist2EdNorm.y);
            }

            half EdgeFalloff(half dMinPx, half edgePx, half curve)
            {
                half t = saturate(1.0 - dMinPx / max(edgePx, 1e-5));
                return pow(t, curve);
            }

            half2 GetNormalXY(half2 uv)
            {
                half hasNormal = step(1.0, _NormalMap_TexelSize.z + _NormalMap_TexelSize.w);
                half3 n = UnpackNormal(tex2D(_NormalMap, uv));
                return lerp(half2(0, 0), n.xy, hasNormal);
            }

            half3 SaturateColor(half3 col, half intensity)
            {
                half grey = dot(col, half3(0.299, 0.587, 0.114));
                return lerp(half3(grey, grey, grey), col, intensity);
            }

            half4 frag(v2f i):SV_Target
            {
                half mask = tex2D(_MainTex, i.uv).a;

                half dMin = SpriteEdgeDMinPx(i.uv);

                #if defined(USE_DISTORTION)
                half edgeT  = EdgeFalloff(dMin, _EdgeWidthPx, _EdgeCurve);

                half distPx = lerp(_DistortionInnerPx, _DistortionEdgePx, edgeT) * edgeT;
                
                half2 Nxy   = GetNormalXY(i.uv);

                half2 referenceRes = half2(1242, 2688);

                half currentAspect = _ScreenParams.x / _ScreenParams.y;
                half referenceAspect = referenceRes.x / referenceRes.y;
                half2 aspectCorrection = half2(referenceAspect / currentAspect, 1.0);

                half2 ofsUV = Nxy * distPx / referenceRes * aspectCorrection;

                half ca     = _ChromaticAmount * edgeT;
                half2 cab   = ofsUV * (ca * 1.5);

                half4 proj = i.uvproj;

                half4 prjG = proj; // green (nötr)
                half4 prjR = proj; // red (+)
                half4 prjB = proj; // blue (-)
                prjR.xy += (ofsUV - cab) * prjR.w;
                prjG.xy +=  ofsUV       * prjG.w;
                prjB.xy += (ofsUV + cab) * prjB.w;

    // 3 örnekli doku okuma ve global blur karışımı
                #if defined(USE_TEXTURE_PROPERTY)
                half3 sampG = lerp(
                    LLG_SAMPLE_PROJ(_ExternalTex,     prjG).rgb,
                    LLG_SAMPLE_PROJ(_ExternalBlurTex, prjG).rgb,
                    _GlobalBlurPercent
                );
                half3 sampR = lerp(
                    LLG_SAMPLE_PROJ(_ExternalTex,     prjR).rgb,
                    LLG_SAMPLE_PROJ(_ExternalBlurTex, prjR).rgb,
                    _GlobalBlurPercent
                );
                half3 sampB = lerp(
                    LLG_SAMPLE_PROJ(_ExternalTex,     prjB).rgb,
                    LLG_SAMPLE_PROJ(_ExternalBlurTex, prjB).rgb,
                    _GlobalBlurPercent
                );
                #else
                half3 sampG = lerp(
                    LLG_SAMPLE_PROJ(_LithLiquidGlassExternalTex,     prjG).rgb,
                    LLG_SAMPLE_PROJ(_LithLiquidGlassExternalBlurTex, prjG).rgb,
                    _GlobalBlurPercent
                );
                half3 sampR = lerp(
                    LLG_SAMPLE_PROJ(_LithLiquidGlassExternalTex,     prjR).rgb,
                    LLG_SAMPLE_PROJ(_LithLiquidGlassExternalBlurTex, prjR).rgb,
                    _GlobalBlurPercent
                );
                half3 sampB = lerp(
                    LLG_SAMPLE_PROJ(_LithLiquidGlassExternalTex,     prjB).rgb,
                    LLG_SAMPLE_PROJ(_LithLiquidGlassExternalBlurTex, prjB).rgb,
                    _GlobalBlurPercent
                );
                #endif

                // Kanalları 3 örnekten birleştir
                half3 scene = half3(sampR.r, sampG.g, sampB.b);
                #else
                half4 proj = i.uvproj;
                #if defined(USE_TEXTURE_PROPERTY)
                half3 scene = lerp(
                    LLG_SAMPLE_PROJ(_ExternalTex, proj).rgb,
                    LLG_SAMPLE_PROJ(_ExternalBlurTex, proj).rgb,
                    _GlobalBlurPercent
                );
                #else
                half3 scene = lerp(
                    LLG_SAMPLE_PROJ(_LithLiquidGlassExternalTex, proj).rgb,
                    LLG_SAMPLE_PROJ(_LithLiquidGlassExternalBlurTex, proj).rgb,
                    _GlobalBlurPercent
                );
                #endif
                #endif
                half3 tinted = lerp(scene, i.col.rgb, _TintIntensity);

                half3 col = tinted;
                half alpha = i.col.a * mask;

                #if defined(USE_SATURATION)
                col = SaturateColor(col, _Saturation);    
                #endif

                #if defined(USE_GLOSS)
                half edgeRamp  = saturate(mask * (1.0 - mask) * 4.0);
                half edgeBand = pow(edgeRamp, _GlossWidth);
                
                half3 glossScene = SaturateColor(scene, 1.5) * 1.2;
                half3 glossCol = glossScene * edgeBand * _GlossIntensity;

                col = lerp(col, col + glossCol, edgeBand);
                #endif

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
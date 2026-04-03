Shader "Custom/LiquidFill"
{
    Properties
    {
        _FillAmount ("Fill Amount", Range(0, 1)) = 0
        _Color      ("Liquid Color", Color) = (0.2, 0.6, 1, 1)
        _TopColor   ("Top Color",    Color) = (0.8, 0.9, 1, 1)
        _FoamColor  ("Foam Color",   Color) = (1, 1, 1, 1)
        _FoamWidth  ("Foam Width",   Range(0, 0.1)) = 0.02
        _MinY       ("Min Y (World)", Float) = 0
        _MaxY       ("Max Y (World)", Float) = 1
        _TiltX      ("Tilt X", Float) = 0
        _TiltZ      ("Tilt Z", Float) = 0
        _CenterX    ("Center X", Float) = 0
        _CenterZ    ("Center Z", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "LiquidFillForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float  _FillAmount;
                float4 _Color;
                float4 _TopColor;
                float4 _FoamColor;
                float  _FoamWidth;
                float  _MinY;
                float  _MaxY;
                float  _TiltX;
                float  _TiltZ;
                float  _CenterX;
                float  _CenterZ;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex   = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                return o;
            }

            half4 frag(v2f i, half facing : VFACE) : SV_Target
            {
                // fillAmount 为 0 时完全不渲染
                clip(_FillAmount - 1e-5);

                // 基础填充高度 + 水面倾斜偏移（以容器中心为原点的倾斜平面）
                float baseFillY  = lerp(_MinY, _MaxY, _FillAmount);
                float tiltOffset = (i.worldPos.x - _CenterX) * _TiltX
                                 + (i.worldPos.z - _CenterZ) * _TiltZ;
                float fillWorldY = baseFillY + tiltOffset;

                // 裁剪水面以上的片元
                clip(step(i.worldPos.y, fillWorldY) - 0.5);

                // 泡沫线：填充高度附近一薄层
                float foamLine = step(i.worldPos.y, fillWorldY)
                               - step(i.worldPos.y, fillWorldY - _FoamWidth);

                half4 liquidCol = lerp(_Color,    _FoamColor, foamLine);
                half4 topCol    = lerp(_TopColor, _FoamColor, foamLine * 0.5);

                // facing > 0 正面（液体侧面），否则顶面
                return facing > 0 ? liquidCol : topCol;
            }
            ENDHLSL
        }
    }
}

Shader "Custom/LiquidFill"
{
    Properties
    {
        _FillAmount ("Fill Amount", Range(0, 1)) = 0
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamWidth ("Foam Width", Range(0, 0.1)) = 0.02
        _MinY ("Min Y (World)", Float) = 0
        _MaxY ("Max Y (World)", Float) = 1
        _TiltX ("Tilt X", Float) = 0
        _TiltZ ("Tilt Z", Float) = 0
        _CenterX ("Center X", Float) = 0
        _CenterZ ("Center Z", Float) = 0
        _SegmentCount("Segment Count", Int) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "LiquidFillForward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

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
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            #define MAX_SEGMENTS 8

            CBUFFER_START(UnityPerMaterial)
                float _FillAmount;
                float4 _FoamColor;
                float _FoamWidth;
                float _MinY;
                float _MaxY;
                float _TiltX;
                float _TiltZ;
                float _CenterX;
                float _CenterZ;
                int _SegmentCount;
            CBUFFER_END

            float4 _SegmentColors[MAX_SEGMENTS];

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.worldPos = TransformObjectToWorld(v.vertex.xyz);
                return o;
            }

            half4 frag(v2f i, half facing : VFACE) : SV_Target
            {
                clip(_FillAmount - 1e-5);

                float baseFillY = lerp(_MinY, _MaxY, _FillAmount);
                float tiltOffset = (i.worldPos.x - _CenterX) * _TiltX
                    + (i.worldPos.z - _CenterZ) * _TiltZ;
                float fillWorldY = baseFillY + tiltOffset;

                clip(step(i.worldPos.y, fillWorldY) - 0.5);

                // 根据片元在液面中的相对高度，确定属于哪一段
                float totalHeight = _MaxY - _MinY;
                float segHeight = totalHeight / max(_SegmentCount, 1);
                float relY = i.worldPos.y - _MinY;
                int segIdx = clamp((int)(relY / segHeight), 0, _SegmentCount - 1);

                half4 segColor = half4(_SegmentColors[segIdx]);

                // 顶面使用填充最高处所在段的颜色，稍微提亮作为水面效果
                float fillRelY = baseFillY - _MinY;
                int topSegIdx = clamp((int)(fillRelY / segHeight), 0, _SegmentCount - 1);
                half4 topSegColor = half4(_SegmentColors[topSegIdx]);
                half4 brightenedTop = saturate(topSegColor * 1.3);

                float foamLine = step(i.worldPos.y, fillWorldY)
                    - step(i.worldPos.y, fillWorldY - _FoamWidth);

                half4 liquidCol = lerp(segColor, _FoamColor, foamLine);
                half4 topCol = lerp(brightenedTop, _FoamColor, foamLine * 0.5);

                return facing > 0 ? liquidCol : topCol;
            }
            ENDHLSL
        }
    }
}
Shader "Unlit/Bottle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShortRadius("ShortRadius",Float) = 0.5
        _EllipseColor("_EllipseColor",Color) = (1,0,0,1)

    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _LineK;
            float _LineB;
            int _LineT;
            float _Angle;
            float4 _EllipseInfoArray[32];
            float _ShortRadius;
            int _EllipseCount;
            float4 _EllipseColor;

            inline float LineEquation(float2 uv)
            {
                float isLineT1 = 1.0 - step(0.01, abs(_LineT - 1.0)); // _LineT == 1 时为1
                float isLineT_1 = 1.0 - step(0.01, abs(_LineT + 1.0)); // _LineT == -1 时为1

                float angle1 = step(0, _Angle) * step(_Angle, 90); // [0,90]
                float angle2 = step(270, _Angle) * step(_Angle, 360); // [270,360]
                float usePositive = saturate(angle1 + angle2);

                // 计算所有可能的结果值
                float result1 = step(uv.y, _LineB); // uv.y <= _LineB
                float result2 = step(_LineK, uv.x); // uv.x >= _LineK
                float lineValue = _LineK * uv.x + _LineB - uv.y;
                float result3 = step(0.0, lineValue); // >= 0
                float result4 = 1.0 - step(0.0, lineValue); // < 0

                float normalCase = (1.0 - isLineT1) * (1.0 - isLineT_1);

                return isLineT1 * result1 +
                    isLineT_1 * result2 +
                    normalCase * usePositive * result3 +
                    normalCase * (1.0 - usePositive) * result4;
            }

            inline float EllipseEquation(int index, float2 uv)
            {
                float4 ellipseInfo = _EllipseInfoArray[index];
                float x0 = ellipseInfo.x;
                float y0 = ellipseInfo.y;
                float a = ellipseInfo.z * 0.5;
                float arc = ellipseInfo.w;
                float b = _ShortRadius * 0.5;

                float sinA = sin(arc);
                float cosA = cos(arc);
                float sinA2 = sinA * sinA;
                float cosA2 = cosA * cosA;
                float a2 = a * a;
                float b2 = b * b;
                float x02 = x0 * x0;
                float y02 = y0 * y0;

                float A = a2 * sinA2 + b2 * cosA2;
                float B = 2 * (b2 - a2) * sinA * cosA;
                float C = a2 * cosA2 + b2 * sinA2;
                float D = -2 * A * x0 - B * y0;
                float E = -B * x0 - 2 * C * y0;
                float F = A * x02 + B * x0 * y0 + C * y02 - a2 * b2;

                float x = uv.x;
                float x2 = x * x;
                float y = uv.y;
                float y2 = y * y;

                float value = A * x2 + B * x * y + C * y2 + D * x + E * y + F;

                return value + step(_EllipseCount, index);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                col.a = lerp(0, col.a, step(0.01, LineEquation(i.uv)));

                float value =
                    EllipseEquation(0, i.uv) *
                    EllipseEquation(1, i.uv) *
                    EllipseEquation(2, i.uv) *
                    EllipseEquation(3, i.uv) *
                    EllipseEquation(4, i.uv);

                col.rgb = lerp(_EllipseColor.rgb, col.rgb, step(0, value));
                col.a = lerp(1.0f, col.a, step(0, value));
                return col;
            }
            ENDCG
        }
    }
}
Shader "Unlit/Bottle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LineK("LineK",float) =1
        _LineB("LineB",float) =0
        _LineT("LineT",int) =0
        _Angle("Angle",Float) =0
        _ShortRadius("ShortRadius",Float) = 0.5
        _EllipseCount("EllipseCount",int) = 1

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

            bool CheckLine(float2 uv)
            {
                if (_LineT == 1)
                {
                    return uv.y <= _LineB;
                }

                if (_LineT == -1)
                {
                    return uv.x >= _LineK;
                }

                if ((_Angle >= 0 && _Angle <= 90) || (_Angle >= 270 && _Angle <= 360))
                {
                    return _LineK * uv.x + _LineB - uv.y >= 0;
                }
                return _LineK * uv.x + _LineB - uv.y < 0;
            }

            float EllipseEquation(int index, float4 ellipseInfo, float2 uv)
            {
                if (index >= _EllipseCount)
                {
                    return 1;
                }
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

                return value;
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
                if (!CheckLine(i.uv))
                {
                    col.a = 0;
                }

                float value =
                    EllipseEquation(0, _EllipseInfoArray[0], i.uv) *
                    EllipseEquation(1, _EllipseInfoArray[1], i.uv) *
                    EllipseEquation(2, _EllipseInfoArray[2], i.uv) *
                    EllipseEquation(3, _EllipseInfoArray[3], i.uv) *
                    EllipseEquation(4, _EllipseInfoArray[4], i.uv);

                if (value <= 0)
                {
                    col.a = 1.0f;
                    col.rgb = float3(1, 0, 0);
                }
                return col;
            }
            ENDCG
        }
    }
}
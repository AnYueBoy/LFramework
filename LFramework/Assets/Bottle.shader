Shader "Unlit/Bottle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LineK("LineK",float) =1
        _LineB("LineB",float) =0
        _LineT("LineT",int) =0
        _Angle("Angle",Float) =0

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
                return col;
            }
            ENDCG
        }
    }
}
Shader "2D Shader Pack/Sprite/Shiny" {
	Properties {
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1, 1, 1, 1)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		[Header(Shiny)]
		_Pos       ("Pos", Range(-1, 1)) = -0.3
		_Size      ("Size", Range(-1, 1)) = -0.1
		_Smooth    ("Smooth", Range(0, 1)) = 0.15
		_Intensity ("Intensity", Range(0, 4)) = 2.7
		_Speed     ("Speed", Range(0, 32)) = 20
	}
	SubShader {
		Tags {
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}
		Cull Off Lighting Off ZWrite Off
		Blend One OneMinusSrcAlpha
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ PIXELSNAP_ON
			#include "UnityCG.cginc"
			#include "Common.cginc"

			float _Pos, _Size, _Smooth, _Intensity, _Speed;

			half4 frag (v2f input) : SV_Target
			{
				float2 uv = input.uv;
				half4 c = tex2D(_MainTex, input.uv) * input.col;

				_Pos = _Pos + sin(_Time * _Speed) * 0.5;
				uv = uv - float2(_Pos, 0.5);
				float a = atan2(uv.x, uv.y) + 1.4, r = 3.1415;
				float d = cos(floor(0.5 + a / r) * r - a) * length(uv);
				float dist = 1.0 - smoothstep(_Size, _Size + _Smooth, d);
				c.rgb += dist*_Intensity;
				c.rgb *= c.a;
				return c;
			}
			ENDCG
		}
	}
	Fallback "Sprites/Default"
}
#ifndef COMMON_INCLUDED
#define COMMON_INCLUDED

half4 _Color;
sampler2D _MainTex;
half4 _MainTex_TexelSize;

struct v2f
{
	float4 pos : SV_POSITION;
	fixed4 col : COLOR;
	float2 uv  : TEXCOORD0;
};
v2f vert (appdata_full v)
{
	v2f o;
	o.pos = UnityObjectToClipPos(v.vertex);
	o.uv = v.texcoord;
	o.col = v.color * _Color;
#ifdef PIXELSNAP_ON
	o.pos = UnityPixelSnap(o.pos);
#endif
	return o;
}

half2 DisplacementUV (half2 uv, half x, half y, half v)
{
	return lerp(uv, uv + half2(x, y), v);
}

#endif
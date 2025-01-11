Shader "Unlit/ToonOutline"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _MainColor("Main Color", Color) = (1,1,1)
        _ShadowColor ("Shadow Color", Color) = (0.7, 0.7, 0.8)
        _ShadowRange ("Shadow Range", Range(0, 1)) = 0.5
        _ShadowSmooth("Shadow Smooth", Range(0, 1)) = 0.2

        [Space(10)]
        _OutlineWidth ("Outline Width", Range(0.01, 2)) = 0.24
        _OutLineColor ("OutLine Color", Color) = (0.5,0.5,0.5,1)
    }
    SubShader
    {

        Pass
        {
            Tags
            {
                "LightMode" ="UniversalForward"
            }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _OutlineWidth;
                half4 _OutLineColor;
                half3 _MainColor;
                half3 _ShadowColor;
                half _ShadowRange;
                half _ShadowSmooth;
            CBUFFER_END


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex);
                o.worldNormal = TransformObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            half4 frag(v2f i) :SV_Target
            {
                half4 col = 1;
                Light mainLight = GetMainLight();
                half4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half3 worldNormal = normalize(i.worldNormal);
                half3 worldLightDir = normalize(mainLight.direction.xyz);
                half halfLambert = dot(worldNormal, worldLightDir) * 0.5 + 0.5;
                half ramp = smoothstep(0, _ShadowSmooth, halfLambert - _ShadowRange);
                half3 diffuse = lerp(_ShadowColor, _MainColor, ramp);
                diffuse *= mainTex;
                col.rgb = mainLight.color * diffuse;
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" ="SRPDefaultUnlit"
            }
            Cull Front
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _OutlineWidth;
                half4 _OutLineColor;
                half3 _MainColor;
                half3 _ShadowColor;
                half _ShadowRange;
                half _ShadowSmooth;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                float4 pos = TransformObjectToHClip(v.vertex);
                // 将法线转到NDC空间
                float3 worldNormal = TransformObjectToWorldNormal(v.normal);
                float3 normalN = mul((float3x3)unity_MatrixVP, worldNormal);
                normalN *= pos.w;
                pos.xyz += 0.01 * _OutlineWidth * normalN;
                o.vertex = pos;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                return _OutLineColor;
            }
            ENDHLSL
        }
    }
}
Shader "LFramework/SimpleLiquid"
{
    Properties
    {
        [Header(Liquid Settings)]
        _FillLevel ("Fill Level (0-1)", Range(0, 1)) = 0.5
        _LiquidColor ("Liquid Color", Color) = (0.2, 0.6, 1, 1)
        [HDR]_TopColor ("Top Surface Color", Color) = (0.4, 0.8, 1, 1)

        [Header(Surface Options)]
        [Toggle]_DoubleSided ("Double Sided", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Geometry"
            "RenderType" = "Opaque"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "LIQUID"

            ZWrite On
            Cull Off
            AlphaToMask Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            float _FillLevel;
            float4 _LiquidColor;
            float4 _TopColor;

            // 从 C# 脚本传入的物体边界信息
            // x = 最低点 Y (local space), y = 最高点 Y (local space), z = 高度, w = 未使用
            float4 _ObjectBounds;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = v.normal;
                return o;
            }

            fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
            {
                // _ObjectBounds.x = 最低点 Y (local space)
                // _ObjectBounds.y = 最高点 Y (local space)
                // _ObjectBounds.z = 高度

                // 在 local space 中计算填充高度
                // FillLevel 0 = 最低点, 1 = 最高点
                float fillHeightLocal = lerp(_ObjectBounds.x, _ObjectBounds.y, _FillLevel);

                // 将填充高度转换到世界坐标
                float3 worldPosOfLocalFill = mul(unity_ObjectToWorld, float4(0, fillHeightLocal, 0, 1)).xyz;
                float fillHeightWorld = worldPosOfLocalFill.y;

                // 使用 step 函数裁剪液面上方的像素
                // 如果世界位置 Y 小于填充高度，则显示液体
                float fillMask = step(i.worldPos.y, fillHeightWorld);

                // 如果完全在液面上方，裁剪掉
                clip(fillMask - 0.001);

                // 判断是否是液面顶部（基于法线）
                float3 worldNormal = mul(unity_ObjectToWorld, float4(i.normal, 0.0)).xyz;
                float isTopSurface = saturate(dot(worldNormal, float3(0, 1, 0)));

                // 基础颜色：液体颜色与顶部颜色的混合
                // 对于背面（facing < 0），我们显示顶部颜色以模拟液体的上表面
                fixed4 finalColor;

                if (facing > 0)
                {
                    // 正面：显示侧面液体颜色
                    finalColor = _LiquidColor;
                }
                else
                {
                    // 背面：显示顶部表面颜色（模拟液面）
                    // 使用 isTopSurface 来平滑过渡
                    finalColor = lerp(_LiquidColor, _TopColor, smoothstep(0.5, 0.9, isTopSurface));
                }

                // 在液面附近添加轻微的渐隐效果，使过渡更平滑
                float distanceToFill = fillHeightWorld - i.worldPos.y;
                float edgeSmooth = smoothstep(0, 0.02, distanceToFill);
                finalColor.a *= edgeSmooth;

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 胶囊体 AABB 膨胀过程可视化工具
/// 挂载到带有 CapsuleCollider 的物体上，在 Scene 视图中实时查看旋转如何导致 AABB 膨胀
///
/// 关键发现：CapsuleCollider.bounds 由 PhysX 精确计算（无膨胀），
/// 但 Renderer.bounds 通过旋转网格局部 AABB 来计算，会产生膨胀误差。
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
[ExecuteAlways]
public class CapsuleAABBVisualizer : MonoBehaviour
{
    [Header("可视化图层")]
    [SerializeField] private bool showRendererAABB     = true;
    [SerializeField] private bool showColliderAABB     = true;
    [SerializeField] private bool showRotatedLocalAABB = true;
    [SerializeField] private bool showPreciseAABB      = true;
    [SerializeField] private bool showActualCapsule    = true;
    [SerializeField] private bool showAABBVertices     = true;
    [SerializeField] private bool showGapArrows        = true;
    [SerializeField] private bool showInfoPanel        = true;

    [Header("顶点球大小")]
    [SerializeField, Range(0.01f, 0.1f)] private float vertexSphereRadius = 0.03f;

    private void OnDrawGizmos()
    {
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule == null) return;

        var rend = GetComponent<Renderer>();

        // ── 基础数据 ────────────────────────────────────────────────
        Bounds colliderBounds = capsule.bounds;
        Bounds rendererBounds = rend != null ? rend.bounds : colliderBounds;
        Vector3 wCenter = transform.TransformPoint(capsule.center);

        Vector3 localAxis = capsule.direction switch
        {
            0 => Vector3.right,
            2 => Vector3.forward,
            _ => Vector3.up
        };
        Vector3 worldAxis = transform.TransformDirection(localAxis).normalized;

        float radius  = capsule.radius * MaxAbsScale();
        float halfLen = Mathf.Max(0f, capsule.height * MaxAbsScaleOnAxis(capsule.direction) / 2f - radius);

        Vector3 topSphere = wCenter + worldAxis * halfLen;
        Vector3 botSphere = wCenter - worldAxis * halfLen;

        // 精确 AABB：两个球心 ± 半径
        float preciseMaxY = Mathf.Max(topSphere.y, botSphere.y) + radius;
        float preciseMinY = Mathf.Min(topSphere.y, botSphere.y) - radius;
        float preciseMaxX = Mathf.Max(topSphere.x, botSphere.x) + radius;
        float preciseMinX = Mathf.Min(topSphere.x, botSphere.x) - radius;
        float preciseMaxZ = Mathf.Max(topSphere.z, botSphere.z) + radius;
        float preciseMinZ = Mathf.Min(topSphere.z, botSphere.z) - radius;
        Bounds preciseBounds = new Bounds();
        preciseBounds.SetMinMax(
            new Vector3(preciseMinX, preciseMinY, preciseMinZ),
            new Vector3(preciseMaxX, preciseMaxY, preciseMaxZ));

        // 局部 AABB 的 8 个顶点（先在局部空间算好长方体，再变换到世界空间）
        Vector3 localHalfSize = GetLocalAABBHalfSize(capsule);
        Vector3 localCenter   = capsule.center;
        Vector3[] rotatedVerts = GetRotatedLocalAABBVertices(localCenter, localHalfSize);

        float topGap = rendererBounds.max.y - preciseMaxY;
        float botGap = preciseMinY - rendererBounds.min.y;

        // ── 1. Renderer.bounds（黄色）—— 有膨胀的那个 ────────────────
        if (showRendererAABB && rend != null)
        {
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.9f);
            Gizmos.DrawWireCube(rendererBounds.center, rendererBounds.size);
        }

        // ── 1b. Collider.bounds（白色虚线）—— PhysX 精确计算 ─────────
        if (showColliderAABB)
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
            DrawDashedWireCube(colliderBounds.center, colliderBounds.size, 0.06f);
        }

        // ── 2. 旋转后的局部 AABB（青色）—— 膨胀的根源 ──────────────
        if (showRotatedLocalAABB)
        {
            Gizmos.color = new Color(0f, 0.85f, 1f, 0.9f);
            DrawRotatedBox(rotatedVerts);
        }

        // ── 3. 精确 AABB（绿色虚线）—— 理想结果 ───────────────────
        if (showPreciseAABB)
        {
            Gizmos.color = new Color(0.15f, 1f, 0.25f, 0.7f);
            DrawDashedWireCube(preciseBounds.center, preciseBounds.size, 0.08f);
        }

        // ── 4. 胶囊体实际形状（绿色半透明）────────────────────────
        if (showActualCapsule)
        {
            Gizmos.color = new Color(0.15f, 1f, 0.25f, 0.6f);
            Gizmos.DrawWireSphere(topSphere, radius);
            Gizmos.DrawWireSphere(botSphere, radius);

            Vector3 perp1 = Vector3.Cross(worldAxis, Vector3.right).normalized;
            if (perp1.sqrMagnitude < 0.01f)
                perp1 = Vector3.Cross(worldAxis, Vector3.forward).normalized;
            Vector3 perp2 = Vector3.Cross(worldAxis, perp1).normalized;

            Gizmos.DrawLine(topSphere + perp1 * radius, botSphere + perp1 * radius);
            Gizmos.DrawLine(topSphere - perp1 * radius, botSphere - perp1 * radius);
            Gizmos.DrawLine(topSphere + perp2 * radius, botSphere + perp2 * radius);
            Gizmos.DrawLine(topSphere - perp2 * radius, botSphere - perp2 * radius);
        }

        // ── 5. 旋转后局部 AABB 的 8 个顶点（红色小球）──────────────
        if (showAABBVertices)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 1f);
            foreach (var v in rotatedVerts)
                Gizmos.DrawSphere(v, vertexSphereRadius);

            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.35f);
            foreach (var v in rotatedVerts)
            {
                if (Mathf.Abs(v.y - rendererBounds.max.y) < 0.02f)
                    Gizmos.DrawLine(v, new Vector3(rendererBounds.max.x, v.y, v.z));
                if (Mathf.Abs(v.y - rendererBounds.min.y) < 0.02f)
                    Gizmos.DrawLine(v, new Vector3(rendererBounds.max.x, v.y, v.z));
            }
        }

        // ── 6. 误差指示箭头 ───────────────────────────────────────
        if (showGapArrows)
        {
            float arrowX  = rendererBounds.max.x + rendererBounds.size.x * 0.35f;
            float tickLen = rendererBounds.size.x * 0.12f;
            float z       = wCenter.z;

            DrawHorizontalTick(arrowX, rendererBounds.max.y, z, tickLen, new Color(1f, 0.85f, 0f));
            DrawHorizontalTick(arrowX, rendererBounds.min.y, z, tickLen, new Color(1f, 0.85f, 0f));

            DrawHorizontalTick(arrowX + tickLen * 1.5f, preciseMaxY, z, tickLen, new Color(0.15f, 1f, 0.25f));
            DrawHorizontalTick(arrowX + tickLen * 1.5f, preciseMinY, z, tickLen, new Color(0.15f, 1f, 0.25f));

            if (Mathf.Abs(topGap) > 0.001f)
                DrawArrow(arrowX + tickLen * 3f, preciseMaxY, rendererBounds.max.y, z,
                          new Color(1f, 0.25f, 0.25f), tickLen * 0.6f);
            if (Mathf.Abs(botGap) > 0.001f)
                DrawArrow(arrowX + tickLen * 3f, preciseMinY, rendererBounds.min.y, z,
                          new Color(1f, 0.25f, 0.25f), tickLen * 0.6f);
        }

#if UNITY_EDITOR
        if (showInfoPanel)
            DrawInfoPanel(rendererBounds, colliderBounds, preciseBounds, topGap, botGap);
#endif
    }

    // ── 局部 AABB 半尺寸（未旋转时紧贴胶囊体）──────────────────────
    private Vector3 GetLocalAABBHalfSize(CapsuleCollider capsule)
    {
        float r = capsule.radius;
        float h = capsule.height;
        return capsule.direction switch
        {
            0 => new Vector3(h / 2f, r, r),
            2 => new Vector3(r, r, h / 2f),
            _ => new Vector3(r, h / 2f, r),
        };
    }

    // 将局部 AABB 的 8 个顶点变换到世界空间
    private Vector3[] GetRotatedLocalAABBVertices(Vector3 localCenter, Vector3 halfSize)
    {
        Vector3[] verts = new Vector3[8];
        int idx = 0;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            Vector3 local = localCenter + new Vector3(halfSize.x * x, halfSize.y * y, halfSize.z * z);
            verts[idx++] = transform.TransformPoint(local);
        }
        return verts;
    }

    // 绘制旋转后的长方体线框（12 条边）
    private static void DrawRotatedBox(Vector3[] v)
    {
        // 顶点顺序: 0(-,-,-) 1(-,-,+) 2(-,+,-) 3(-,+,+) 4(+,-,-) 5(+,-,+) 6(+,+,-) 7(+,+,+)
        DrawEdge(v, 0, 1); DrawEdge(v, 2, 3); DrawEdge(v, 4, 5); DrawEdge(v, 6, 7); // z 方向
        DrawEdge(v, 0, 2); DrawEdge(v, 1, 3); DrawEdge(v, 4, 6); DrawEdge(v, 5, 7); // y 方向
        DrawEdge(v, 0, 4); DrawEdge(v, 1, 5); DrawEdge(v, 2, 6); DrawEdge(v, 3, 7); // x 方向
    }

    private static void DrawEdge(Vector3[] v, int a, int b)
    {
        Gizmos.DrawLine(v[a], v[b]);
    }

    // 虚线版 WireCube
    private static void DrawDashedWireCube(Vector3 center, Vector3 size, float dashLen)
    {
        Vector3 h = size * 0.5f;
        Vector3[] corners = new Vector3[8];
        int idx = 0;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
            corners[idx++] = center + new Vector3(h.x * x, h.y * y, h.z * z);

        int[] edges = {
            0,1, 2,3, 4,5, 6,7,
            0,2, 1,3, 4,6, 5,7,
            0,4, 1,5, 2,6, 3,7
        };
        for (int i = 0; i < edges.Length; i += 2)
            DrawDashedLine(corners[edges[i]], corners[edges[i + 1]], dashLen);
    }

    private static void DrawDashedLine(Vector3 from, Vector3 to, float dashLen)
    {
        Vector3 dir  = to - from;
        float   dist = dir.magnitude;
        dir /= dist;
        float t = 0f;
        bool  draw = true;
        while (t < dist)
        {
            float next = Mathf.Min(t + dashLen, dist);
            if (draw)
                Gizmos.DrawLine(from + dir * t, from + dir * next);
            t    = next;
            draw = !draw;
        }
    }

    private static void DrawHorizontalTick(float x, float y, float z, float half, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(x - half, y, z), new Vector3(x + half, y, z));
    }

    private static void DrawArrow(float x, float fromY, float toY, float z, Color color, float arrowSize)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(new Vector3(x, fromY, z), new Vector3(x, toY, z));
        float sign = Mathf.Sign(toY - fromY);
        Gizmos.DrawLine(new Vector3(x, toY, z),
                        new Vector3(x - arrowSize * 0.5f, toY - sign * arrowSize, z));
        Gizmos.DrawLine(new Vector3(x, toY, z),
                        new Vector3(x + arrowSize * 0.5f, toY - sign * arrowSize, z));
    }

    private float MaxAbsScale()
    {
        Vector3 s = transform.lossyScale;
        return Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
    }

    private float MaxAbsScaleOnAxis(int direction)
    {
        Vector3 s = transform.lossyScale;
        return direction switch
        {
            0 => Mathf.Abs(s.x),
            2 => Mathf.Abs(s.z),
            _ => Mathf.Abs(s.y),
        };
    }

#if UNITY_EDITOR
    private void DrawInfoPanel(Bounds rendBounds, Bounds collBounds, Bounds preciseBounds,
                               float topGap, float botGap)
    {
        Handles.BeginGUI();

        const float x      = 10f;
        const float startY = 10f;
        const float w      = 380f;
        const float lineH  = 18f;
        const float rows   = 18f;

        EditorGUI.DrawRect(new Rect(x, startY, w, lineH * rows + 14f),
                           new Color(0.08f, 0.08f, 0.12f, 0.88f));

        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal  = { textColor = Color.white },
            fontSize = 13
        };
        var gray = new GUIStyle(EditorStyles.label)
        {
            normal  = { textColor = new Color(0.75f, 0.75f, 0.75f) },
            fontSize = 11
        };

        float y = startY + 6f;
        GUI.Label(new Rect(x + 8f, y, w, lineH), "Renderer vs Collider bounds 对比", titleStyle);
        y += lineH + 4f;

        Color yellow = new Color(1f, 0.9f, 0.1f);
        Color white  = new Color(0.85f, 0.85f, 0.85f);
        Color green  = new Color(0.2f, 1f, 0.3f);
        Color red    = new Color(1f, 0.4f, 0.4f);
        Color cyan   = new Color(0.3f, 0.9f, 1f);

        DrawColorBlock(x + 8f, y, yellow, "■ 黄色", "Renderer.bounds (有膨胀!)", gray);
        y += lineH;
        DrawColorBlock(x + 8f, y, white, "■ 白色", "Collider.bounds (PhysX 精确)", gray);
        y += lineH;
        DrawColorBlock(x + 8f, y, cyan, "■ 青色", "网格局部 AABB 旋转后 (膨胀根源)", gray);
        y += lineH;
        DrawColorBlock(x + 8f, y, green, "■ 绿色", "精确 AABB / 实际胶囊体", gray);
        y += lineH;
        DrawColorBlock(x + 8f, y, red, "■ 红色", "顶点 & 误差", gray);
        y += lineH + 6f;

        DrawRow(x, y, w, lineH, "Renderer max.y", $"{rendBounds.max.y:F4}",    yellow, gray); y += lineH;
        DrawRow(x, y, w, lineH, "Collider max.y", $"{collBounds.max.y:F4}",    white,  gray); y += lineH;
        DrawRow(x, y, w, lineH, "精确     max.y", $"{preciseBounds.max.y:F4}", green,  gray); y += lineH;
        DrawRow(x, y, w, lineH, "Renderer 膨胀",  $"+{topGap:F4}",             red,    gray); y += lineH + 4f;

        DrawRow(x, y, w, lineH, "Renderer min.y", $"{rendBounds.min.y:F4}",    yellow, gray); y += lineH;
        DrawRow(x, y, w, lineH, "Collider min.y", $"{collBounds.min.y:F4}",    white,  gray); y += lineH;
        DrawRow(x, y, w, lineH, "精确     min.y", $"{preciseBounds.min.y:F4}", green,  gray); y += lineH;
        DrawRow(x, y, w, lineH, "Renderer 膨胀",  $"+{Mathf.Abs(botGap):F4}",  red,    gray);

        Handles.EndGUI();
    }

    private static void DrawColorBlock(float x, float y, Color color, string symbol, string desc, GUIStyle baseStyle)
    {
        var colorStyle = new GUIStyle(baseStyle) { normal = { textColor = color }, fontStyle = FontStyle.Bold };
        GUI.Label(new Rect(x, y, 30f, 18f), symbol, colorStyle);
        GUI.Label(new Rect(x + 30f, y, 280f, 18f), desc, baseStyle);
    }

    private static void DrawRow(float x, float y, float w, float h,
                                 string label, string value, Color valueColor, GUIStyle baseStyle)
    {
        var valStyle = new GUIStyle(baseStyle)
        {
            normal    = { textColor = valueColor },
            fontStyle = FontStyle.Bold
        };
        GUI.Label(new Rect(x + 10f,  y, 100f, h), label, baseStyle);
        GUI.Label(new Rect(x + 110f, y, 10f,  h), "=",   baseStyle);
        GUI.Label(new Rect(x + 122f, y, 140f, h), value, valStyle);
    }
#endif
}

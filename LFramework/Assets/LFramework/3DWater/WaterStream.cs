using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterStream : MonoBehaviour
{
    [Header("端点")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("形状")]
    [Tooltip("弧顶控制点相对中点的向上偏移")] public float arcHeight = 0.3f;
    [Tooltip("管道半径")] public float streamRadius = 0.05f;
    [Tooltip("沿曲线采样段数")] public int curveSegments = 20;
    [Tooltip("截面圆环顶点数")] public int ringVertices = 8;

    [Header("外观")]
    public Color streamColor = new Color(0.2f, 0.6f, 1f, 1f);

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;

    private Vector3 _prevStart;
    private Vector3 _prevEnd;

    void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh { name = "WaterStream" };
        _meshFilter.mesh = _mesh;

        // 创建并赋值材质
        var shader = Shader.Find("Custom/WaterStream");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        mat.color = streamColor;
        _meshRenderer.material = mat;
    }

    void OnEnable()
    {
        RebuildMesh();
    }

    void Update()
    {
        if (startPoint == null || endPoint == null) return;

        // 只在端点移动时重建，避免每帧重建浪费性能
        if (startPoint.position != _prevStart || endPoint.position != _prevEnd)
        {
            RebuildMesh();
            _prevStart = startPoint.position;
            _prevEnd = endPoint.position;
        }

        // 同步颜色
        _meshRenderer.material.color = streamColor;
    }

    void OnDisable()
    {
        if (_mesh != null)
            _mesh.Clear();
    }

    void OnDestroy()
    {
        if (_mesh != null)
            Destroy(_mesh);
    }

    public void RebuildMesh()
    {
        if (startPoint == null || endPoint == null) return;

        Vector3 p0 = startPoint.position;
        Vector3 p2 = endPoint.position;
        Vector3 p1 = (p0 + p2) * 0.5f + Vector3.up * arcHeight;

        int rings = curveSegments + 1;
        int rv = ringVertices;

        // 顶点数 = 环数 * 每环顶点数 + 2（首尾封口中心点）
        var vertices = new Vector3[rings * rv + 2];
        var triangles = new System.Collections.Generic.List<int>();

        // 采样曲线点与切线
        var curvePoints = new Vector3[rings];
        var curveTangents = new Vector3[rings];

        for (int i = 0; i < rings; i++)
        {
            float t = (float)i / curveSegments;
            curvePoints[i] = QuadBezier(p0, p1, p2, t);
            curveTangents[i] = QuadBezierTangent(p0, p1, p2, t).normalized;
        }

        // 生成圆环顶点（平行传输，避免截面扭转）
        // 初始化第一帧坐标系
        Vector3 t0 = curveTangents[0];
        Vector3 initUp = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(t0, initUp)) > 0.99f)
            initUp = Vector3.right;
        Vector3 frameRight = Vector3.Cross(initUp, t0).normalized;
        Vector3 frameUp = Vector3.Cross(t0, frameRight).normalized;

        for (int i = 0; i < rings; i++)
        {
            // 平行传输：将上一帧的 right 投影到当前切线的法平面，保持连续性
            if (i > 0)
            {
                Vector3 curTangent = curveTangents[i];
                // 将 frameRight 投影到当前切线法平面并重新归一化
                frameRight = (frameRight - Vector3.Dot(frameRight, curTangent) * curTangent).normalized;
                frameUp = Vector3.Cross(curTangent, frameRight).normalized;
            }

            for (int j = 0; j < rv; j++)
            {
                float angle = 2f * Mathf.PI * j / rv;
                Vector3 offset = (Mathf.Cos(angle) * frameRight + Mathf.Sin(angle) * frameUp) * streamRadius;
                vertices[i * rv + j] = transform.InverseTransformPoint(curvePoints[i] + offset);
            }
        }

        // 首尾封口中心点（局部坐标）
        int capStartIdx = rings * rv;
        int capEndIdx = rings * rv + 1;
        vertices[capStartIdx] = transform.InverseTransformPoint(curvePoints[0]);
        vertices[capEndIdx] = transform.InverseTransformPoint(curvePoints[rings - 1]);

        // 侧面三角形（相邻两环之间的四边形 → 两个三角形）
        for (int i = 0; i < curveSegments; i++)
        {
            int baseA = i * rv;
            int baseB = (i + 1) * rv;
            for (int j = 0; j < rv; j++)
            {
                int next = (j + 1) % rv;
                // 四边形 A-B-B'-A'
                triangles.Add(baseA + j);
                triangles.Add(baseB + j);
                triangles.Add(baseB + next);

                triangles.Add(baseA + j);
                triangles.Add(baseB + next);
                triangles.Add(baseA + next);
            }
        }

        // 起始端面封口（fan）
        for (int j = 0; j < rv; j++)
        {
            int next = (j + 1) % rv;
            triangles.Add(capStartIdx);
            triangles.Add(next);        // 反向保证法线朝外
            triangles.Add(j);
        }

        // 末端端面封口（fan）
        int lastRingBase = (rings - 1) * rv;
        for (int j = 0; j < rv; j++)
        {
            int next = (j + 1) % rv;
            triangles.Add(capEndIdx);
            triangles.Add(lastRingBase + j);
            triangles.Add(lastRingBase + next);
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles.ToArray();
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    // 二次贝塞尔曲线采样
    static Vector3 QuadBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    // 二次贝塞尔切线（一阶导数）
    static Vector3 QuadBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        curveSegments = Mathf.Max(2, curveSegments);
        ringVertices = Mathf.Max(3, ringVertices);
        streamRadius = Mathf.Max(0.001f, streamRadius);

        if (Application.isPlaying && isActiveAndEnabled)
            RebuildMesh();
    }
#endif
}

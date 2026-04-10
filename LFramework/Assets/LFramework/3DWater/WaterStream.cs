using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterStream : MonoBehaviour
{
    [Header("端点")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("形状")]
    [Tooltip("起始处半径")] public float startRadius = 0.04f;
    [Tooltip("末端半径")] public float endRadius = 0.015f;
    [Tooltip("沿曲线采样段数")] public int curveSegments = 24;
    [Tooltip("截面圆环顶点数")] public int ringVertices = 8;

    [Header("物理")]
    [Tooltip("重力加速度")] public float gravity = 9.81f;
    [Tooltip("水流出射速度倍数")] public float exitSpeedScale = 1.5f;

    [Header("外观")]
    public Color streamColor = new Color(0.2f, 0.6f, 1f, 0.85f);

    [Header("状态")]
    [Tooltip("是否正在倒水")] public bool isPouring;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private Material _mat;

    private Vector3 _prevStart;
    private Vector3 _prevEnd;
    private bool _prevPouring;

    void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();

        _mesh = new Mesh { name = "WaterStream" };
        _meshFilter.mesh = _mesh;

        var shader = Shader.Find("Custom/WaterStream");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        _mat = new Material(shader);
        _mat.SetColor("_Color", streamColor);
        _meshRenderer.material = _mat;
    }

    void Update()
    {
        if (!isPouring || startPoint == null || endPoint == null)
        {
            if (_mesh.vertexCount > 0)
                _mesh.Clear();
            _prevPouring = isPouring;
            return;
        }

        bool changed = startPoint.position != _prevStart
                     || endPoint.position != _prevEnd
                     || isPouring != _prevPouring;

        if (changed)
        {
            RebuildMesh();
            _prevStart = startPoint.position;
            _prevEnd = endPoint.position;
            _prevPouring = isPouring;
        }

        if (_mat != null)
            _mat.SetColor("_Color", streamColor);
    }

    void OnDisable()
    {
        if (_mesh != null)
            _mesh.Clear();
    }

    void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
        if (_mat != null) Destroy(_mat);
    }

    public void RebuildMesh()
    {
        if (startPoint == null || endPoint == null) return;

        Vector3 p0 = startPoint.position;
        Vector3 pEnd = endPoint.position;

        var curvePoints = ComputeParabolicArc(p0, pEnd, curveSegments + 1);
        int rings = curvePoints.Length;
        int rv = ringVertices;

        var vertices = new Vector3[rings * rv + 2];
        var uvs = new Vector2[vertices.Length];
        var normals = new Vector3[vertices.Length];
        var triangles = new System.Collections.Generic.List<int>((rings - 1) * rv * 6 + rv * 6);

        var tangents = new Vector3[rings];
        for (int i = 0; i < rings; i++)
        {
            if (i < rings - 1)
                tangents[i] = (curvePoints[i + 1] - curvePoints[i]).normalized;
            else
                tangents[i] = (curvePoints[i] - curvePoints[i - 1]).normalized;
        }

        Vector3 t0 = tangents[0];
        Vector3 initUp = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(t0, initUp)) > 0.99f)
            initUp = Vector3.right;
        Vector3 frameRight = Vector3.Cross(initUp, t0).normalized;
        Vector3 frameUp = Vector3.Cross(t0, frameRight).normalized;

        float totalLen = 0f;
        var cumLengths = new float[rings];
        cumLengths[0] = 0f;
        for (int i = 1; i < rings; i++)
        {
            totalLen += Vector3.Distance(curvePoints[i], curvePoints[i - 1]);
            cumLengths[i] = totalLen;
        }

        for (int i = 0; i < rings; i++)
        {
            if (i > 0)
            {
                Vector3 curTangent = tangents[i];
                frameRight = (frameRight - Vector3.Dot(frameRight, curTangent) * curTangent).normalized;
                frameUp = Vector3.Cross(curTangent, frameRight).normalized;
            }

            float t = (float)i / (rings - 1);
            float radius = Mathf.Lerp(startRadius, endRadius, t);
            float u = totalLen > 0 ? cumLengths[i] / totalLen : t;

            for (int j = 0; j < rv; j++)
            {
                float angle = 2f * Mathf.PI * j / rv;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                Vector3 offset = (cos * frameRight + sin * frameUp) * radius;
                Vector3 wp = curvePoints[i] + offset;

                int idx = i * rv + j;
                vertices[idx] = transform.InverseTransformPoint(wp);
                uvs[idx] = new Vector2(u, (float)j / rv);
                normals[idx] = (cos * frameRight + sin * frameUp).normalized;
            }
        }

        int capStartIdx = rings * rv;
        int capEndIdx = rings * rv + 1;
        vertices[capStartIdx] = transform.InverseTransformPoint(curvePoints[0]);
        vertices[capEndIdx] = transform.InverseTransformPoint(curvePoints[rings - 1]);
        uvs[capStartIdx] = new Vector2(0, 0.5f);
        uvs[capEndIdx] = new Vector2(1, 0.5f);
        normals[capStartIdx] = transform.InverseTransformDirection(-tangents[0]);
        normals[capEndIdx] = transform.InverseTransformDirection(tangents[rings - 1]);

        for (int i = 0; i < rings - 1; i++)
        {
            int baseA = i * rv;
            int baseB = (i + 1) * rv;
            for (int j = 0; j < rv; j++)
            {
                int next = (j + 1) % rv;
                triangles.Add(baseA + j);
                triangles.Add(baseB + j);
                triangles.Add(baseB + next);

                triangles.Add(baseA + j);
                triangles.Add(baseB + next);
                triangles.Add(baseA + next);
            }
        }

        for (int j = 0; j < rv; j++)
        {
            int next = (j + 1) % rv;
            triangles.Add(capStartIdx);
            triangles.Add(next);
            triangles.Add(j);
        }

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
        _mesh.normals = normals;
        _mesh.uv = uvs;
        _mesh.triangles = triangles.ToArray();
        _mesh.RecalculateBounds();
    }

    /// <summary>
    /// 水平匀速 + 竖直自由落体，v0y = 0，水只往下掉。
    /// </summary>
    Vector3[] ComputeParabolicArc(Vector3 from, Vector3 to, int sampleCount)
    {
        var points = new Vector3[sampleCount];

        float dy = to.y - from.y;
        float fallHeight = Mathf.Max(-dy, 0.01f);

        // 自由落体时间: h = 0.5*g*T^2
        float T = Mathf.Sqrt(2f * fallHeight / gravity);
        T = Mathf.Max(T, 0.05f);

        Vector3 horizDelta = new Vector3(to.x - from.x, 0, to.z - from.z);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / (sampleCount - 1);
            float time = t * T;

            // 水平方向匀速插值到终点
            Vector3 h = from + horizDelta * t;
            // 竖直方向纯自由落体（v0y = 0，只往下掉）
            float y = from.y - 0.5f * gravity * time * time;

            points[i] = new Vector3(h.x, y, h.z);
        }

        points[0] = from;
        points[sampleCount - 1] = to;

        return points;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        curveSegments = Mathf.Max(2, curveSegments);
        ringVertices = Mathf.Max(3, ringVertices);
        startRadius = Mathf.Max(0.001f, startRadius);
        endRadius = Mathf.Max(0.001f, endRadius);

        if (Application.isPlaying && isActiveAndEnabled)
            RebuildMesh();
    }

    void OnDrawGizmosSelected()
    {
        if (startPoint == null || endPoint == null) return;
        var pts = ComputeParabolicArc(startPoint.position, endPoint.position, 30);
        Gizmos.color = streamColor;
        for (int i = 0; i < pts.Length - 1; i++)
            Gizmos.DrawLine(pts[i], pts[i + 1]);
        Gizmos.DrawWireSphere(startPoint.position, startRadius);
        Gizmos.DrawWireSphere(endPoint.position, endRadius);
    }
#endif
}

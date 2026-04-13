using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class LiquidControl : MonoBehaviour
{
    public const int MaxSegments = 8;

    [Range(0f, 1f)] public float fillAmount = 0f;

    [Range(1, MaxSegments)] public int segmentCount = 1;

    public Color[] segmentColors = { new Color(0.2f, 0.6f, 1f, 1f) };

    #region 水面弹性阻尼

    /// <summary>
    /// 弹簧刚度：值越大水面回正越快
    /// </summary>
    [SerializeField] private float stiffness = 8f;

    /// <summary>
    /// 阻尼：值越大振荡衰减越快 
    /// </summary>
    [SerializeField] private float damping = 1.5f;

    /// <summary>
    /// 惯性强度：容器加速度对水面倾斜的影响倍数
    /// </summary>
    [SerializeField] private float inertiaScale = 0.08f;

    /// <summary>
    /// 最大倾斜坡度（世界单位/米），防止穿模
    /// </summary>
    [SerializeField] private float maxTilt = 0.5f;

    #endregion

    private Renderer _renderer;
    private Collider _collider;

    private Vector2 _tilt;
    private Vector2 _tiltVelocity;

    private Vector3 _prevPosition;
    private Vector3 _prevVelocity;

    #region Shader Keyword

    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
    private static readonly int MinYID = Shader.PropertyToID("_MinY");
    private static readonly int MaxYID = Shader.PropertyToID("_MaxY");
    private static readonly int TiltXID = Shader.PropertyToID("_TiltX");
    private static readonly int TiltZID = Shader.PropertyToID("_TiltZ");
    private static readonly int CenterXID = Shader.PropertyToID("_CenterX");
    private static readonly int CenterZID = Shader.PropertyToID("_CenterZ");
    private static readonly int SegmentCountID = Shader.PropertyToID("_SegmentCount");
    private static readonly int SegmentColorsID = Shader.PropertyToID("_SegmentColors");

    #endregion

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider>();
        _prevPosition = transform.position; // 初始化位置，防止第一帧计算出错误的加速度
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
        {
            UpdateMaterial();
            return;
        }

        // 计算容器速度与加速度
        Vector3 currentVelocity = (transform.position - _prevPosition) / dt;
        Vector3 acceleration = (currentVelocity - _prevVelocity) / dt;
        _prevPosition = transform.position;
        _prevVelocity = currentVelocity;

        // 弹簧阻尼模拟（X轴）
        // 惯性力与加速度方向相反（水面向运动反方向倾斜）
        float forceX = -acceleration.x * inertiaScale - stiffness * _tilt.x - damping * _tiltVelocity.x;
        _tiltVelocity.x += forceX * dt;
        _tilt.x += _tiltVelocity.x * dt;

        // 弹簧阻尼模拟（Z轴）
        float forceZ = -acceleration.z * inertiaScale - stiffness * _tilt.y - damping * _tiltVelocity.y;
        _tiltVelocity.y += forceZ * dt;
        _tilt.y += _tiltVelocity.y * dt;

        // 限制最大倾斜量
        _tilt = Vector2.ClampMagnitude(_tilt, maxTilt);

        UpdateMaterial();
    }

    void UpdateMaterial()
    {
        Bounds bounds = _collider.bounds;
        Material mat = _renderer.material;

        mat.SetFloat(MinYID, bounds.min.y);
        mat.SetFloat(MaxYID, bounds.max.y);
        mat.SetFloat(FillAmountID, fillAmount);
        mat.SetFloat(TiltXID, _tilt.x);
        mat.SetFloat(TiltZID, _tilt.y);
        mat.SetFloat(CenterXID, bounds.center.x);
        mat.SetFloat(CenterZID, bounds.center.z);

        int count = Mathf.Clamp(segmentCount, 1, MaxSegments);
        mat.SetInt(SegmentCountID, count);

        var colors = new Vector4[MaxSegments];
        for (int i = 0; i < MaxSegments; i++)
        {
            if (segmentColors != null && i < segmentColors.Length)
                colors[i] = segmentColors[i];
            else
                colors[i] = Color.white;
        }

        mat.SetVectorArray(SegmentColorsID, colors);
    }
}
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class LiquidControl : MonoBehaviour
{
    [Range(0f, 1f)] public float fillAmount = 0f;

    [Header("Slosh")] [Tooltip("弹簧刚度：值越大水面回正越快")]
    public float stiffness = 8f;

    [Tooltip("阻尼：值越大振荡衰减越快")] public float damping = 1.5f;
    [Tooltip("惯性强度：容器加速度对水面倾斜的影响倍数")] public float inertiaScale = 0.08f;
    [Tooltip("最大倾斜坡度（世界单位/米），防止穿模")] public float maxTilt = 0.5f;

    private Renderer _renderer;

    // 水面倾斜坡度（X轴、Z轴各独立的弹簧阻尼系统）
    private Vector2 _tilt; // 当前倾斜量 (tiltX, tiltZ)
    private Vector2 _tiltVelocity; // 倾斜速度

    private Vector3 _prevPosition;
    private Vector3 _prevVelocity;

    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
    private static readonly int MinYID = Shader.PropertyToID("_MinY");
    private static readonly int MaxYID = Shader.PropertyToID("_MaxY");
    private static readonly int TiltXID = Shader.PropertyToID("_TiltX");
    private static readonly int TiltZID = Shader.PropertyToID("_TiltZ");
    private static readonly int CenterXID = Shader.PropertyToID("_CenterX");
    private static readonly int CenterZID = Shader.PropertyToID("_CenterZ");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _prevPosition = transform.position;
    }

    void Update()
    {
#if UNITY_EDITOR
        // 编辑器非Play模式下不模拟物理，直接归零倾斜
        if (!Application.isPlaying)
        {
            _tilt = Vector2.zero;
            _tiltVelocity = Vector2.zero;
            _prevPosition = transform.position;
            _prevVelocity = Vector3.zero;
            UpdateMaterial();
            return;
        }
#endif
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
        Bounds bounds = _renderer.bounds;
        _renderer.material.SetFloat(MinYID, bounds.min.y);
        _renderer.material.SetFloat(MaxYID, bounds.max.y);
        _renderer.material.SetFloat(FillAmountID, fillAmount);
        _renderer.material.SetFloat(TiltXID, _tilt.x);
        _renderer.material.SetFloat(TiltZID, _tilt.y);
        _renderer.material.SetFloat(CenterXID, bounds.center.x);
        _renderer.material.SetFloat(CenterZID, bounds.center.z);
    }
}
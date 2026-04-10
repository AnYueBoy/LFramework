using UnityEngine;

/// <summary>
/// 手动控制倒水：指定出水口和落水点的锚点，按快捷键或 Inspector 开关触发。
/// </summary>
public class LiquidPourController : MonoBehaviour
{
    [Header("锚点")]
    [Tooltip("出水口锚点（你创建的空物体）")]
    public Transform pourPoint;

    [Tooltip("落水点锚点（你创建的空物体）")]
    public Transform targetPoint;

    [Header("水流")]
    [Tooltip("WaterStream 组件引用（若空则自动创建）")]
    public WaterStream waterStream;

    [Header("控制")]
    [Tooltip("Inspector 开关：勾选即倒水")]
    public bool pouring;

    [Tooltip("快捷键（默认 P）")]
    public KeyCode pourKey = KeyCode.P;

    [Header("倒水参数")]
    [Tooltip("每秒倒出的液面量（fillAmount 单位）")]
    public float pourRate = 0.15f;

    [Tooltip("水流颜色跟随瓶中顶层液体")]
    public bool colorFollowsLiquid = true;

    [Header("容器引用")]
    [Tooltip("出水瓶（有 LiquidControl，默认取自身）")]
    public LiquidControl sourceContainer;

    [Tooltip("接水瓶（有 LiquidControl，可选）")]
    public LiquidControl targetContainer;

    void Awake()
    {
        if (sourceContainer == null)
            sourceContainer = GetComponent<LiquidControl>();

        if (waterStream == null && pourPoint != null && targetPoint != null)
            CreateWaterStream();
    }

    void CreateWaterStream()
    {
        var go = new GameObject("WaterStream_Auto");
        go.transform.SetParent(transform.root);
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        waterStream = go.AddComponent<WaterStream>();
        waterStream.startPoint = pourPoint;
        waterStream.endPoint = targetPoint;
    }

    void Update()
    {
        if (Input.GetKeyDown(pourKey))
            pouring = !pouring;

        if (pourPoint == null || targetPoint == null) return;

        if (waterStream == null && pouring)
            CreateWaterStream();

        if (waterStream != null)
        {
            waterStream.isPouring = pouring;
            if (pouring)
            {
                waterStream.startPoint = pourPoint;
                waterStream.endPoint = targetPoint;
                SyncColor();
                waterStream.RebuildMesh();
            }
        }
    }

    void SyncColor()
    {
        if (!colorFollowsLiquid || sourceContainer == null) return;
        if (sourceContainer.segmentColors == null || sourceContainer.segmentColors.Length == 0) return;

        int topIdx = Mathf.Clamp(sourceContainer.segmentCount - 1, 0, sourceContainer.segmentColors.Length - 1);
        Color c = sourceContainer.segmentColors[topIdx];
        c.a = 0.85f;
        waterStream.streamColor = c;
    }

    void TransferLiquid()
    {
        float amount = pourRate * Time.deltaTime;

        if (sourceContainer != null)
        {
            float actual = Mathf.Min(amount, sourceContainer.fillAmount);
            sourceContainer.fillAmount -= actual;
            amount = actual;
        }

        if (targetContainer != null)
        {
            float space = 1f - targetContainer.fillAmount;
            targetContainer.fillAmount += Mathf.Min(amount, space);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (pourPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(pourPoint.position, 0.02f);
        }
        if (targetPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPoint.position, 0.02f);
        }
        if (pourPoint != null && targetPoint != null)
        {
            Gizmos.color = pouring ? Color.green : Color.gray;
            Gizmos.DrawLine(pourPoint.position, targetPoint.position);
        }
    }
#endif
}

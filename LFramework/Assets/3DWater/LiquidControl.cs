using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class LiquidControl : MonoBehaviour
{
    [Range(0f, 1f)]
    public float fillAmount = 0f;

    private Renderer _renderer;
    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
    private static readonly int MinYID = Shader.PropertyToID("_MinY");
    private static readonly int MaxYID = Shader.PropertyToID("_MaxY");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    void Update()
    {
        Bounds bounds = _renderer.bounds;
        _renderer.material.SetFloat(MinYID, bounds.min.y);
        _renderer.material.SetFloat(MaxYID, bounds.max.y);
        _renderer.material.SetFloat(FillAmountID, fillAmount);
    }
}

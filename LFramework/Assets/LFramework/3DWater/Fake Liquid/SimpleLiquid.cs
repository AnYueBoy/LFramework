using UnityEngine;

namespace LFramework
{
    /// <summary>
    /// 简单液体控制组件
    /// 用于控制 SimpleLiquid Shader 的填充高度
    /// </summary>
    [ExecuteInEditMode]
    public class SimpleLiquid : MonoBehaviour
    {
        [Header("Liquid Settings")]
        [Range(0f, 1f)]
        [Tooltip("液体填充高度 (0 = 空, 1 = 满)")]
        [SerializeField] private float _fillLevel = 0.5f;

        [Tooltip("液体颜色")]
        [SerializeField] private Color _liquidColor = new Color(0.2f, 0.6f, 1f, 1f);

        [Tooltip("液面顶部颜色")]
        [SerializeField] private Color _topColor = new Color(0.4f, 0.8f, 1f, 1f);

        // 组件引用
        private Renderer _renderer;
        private MeshFilter _meshFilter;
        private MaterialPropertyBlock _propertyBlock;

        // Shader 属性 ID（用于优化）
        private static readonly int FillLevelProperty = Shader.PropertyToID("_FillLevel");
        private static readonly int LiquidColorProperty = Shader.PropertyToID("_LiquidColor");
        private static readonly int TopColorProperty = Shader.PropertyToID("_TopColor");
        private static readonly int ObjectBoundsProperty = Shader.PropertyToID("_ObjectBounds");

        // 物体边界缓存
        private Vector4 _objectBounds;

        /// <summary>
        /// 获取或设置填充高度 (0-1)
        /// </summary>
        public float FillLevel
        {
            get => _fillLevel;
            set
            {
                _fillLevel = Mathf.Clamp01(value);
                UpdateMaterialProperties();
            }
        }

        /// <summary>
        /// 获取或设置液体颜色
        /// </summary>
        public Color LiquidColor
        {
            get => _liquidColor;
            set
            {
                _liquidColor = value;
                UpdateMaterialProperties();
            }
        }

        /// <summary>
        /// 获取或设置液面顶部颜色
        /// </summary>
        public Color TopColor
        {
            get => _topColor;
            set
            {
                _topColor = value;
                UpdateMaterialProperties();
            }
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void OnValidate()
        {
            // 在编辑器中调整属性时立即更新
            if (Application.isEditor)
            {
                Initialize();
                UpdateMaterialProperties();
            }
        }

        private void Update()
        {
            UpdateMaterialProperties();
        }

        /// <summary>
        /// 初始化组件引用
        /// </summary>
        private void Initialize()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }

            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
            }

            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }

            // 计算物体边界
            CalculateObjectBounds();
        }

        /// <summary>
        /// 计算物体在 Local Space 中的 Y 轴边界
        /// </summary>
        private void CalculateObjectBounds()
        {
            if (_meshFilter == null || _meshFilter.sharedMesh == null)
            {
                // 默认边界（Unity 标准 Cube 的范围）
                _objectBounds = new Vector4(-0.5f, 0.5f, 1f, 0f);
                return;
            }

            Bounds bounds = _meshFilter.sharedMesh.bounds;

            // x = 最低点 Y (local space)
            // y = 最高点 Y (local space)
            // z = 高度
            // w = 未使用
            float minY = bounds.min.y;
            float maxY = bounds.max.y;
            float height = maxY - minY;

            _objectBounds = new Vector4(minY, maxY, height, 0f);
        }

        /// <summary>
        /// 更新材质属性
        /// </summary>
        private void UpdateMaterialProperties()
        {
            if (_renderer == null) return;

            // 确保边界信息已计算
            if (_objectBounds.z <= 0f)
            {
                CalculateObjectBounds();
            }

            // 使用 MaterialPropertyBlock 避免创建材质实例
            _renderer.GetPropertyBlock(_propertyBlock);

            _propertyBlock.SetFloat(FillLevelProperty, _fillLevel);
            _propertyBlock.SetColor(LiquidColorProperty, _liquidColor);
            _propertyBlock.SetColor(TopColorProperty, _topColor);
            _propertyBlock.SetVector(ObjectBoundsProperty, _objectBounds);

            _renderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>
        /// 设置填充高度（程序化控制接口）
        /// </summary>
        /// <param name="level">填充高度 (0-1)</param>
        public void SetFillLevel(float level)
        {
            FillLevel = level;
        }
    }
}

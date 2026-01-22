using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace LFramework.SoftMask
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SoftMask : UIBehaviour, ISoftMask, ICanvasRaycastFilter
    {
        [SerializeField] private MaskSource _source = MaskSource.Graphic;
        [SerializeField] private RectTransform _separateMask;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private BorderMode _spriteBorderMode = BorderMode.Simple;
        [SerializeField] private float _spritePixelsPerUnitMultiplier = 1f;
        [SerializeField] private Texture _texture;
        [SerializeField] private Rect _textureUVRect = new Rect(0f, 0f, 1f, 1f);
        [SerializeField] private Color _channelWeight = MaskChannel.Alpha;
        [SerializeField] private float _raycastThreshold;
        [SerializeField] private bool _invertMask;
        [SerializeField] private bool _invertOutsides;

        private MaterialParameters _matParameters;
        private WarningReporter _warningReporter;
        private readonly MaterialReplacement _materialReplacement;
        private bool _dirty;
        private bool _destroyed;
        private bool _maskingWasEnable;
        private Rect _lastMaskRect;

        private RectTransform _maskRectTrans;
        private Graphic _graphic;
        private Canvas _bindCanvas;

        private readonly Queue<Transform> transToSpawnMaskableQueue = new Queue<Transform>();

        public SoftMask()
        {
            _materialReplacement = new MaterialReplacement(new MatDefaultReplacer(), MatParameterHandler);
            _warningReporter = new WarningReporter(this);
        }

        #region Property

        public MaskSource Source
        {
            get => _source;
            set
            {
                _source = value;
                SetDirty();
            }
        }

        public RectTransform SeparateMask
        {
            get => _separateMask;
            set
            {
                if (_separateMask == value)
                {
                    return;
                }

                _separateMask = value;
                _graphic = null;
                _maskRectTrans = null;
                SetDirty();
            }
        }

        public Sprite Sprite
        {
            get => _sprite;
            set
            {
                if (_sprite == value)
                {
                    return;
                }

                _sprite = value;
                SetDirty();
            }
        }

        public BorderMode SpriteBorderMode
        {
            get => _spriteBorderMode;
            set
            {
                if (_spriteBorderMode == value)
                {
                    return;
                }

                _spriteBorderMode = value;
                SetDirty();
            }
        }

        public float SpritePixelsPerUnitMultiplier
        {
            get => _spritePixelsPerUnitMultiplier;
            set
            {
                if (Mathf.Approximately(_spritePixelsPerUnitMultiplier, value))
                {
                    return;
                }

                _spritePixelsPerUnitMultiplier = value;
                _spritePixelsPerUnitMultiplier = Mathf.Max(0.01f, _spritePixelsPerUnitMultiplier);
                SetDirty();
            }
        }

        public Texture2D Texture
        {
            get => _texture as Texture2D;
            set
            {
                if (_texture == value)
                {
                    return;
                }

                _texture = value;
                SetDirty();
            }
        }

        public RenderTexture RenderTexture
        {
            get => _texture as RenderTexture;
            set
            {
                if (_texture == value)
                {
                    return;
                }

                _texture = value;
                SetDirty();
            }
        }

        public Rect TextureUVRect
        {
            get => _textureUVRect;
            set
            {
                if (_textureUVRect == value)
                {
                    return;
                }

                _textureUVRect = value;
                SetDirty();
            }
        }

        public Color ChannelWeight
        {
            get => _channelWeight;
            set
            {
                if (_channelWeight == value)
                {
                    return;
                }

                _channelWeight = value;
                SetDirty();
            }
        }

        public float RaycastThreshold
        {
            get => _raycastThreshold;
            set => _raycastThreshold = value;
        }

        public bool InvertMask
        {
            get => _invertMask;
            set
            {
                if (_invertMask == value)
                {
                    return;
                }

                _invertMask = value;
                SetDirty();
            }
        }

        public bool InvertOutsides
        {
            get => _invertOutsides;
            set
            {
                if (_invertOutsides == value)
                {
                    return;
                }

                _invertOutsides = value;
                SetDirty();
            }
        }

        #endregion

        private Canvas BindCanvas
        {
            get
            {
                if (_bindCanvas == null)
                {
                    _bindCanvas = NearestEnabledCanvas();
                }

                return _bindCanvas;
            }
        }

        private RectTransform MaskRectTrans
        {
            get
            {
                if (_maskRectTrans != null)
                {
                    return _maskRectTrans;
                }

                if (_separateMask != null)
                {
                    return _maskRectTrans = _separateMask;
                }

                return _maskRectTrans = GetComponent<RectTransform>();
            }
        }

        private bool IsBasedOnGraphic => _source == MaskSource.Graphic;

        public bool IsUsingRaycastFiltering => _raycastThreshold > 0f;

        public bool IsAlive => this && !_destroyed;
        public bool IsMaskingEnable => isActiveAndEnabled && BindCanvas != null;

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(MaskRectTrans, sp, eventCamera,
                    out var localPoint))
            {
                return false;
            }

            if (!MathOP.Inside(localPoint, LocalMaskRect(Vector4.zero)))
            {
                return _invertOutsides;
            }

            if (_matParameters.texture == null)
            {
                return true;
            }

            if (!IsUsingRaycastFiltering)
            {
                return true;
            }

            var sampleMaskResult = _matParameters.SampleMask(localPoint, out var mask);
            _warningReporter.TextureRead(_matParameters.texture, sampleMaskResult);
            if (sampleMaskResult != SampleMaskResult.Success)
            {
                return true;
            }

            if (_invertMask)
            {
                mask = 1 - mask;
            }

            return mask >= _raycastThreshold;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeOnWillRenderCanvases();
            MarkTransformForMaskableSpawn(transform);
            FindGraphic();
            if (IsMaskingEnable)
            {
                UpdateMaskParameters();
            }

            NotifyChildrenThatMaskingMightChanged();
        }

        private void LateUpdate()
        {
            if (IsMaskingEnable)
            {
                if (!_maskingWasEnable)
                {
                    MarkTransformForMaskableSpawn(transform);
                }

                SpawnMaskables();
                var preGraphic = _graphic;
                FindGraphic();
                if (_lastMaskRect != MaskRectTrans.rect || !ReferenceEquals(_graphic, preGraphic))
                {
                    SetDirty();
                }
            }

            _maskingWasEnable = IsMaskingEnable;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetDirty();
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            SetDirty();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            _bindCanvas = null;
            SetDirty();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            _bindCanvas = null;
            SetDirty();
            NotifyChildrenThatMaskingMightChanged();
        }

        private void OnTransformChildrenChanged()
        {
            MarkTransformForMaskableSpawn(transform);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromWillRenderCanvases();
            if (_graphic != null)
            {
                _graphic.UnregisterDirtyVerticesCallback(SetGraphicDirty);
                _graphic.UnregisterDirtyMaterialCallback(SetGraphicDirty);
                _graphic = null;
            }

            NotifyChildrenThatMaskingMightChanged();
            _materialReplacement.DestroyAllAndClear();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _destroyed = true;
            NotifyChildrenThatMaskingMightChanged();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            _spritePixelsPerUnitMultiplier = Mathf.Max(0.01f, _spritePixelsPerUnitMultiplier);
            SetDirty();
            _maskRectTrans = null;
            _graphic = null;
        }
#endif

        private Vector4 LocalMaskRect(Vector4 border)
        {
            return MathOP.ApplyBorder(MathOP.ToVector(MaskRectTrans.rect), border);
        }

        public Material GetReplaceMat(Material originalMat)
        {
            return _materialReplacement.Get(originalMat);
        }

        public void ReleaseReplaceMat(Material replaceMat)
        {
            _materialReplacement.Release(replaceMat);
        }

        private void MatParameterHandler(Material material)
        {
            _matParameters.Apply(material);
        }

        private Canvas[] _allCanvasList;

        private Canvas NearestEnabledCanvas()
        {
            if (_allCanvasList == null)
            {
                _allCanvasList = GetComponentsInParent<Canvas>(false);
            }

            for (int i = 0; i < _allCanvasList.Length; i++)
            {
                var canvas = _allCanvasList[i];
                if (canvas.isActiveAndEnabled)
                {
                    return canvas;
                }
            }

            return null;
        }

        private void SetDirty()
        {
            _dirty = true;
        }

        private void SetGraphicDirty()
        {
            if (!IsBasedOnGraphic)
            {
                return;
            }

            SetDirty();
        }

        private void SubscribeOnWillRenderCanvases()
        {
            // 为了在layout和 graphics 更新完成之后调用，我们应该订阅在 CanvasUpdateRegistry PerformUpdateu函数执行之后，
            // 但是CanvasUpdateRegistry中PerformUpdate函数的订阅是在 CanvasUpdateRegistry 的构造函数中，因此
            // 我们在此处手动调用一下 CanvasUpdateRegistry 的单例，让它构造一下对象 
            EmptyCallHandler(CanvasUpdateRegistry.instance);
            Canvas.willRenderCanvases += OnWillRenderCanvases;
        }

        private void UnsubscribeFromWillRenderCanvases()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
        }

        private T EmptyCallHandler<T>(T instance)
        {
            return instance;
        }

        private void OnWillRenderCanvases()
        {
            SpawnMaskables();
            if (IsMaskingEnable)
            {
                UpdateMaskParameters();
            }
        }

        private void MarkTransformForMaskableSpawn(Transform trans)
        {
            // 我们将SoftMaskable的生成推迟到LateUpdate中。这让我们可以规避 
            // 在创建新对象时，MaskableGraphic 不会考虑在组件堆栈中位于它上面的 IMaterialModifiers。
            // 的问题。这可以解决TextMesh Pro Text 多行文本的问题：TMP 创建 SubMesh 之后在单独的
            // 添加Graphic 组件。将SoftMaskable推迟到LateUpdate中，能够保证SoftMaskable是在Graphic
            // 生成之后再生成的。

            if (!transToSpawnMaskableQueue.Contains(trans))
            {
                transToSpawnMaskableQueue.Enqueue(trans);
            }
        }

        private void FindGraphic()
        {
            if (_graphic == null && IsBasedOnGraphic)
            {
                _graphic = MaskRectTrans.GetComponent<Graphic>();
                if (_graphic != null)
                {
                    _graphic.RegisterDirtyVerticesCallback(SetGraphicDirty);
                    _graphic.RegisterDirtyMaterialCallback(SetGraphicDirty);
                }
            }
        }

        private void UpdateMaskParameters()
        {
            if (_dirty || MaskRectTrans.hasChanged)
            {
                CalculateMaskParameters();
                MaskRectTrans.hasChanged = false;
                _lastMaskRect = MaskRectTrans.rect;
                SetDirty();
            }

            _materialReplacement.ApplyAll();
        }

        private void CalculateMaskParameters()
        {
            var sourceParams = DeduceSourceParameters();
            _warningReporter.ImageUsed(sourceParams.image);
            var spriteErrors = _warningReporter.CheckSprite(sourceParams.sprite);
            _warningReporter.SpriteUsed(sourceParams.sprite, spriteErrors);
            if (sourceParams.sprite)
            {
                if (spriteErrors == ErrorType.NoError)
                {
                    CalculateSpriteBased(sourceParams.sprite, sourceParams.spriteBorderMode,
                        sourceParams.spritePixelsPerUnit);
                }
                else
                {
                    CalculateSolidFill();
                }
            }
            else if (sourceParams.texture)
            {
                CalculateTextureBased(sourceParams.texture, sourceParams.textureUVRect);
            }
            else
            {
                CalculateSolidFill();
            }
        }

        private void CalculateSpriteBased(Sprite sp, BorderMode borderMode, float spritePixelsPerUnit)
        {
            FillCommonParameters();
            var inner = DataUtility.GetInnerUV(sp);
            var outer = DataUtility.GetOuterUV(sp);
            var padding = DataUtility.GetPadding(sp);
            var fullMaskRect = LocalMaskRect(Vector4.zero);
            _matParameters.maskRectUV = outer;

            if (borderMode == BorderMode.Simple)
            {
                if (IsPreserveAspect())
                {
                    fullMaskRect = PreserveSpriteAspectRatio(fullMaskRect, sp.rect.size);
                }

                // 归一化 padding
                var normalizedPadding = MathOP.Div(padding, sp.rect.size);
                _matParameters.maskRect =
                    MathOP.ApplyBorder(fullMaskRect, MathOP.Mul(normalizedPadding, MathOP.Size(fullMaskRect)));
            }
            else
            {
                var spriteToCanvasScale = SpriteToCanvasScale(spritePixelsPerUnit);
                _matParameters.maskRect = MathOP.ApplyBorder(fullMaskRect, padding * spriteToCanvasScale);
                var adjustedBorder = AdjustBorders(sp.border * spriteToCanvasScale, fullMaskRect);
                _matParameters.maskBorder = LocalMaskRect(adjustedBorder);
                _matParameters.maskBorderUV = inner;
            }

            _matParameters.texture = sp.texture;
            _matParameters.borderMode = borderMode;
            if (borderMode == BorderMode.Tiled)
            {
                _matParameters.tileRepeat = MaskRepeat(sp, spritePixelsPerUnit, _matParameters.maskBorder);
            }
        }

        private void CalculateTextureBased(Texture tex, Rect uvRect)
        {
            FillCommonParameters();

            _matParameters.maskRect = LocalMaskRect(Vector4.zero);
            _matParameters.maskRectUV = MathOP.ToVector(uvRect);
            _matParameters.texture = tex;
            _matParameters.borderMode = BorderMode.Simple;
        }

        private readonly Rect defaultUVRect = new Rect(0, 0, 1, 1);

        private void CalculateSolidFill()
        {
            CalculateTextureBased(null, defaultUVRect);
        }

        private Vector4 AdjustBorders(Vector4 border, Vector4 rect)
        {
            var size = MathOP.Size(rect);
            for (int axis = 0; axis <= 1; axis++)
            {
                float combinedBorders = border[axis] + border[axis + 2];
                // 如果矩形小于combinedBorders尺寸，那么就没有足够的空间来显示边框的正常尺寸。
                // 为了避免边框重叠产生的显示瑕疵，我们会缩小边框以使其适应。
                if (size[axis] < combinedBorders && combinedBorders != 0)
                {
                    float borderScaleRatio = size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }

            return border;
        }

        private Vector2 MaskRepeat(Sprite sp, float spritePixelsPerUnit, Vector4 centralPart)
        {
            var textureCenter = MathOP.ApplyBorder(MathOP.ToVector(sp.rect), sp.border);
            return MathOP.Div(MathOP.Size(centralPart),
                MathOP.Size(textureCenter) * SpriteToCanvasScale(spritePixelsPerUnit));
        }

        private void FillCommonParameters()
        {
            _matParameters.worldToMask = WorldToMask();
            _matParameters.maskChannelWeights = _channelWeight;
            _matParameters.invertMask = _invertMask;
            _matParameters.invertOutsides = _invertOutsides;
        }

        private Matrix4x4 WorldToMask()
        {
            return MaskRectTrans.worldToLocalMatrix * BindCanvas.rootCanvas.transform.localToWorldMatrix;
        }

        private bool IsPreserveAspect()
        {
            if (!IsBasedOnGraphic || _graphic is not Image img)
            {
                return false;
            }

            return img.preserveAspect;
        }

        private Vector4 PreserveSpriteAspectRatio(Vector4 rect, Vector2 spriteSize)
        {
            var spriteRatio = spriteSize.x / spriteSize.y;
            var rectWidth = rect.z - rect.x;
            var rectHeight = rect.w - rect.y;
            var rectRatio = rectWidth / rectHeight;
            if (spriteRatio > rectRatio)
            {
                var scale = rectRatio / spriteRatio;
                return new Vector4(rect.x, rect.y * scale, rect.z, rect.w * scale);
            }
            else
            {
                var scale = spriteRatio / rectRatio;
                return new Vector4(rect.x * scale, rect.y, rect.z * scale, rect.w);
            }
        }

        private float SpriteToCanvasScale(float spritePixelsPerUnit)
        {
            var canvasPixelsPerUnit = BindCanvas != null ? BindCanvas.referencePixelsPerUnit : 100;
            return canvasPixelsPerUnit / spritePixelsPerUnit;
        }

        private readonly float defaultPixelPerUnit = 100f;

        private SourceParameters DeduceSourceParameters()
        {
            var result = new SourceParameters();
            if (_source == MaskSource.Graphic)
            {
                if (_graphic is Image image)
                {
                    var sp = image.sprite;
                    result.image = image;
                    result.sprite = sp;
                    result.spriteBorderMode = Util.ImageTypeToBorderMode(image.type);
                    if (sp != null)
                    {
                        result.spritePixelsPerUnit = sp.pixelsPerUnit * image.pixelsPerUnitMultiplier;
                        result.texture = sp.texture;
                    }
                    else
                    {
                        result.spritePixelsPerUnit = defaultPixelPerUnit;
                    }
                }
                else if (_graphic is RawImage rawImage)
                {
                    result.texture = rawImage.texture;
                    result.textureUVRect = rawImage.uvRect;
                }
            }
            else if (_source == MaskSource.Sprite)
            {
                result.sprite = _sprite;
                result.spriteBorderMode = _spriteBorderMode;
                if (_sprite != null)
                {
                    result.spritePixelsPerUnit = _sprite.pixelsPerUnit * _spritePixelsPerUnitMultiplier;
                    result.texture = _sprite.texture;
                }
                else
                {
                    result.spritePixelsPerUnit = defaultPixelPerUnit;
                }
            }
            else if (_source == MaskSource.Texture)
            {
                result.texture = _texture;
                result.textureUVRect = _textureUVRect;
            }
            else
            {
                Debug.LogError($"未知的 MaskSource类型 {_source}");
            }

            return result;
        }

        private readonly List<SoftMaskable> _softMaskableTmpList = new List<SoftMaskable>();

        private void NotifyChildrenThatMaskingMightChanged()
        {
            _softMaskableTmpList.Clear();
            transform.GetComponentsInChildren(true, _softMaskableTmpList);
            for (int i = 0; i < _softMaskableTmpList.Count; i++)
            {
                var maskable = _softMaskableTmpList[i];
                if (maskable != null && maskable.gameObject != gameObject)
                {
                    maskable.MaskingMightChanged();
                }
            }
        }

        private void SpawnMaskables()
        {
            while (transToSpawnMaskableQueue.Count > 0)
            {
                var transForSpawn = transToSpawnMaskableQueue.Dequeue();
                if (transForSpawn != null)
                {
                    SpawnMaskablesInChildren(transform);
                }
            }
        }

        private void SpawnMaskablesInChildren(Transform root)
        {
            _softMaskableTmpList.Clear();
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                child.GetComponents(_softMaskableTmpList);
                var hasMaskable = false;
                for (int j = 0; j < _softMaskableTmpList.Count; j++)
                {
                    var maskable = _softMaskableTmpList[j];
                    if (maskable == null || maskable.IsDestroyed())
                    {
                        continue;
                    }

                    hasMaskable = true;
                    break;
                }

                if (!hasMaskable)
                {
                    child.gameObject.AddComponent<SoftMaskable>();
                }
            }
        }
    }
}
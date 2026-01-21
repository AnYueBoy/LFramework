using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

        private RectTransform _maskRectTrans;
        private Graphic _graphic;
        private Canvas _bindCanvas;

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

        public bool IsUsingRaycastFiltering => _raycastThreshold > 0f;

        public bool IsAlive { get; }
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
            // TODO:
        }

        private void LateUpdate()
        {
            // TODO: 
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            _dirty = true;
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            _dirty = true;
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            _bindCanvas = null;
            _dirty = true;
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            _bindCanvas = null;
            _dirty = true;
            
            // TODO:
        }

        private void OnTransformChildrenChanged()
        {
           // TODO: 
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // TODO:
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _destroyed = true;
            //TODO:
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            //TODO:
        }
#endif

        private Vector4 LocalMaskRect(Vector4 border)
        {
            return MathOP.ApplyBorder(MathOP.ToVector(MaskRectTrans.rect), border);
        }

        public Material GetReplaceMat(Material originalMat)
        {
            throw new System.NotImplementedException();
        }

        public void ReleaseReplaceMat(Material replaceMat)
        {
            throw new System.NotImplementedException();
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
    }
}
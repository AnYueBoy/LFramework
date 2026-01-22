using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LFramework.SoftMask
{
    [ExecuteInEditMode]
    public class SoftMaskable : UIBehaviour, IMaterialModifier
    {
        private ISoftMask _bindMask;
        private Graphic _graphic;
        private Material _replaceMat;
        private bool _affectedByMask;

        public bool Destroyed { get; private set; }
        public bool ShaderIsNotSupported { get; private set; }

        public bool IsMaskingEnable =>
            _bindMask != null
            && _bindMask.IsAlive
            && _bindMask.IsMaskingEnable
            && _affectedByMask
            && IsGraphicMaskable;

        public ISoftMask BindMask
        {
            get => _bindMask;
            set
            {
                if (_bindMask == value)
                {
                    return;
                }

                if (_bindMask != null)
                {
                    ReplaceMat = null;
                }

                if (value != null && value.IsAlive)
                {
                    _bindMask = value;
                }
                else
                {
                    _bindMask = null;
                }

                Invalidate();
            }
        }

        private Graphic Graphic
        {
            get
            {
                if (_graphic == null)
                {
                    _graphic = GetComponent<Graphic>();
                }

                return _graphic;
            }
        }

        private bool IsGraphicMaskable
        {
            get
            {
                if (_graphic == null)
                {
                    return false;
                }

                var maskableGraphics = _graphic as MaskableGraphic;
                if (!maskableGraphics)
                {
                    return true;
                }

                return maskableGraphics.maskable;
            }
        }

        private Material ReplaceMat
        {
            get => _replaceMat;
            set
            {
                if (_replaceMat == value)
                {
                    return;
                }

                if (_replaceMat != null && BindMask != null)
                {
                    // 释放旧的替换材质
                    BindMask.ReleaseReplaceMat(_replaceMat);
                }

                _replaceMat = value;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            hideFlags = HideFlags.HideInHierarchy;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (FindMaskOrDie())
            {
                RequestChildTransformUpdate();
            }
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            FindMaskOrDie();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            FindMaskOrDie();
        }

        private void OnTransformChildrenChanged()
        {
            RequestChildTransformUpdate();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            BindMask = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroyed = true;
        }

        private void Invalidate()
        {
            if (Graphic != null)
            {
                Graphic.SetMaterialDirty();
            }
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!IsMaskingEnable)
            {
                ShaderIsNotSupported = false;
                ReplaceMat = null;
                return baseMaterial;
            }

            // 根据绑定的Mask交换出该使用的材质
            ReplaceMat = BindMask.GetReplaceMat(baseMaterial);
            if (ReplaceMat != null)
            {
                ShaderIsNotSupported = false;
                return ReplaceMat;
            }

            if (!baseMaterial.HasDefaultUIShader())
            {
                ShaderIsNotSupported = true;
                Debug.LogWarning($"{Graphic} 不支持自定义的材质");
            }

            return baseMaterial;
        }

        public void MaskingMightChanged()
        {
            if (FindMaskOrDie())
            {
                Invalidate();
            }
        }

        private bool FindMaskOrDie()
        {
            if (Destroyed)
            {
                return false;
            }

            BindMask = NearestMask(transform) ?? NearestMask(transform, false);
            if (BindMask == null)
            {
                Destroyed = true;
                DestroySelf();
                return false;
            }

            return true;
        }

        private ISoftMask NearestMask(Transform transform, bool enableOnly = true)
        {
            _affectedByMask = true;
            var current = transform;
            while (true)
            {
                if (current == null)
                {
                    return null;
                }

                if (current != transform)
                {
                    var mask = GetISoftMask(current, enableOnly);
                    if (mask != null)
                    {
                        return mask;
                    }
                }

                if (IsOverridingSortingCanvas(current))
                {
                    _affectedByMask = false;
                }

                current = current.parent;
            }
        }

        private static readonly List<ISoftMask> _softMaskTempList = new List<ISoftMask>();

        private ISoftMask GetISoftMask(Transform current, bool shouldEnable = true)
        {
            _softMaskTempList.Clear();
            current.GetComponents(_softMaskTempList);
            var mask = _softMaskTempList.Count > 0 ? _softMaskTempList[0] : null;

            if (mask != null && mask.IsAlive && (!shouldEnable || mask.IsMaskingEnable))
            {
                return mask;
            }

            return null;
        }

        private static readonly List<Canvas> _canvasTempList = new List<Canvas>();

        private bool IsOverridingSortingCanvas(Transform transform)
        {
            _canvasTempList.Clear();
            transform.GetComponents(_canvasTempList);
            var canvas = _canvasTempList.Count > 0 ? _canvasTempList[0] : null;
            if (canvas != null && canvas.overrideSorting)
            {
                return true;
            }

            return false;
        }

        private void RequestChildTransformUpdate()
        {
            BindMask?.UpdateTransformChildren(transform);
        }

        private void DestroySelf()
        {
            if (Application.isPlaying)
            {
                Destroy(this);
            }
            else
            {
                DestroyImmediate(this);
            }
        }
    }
}
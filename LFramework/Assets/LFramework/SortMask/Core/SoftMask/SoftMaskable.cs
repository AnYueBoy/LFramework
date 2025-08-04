using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LFramework.SortMask
{
    [ExecuteInEditMode]
    public class SoftMaskable : UIBehaviour, IMaterialModifier
    {
        private ISoftMask mask;
        private Graphic graphic;
        private Material replacement;
        private bool affectedByMask;
        private bool destroyed;

        private Graphic Graphic => graphic ? graphic : graphic = GetComponent<Graphic>();

        private bool IsGraphicMaskable
        {
            get
            {
                if (!Graphic)
                {
                    return false;
                }

                var maskableGraphic = Graphic as MaskableGraphic;
                if (maskableGraphic == null)
                {
                    return true;
                }

                return maskableGraphic.maskable;
            }
        }

        private Material Replacement
        {
            get => replacement;
            set
            {
                if (replacement == value)
                {
                    return;
                }

                if (replacement != null && Mask != null)
                {
                    Mask.ReleaseReplacement(replacement);
                }

                replacement = value;
            }
        }

        public bool ShaderIsNotSupported { get; private set; }

        public bool IsMaskingEnabled =>
            Mask != null &&
            Mask.IsAlive &&
            Mask.IsMaskingEnable &&
            affectedByMask &&
            IsGraphicMaskable;

        public ISoftMask Mask
        {
            get => mask;
            private set
            {
                if (mask == value)
                {
                    return;
                }

                if (mask != null)
                {
                    Replacement = null;
                }

                if (value != null && value.IsAlive)
                {
                    mask = value;
                }
                else
                {
                    mask = null;
                }

                Invalidate();
            }
        }

        public bool IsDestroyed => destroyed;

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!IsMaskingEnabled)
            {
                ShaderIsNotSupported = false;
                Replacement = null;
                return baseMaterial;
            }

            var newMat = Mask.GetReplacement(baseMaterial);
            Replacement = newMat;
            if (Replacement != null)
            {
                ShaderIsNotSupported = false;
                return Replacement;
            }

            // 不是UI默认材质时警告
            if (!baseMaterial.HasDefaultUIShader() && !ShaderIsNotSupported)
            {
                Debug.LogWarningFormat(
                    gameObject,
                    "SoftMask 在 {0}失效，材质{1}不支持遮罩 " +
                    "给材质添加遮罩支持或者将Graphic的材质设置为空",
                    Graphic,
                    baseMaterial);
                ShaderIsNotSupported = true;
            }

            return baseMaterial;
        }

        public void Invalidate()
        {
            if (Graphic == null)
            {
                return;
            }

            Graphic.SetMaterialDirty();
        }

        public void MaskMightChanged()
        {
            if (FindMaskOrDie())
            {
                Invalidate();
            }
        }

        private bool FindMaskOrDie()
        {
            if (destroyed)
            {
                return false;
            }

            Mask = NearestMask(transform, out affectedByMask) ?? NearestMask(transform, out affectedByMask, false);
            if (Mask == null)
            {
                destroyed = true;
                DestroySelf();
                return false;
            }

            return true;
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


        #region UIMonobehavior LifeCycle

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

        protected override void OnDisable()
        {
            base.OnDisable();
            Mask = null;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            destroyed = true;
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

        private void RequestChildTransformUpdate()
        {
            Mask?.UpdateTransformChildren(transform);
        }

        #endregion

        #region Static

        private static List<ISoftMask> softMaskList = new List<ISoftMask>();
        private static List<Canvas> canvasList = new List<Canvas>();

        private static ISoftMask GetISoftMask(Transform current, bool shouldBeEnabled = true)
        {
            ISoftMask targetMask = null;
            current.GetComponents(softMaskList);
            if (softMaskList.Count > 0)
            {
                targetMask = softMaskList[0];
            }

            if (targetMask != null && targetMask.IsAlive && (!shouldBeEnabled || targetMask.IsMaskingEnable))
            {
                return targetMask;
            }

            return null;
        }

        private static bool IsOverridingSortingCanvas(Transform transform)
        {
            Canvas targetCanvas = null;
            transform.GetComponents(canvasList);
            if (canvasList.Count > 0)
            {
                targetCanvas = canvasList[0];
            }

            if (targetCanvas != null && targetCanvas.overrideSorting)
            {
                return true;
            }

            return false;
        }

        private static ISoftMask NearestMask(Transform transform, out bool processedByThisMask, bool enableOnly = true)
        {
            processedByThisMask = true;
            var current = transform;
            while (true)
            {
                if (current == null)
                {
                    return null;
                }

                if (current != transform)
                {
                    // 遮罩无法遮罩自身
                    var mask = GetISoftMask(current, enableOnly);
                    if (mask != null)
                    {
                        return mask;
                    }
                }

                if (IsOverridingSortingCanvas(current))
                {
                    processedByThisMask = false;
                }

                current = current.parent;
            }
        }

        #endregion
    }
}
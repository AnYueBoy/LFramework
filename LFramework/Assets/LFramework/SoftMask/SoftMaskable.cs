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
            // TODO:
        }
    }
}
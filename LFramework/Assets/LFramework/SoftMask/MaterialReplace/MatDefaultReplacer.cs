using JetBrains.Annotations;
using UnityEngine;

namespace LFramework.SoftMask
{
    public class MatDefaultReplacer : IMaterialReplacer
    {
        public Material Replace(Material originalMat)
        {
            if (originalMat == null || originalMat.HasDefaultUIShader())
            {
                return Replace(originalMat, DefaultUIShader);
            }

            if (originalMat.HasDefaultETC1UIShader())
            {
                return Replace(originalMat, DefaultUIETC1Shader);
            }

            if (originalMat.SupportSoftMask())
            {
                return new Material(originalMat);
            }

            return null;
        }

        private Material Replace(Material originalMat, Shader defaultReplaceShader)
        {
            if (defaultReplaceShader == null)
            {
                return null;
            }

            var replacement = new Material(defaultReplaceShader);
            if (replacement != null && originalMat != null)
            {
                replacement.CopyPropertiesFromMaterial(originalMat);
            }

            return replacement;
        }

        private Shader _defaultUIShader;
        private Shader _defaultUIETC1Shader;

        private Shader DefaultUIShader
        {
            get
            {
                if (_defaultUIShader == null)
                {
                    _defaultUIShader = Resources.Load<Shader>(DefaultUIShaderName);
                }

                return _defaultUIShader;
            }
        }

        private Shader DefaultUIETC1Shader
        {
            get
            {
                if (_defaultUIETC1Shader == null)
                {
                    _defaultUIETC1Shader = Resources.Load<Shader>(DefaultUIETC1ShaderName);
                }

                return _defaultUIETC1Shader;
            }
        }

        private string DefaultUIShaderName
        {
            get
            {
#if UNITY_2020_1_OR_NEWER
                return "SoftMaskPremultipliedAlpha";
#endif
                return "SoftMask";
            }
        }

        private string DefaultUIETC1ShaderName
        {
            get
            {
#if UNITY_2020_1_OR_NEWER
                return "SoftMaskETC1PremultipliedAlpha";
#endif
                return "SoftMaskETC1";
            }
        }
    }
}
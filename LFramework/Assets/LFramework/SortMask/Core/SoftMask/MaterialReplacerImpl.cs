using UnityEngine;

namespace LFramework.SortMask
{
    public class MaterialReplacerImpl : IMaterialReplacer
    {
        public int Order => 0;

        public Material Replace(Material original)
        {
            if (original == null || original.HasDefaultUIShader())
            {
                return ReplaceInner(original, Resources.Load<Shader>("SoftMaskPremultipliedAlpha"));
            }

            if (original.HasDefaultETC1UIShader())
            {
                return ReplaceInner(original, Resources.Load<Shader>("SoftMaskETC1PremultipliedAlpha"));
            }

            if (original.SupportSoftMask())
            {
                return new Material(original);
            }

            return null;
        }

        private Material ReplaceInner(Material original, Shader defaultReplacementShader)
        {
            var replacement = defaultReplacementShader ? new Material(defaultReplacementShader) : null;

            if (replacement != null && original != null)
            {
                replacement.CopyPropertiesFromMaterial(original);
            }

            return replacement;
        }
    }
}
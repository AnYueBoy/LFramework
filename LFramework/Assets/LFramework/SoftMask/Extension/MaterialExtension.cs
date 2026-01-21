using UnityEngine;

namespace LFramework.SoftMask
{
    public static class MaterialExtension
    {
        public static bool SupportSoftMask(this Material material)
        {
            return material.HasProperty("_SoftMask");
        }

        public static bool HasDefaultUIShader(this Material material)
        {
            return material.shader == Canvas.GetDefaultCanvasMaterial().shader;
        }

        public static bool HasDefaultETC1UIShader(this Material material)
        {
            return material.shader == Canvas.GetETC1SupportedCanvasMaterial().shader;
        }

        public static void EnableKeyword(this Material material, string keyword, bool enable)
        {
            if (enable)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }
    }
}
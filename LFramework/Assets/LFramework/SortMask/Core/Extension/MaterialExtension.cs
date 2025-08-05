using UnityEngine;

namespace LFramework.SortMask
{
    public static class MaterialExtension
    {
        public static bool HasDefaultUIShader(this Material mat)
        {
            return mat.shader == Canvas.GetDefaultCanvasMaterial().shader;
        }

        public static void EnableKeyword(this Material mat, string keyword, bool enable)
        {
            if (enable)
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }
        }
    }
}
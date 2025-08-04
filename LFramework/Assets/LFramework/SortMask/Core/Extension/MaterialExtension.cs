using UnityEngine;

namespace LFramework.SortMask
{
    public static class MaterialExtension
    {
        public static bool HasDefaultUIShader(this Material mat)
        {
            return mat.shader == Canvas.GetDefaultCanvasMaterial().shader;
        }
    }
}
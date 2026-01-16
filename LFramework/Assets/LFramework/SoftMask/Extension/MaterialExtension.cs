using UnityEngine;

namespace LFramework.SoftMask
{
    public static class MaterialExtension
    {
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
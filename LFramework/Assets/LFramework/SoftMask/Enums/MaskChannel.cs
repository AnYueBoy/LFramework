using UnityEngine;

namespace LFramework.SoftMask
{
    public static class MaskChannel
    {
        public static Color Alpha = new Color(0, 0, 0, 1f);
        public static Color Red = new Color(1, 0, 0, 0f);
        public static Color Green = new Color(0, 1, 0, 0f);
        public static Color Blue = new Color(0, 0, 1, 0f);
        public static Color Gray = new Color(1 / 3f, 1 / 3f, 1 / 3f, 0f);
    }
}
using UnityEngine;

namespace LFramework.SortMask
{
    public static class MathLib
    {
        private static Vector2 Min(Vector4 r) => new Vector2(r.x, r.y);
        private static Vector2 Max(Vector4 r) => new Vector2(r.z, r.w);

        public static Vector4 Div(Vector4 v, Vector4 s) => new Vector4(v.x / s.x, v.y / s.y, v.z / s.z, v.w / s.w);
        public static Vector2 Div(Vector2 v, Vector2 s) => new Vector2(v.x / s.x, v.y / s.y);

        /// <summary>
        /// 二维UV重映射
        /// </summary>
        public static Vector2 Remap(Vector2 c, Vector4 from, Vector4 to)
        {
            var fromSize = Max(from) - Min(from);
            var toSize = Max(to) - Min(to);
            // 将在原Rect下的信息重映射为另一个Rect下
            return Vector2.Scale(
                Div(c - Min(from), fromSize), toSize
            ) + Min(to);
        }

        public static float Remap(float v, float x1, float x2, float u1, float u2, float repeat = 1)
        {
            var w = x2 - x1;
            var lerp = 0f;
            if (w != 0)
            {
                lerp = Frac((v - x1) / w * repeat);
            }

            return Mathf.Lerp(u1, u2, lerp);
        }

        public static float Frac(float v)
        {
            return v - Mathf.Floor(v);
        }
    }
}
using UnityEngine;

namespace LFramework.SoftMask
{
    public static class MathOP
    {
        public static Vector4 ToVector(Rect r)
        {
            return new Vector4(r.xMin, r.yMin, r.xMax, r.yMax);
        }

        public static Vector2 Div(Vector2 v, Vector2 s)
        {
            return new Vector2(v.x / s.x, v.y / s.y);
        }

        private static Vector2 Min(Vector4 r)
        {
            return new Vector2(r.x, r.y);
        }

        private static Vector2 Max(Vector4 r)
        {
            return new Vector2(r.z, r.w);
        }

        public static Vector2 Remap(Vector2 c, Vector4 from, Vector4 to)
        {
            var fromSize = Max(from) - Min(from);
            var toSize = Max(to) - Min(to);
            return Vector2.Scale(
                       Div(c - Min(from), fromSize),
                       toSize)
                   + Min(to);
        }

        public static bool Inside(Vector2 v, Vector4 r)
        {
            if (v.x >= r.x && v.x <= r.z && v.y >= r.y && v.y <= r.w)
            {
                return true;
            }

            return false;
        }

        public static Vector4 ApplyBorder(Vector4 v, Vector4 b)
        {
            return new Vector4(v.x + b.x, v.y + b.y, v.z - b.z, v.w - b.w);
        }
    }
}
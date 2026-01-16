using UnityEngine;

namespace LFramework.SoftMask
{
    public static class MathOP
    {
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
    }
}
using UnityEngine;
using UnityEngine.UI;

namespace LFramework.SoftMask
{
    public static class Util
    {
        public static BorderMode ImageTypeToBorderMode(Image.Type type)
        {
            switch (type)
            {
                case Image.Type.Simple:
                    return BorderMode.Simple;
                case Image.Type.Sliced:
                    return BorderMode.Sliced;
                case Image.Type.Tiled:
                    return BorderMode.Tiled;
                default:
                    return BorderMode.Simple;
            }
        }
    }
}
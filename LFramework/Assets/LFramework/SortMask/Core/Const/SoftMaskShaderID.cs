using UnityEngine;

namespace LFramework.SortMask
{
    public static class SoftMaskShaderID
    {
        public static readonly int SoftMask = Shader.PropertyToID("_SoftMask");
        public static readonly int SoftMaskRect = Shader.PropertyToID("_SoftMaskRect");
        public static readonly int SoftMaskUVRect = Shader.PropertyToID("_SoftMaskUVRect");
        public static readonly int SoftMaskChannelWeights = Shader.PropertyToID("_SoftMaskChannelWeights");
        public static readonly int SoftMaskWorldToMask = Shader.PropertyToID("_SoftMaskWorldToMask");
        public static readonly int SoftMaskBorderRect = Shader.PropertyToID("_SoftMaskBorderRect");
        public static readonly int SoftMaskUVBorderRect = Shader.PropertyToID("_SoftMaskUVBorderRect");
        public static readonly int SoftMaskTileRepeat = Shader.PropertyToID("_SoftMaskTileRepeat");
        public static readonly int SoftMaskInvertMask = Shader.PropertyToID("_SoftMaskInvertMask");
        public static readonly int SoftMaskInvertOutsides = Shader.PropertyToID("_SoftMaskInvertOutsides");
    }
}
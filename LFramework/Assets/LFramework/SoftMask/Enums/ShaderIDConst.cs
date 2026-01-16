using UnityEngine;

namespace LFramework.SoftMask
{
    public static class ShaderIDConst
    {
        public static readonly int SoftMask = Shader.PropertyToID("_SoftMask");
        public static readonly int SoftMask_Rect = Shader.PropertyToID("_SoftMask_Rect");
        public static readonly int SoftMask_UVRect = Shader.PropertyToID("_SoftMask_UVRect");
        public static readonly int SoftMask_ChannelWeights = Shader.PropertyToID("_SoftMask_ChannelWeights");
        public static readonly int SoftMask_WorldToMask = Shader.PropertyToID("_SoftMask_WorldToMask");
        public static readonly int SoftMask_BorderRect = Shader.PropertyToID("_SoftMask_BorderRect");
        public static readonly int SoftMask_UVBorderRect = Shader.PropertyToID("_SoftMask_UVBorderRect");
        public static readonly int SoftMask_TileRepeat = Shader.PropertyToID("_SoftMask_TileRepeat");
        public static readonly int SoftMask_InvertMask = Shader.PropertyToID("_SoftMask_InvertMask");
        public static readonly int SoftMask_InvertOutsides = Shader.PropertyToID("_SoftMask_InvertOutsides");
    }
}
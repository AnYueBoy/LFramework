using System;

namespace LFramework.SoftMask
{
    [Flags]
    [Serializable]
    public enum ErrorType
    {
        NoError = 0,
        UnsupportedShaders = 1 << 0,
        NestedMasks = 1 << 1,
        TightPackedSprite = 1 << 2,
        AlphaSplitSprite = 1 << 3,
        UnsupportedImageType = 1 << 4,
        UnreadableTexture = 1 << 5,
        UnreadableRenderTexture = 1 << 6,
    }
}
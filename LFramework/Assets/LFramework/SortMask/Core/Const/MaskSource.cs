using System;

namespace LFramework.SortMask
{
    [Serializable]
    public enum MaskSource
    {
        /// <summary>
        /// Mask Image 从包含了GameObject的Graphic组件获取。
        /// 只有Image和RawImage组件支持。
        /// 如果GameObject上没有合适的Graphic,将会使用RectTransform 固定矩形的尺寸。
        /// </summary>
        Graphic,
        /// <summary>
        /// Mask Image 应取自于明确指定的Sprite。
        /// 使用此模式时，还可以设置 spriteBorderMode 来决定如何处理Sprite 的边界。
        /// 如果Sprite没有设置，将使用RectTransform尺寸的固定矩形。
        /// 这种模式类似于使用根据Sprite和类型设置的图像。
        /// </summary>
        Sprite,
        /// <summary>
        ///  Mask Image 应取自于明确指定的Texture2D 或者 RenderTexture。
        /// 使用此模式时，可以设置textureUVRect来决定texture的那个部分应该被使用。
        /// 如果texture没有设置，将使用RectTransform尺寸的固定矩形。
        /// 该模式类似于使用根据纹理和 uvRect 设置的 RawImage。
        /// </summary>
        Texture,
    }
}
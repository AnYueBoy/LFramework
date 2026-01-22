using UnityEngine;
using UnityEngine.UI;

namespace LFramework.SoftMask
{
    public class WarningReporter
    {
        private readonly Object _owner;
        private Texture _lastReadTexture;
        private Sprite _lastUsedSprite;
        private Sprite _lastUsedImageSprite;
        private Image.Type _lastUsedImageType;

        public WarningReporter(Object owner)
        {
            _owner = owner;
            _lastReadTexture = null;
            _lastUsedSprite = null;
            _lastUsedImageSprite = null;
            _lastUsedImageType = Image.Type.Simple;
        }

        public void TextureRead(Texture texture, SampleMaskResult sampleMaskResult)
        {
            if (_lastReadTexture == texture)
            {
                return;
            }

            _lastReadTexture = texture;

            if (sampleMaskResult == SampleMaskResult.NonReadable)
            {
                Debug.LogWarning($"{_owner} 射线检测时的阈值大于0，且{texture.name} 是非可读写的");
                return;
            }

            if (sampleMaskResult == SampleMaskResult.NonTexture2D)
            {
                Debug.LogWarning($"{_owner} 射线检测时的阈值大于0，且{texture.name} 不是一个Texture2D类型");
            }
        }

        public void ImageUsed(Image image)
        {
            if (image == null)
            {
                _lastUsedImageSprite = null;
                _lastUsedImageType = Image.Type.Simple;
                return;
            }

            if (_lastUsedImageSprite == image.sprite && _lastUsedImageType == image.type)
            {
                return;
            }

            _lastUsedImageSprite = image.sprite;
            _lastUsedImageType = image.type;
            if (IsImageTypeSupported(image.type))
            {
                return;
            }

            Debug.LogWarning($"{_owner}不支持{image.type} 类型，使用Simple类型");
        }

        public ErrorType CheckSprite(Sprite sprite)
        {
            var result = ErrorType.NoError;
            if (sprite == null)
            {
                return result;
            }

            if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
            {
                result |= ErrorType.TightPackedSprite;
            }

            if (sprite.associatedAlphaSplitTexture != null)
            {
                result |= ErrorType.AlphaSplitSprite;
            }

            return result;
        }

        public void SpriteUsed(Sprite sprite, ErrorType error)
        {
            if (_lastUsedSprite == sprite)
            {
                return;
            }

            _lastUsedSprite = sprite;
            if ((error & ErrorType.TightPackedSprite) != 0)
            {
                Debug.LogError($"SoftMask 不支持 tight packed 的sprite");
            }

            if ((error & ErrorType.AlphaSplitSprite) != 0)
            {
                Debug.LogError($"SoftMask 不支持透明通道分离的sprite");
            }
        }

        private bool IsImageTypeSupported(Image.Type type)
        {
            return type == Image.Type.Simple
                   || type == Image.Type.Sliced
                   || type == Image.Type.Tiled;
        }
    }
}
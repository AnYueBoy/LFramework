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

        // public void SpriteUsed(Sprite sprite,ErrorType error)
        // {
        //     if (_lastUsedSprite == sprite)
        //     {
        //         return;
        //     }
        //
        //     _lastUsedSprite = sprite;
        //     if((error& ))
        // }
    }
}
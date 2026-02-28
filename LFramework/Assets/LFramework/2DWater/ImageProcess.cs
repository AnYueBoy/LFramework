using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ImageProcess : MonoBehaviour
{
    [SerializeField] private Sprite spriteAsset;
    [SerializeField] private int examinePixelCount = 2;
    [SerializeField] private int alphaThreshold = 200;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    private int width, height;

    private Color32[] pixelArray;

    [SerializeField] private Material _material;

    [Button("修改图片")]
    private void ApplyTexture()
    {
        pixelArray = spriteAsset.texture.GetPixels32();
        width = spriteAsset.texture.width;
        height = spriteAsset.texture.height;
        CompatiblePixel();

        var newTexture = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point
        };
        newTexture.SetPixels32(pixelArray);
        newTexture.Apply();
        _spriteRenderer.sprite = Sprite.Create(newTexture, new Rect(0f, 0f, width, height), Vector2.one * 0.5f);
        _material.mainTexture = newTexture;
    }

    private void CompatiblePixel()
    {
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                var index = i * width + j;
                int emptyPixelCount = 0;
                int nonEmptyPixelCount = 0;
                int leftIndex = index - examinePixelCount;
                if (leftIndex <= i * width)
                {
                    leftIndex = i * width;
                }

                int rightIndex = index + examinePixelCount;
                if (rightIndex >= (i + 1) * width - 1)
                {
                    rightIndex = (i + 1) * width - 1;
                }

                for (int k = leftIndex; k <= rightIndex; k++)
                {
                    var examinePixel = pixelArray[k];
                    if (examinePixel.a <= alphaThreshold)
                    {
                        emptyPixelCount++;
                    }
                    else
                    {
                        nonEmptyPixelCount++;
                    }
                }

                if (emptyPixelCount > nonEmptyPixelCount)
                {
                    pixelArray[index].a = 0;
                }
                else
                {
                    pixelArray[index].a = 255;
                }
            }
        }
    }
}
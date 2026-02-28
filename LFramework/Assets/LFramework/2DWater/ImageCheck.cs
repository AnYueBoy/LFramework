using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class ImageCheck : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _spComp;

    private Color32[] originColorArray;
    private Color32[] executeColorArray;
    [SerializeField] private Material spriteMat;
    private Texture2D _texture2D;
    [ShowInInspector] [ReadOnly] private int width, height;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        var sprite = _spComp.sprite;
        width = sprite.texture.width;
        height = sprite.texture.height;
        executeColorArray = new Color32[width * height];
        originColorArray = sprite.texture.GetPixels32();
    }

    [SerializeField] private int checkWidth;
    [SerializeField] private int checkHeight;
    [SerializeField] private bool fullWidth;
    private GameObject pointNode;

    [Button("处理")]
    private void Test()
    {
        for (int i = 0; i < originColorArray.Length; i++)
        {
            var originColor = originColorArray[i];
            executeColorArray[i] = originColor;
        }

        if (fullWidth)
        {
            for (int i = 0; i < width; i++)
            {
                var index = checkHeight * width + i;
                var originPixel = originColorArray[index];
                executeColorArray[index] = new Color32(0, 0, 0, originPixel.a);
            }
        }
        else
        {
            var index = checkHeight * width + checkWidth;
            executeColorArray[index] = new Color32(0, 0, 0, 255);
        }

        _texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false)
        {
            filterMode = FilterMode.Point
        };
        _texture2D.SetPixels32(executeColorArray);
        _texture2D.Apply();
        spriteMat.mainTexture = _texture2D;
        _spComp.sprite = Sprite.Create(_texture2D, new Rect(0f, 0f, width, height), Vector2.one * 0.5f);

        if (pointNode == null)
        {
            pointNode = new GameObject("Point");
            pointNode.transform.SetParent(transform);
            pointNode.transform.localEulerAngles = Vector3.zero;
            pointNode.transform.localScale = Vector3.one;
        }

        var x = checkWidth - width / 2.0f;
        var y = checkHeight - height / 2.0f;

        pointNode.transform.localPosition = new Vector3(x / 100f, y / 100f, 0);
    }
}
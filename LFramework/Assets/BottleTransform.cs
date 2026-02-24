using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteInEditMode]
public class BottleTransform : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _srComp;
    [SerializeField] private Material bottleMat;

    [SerializeField] private float fillAmount = 0.5f;
    private int width, height;
    private PixelData[] _pixelDataArray;

    private int effectVolume;

    private Vector3 lbLocalPoint, rbLocalPoint, ltLocalPoint, rtLocalPoint;
    private Vector3 lbWorldPoint, rbWorldPoint, ltWorldPoint, rtWorldPoint;

    private float minY, maxY;

    private bool initialized;

    private void Awake()
    {
        initialized = false;
        Initialize();
        initialized = true;
    }

    [Button("初始化")]
    public void Initialize()
    {
        var spriteAsset = _srComp.sprite;
        width = (int)spriteAsset.rect.width;
        height = (int)spriteAsset.rect.height;
        _pixelDataArray = new PixelData[width * height];

        InitializePixelData(spriteAsset);
    }

    private void InitializePixelData(Sprite spriteAsset)
    {
        var pixelArray = spriteAsset.texture.GetPixels32();
        var spriteSize = spriteAsset.bounds.size;

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                var index = i * width + j;
                var pixel = pixelArray[index];
                float localX = (j / (float)width - 0.5f) * spriteSize.x;
                float localY = (i / (float)height - 0.5f) * spriteSize.y;
                var localPos = new Vector3(localX, localY, 0f);
                var worldPos = transform.TransformPoint(localPos);
                _pixelDataArray[index] = new PixelData(pixel, localPos, worldPos);

                if (pixel.a > 144)
                {
                    effectVolume++;
                }
            }
        }

        // 初始化边界点信息
        lbLocalPoint = new Vector3(-spriteSize.x / 2f, -spriteSize.y / 2f, 0f);
        rbLocalPoint = new Vector3(spriteSize.x / 2f, -spriteSize.y / 2f, 0f);
        ltLocalPoint = new Vector3(-spriteSize.x / 2f, spriteSize.y / 2f, 0f);
        rtLocalPoint = new Vector3(spriteSize.x / 2f, spriteSize.y / 2f, 0f);

        UpdateWorldBoundPos();
    }

    private float preAngle = -1;

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        if (!Mathf.Approximately(preAngle, transform.eulerAngles.z))
        {
            preAngle = transform.eulerAngles.z;
            UpdateWorldBoundPos();
        }
    }

    private void InitializeBoundPoint()
    {
    }

    private void CalculateFillAmount()
    {
        var fillVolume = effectVolume * fillAmount;
    }

    public void UpdatePixelPos()
    {
        for (int i = 0; i < _pixelDataArray.Length; i++)
        {
            var pixelData = _pixelDataArray[i];
            pixelData.worldPos = transform.TransformPoint(pixelData.localPos);
        }
    }

    [Button("角度更新")]
    public void UpdateWorldBoundPos()
    {
        lbWorldPoint = transform.TransformPoint(lbLocalPoint);
        rbWorldPoint = transform.TransformPoint(rbLocalPoint);
        ltWorldPoint = transform.TransformPoint(ltLocalPoint);
        rtWorldPoint = transform.TransformPoint(rtLocalPoint);

        minY = Mathf.Min(lbWorldPoint.y, rbWorldPoint.y, ltWorldPoint.y, rtWorldPoint.y);
        maxY = Mathf.Max(lbWorldPoint.y, rbWorldPoint.y, ltWorldPoint.y, rtWorldPoint.y);

        var worldY = transform.position.y;
        var bottomIntersectPoint = CalculateIntersectPoint(lbWorldPoint, rbWorldPoint, worldY);
        var leftIntersectPoint = CalculateIntersectPoint(lbWorldPoint, ltWorldPoint, worldY);
        var rightIntersectPoint = CalculateIntersectPoint(rbWorldPoint, rtWorldPoint, worldY);
        var topIntersectPoint = CalculateIntersectPoint(ltWorldPoint, rtWorldPoint, worldY);

        List<Vector3> nonNullPointList = new List<Vector3>();
        if (bottomIntersectPoint != null)
        {
            nonNullPointList.Add(bottomIntersectPoint.Value);
        }

        if (leftIntersectPoint != null)
        {
            nonNullPointList.Add(leftIntersectPoint.Value);
        }

        if (rightIntersectPoint != null)
        {
            nonNullPointList.Add(rightIntersectPoint.Value);
        }

        if (topIntersectPoint != null)
        {
            nonNullPointList.Add(topIntersectPoint.Value);
        }

        var uv1 = ConvertToUV(nonNullPointList[0]);
        var uv2 = ConvertToUV(nonNullPointList[1]);

        float k, b;
        int t;
        if (Mathf.Approximately(uv2.x, uv1.x))
        {
            t = -1;
            k = uv2.x;
            b = -1;
        }
        else if (Mathf.Approximately(uv2.y, uv1.y))
        {
            t = 1;
            k = 0;
            b = uv2.y;
        }
        else
        {
            t = 0;
            k = (uv2.y - uv1.y) / (uv2.x - uv1.x);
            b = uv2.y - k * uv2.x;
        }

        bottleMat.SetFloat("_LineK", k);
        bottleMat.SetFloat("_LineB", b);
        bottleMat.SetInt("_LineT", t);
        bottleMat.SetFloat("_Angle",  transform.eulerAngles.z);
    }

    private Vector2 ConvertToUV(Vector3 point)
    {
        var localPos = transform.InverseTransformPoint(point);
        return new Vector2(localPos.x / width + 0.5f, localPos.y / height + 0.5f);
    }

    private float executeWorldY;

    private void CalculateY()
    {
    }

    private void CalculateFillVolume()
    {
        var count = Mathf.CeilToInt(maxY - minY);
        for (int i = 0; i < count; i++)
        {
        }
    }

    private Vector3? CalculateIntersectPoint(Vector3 firstPoint, Vector3 secondPoint, float y)
    {
        if (Mathf.Approximately(secondPoint.x, firstPoint.x))
        {
            return new Vector3(firstPoint.x, y, 0f);
        }

        if (Mathf.Approximately(secondPoint.y, firstPoint.y))
        {
            return null;
        }

        var slope = (secondPoint.y - firstPoint.y) / (secondPoint.x - firstPoint.x);
        var x = (y - firstPoint.y) / slope + firstPoint.x;

        var leftX = Mathf.Min(firstPoint.x, secondPoint.x);
        var rightX = Mathf.Max(firstPoint.x, secondPoint.x);
        if (x < leftX || x > rightX)
        {
            return null;
        }

        return new Vector3(x, y, 0);
    }

    private class PixelData
    {
        public Color32 color;
        public Vector3 worldPos;
        public Vector3 localPos;

        public PixelData(Color32 color, Vector3 localPos, Vector3 worldPos)
        {
            this.color = color;
            this.worldPos = worldPos;
        }
    }
}
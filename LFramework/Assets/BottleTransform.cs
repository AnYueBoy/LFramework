using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class BottleTransform : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _srComp;
    [SerializeField] private Material bottleMat;
#if UNITY_EDITOR
    [OnValueChanged(nameof(SetFillAmount))] [SerializeField]
    private float editorFillAmount = 0.5f;
#endif
    private float fillAmount = 0.5f;

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
        ellipseInfoArray = new Vector4[32];

        InitializePixelData(spriteAsset);
    }

    [SerializeField] private int examinePixelCount = 2;

    private Color32[] CompatiblePixel(Sprite spriteAsset)
    {
        var pixelArray = spriteAsset.texture.GetPixels32();
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
                    if (examinePixel.a <= 0)
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

        return pixelArray;
    }

    private void InitializePixelData(Sprite spriteAsset)
    {
        // 兼容处理像素信息
        var pixelArray = CompatiblePixel(spriteAsset);
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

                if (pixel.a > 0)
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

    public void SetFillAmount(float value)
    {
        fillAmount = Mathf.Clamp(value, 0.01f, 0.99f);
        UpdateWorldBoundPos();
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

        var worldY = CalculateFillVolume();
        GenerateIntersectPoints(worldY);
        var uv1 = ConvertToUV(intersectPointList[0]);
        var uv2 = ConvertToUV(intersectPointList[1]);

        float k, b, arc;
        int t;
        if (Mathf.Approximately(uv2.x, uv1.x))
        {
            t = -1;
            k = uv2.x;
            b = -1;
            arc = Mathf.Deg2Rad * 90f;
        }
        else if (Mathf.Approximately(uv2.y, uv1.y))
        {
            t = 1;
            k = 0;
            b = uv2.y;
            arc = Mathf.Atan(k);
        }
        else
        {
            t = 0;
            k = (uv2.y - uv1.y) / (uv2.x - uv1.x);
            b = uv2.y - k * uv2.x;
            arc = Mathf.Atan(k);
        }

        bottleMat.SetFloat("_LineK", k);
        bottleMat.SetFloat("_LineB", b);
        bottleMat.SetInt("_LineT", t);
        bottleMat.SetFloat("_Angle", transform.eulerAngles.z);

        if (intersectPointList.Count >= 2)
        {
            CalculateEllipse(intersectPointList[0], intersectPointList[1], arc);
        }
    }

    private Vector4[] ellipseInfoArray;

    private void CalculateEllipse(Vector3 point1, Vector3 point2, float arc)
    {
        int horizontal = Mathf.FloorToInt(Mathf.Abs(point2.x - point1.x) * 100f);
        float minX = Mathf.Min(point1.x, point2.x);
        int pixelCount = 0;
        PixelType prePixelType = PixelType.None;
        int dataIndex = 0;
        for (int i = 0; i < horizontal; i++)
        {
            float step = i / 100f;
            var samplePoint = new Vector3(minX + step, point1.y, 0f);
            var sampleUV = ConvertToUV(samplePoint);
            var readHeight = (int)(sampleUV.y * height);
            readHeight = Mathf.Min(height - 1, readHeight);
            var index = Mathf.FloorToInt(readHeight * width + sampleUV.x * width);
            index = Mathf.Clamp(index, 0, _pixelDataArray.Length - 1);
            var pixelData = _pixelDataArray[index];
            if (pixelData.color.a <= 0)
            {
                if (prePixelType == PixelType.None)
                {
                    prePixelType = PixelType.Empty;
                }

                if (prePixelType != PixelType.Empty)
                {
                    var preX = minX + step - pixelCount / 100f;
                    var preSamplePoint = new Vector3(preX, point1.y, 0f);
                    var preSampleUV = ConvertToUV(preSamplePoint);
                    var centerUVPoint = Vector2.Lerp(preSampleUV, sampleUV, 0.5f);
                    var longRadius = (sampleUV - preSampleUV).magnitude;
                    ellipseInfoArray[dataIndex++] = new Vector4(centerUVPoint.x, centerUVPoint.y, longRadius, arc);
                }

                prePixelType = PixelType.Empty;
                pixelCount = 0;
            }
            else
            {
                prePixelType = PixelType.NonEmpty;
            }

            pixelCount++;
        }

        var lastSamplePoint = new Vector3(minX + (horizontal - 1) / 100f, point1.y, 0f);
        var lastSampleUV = ConvertToUV(lastSamplePoint);
        var lastRealHeight = (int)(lastSampleUV.y * height);
        lastRealHeight = Mathf.Min(lastRealHeight, height - 1);
        var lastIndex = Mathf.FloorToInt(lastRealHeight * width + lastSampleUV.x * width);
        lastIndex = Mathf.Clamp(lastIndex, 0, _pixelDataArray.Length - 1);
        var lastPixelData = _pixelDataArray[lastIndex];
        if (lastPixelData.color.a > 0)
        {
            var preX = minX + (horizontal - 1 - pixelCount) / 100f;
            var preSamplePoint = new Vector3(preX, point1.y, 0f);
            var preSampleUV = ConvertToUV(preSamplePoint);
            var centerUVPoint = Vector2.Lerp(preSampleUV, lastSampleUV, 0.5f);
            var longRadius = (lastSampleUV - preSampleUV).magnitude;
            ellipseInfoArray[dataIndex++] = new Vector4(centerUVPoint.x, centerUVPoint.y, longRadius, arc);
        }

        bottleMat.SetInt("_EllipseCount", dataIndex);
        bottleMat.SetVectorArray("_EllipseInfoArray", ellipseInfoArray);
    }

    private Vector2 ConvertToUV(Vector3 point)
    {
        var localPos = transform.InverseTransformPoint(point);
        return new Vector2(localPos.x / (width / 100f) + 0.5f, localPos.y / (height / 100f) + 0.5f);
    }

    private readonly List<Vector3> intersectPointList = new List<Vector3>();

    private void GenerateIntersectPoints(float worldY)
    {
        var bottomIntersectPoint = CalculateIntersectPoint(lbWorldPoint, rbWorldPoint, worldY);
        var leftIntersectPoint = CalculateIntersectPoint(lbWorldPoint, ltWorldPoint, worldY);
        var rightIntersectPoint = CalculateIntersectPoint(rbWorldPoint, rtWorldPoint, worldY);
        var topIntersectPoint = CalculateIntersectPoint(ltWorldPoint, rtWorldPoint, worldY);

        intersectPointList.Clear();
        if (bottomIntersectPoint != null)
        {
            intersectPointList.Add(bottomIntersectPoint.Value);
        }

        if (leftIntersectPoint != null)
        {
            intersectPointList.Add(leftIntersectPoint.Value);
        }

        if (rightIntersectPoint != null)
        {
            intersectPointList.Add(rightIntersectPoint.Value);
        }

        if (topIntersectPoint != null)
        {
            intersectPointList.Add(topIntersectPoint.Value);
        }
    }

    private float CalculateFillVolume()
    {
        var realVolume = (int)(Mathf.Clamp01(fillAmount) * effectVolume);

        int cumulativeVolume = 0;
        var count = Mathf.FloorToInt((maxY - minY) * 100f);
        for (int i = 0; i < count; i++)
        {
            var yStep = i / 100f;
            var y = minY + yStep;

            // 根据y值 产生两个相交点
            GenerateIntersectPoints(y);
            if (intersectPointList.Count < 2)
            {
                continue;
            }

            var point1 = intersectPointList[0];
            var point2 = intersectPointList[1];
            float minX = Mathf.Min(point1.x, point2.x);

            int horizontal = Mathf.FloorToInt(Mathf.Abs(point2.x - point1.x) * 100f);
            for (int j = 0; j < horizontal - 1; j++)
            {
                var xStep = j / 100f;
                var samplePoint = new Vector3(minX + xStep, y, 0f);
                var sampleUV = ConvertToUV(samplePoint);
                var readHeight = (int)(sampleUV.y * height);
                readHeight = Mathf.Min(height - 1, readHeight);
                var index = Mathf.FloorToInt(readHeight * width + sampleUV.x * width);
                index = Mathf.Clamp(index, 0, _pixelDataArray.Length - 1);

                var pixel = _pixelDataArray[index].color;
                if (pixel.a <= 0)
                {
                    continue;
                }

                cumulativeVolume++;
                if (cumulativeVolume < realVolume)
                {
                    continue;
                }

                return y;
            }
        }

        return maxY;
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

    private enum PixelType
    {
        None,
        Empty,
        NonEmpty,
    }
}
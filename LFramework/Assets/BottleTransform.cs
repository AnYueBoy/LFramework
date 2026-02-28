using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;

public class BottleTransform : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _srComp;
    [SerializeField] private Material bottleMat;
    [SerializeField] private int alphaThreshold = 200;
#if UNITY_EDITOR
    [SerializeField] [OnValueChanged(nameof(SetFillAmount))]
    private float editorFillAmount = 0.5f;
#endif

    private float fillAmount = 0.5f;
    private int width, height;
    private Color32[] pixelArray;
    private int effectVolume;
    private Vector3 lbLocalPoint, rbLocalPoint, ltLocalPoint, rtLocalPoint;
    private Vector3 lbWorldPoint, rbWorldPoint, ltWorldPoint, rtWorldPoint;
    private float minY, maxY;
    private bool initialized;

    #region Shader 属性

    private static readonly int LineK = Shader.PropertyToID("_LineK");
    private static readonly int LineB = Shader.PropertyToID("_LineB");
    private static readonly int LineT = Shader.PropertyToID("_LineT");
    private static readonly int Angle = Shader.PropertyToID("_Angle");
    private static readonly int EllipseCount = Shader.PropertyToID("_EllipseCount");
    private static readonly int EllipseInfoArray = Shader.PropertyToID("_EllipseInfoArray");

    #endregion

    private void Awake()
    {
        initialized = false;
        Initialize();
        initialized = true;
    }

    private void Initialize()
    {
        var spriteAsset = _srComp.sprite;
        width = (int)spriteAsset.rect.width;
        height = (int)spriteAsset.rect.height;
        ellipseInfoArray = new Vector4[32];

        UpdateTransformMatrix();

        InitializePixelData(spriteAsset);
    }

    private void UpdateTransformMatrix()
    {
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;

        // 构建只包含旋转和平移的矩阵
        Matrix4x4 trs = Matrix4x4.TRS(position, rotation, Vector3.one);
        localToWorldMatrix = trs;
        worldToLocalMatrix = trs.inverse;
    }

    [SerializeField] private int examinePixelCount = 2;

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

    private void InitializePixelData(Sprite spriteAsset)
    {
        pixelArray = spriteAsset.texture.GetPixels32();

        // 兼容处理像素信息
        CompatiblePixel();

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                var index = i * width + j;

                var pixel = pixelArray[index];
                if (pixel.a > 0)
                {
                    effectVolume++;
                }
            }
        }

        var spriteSize = spriteAsset.bounds.size;

        // 初始化边界点信息
        lbLocalPoint = new Vector3(-spriteSize.x / 2f, -spriteSize.y / 2f, 0f);
        rbLocalPoint = new Vector3(spriteSize.x / 2f, -spriteSize.y / 2f, 0f);
        ltLocalPoint = new Vector3(-spriteSize.x / 2f, spriteSize.y / 2f, 0f);
        rtLocalPoint = new Vector3(spriteSize.x / 2f, spriteSize.y / 2f, 0f);

        UpdateWorldBoundPos();
        preAngle = transform.eulerAngles.z;
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
        CalculateWaterParams();
    }

    private void UpdateWorldBoundPos()
    {
        UpdateTransformMatrix();

        lbWorldPoint = localToWorldMatrix.MultiplyPoint3x4(lbLocalPoint);
        rbWorldPoint = localToWorldMatrix.MultiplyPoint3x4(rbLocalPoint);
        ltWorldPoint = localToWorldMatrix.MultiplyPoint3x4(ltLocalPoint);
        rtWorldPoint = localToWorldMatrix.MultiplyPoint3x4(rtLocalPoint);

        minY = Mathf.Min(lbWorldPoint.y, rbWorldPoint.y, ltWorldPoint.y, rtWorldPoint.y);
        maxY = Mathf.Max(lbWorldPoint.y, rbWorldPoint.y, ltWorldPoint.y, rtWorldPoint.y);

        CalculateWaterParams();
    }

    [Button("计算")]
    private void CalculateWaterParams()
    {
        // 计算水参数

        // 计算水面参数
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

        bottleMat.SetFloat(LineK, k);
        bottleMat.SetFloat(LineB, b);
        bottleMat.SetInt(LineT, t);
        bottleMat.SetFloat(Angle, transform.eulerAngles.z);

        // 计算水面椭圆参数
        if (intersectPointList.Count >= 2)
        {
            CalculateEllipse(intersectPointList[0], intersectPointList[1], arc);
        }
    }

    private Vector4[] ellipseInfoArray;

    private List<Vector3> pointList = new List<Vector3>();

    private void CalculateEllipse(Vector3 point1, Vector3 point2, float arc)
    {
        pointList.Clear();
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
            var realHeight = (int)(sampleUV.y * height);
            var index = Mathf.FloorToInt(realHeight * width + sampleUV.x * width);
            index = Mathf.Clamp(index, 0, pixelArray.Length - 1);
            var pixel = pixelArray[index];
            if (pixel.a <= 0)
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
                    pointList.Add(preSamplePoint);
                    pointList.Add(samplePoint);
                }

                prePixelType = PixelType.Empty;
                pixelCount = 0;
            }
            else
            {
                prePixelType = PixelType.NonEmpty;
                pixelCount++;
            }
        }

        var lastSamplePoint = new Vector3(minX + (horizontal - 1) / 100f, point1.y, 0f);
        var lastSampleUV = ConvertToUV(lastSamplePoint);
        var lastRealHeight = (int)(lastSampleUV.y * height);
        var lastIndex = Mathf.FloorToInt(lastRealHeight * width + lastSampleUV.x * width);
        lastIndex = Mathf.Clamp(lastIndex, 0, pixelArray.Length - 1);
        var lastPixel = pixelArray[lastIndex];
        if (lastPixel.a > 0)
        {
            var preX = minX + (horizontal - 1 - pixelCount) / 100f;
            var preSamplePoint = new Vector3(preX, point1.y, 0f);
            var preSampleUV = ConvertToUV(preSamplePoint);
            var centerUVPoint = Vector2.Lerp(preSampleUV, lastSampleUV, 0.5f);
            var longRadius = (lastSampleUV - preSampleUV).magnitude;
            ellipseInfoArray[dataIndex++] = new Vector4(centerUVPoint.x, centerUVPoint.y, longRadius, arc);
            pointList.Add(preSamplePoint);
            pointList.Add(lastSamplePoint);
        }

        bottleMat.SetInt(EllipseCount, dataIndex);
        bottleMat.SetVectorArray(EllipseInfoArray, ellipseInfoArray);
    }

    private Matrix4x4 worldToLocalMatrix;
    private Matrix4x4 localToWorldMatrix;

    private Vector2 ConvertToUV(Vector3 point)
    {
        var m00 = worldToLocalMatrix.m00;
        var m01 = worldToLocalMatrix.m01;
        var m02 = worldToLocalMatrix.m02;
        var m03 = worldToLocalMatrix.m03;
        var m10 = worldToLocalMatrix.m10;
        var m11 = worldToLocalMatrix.m11;
        var m12 = worldToLocalMatrix.m12;
        var m13 = worldToLocalMatrix.m13;
        float localX = m00 * point.x + m01 * point.y + m02 * point.z + m03;
        float localY = m10 * point.x + m11 * point.y + m12 * point.z + m13;
        return new Vector2(localX * 100f / width + 0.5f, localY * 100f / height + 0.5f);
    }

    private void ConvertToUV(float x, float y, float z, out float u, out float v)
    {
        var m00 = worldToLocalMatrix.m00;
        var m01 = worldToLocalMatrix.m01;
        var m02 = worldToLocalMatrix.m02;
        var m03 = worldToLocalMatrix.m03;
        var m10 = worldToLocalMatrix.m10;
        var m11 = worldToLocalMatrix.m11;
        var m12 = worldToLocalMatrix.m12;
        var m13 = worldToLocalMatrix.m13;
        float localX = m00 * x + m01 * y + m02 * z + m03;
        float localY = m10 * x + m11 * y + m12 * z + m13;
        u = localX / (width / 100f) + 0.5f;
        v = localY / (height / 100f) + 0.5f;
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
        var count = (int)((maxY - minY) * 100f);
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
                float x = minX + xStep;

                // 此处内联函数以提升性能
                // ConvertToUV(x, y, 0f, out var su, out var sv);

                #region 内联函数

                var m00 = worldToLocalMatrix.m00;
                var m01 = worldToLocalMatrix.m01;
                var m02 = worldToLocalMatrix.m02;
                var m03 = worldToLocalMatrix.m03;
                var m10 = worldToLocalMatrix.m10;
                var m11 = worldToLocalMatrix.m11;
                var m12 = worldToLocalMatrix.m12;
                var m13 = worldToLocalMatrix.m13;
                float localX = m00 * x + m01 * y + m02 * 0f + m03;
                float localY = m10 * x + m11 * y + m12 * 0f + m13;
                var su = localX / (width / 100f) + 0.5f;
                var sv = localY / (height / 100f) + 0.5f;

                #endregion

                var realHeight = (int)(sv * height);
                var index = (int)(realHeight * width + su * width);
                var pixelArrayLength = pixelArray.Length;
                if (index < 0)
                {
                    index = 0;
                }
                else if (index >= pixelArrayLength - 1)
                {
                    index = pixelArrayLength - 1;
                }

                var pixel = pixelArray[index];
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

    private const float referenceValue = 0.0001f;

    private Vector3? CalculateIntersectPoint(Vector3 firstPoint, Vector3 secondPoint, float y)
    {
        if (Abs(secondPoint.x - firstPoint.x) < referenceValue)
        {
            return new Vector3(firstPoint.x, y, 0f);
        }

        if (Abs(secondPoint.y - firstPoint.y) < referenceValue)
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

    private void OnDrawGizmos()
    {
        var originColor = Gizmos.color;
        for (int i = 0; i < pointList.Count - 1; i += 2)
        {
            var pont1 = pointList[i];
            var point2 = pointList[i + 1];
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pont1, point2);
        }

        Gizmos.color = originColor;
    }

    private float Abs(float value)
    {
        if (value >= 0)
        {
            return value;
        }

        return -value;
    }

    private enum PixelType
    {
        None,
        Empty,
        NonEmpty,
    }
}
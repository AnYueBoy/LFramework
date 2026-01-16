using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;

namespace Lith.LiquidGlass
{
    public class NormalMapBaker : EditorWindow
    {
        public enum Mode { FromSprite, ProceduralRoundedRectCustom, ProceduralRoundedRectFromRectTransform }
        public enum MaskMode { None, SoftGrayscale, HardBinary }
        public enum MaskTextureType { Default, Sprite }
        public struct NormalMapBakerSettings
        {
            public Mode mode;
            public int width;
            public int height;
            public float normalScaleFactor;
            public float intensity;
            public bool invertX;
            public bool invertY;
            // FromSprite
            public Texture2D sourceSprite;
            public float blurPx;
            // ProceduralRoundedRect
            public float edgeWidthPx;
            public float edgeCurve;
            public float cornerRadiusPx;
            // Procedural RoundedRect Edges
            public bool enableTop;
            public bool enableBottom;
            public bool enableLeft;
            public bool enableRight;
            public float maskScaleFactor;
            public MaskMode maskMode;
            public MaskTextureType maskTextureType;
            public float hardThreshold;
            public float maskGamma;
            public bool invertMask;
            public bool createOutline;
            public float outlineThicknessPx;
            public string lastSaveDir;
            public string lastSaveName;
            public bool bakeAuto;
        }
        public static void OnGUI(ref Mode mode, ref int width, ref int height, ref float normalScaleFactor, ref float intensity, ref bool invertX, ref bool invertY,
        ref Texture2D sourceSprite, ref float blurPx, ref float edgeWidthPx, ref float edgeCurve, ref float cornerRadiusPx, ref bool enableTop,
        ref bool enableBottom, ref bool enableRight, ref bool enableLeft, ref float maskScaleFactor, ref MaskMode maskMode,
        ref MaskTextureType maskTextureType, ref float hardThreshold, ref float maskGamma, ref bool invertMask, ref bool createOutline,
        ref float outlineThicknessPx, ref string lastSaveDir, ref string lastSaveName, ref bool bakeAuto, MaterialProperty normalMapProperty, Image image, RawImage rawImage)
        {
            mode = (Mode)EditorGUILayout.EnumPopup("Mode", mode);
            intensity = EditorGUILayout.Slider("Intensity", intensity, 0.1f, 32f);
            invertX = EditorGUILayout.Toggle("Invert X", invertX);
            invertY = EditorGUILayout.Toggle("Invert Y", invertY);

            if (mode == Mode.FromSprite)
            {
                width = EditorGUILayout.IntField("Width", width);
                height = EditorGUILayout.IntField("Height", height);
                sourceSprite = (Texture2D)EditorGUILayout.ObjectField("Source (Sprite/Mask)", sourceSprite, typeof(Texture2D), false);
                blurPx = EditorGUILayout.Slider("Pre-Blur (px)", blurPx, 0f, 10f);
            }
            else if (mode == Mode.ProceduralRoundedRectCustom)
            {
                width = EditorGUILayout.IntField("Width", width);
                height = EditorGUILayout.IntField("Height", height);
                edgeWidthPx = EditorGUILayout.Slider("Edge Width (px)", edgeWidthPx, 0f, 256f);
                edgeCurve = EditorGUILayout.Slider("Edge Curve", edgeCurve, 0.25f, 8f);
                cornerRadiusPx = EditorGUILayout.Slider("Corner Radius (px)", cornerRadiusPx, 0f, 256f);

                GUILayout.Space(20);

                GUILayout.Label("Enabled Edges:");
                enableTop = EditorGUILayout.Toggle("Top", enableTop);
                enableBottom = EditorGUILayout.Toggle("Bottom", enableBottom);
                enableLeft = EditorGUILayout.Toggle("Left", enableLeft);
                enableRight = EditorGUILayout.Toggle("Right", enableRight);
            }
            else
            {
                GUI.enabled = false;
                EditorGUILayout.IntField("Width", width);
                EditorGUILayout.IntField("Height", height);
                GUI.enabled = true;

                normalScaleFactor = EditorGUILayout.Slider("Scale Factor", normalScaleFactor, 0.1f, 4f);

                edgeWidthPx = EditorGUILayout.Slider("Edge Width (px)", edgeWidthPx, 0f, 256f);
                edgeCurve = EditorGUILayout.Slider("Edge Curve", edgeCurve, 0.25f, 8f);
                cornerRadiusPx = EditorGUILayout.Slider("Corner Radius (px)", cornerRadiusPx, 0f, 256f);
            }

            GUILayout.Space(20);

            maskMode = (MaskMode)EditorGUILayout.EnumPopup("Mask Output", maskMode);

            if (maskMode == MaskMode.SoftGrayscale)
            {
                maskGamma = EditorGUILayout.Slider("Mask Gamma", maskGamma, 0.1f, 10f);
                invertMask = EditorGUILayout.Toggle("Invert Mask", invertMask);
            }
            else if (maskMode == MaskMode.HardBinary)
            {
                hardThreshold = EditorGUILayout.Slider("Hard Threshold", hardThreshold, 0f, 1f);
                invertMask = EditorGUILayout.Toggle("Invert Mask", invertMask);
            }

            if (maskMode != MaskMode.None)
            {
                maskScaleFactor = EditorGUILayout.Slider("Mask Scale Factor", maskScaleFactor, 0.1f, 10f);
                maskTextureType = (MaskTextureType)EditorGUILayout.EnumPopup("Mask Texture Type", maskTextureType);

                GUILayout.Space(20);

                createOutline = EditorGUILayout.Toggle("Create Outline", createOutline);

                if (createOutline)
                    outlineThicknessPx = EditorGUILayout.Slider("Outline Thickness (px)", outlineThicknessPx, 0.01f, 20f);
            }
            
            GUILayout.Space(20);

            bakeAuto = EditorGUILayout.Toggle("Bake Auto and Set", bakeAuto);

            GUILayout.Space(20);

            if (!bakeAuto && GUILayout.Button("Bake Normal Map"))
            {
                BakeNormalMap(width, height, normalScaleFactor, mode, sourceSprite, blurPx, edgeWidthPx, edgeCurve, cornerRadiusPx, enableTop, enableBottom, enableLeft, enableRight, maskScaleFactor, maskMode,
                maskTextureType, hardThreshold, maskGamma, invertMask, intensity, invertX, invertY, createOutline, outlineThicknessPx, ref lastSaveDir, ref lastSaveName, bakeAuto, normalMapProperty, image, rawImage);
            }
        }

        public static void BakeNormalMap(int width, int height, float scale, Mode mode, Texture2D sourceSprite, float blurPx, float edgeWidthPx, float edgeCurve, float cornerRadiusPx, bool enableTop, bool enableBottom, bool enableLeft, bool enableRight,
        float maskScaleFactor, MaskMode maskMode,
        MaskTextureType maskTextureType, float hardThreshold, float maskGamma, bool invertMask,
        float intensity, bool invertX, bool invertY, bool createOutline, float outlineThicknessPx, ref string lastSaveDir, ref string lastSaveName, bool bakeAuto, MaterialProperty normalMapProperty, Image image, RawImage rawImage)
        {
            if (mode == Mode.ProceduralRoundedRectFromRectTransform)
            {
                width = (int)(width * scale);
                height = (int)(height * scale);
            }
            
            float[] heightBuf;
            var tex = Bake(out heightBuf, width, height, mode, sourceSprite, blurPx, edgeWidthPx, edgeCurve, cornerRadiusPx, enableTop, enableBottom, enableLeft, enableRight, intensity, invertX, invertY);

            if (tex != null)
            {
                var path = !bakeAuto || lastSaveDir == null || lastSaveDir == null ? EditorUtility.SaveFilePanelInProject(
                    "Save Normal Map", lastSaveName, "png", "Choose a path"
                , lastSaveDir) : Path.Combine(lastSaveDir, lastSaveName + ".png");

                if (!string.IsNullOrEmpty(path))
                {
                    var dirName = Path.GetDirectoryName(path);
                    var fileName = Path.GetFileNameWithoutExtension(path);

                    if (!string.IsNullOrEmpty(dirName))
                        lastSaveDir = dirName;

                    if (!string.IsNullOrEmpty(fileName))
                        lastSaveName = fileName;

                    Directory.CreateDirectory(dirName);

                    var normalPath = Path.Combine(dirName, fileName + ".png");
                    normalPath = normalPath.Replace("\\", "/");
                    File.WriteAllBytes(normalPath, tex.EncodeToPNG());
                    AssetDatabase.Refresh();

                    var impN = (TextureImporter)AssetImporter.GetAtPath(normalPath);
                    if (impN != null)
                    {
                        impN.textureType = TextureImporterType.NormalMap;
                        impN.alphaSource = TextureImporterAlphaSource.None;
                        impN.sRGBTexture = false;
                        impN.mipmapEnabled = false;
                        impN.SaveAndReimport();
                    }

                    var writtenTexture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                    if (writtenTexture2D == null)
                    {
                        Debug.LogError("Normal map texture not found: " + normalPath);
                        return;
                    }

                    normalMapProperty.textureValue = writtenTexture2D;

                    if (maskMode != MaskMode.None)
                    {
                        var maskWidth = (int)(width * maskScaleFactor);
                        var maskHeight = (int)(height * maskScaleFactor);

                        var maskHeightBuf = ResampleHeightBuf(heightBuf, width, height, maskWidth, maskHeight);
                        var maskTex = MakeMaskTexture(maskHeightBuf, maskWidth, maskHeight, maskMode, hardThreshold, maskGamma, invertMask);
                        var maskPath = Path.Combine(dirName, fileName + "_mask.png");
                        maskPath = maskPath.Replace("\\", "/");
                        File.WriteAllBytes(maskPath, maskTex.EncodeToPNG());
                        AssetDatabase.Refresh();

                        var impM = (TextureImporter)AssetImporter.GetAtPath(maskPath);
                        if (impM != null)
                        {
                            if (maskTextureType == MaskTextureType.Sprite)
                            {
                                impM.textureType = TextureImporterType.Sprite;
                                impM.spritePixelsPerUnit = 100;
                                impM.alphaIsTransparency = true;
                            }
                            else
                            {
                                impM.textureType = TextureImporterType.Default;
                                impM.sRGBTexture = false;
                                impM.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                                impM.alphaIsTransparency = false;
                            }

                            impM.mipmapEnabled = false;
                            impM.SaveAndReimport();
                        }

                        if (image != null)
                        {
                            var writtenSprite = AssetDatabase.LoadAssetAtPath<Sprite>(maskPath);
                            if (writtenSprite == null)
                            {
                                Debug.LogError("Mask texture not found: " + maskPath);
                                return;
                            }
                            image.sprite = writtenSprite;
                        }

                        if (rawImage != null)
                        {
                            var writtenTexture = AssetDatabase.LoadAssetAtPath<Texture>(maskPath);
                            if (writtenTexture == null)
                            {
                                Debug.LogError("Mask texture not found: " + maskPath);
                                return;
                            }
                            rawImage.texture = writtenTexture;
                        }

                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));

                        if (createOutline)
                        {
                            var outlineTex = MakeOutlineTexture(maskHeightBuf, maskWidth, maskHeight, outlineThicknessPx, hardThreshold, invertMask);
                            var outlinePath = Path.Combine(dirName, fileName + "_outline.png");
                            outlinePath = outlinePath.Replace("\\", "/");
                            File.WriteAllBytes(outlinePath, outlineTex.EncodeToPNG());
                            AssetDatabase.Refresh();

                            var impO = (TextureImporter)AssetImporter.GetAtPath(outlinePath);
                            if (impO != null)
                            {
                                impO.textureType = TextureImporterType.Sprite;
                                impO.spritePixelsPerUnit = 100;
                                impO.alphaIsTransparency = true;
                                impO.mipmapEnabled = false;
                                impO.SaveAndReimport();
                            }
                            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Texture2D>(outlinePath));
                        }
                    }

                    var saved = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    EditorGUIUtility.PingObject(saved);
                }
            }
        }

        static Texture2D Bake(out float[] heightBuf, int width, int height, Mode mode, Texture2D sourceSprite, float blurPx, float edgeWidthPx, float edgeCurve, float cornerRadiusPx, bool enableTop, bool enableBottom, bool enableLeft, bool enableRight,
        float intensity, bool invertX, bool invertY)
        {
            heightBuf = null;

            if (width <= 2 || height <= 2) { Debug.LogError("Invalid size"); return null; }

            heightBuf = new float[width * height];

            if (mode == Mode.FromSprite)
            {
                if (sourceSprite == null) { Debug.LogError("Source sprite missing"); return null; }
                SampleSpriteToHeight(sourceSprite, width, height, heightBuf);

                if (blurPx > 0.01f)
                    GaussianBlur(heightBuf, width, height, blurPx);
            }
            else
            {
                BakeRoundedRectHeight(width, height, edgeWidthPx, edgeCurve, cornerRadiusPx, ref heightBuf, enableTop, enableBottom, enableLeft, enableRight);
            }

            var normal = HeightToNormal(heightBuf, width, height, intensity, invertX, invertY);
            normal.wrapMode = TextureWrapMode.Clamp;
            normal.filterMode = FilterMode.Bilinear;
            return normal;
        }

        static Texture2D MakeMaskTexture(float[] heightBuf, int w, int h,
        MaskMode mode, float hardThreshold, float gamma, bool invert)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            var px = new Color32[w * h];

            for (int i = 0; i < px.Length; i++)
            {
                float v = Mathf.Clamp01(heightBuf[i]);
                if (mode == MaskMode.SoftGrayscale)
                {
                    v = Mathf.Pow(v, Mathf.Max(0.1f, gamma));
                    if (!invert) v = 1f - v;
                }
                else
                {
                    float t = !invert ? (1f - v) : v;
                    v = t >= hardThreshold ? 1f : 0f;
                }

                byte b = (byte)Mathf.RoundToInt(v * 255f);
                px[i] = new Color32(b, b, b, b);
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }
        static Texture2D MakeOutlineTexture(float[] heightBuf, int w, int h, float thicknessPx, float hardThreshold, bool invert)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            var px = new Color32[w * h];

            int radius = Mathf.CeilToInt(thicknessPx);

            bool[] maskBuf = new bool[w * h];
            for (int i = 0; i < maskBuf.Length; i++)
            {
                float v = Mathf.Clamp01(heightBuf[i]);
                float t = !invert ? (1f - v) : v;
                maskBuf[i] = t >= hardThreshold;
            }

            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    if (!maskBuf[row + x])
                    {
                        px[row + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    bool isEdge = false;
                    for (int oy = -radius; oy <= radius && !isEdge; oy++)
                    {
                        for (int ox = -radius; ox <= radius && !isEdge; ox++)
                        {
                            int nx = Mathf.Clamp(x + ox, 0, w - 1);
                            int ny = Mathf.Clamp(y + oy, 0, h - 1);
                            if (!maskBuf[ny * w + nx])
                                isEdge = true;
                        }
                    }

                    px[row + x] = isEdge ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }


        static void SampleSpriteToHeight(Texture2D tex, int w, int h, float[] outH)
        {
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(tex, rt);
            var tmp = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
            var active = RenderTexture.active; RenderTexture.active = rt;
            tmp.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            tmp.Apply(false, false);
            RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt);

            var cols = tmp.GetRawTextureData<Color32>();
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    var c = cols[row + x];
                    float a = c.a / 255f;
                    float luma = (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
                    outH[row + x] = (a > 0.0001f) ? a : luma;
                }
            }
            DestroyImmediate(tmp);
        }

        static void BakeRoundedRectHeight(
            int w, int h, float edgePx, float curve, float radiusPx,
            ref float[] outH, bool top = true, bool bottom = true, bool left = true, bool right = true)
        {
            float r = Mathf.Max(0f, radiusPx);
            float e = Mathf.Max(1e-5f, edgePx);

            float EdgeT(float dist) => Mathf.Pow(Mathf.Clamp01(1f - dist / e), curve);
            float CornerT(float px, float py, float cx, float cy, float rad)
            {
                float dx = px - cx, dy = py - cy;
                float len = Mathf.Sqrt(dx * dx + dy * dy);
                float d = rad - len;
                return Mathf.Pow(Mathf.Clamp01(1f - d / e), curve);
            }

            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    bool inBL = bottom && left && (px <= r + 1.0f) && (py <= r + 1.0f);
                    bool inBR = bottom && right && (px >= w - r - 1.0f) && (py <= r + 1.0f);
                    bool inTL = top && left && (px <= r + 1.0f) && (py >= h - r - 1.0f);
                    bool inTR = top && right && (px >= w - r - 1.0f) && (py >= h - r - 1.0f);

                    float v = 0f;

                    if (top && !(inTL || inTR))
                    {
                        float distTop = (h - 0.5f) - py;
                        v = Mathf.Max(v, EdgeT(distTop));
                    }
                    if (bottom && !(inBL || inBR))
                    {
                        float distBottom = py - 0.5f;
                        v = Mathf.Max(v, EdgeT(distBottom));
                    }
                    if (left && !(inTL || inBL))
                    {
                        float distLeft = px - 0.5f;
                        v = Mathf.Max(v, EdgeT(distLeft));
                    }
                    if (right && !(inTR || inBR))
                    {
                        float distRight = (w - 0.5f) - px;
                        v = Mathf.Max(v, EdgeT(distRight));
                    }

                    if (inBL || inBR || inTL || inTR)
                    {
                        bool isLeft = (inBL || inTL);
                        bool isBottom = (inBL || inBR);

                        float cx = isLeft ? (r + 0.5f) : (w - (r + 0.5f));
                        float cy = isBottom ? (r + 0.5f) : (h - (r + 0.5f));

                        v = Mathf.Max(v, CornerT(px, py, cx, cy, r));
                    }

                    outH[row + x] = Mathf.Clamp01(v);
                }
            }
        }

        static void GaussianBlur(float[] buf, int w, int h, float radiusPx)
        {
            if (radiusPx < 0.5f) return;
            int r = Mathf.CeilToInt(radiusPx);
            int size = 2 * r + 1;
            float sigma = radiusPx * 0.5f;
            float twoSigma2 = 2f * sigma * sigma;

            float[] k = new float[size];
            float sum = 0;
            for (int i = 0; i < size; i++) { int x = i - r; k[i] = Mathf.Exp(-(x * x) / twoSigma2); sum += k[i]; }
            for (int i = 0; i < size; i++) k[i] /= sum;

            float[] tmp = new float[w * h];

            // horizontal
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                {
                    float acc = 0;
                    for (int i = 0; i < size; i++)
                    {
                        int xx = Mathf.Clamp(x + i - r, 0, w - 1);
                        acc += buf[row + xx] * k[i];
                    }
                    tmp[row + x] = acc;
                }
            }
            // vertical
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    float acc = 0;
                    for (int i = 0; i < size; i++)
                    {
                        int yy = Mathf.Clamp(y + i - r, 0, h - 1);
                        acc += tmp[yy * w + x] * k[i];
                    }
                    buf[y * w + x] = acc;
                }
            }
        }
        static Texture2D HeightToNormal(float[] h, int w, int hgt, float intensity, bool invertX, bool invertY)
        {
            var tex = new Texture2D(w, hgt, TextureFormat.RGBA32, false, true);
            var px = new Color32[w * hgt];

            int W = w, H = hgt;
            float ix = (invertX ? -1f : 1f) * intensity;
            float iy = (invertY ? -1f : 1f) * intensity;

            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float h00 = GetH(x - 1, y - 1); float h10 = GetH(x, y - 1); float h20 = GetH(x + 1, y - 1);
                    float h01 = GetH(x - 1, y); float h11 = GetH(x, y); float h21 = GetH(x + 1, y);
                    float h02 = GetH(x - 1, y + 1); float h12 = GetH(x, y + 1); float h22 = GetH(x + 1, y + 1);

                    float gx = (h20 + 2 * h21 + h22) - (h00 + 2 * h01 + h02); // d/dx
                    float gy = (h02 + 2 * h12 + h22) - (h00 + 2 * h10 + h20); // d/dy

                    float3 n = Normalize(new float3(-gx * ix, -gy * iy, 1f));
                    byte r = (byte)Mathf.RoundToInt((n.x * 0.5f + 0.5f) * 255f);
                    byte g = (byte)Mathf.RoundToInt((n.y * 0.5f + 0.5f) * 255f);
                    byte b = (byte)Mathf.RoundToInt((n.z * 0.5f + 0.5f) * 255f);
                    px[y * W + x] = new Color32(r, g, b, 255);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;

            float GetH(int xx, int yy) { xx = Mathf.Clamp(xx, 0, W - 1); yy = Mathf.Clamp(yy, 0, H - 1); return h[yy * W + xx]; }
        }
        static float[] ResampleHeightBuf(float[] src, int srcW, int srcH, int dstW, int dstH)
        {
            var dst = new float[dstW * dstH];
            for (int y = 0; y < dstH; y++)
            {
                float v = (y + 0.5f) / dstH * srcH - 0.5f;
                int y0 = Mathf.Clamp(Mathf.FloorToInt(v), 0, srcH - 1);
                int y1 = Mathf.Clamp(y0 + 1, 0, srcH - 1);
                float fy = v - y0;
                for (int x = 0; x < dstW; x++)
                {
                    float u = (x + 0.5f) / dstW * srcW - 0.5f;
                    int x0 = Mathf.Clamp(Mathf.FloorToInt(u), 0, srcW - 1);
                    int x1 = Mathf.Clamp(x0 + 1, 0, srcW - 1);
                    float fx = u - x0;

                    float a = Mathf.Lerp(src[y0 * srcW + x0], src[y0 * srcW + x1], fx);
                    float b = Mathf.Lerp(src[y1 * srcW + x0], src[y1 * srcW + x1], fx);
                    dst[y * dstW + x] = Mathf.Lerp(a, b, fy);
                }
            }
            return dst;
        }

        struct float2 { public float x, y; public float2(float X, float Y) { x = X; y = Y; } public static float2 operator -(float2 a, float2 b) => new float2(a.x - b.x, a.y - b.y); public static float2 operator *(float2 a, float s) => new float2(a.x * s, a.y * s); }
        struct float3 { public float x, y, z; public float3(float X, float Y, float Z) { x = X; y = Y; z = Z; } }
        static float2 Abs(float2 v) => new float2(Mathf.Abs(v.x), Mathf.Abs(v.y));
        static float2 Max(float2 a, float b) => new float2(Mathf.Max(a.x, b), Mathf.Max(a.y, b));
        static float Length(float2 v) => Mathf.Sqrt(v.x * v.x + v.y * v.y);
        static float3 Normalize(float3 v) { float l = Mathf.Max(1e-6f, Mathf.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z)); return new float3(v.x / l, v.y / l, v.z / l); }
    }
}
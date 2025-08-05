using System;
using UnityEngine;

namespace LFramework.SortMask
{
    public struct MaterialParameters
    {
        public Vector4 maskRect;
        public Vector4 maskBorder;
        public Vector4 maskRectUV;
        public Vector4 maskBorderUV;
        public Vector2 tileRepeat;
        public Color maskChannelWeights;
        public Matrix4x4 worldToMask;
        public Texture texutre;
        public BorderMode borderMode;
        public bool invertMask;
        public bool invertOutsides;

        private Texture activeTexture => texutre ?? Texture2D.whiteTexture;

        public SampleMaskResult SampleMask(Vector2 localPos, out float mask)
        {
            mask = 0;
            var texture2D = texutre as Texture2D;
            if (texture2D == null)
            {
                return SampleMaskResult.NonTexture2D;
            }

            var uv = XYToUV(localPos);
            try
            {
                mask = MaskValue(texture2D.GetPixelBilinear(uv.x, uv.y));
                return SampleMaskResult.Success;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return SampleMaskResult.NonReadable;
            }
        }

        public void Apply(Material mat)
        {
            mat.SetTexture(SoftMaskShaderID.SoftMask, activeTexture);
            mat.SetVector(SoftMaskShaderID.SoftMaskRect, maskRect);
            mat.SetVector(SoftMaskShaderID.SoftMaskUVRect, maskRectUV);
            mat.SetColor(SoftMaskShaderID.SoftMaskChannelWeights, maskChannelWeights);
            mat.SetMatrix(SoftMaskShaderID.SoftMaskWorldToMask, worldToMask);
            mat.SetFloat(SoftMaskShaderID.SoftMaskInvertMask, invertMask ? 1 : 0);
            mat.SetFloat(SoftMaskShaderID.SoftMaskInvertOutsides, invertOutsides ? 1 : 0);

            mat.EnableKeyword("SOFTMASK_SIMPLE", borderMode == BorderMode.Simple);
            mat.EnableKeyword("SOFTMASK_SLICED", borderMode == BorderMode.Sliced);
            mat.EnableKeyword("SOFTMASK_TILED", borderMode == BorderMode.Tiled);

            if (borderMode != BorderMode.Simple)
            {
                mat.SetVector(SoftMaskShaderID.SoftMaskBorderRect, maskBorder);
                mat.SetVector(SoftMaskShaderID.SoftMaskUVBorderRect, maskBorderUV);

                if (borderMode == BorderMode.Tiled)
                {
                    mat.SetVector(SoftMaskShaderID.SoftMaskTileRepeat, tileRepeat);
                }
            }
        }

        private Vector2 XYToUV(Vector2 localPos)
        {
            switch (borderMode)
            {
                case BorderMode.Simple:
                    return MapSimple(localPos);

                case BorderMode.Sliced:
                    return MapBorder(localPos, false);

                case BorderMode.Tiled:
                    return MapBorder(localPos, true);

                default:
                    Debug.LogError($"未知的BoardMode {borderMode}");
                    return MapSimple(localPos);
            }
        }

        private Vector2 MapSimple(Vector2 localPos)
        {
            return MathLib.Remap(localPos, maskRect, maskRectUV);
        }

        private Vector2 MapBorder(Vector2 localPos, bool repeat)
        {
            return
                new Vector2(
                    Remap(localPos.x,
                        maskRect.x, maskBorder.x, maskBorder.z, maskRect.z,
                        maskRectUV.x, maskBorderUV.x, maskBorderUV.z, maskRectUV.z,
                        repeat ? tileRepeat.x : 1),
                    Remap(localPos.y,
                        maskRect.y, maskBorder.y, maskBorder.w, maskRect.w,
                        maskRectUV.y, maskBorderUV.y, maskBorderUV.w, maskRectUV.w,
                        repeat ? tileRepeat.y : 1));
        }

        private float Remap(float v, float x1, float x2, float x3, float x4, float u1, float u2, float u3,
            float u4,
            float repeat = 1)
        {
            if (v < x2)
            {
                return MathLib.Remap(v, x1, x2, u1, u2);
            }

            if (v < x3)
            {
                return MathLib.Remap(v, x2, x3, u2, u3, repeat);
            }

            return MathLib.Remap(v, x3, x4, u3, u4);
        }

        private float MaskValue(Color mask)
        {
            var value = mask * maskChannelWeights;
            return value.a + value.r + value.g + value.b;
        }
    }
}
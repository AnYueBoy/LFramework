using UnityEngine;

namespace LFramework.SoftMask
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
        public Texture texture;
        public BorderMode borderMode;
        public bool invertMask;
        public bool invertOutsides;

        private Texture ActiveTexture
        {
            get
            {
                if (texture != null)
                {
                    return texture;
                }

                return Texture2D.whiteTexture;
            }
        }

        public SampleMaskResult SampleMask(Vector2 localPos, out float mask)
        {
            mask = 0;
            var texture2D = texture as Texture2D;
            if (texture2D == null)
            {
                return SampleMaskResult.NonTexture2D;
            }

            if (!texture2D.isReadable)
            {
                return SampleMaskResult.NonReadable;
            }

            var uv = XY2UV(localPos);
            mask = MaskValue(texture2D.GetPixelBilinear(uv.x, uv.y));
            return SampleMaskResult.Success;
        }

        private Vector2 XY2UV(Vector2 localPos)
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
                    Debug.LogAssertion($"错误的Board Mode: {borderMode}");
                    return MapSimple(localPos);
            }
        }

        private Vector2 MapSimple(Vector2 localPos)
        {
            return MathOP.Remap(localPos, maskRect, maskRectUV);
        }

        private Vector2 MapBorder(Vector2 localPos, bool repeat)
        {
            return
                new Vector2(
                    Inset(
                        localPos.x,
                        maskRect.x, maskBorder.x, maskBorder.z, maskRect.z,
                        maskRectUV.x, maskBorderUV.x, maskBorderUV.z, maskRectUV.z,
                        repeat ? tileRepeat.x : 1),
                    Inset(
                        localPos.y,
                        maskRect.y, maskBorder.y, maskBorder.w, maskRect.w,
                        maskRectUV.y, maskBorderUV.y, maskBorderUV.w, maskRectUV.w,
                        repeat ? tileRepeat.y : 1
                    )
                );
        }

        private float Frac(float v)
        {
            return v - Mathf.Floor(v);
        }

        private float Inset(float v, float x1, float x2, float u1, float u2, float repeat = 1)
        {
            var w = x2 - x1;
            return Mathf.Lerp(u1, u2, w != 0f ? Frac((v - x1) / w * repeat) : 0f);
        }

        private float Inset(float v, float x1, float x2, float x3, float x4, float u1, float u2, float u3, float u4,
            float repeat = 1)
        {
            if (v < x2)
            {
                return Inset(v, x1, x2, u1, u2);
            }

            if (v < x3)
            {
                return Inset(v, x2, x3, u2, u3, repeat);
            }

            return Inset(v, x3, x4, u3, u4);
        }

        private float MaskValue(Color mask)
        {
            var value = mask * maskChannelWeights;
            return value.r + value.g + value.b + value.a;
        }

        public void Apply(Material mat)
        {
            mat.SetTexture(ShaderIDConst.SoftMask, ActiveTexture);
            mat.SetVector(ShaderIDConst.SoftMask_Rect, maskRect);
            mat.SetVector(ShaderIDConst.SoftMask_UVRect, maskRectUV);
            mat.SetColor(ShaderIDConst.SoftMask_ChannelWeights, maskChannelWeights);
            mat.SetMatrix(ShaderIDConst.SoftMask_WorldToMask, worldToMask);
            mat.SetFloat(ShaderIDConst.SoftMask_InvertMask, invertMask ? 1 : 0);
            mat.SetFloat(ShaderIDConst.SoftMask_InvertOutsides, invertOutsides ? 1 : 0);

            mat.EnableKeyword("SOFTMASK_SIMPLE", borderMode == BorderMode.Simple);
            mat.EnableKeyword("SOFTMASK_SLICED", borderMode == BorderMode.Sliced);
            mat.EnableKeyword("SOFTMASK_TILED", borderMode == BorderMode.Tiled);

            if (borderMode != BorderMode.Simple)
            {
                mat.SetVector(ShaderIDConst.SoftMask_BorderRect, maskBorder);
                mat.SetVector(ShaderIDConst.SoftMask_UVBorderRect, maskBorderUV);
                if (borderMode == BorderMode.Tiled)
                {
                    mat.SetVector(ShaderIDConst.SoftMask_TileRepeat, tileRepeat);
                }
            }
        }
    }
}
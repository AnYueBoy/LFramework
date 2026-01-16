#if LLG_USE_HDRP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Lith.LiquidGlass
{
    [System.Serializable]
    public class LiquidGlassHDRPSettings
    {
        [Tooltip("Downsample factor: 1 = full res, 2 = half, etc.")]
        [Range(1, 16)]
        public int downsample = 1;

        [Range(1, 16)]
        [Tooltip("Blur downsample factor: 1 = full res, 2 = half, etc.")]
        public int blurDownsample = 16;

        [Range(0, 6)]
        [Tooltip("Number of blur iterations")]
        public int blurIterations = 2;

        [Range(0, 4)]
        [Tooltip("Blur offset step size")]
        public float blurOffset = 0.41;
    }

    public class LiquidGlassCustomPass : CustomPass
    {
        public LiquidGlassHDRPSettings settings = new LiquidGlassHDRPSettings();

        RTHandle tempRT, pingRT, pongRT;
        Material blurMat;
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            Shader.DisableKeyword("LLG_USE_URP");
            Shader blurShader = Shader.Find("Hidden/Lith/Blur");
            if (blurShader != null)
                blurMat = CoreUtils.CreateEngineMaterial(blurShader);
        }

        protected override void Execute(CustomPassContext ctx)
        {
    #if UNITY_EDITOR
            if (ctx.hdCamera.camera.cameraType == CameraType.SceneView)
                return;
    #endif

            var desc = ctx.cameraColorBuffer.rt.descriptor;
            var currWidth = desc.width;
            var currHeight = desc.height;
            
            desc.depthBufferBits = 0;

            desc.width = currWidth / settings.downsample;
            desc.height = currHeight / settings.downsample;

            if (tempRT == null || tempRT.rt == null || tempRT.rt.width != desc.width || tempRT.rt.height != desc.height)
            {
                tempRT?.Release();
                tempRT = RTHandles.Alloc(desc.width, desc.height, dimension: TextureDimension.Tex2D,
                                        colorFormat: desc.graphicsFormat, name: "_LiquidGlassRT", filterMode: FilterMode.Bilinear);
            }

            desc.width = currWidth / settings.blurDownsample;
            desc.height = currHeight / settings.blurDownsample;
                
            if (pingRT == null || pingRT.rt == null || pingRT.rt.width != desc.width || pingRT.rt.height != desc.height)
            {
                pingRT?.Release();
                pongRT?.Release();

                pingRT = RTHandles.Alloc(desc.width, desc.height, dimension: TextureDimension.Tex2D,
                                        colorFormat: desc.graphicsFormat, name: "_LiquidGlassPing", filterMode: FilterMode.Bilinear);
                pongRT = RTHandles.Alloc(desc.width, desc.height, dimension: TextureDimension.Tex2D,
                                        colorFormat: desc.graphicsFormat, name: "_LiquidGlassPong", filterMode: FilterMode.Bilinear);
            }

            var cmd = ctx.cmd;

            Blitter.BlitCameraTexture(cmd, ctx.cameraColorBuffer, tempRT);

            if (blurMat != null && settings.blurIterations > 0)
            {
                cmd.Blit(tempRT.nameID, pingRT.nameID);
                
                for (int i = 0; i < settings.blurIterations; i++)
                {
                    float currentOffset = settings.blurOffset * (i + 1);
                    blurMat.SetFloat(LiquidGlassCapture.matOffsetName, currentOffset);

                    // Horizontal pass (Pass 0)
                    cmd.Blit(pingRT.nameID, pongRT.nameID, blurMat, 0);

                    // Vertical pass (Pass 1) 
                    cmd.Blit(pongRT.nameID, pingRT.nameID, blurMat, 1);
                }

                cmd.SetGlobalTexture(LiquidGlassCapture.globalBlurTexName, pingRT);
            }
            else
                cmd.SetGlobalTexture(LiquidGlassCapture.globalBlurTexName, tempRT);

            cmd.SetGlobalTexture(LiquidGlassCapture.globalTexName, tempRT);
            
            LiquidGlassCapture.OnRenderFeatureExecute(ctx);
        }

        protected override void Cleanup()
        {
            CoreUtils.Destroy(blurMat);
            tempRT?.Release();
            pingRT?.Release();
            pongRT?.Release();
            tempRT = null;
            pingRT = null;
            pongRT = null;
        }
    }
}
#endif

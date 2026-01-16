#if LLG_USE_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace Lith.LiquidGlass
{
    [System.Serializable]
    public class LiquidGlassSettings
    {
        [Tooltip("Injection point in the frame")]
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;

        [Range(1, 16)]
        [Tooltip("Downsample factor: 1 = full res, 2 = half, etc.")]
        public int downsample = 1;

        [Range(1, 16)]
        [Tooltip("Blur downsample factor: 1 = full res, 2 = half, etc.")]
        public int blurDownsample = 16;

        [Range(0, 6)]
        [Tooltip("Number of blur iterations")]
        public int blurIterations = 2;

        [Range(0, 4)]
        [Tooltip("Blur offset step size")]
        public float blurOffset = 0.41f;
    }

    public class LiquidGlassRenderFeature : ScriptableRendererFeature
    {
#if UNITY_6000_0_OR_NEWER
        class BlurRenderPass : ScriptableRenderPass
        {
            readonly LiquidGlassSettings settings;
            readonly Material blurMat;

            static readonly Vector4 kIdentity = new(1, 1, 0, 0);
            static readonly int OffsetID = Shader.PropertyToID("_Offset");

            readonly PassData passDataCache = new();

            class PassData
            {
                public TextureHandle src;
                public TextureHandle dst;
                public TextureHandle cameraColor;
                public Material mat;
                public float offset;
                public int passIndex;
            }

            public BlurRenderPass(LiquidGlassSettings s)
            {
                settings = s;
                renderPassEvent = s.injectionPoint;

                Shader.EnableKeyword("LLG_USE_URP");
                Shader sh = Shader.Find("Hidden/Lith/Blur");
                if (sh != null) blurMat = CoreUtils.CreateEngineMaterial(sh);
            }

            public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
            {
                if (blurMat == null) return;

                var camData = frameData.Get<UniversalCameraData>();

#if UNITY_EDITOR
                if (camData.camera.cameraType == CameraType.SceneView) return;
#endif

                var desc = camData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                desc.enableRandomWrite = false;

                var tempDesc = desc;

                var currWidth = tempDesc.width;
                var currHeight = tempDesc.height;

                tempDesc.width = currWidth / settings.downsample;
                tempDesc.height = currHeight / settings.downsample;
                
                var temp = UniversalRenderer.CreateRenderGraphTexture(rg, tempDesc, "_LGTemp", false, FilterMode.Bilinear);

                TextureHandle ping = default;
                TextureHandle pong = default;
                
                if (settings.blurIterations > 0)
                {
                    tempDesc.width = currWidth / settings.blurDownsample;
                    tempDesc.height = currHeight / settings.blurDownsample;

                    ping = UniversalRenderer.CreateRenderGraphTexture(rg, tempDesc, "_LGPing", false, FilterMode.Bilinear);
                    pong = UniversalRenderer.CreateRenderGraphTexture(rg, tempDesc, "_LGPong", false, FilterMode.Bilinear);
                }

                var resData = frameData.Get<UniversalResourceData>();
                
                using (var pass = rg.AddRasterRenderPass<PassData>("LG Copy+Downsample", out var data))
                {
                    data.src = resData.activeColorTexture;
                    data.dst = temp;

                    pass.UseTexture(data.src, AccessFlags.Read);
                    pass.SetRenderAttachment(data.dst, 0);

                    pass.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, d.src, kIdentity, 0f, true);
                    });
                }

                TextureHandle finalBlurTexture = temp;

                if (settings.blurIterations > 0)
                {
                    using (var pass = rg.AddRasterRenderPass<PassData>("LG PreCopy", out var data))
                    {
                        data.src = temp;
                        data.dst = ping;

                        pass.UseTexture(data.src, AccessFlags.Read);
                        pass.SetRenderAttachment(data.dst, 0);

                        pass.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                        {
                            Blitter.BlitTexture(ctx.cmd, d.src, kIdentity, 0f, true);
                        });
                    }

                    for (int i = 0; i < settings.blurIterations; i++)
                    {
                        float ofs = settings.blurOffset * (i + 1);

                        using (var pass = rg.AddRasterRenderPass<PassData>($"LG Blur {i}", out var data))
                        {
                            data.src = ping;
                            data.dst = pong;
                            data.mat = blurMat;
                            data.offset = ofs;

                            pass.UseTexture(data.src, AccessFlags.Read);
                            pass.SetRenderAttachment(data.dst, 0);

                            pass.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                            {
                                d.mat.SetFloat(OffsetID, d.offset);
                                Blitter.BlitTexture(ctx.cmd, d.src, kIdentity, d.mat, 0);
                            });
                        }

                        using (var pass = rg.AddRasterRenderPass<PassData>($"LG BlurV {i}", out var data))
                        {
                            data.src = pong;
                            data.dst = ping;
                            data.mat = blurMat;
                            data.offset = ofs;

                            pass.UseTexture(data.src, AccessFlags.Read);
                            pass.SetRenderAttachment(data.dst, 0);
                            
                            if (i == settings.blurIterations - 1)
                                pass.AllowPassCulling(false);

                            pass.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                            {
                                Blitter.BlitTexture(ctx.cmd, d.src, kIdentity, d.mat, 1);
                            });
                        }
                    }

                    finalBlurTexture = ping;
                }

                using (var pass = rg.AddRasterRenderPass<PassData>("LG SetGlobals", out var data))
                {
                    data.src = finalBlurTexture;
                    data.cameraColor = temp;

                    pass.UseTexture(data.src, AccessFlags.Read);
                    pass.UseTexture(data.cameraColor, AccessFlags.Read);

                    pass.AllowGlobalStateModification(true);
                    pass.AllowPassCulling(false);

                    pass.SetRenderFunc((PassData d, RasterGraphContext ctx) =>
                    {
                        ctx.cmd.SetGlobalTexture(LiquidGlassCapture.globalTexName, d.cameraColor);
                        ctx.cmd.SetGlobalTexture(LiquidGlassCapture.globalBlurTexName, d.src);
                    });
                }
            }

            public void Dispose()
            {
                if (blurMat != null)
                {
                    CoreUtils.Destroy(blurMat);
                }
            }
        }
#else
        class BlurRenderPassLegacy : ScriptableRenderPass
        {
            private LiquidGlassSettings settings;
            private RTHandle tempTexture, pingRT, pongRT;
            private Material blurMat;
            
            static readonly int OffsetID = Shader.PropertyToID("_Offset");

            public BlurRenderPassLegacy(LiquidGlassSettings settings)
            {
                this.settings = settings;
                renderPassEvent = settings.injectionPoint;

                Shader.DisableKeyword("LLG_USE_URP");
                Shader blurShader = Shader.Find("Hidden/Lith/Blur");
                if (blurShader != null)
                    blurMat = CoreUtils.CreateEngineMaterial(blurShader);
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
#if UNITY_EDITOR
                if (renderingData.cameraData.camera.cameraType == CameraType.SceneView)
                    return;
#endif
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                
                var currWidth = desc.width;
                var currHeight = desc.height;

                desc.width = currWidth / settings.downsample;
                desc.height = currHeight / settings.downsample;

                RenderingUtils.ReAllocateIfNeeded(ref tempTexture, desc, FilterMode.Bilinear, name: "_LGTemp");
                
                desc.width = currWidth / settings.blurDownsample;
                desc.height = currHeight / settings.blurDownsample;
                
                if (settings.blurIterations > 0)
                {
                    RenderingUtils.ReAllocateIfNeeded(ref pingRT, desc, FilterMode.Bilinear, name: "_LGPing");
                    RenderingUtils.ReAllocateIfNeeded(ref pongRT, desc, FilterMode.Bilinear, name: "_LGPong");
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
#if UNITY_EDITOR
                if (renderingData.cameraData.camera.cameraType == CameraType.SceneView)
                    return;
#endif
                if (tempTexture == null) return;

                var cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
                CommandBuffer cmd = CommandBufferPool.Get("LG Blur");

                cmd.Blit(cameraColorTarget.nameID, tempTexture.nameID);

                RenderTargetIdentifier finalBlurTexture = tempTexture.nameID;

                if (blurMat != null && settings.blurIterations > 0 && pingRT != null && pongRT != null)
                {
                    cmd.Blit(tempTexture.nameID, pingRT.nameID);

                    for (int i = 0; i < settings.blurIterations; i++)
                    {
                        float currentOffset = settings.blurOffset * (i + 1);
                        blurMat.SetFloat(OffsetID, currentOffset);

                        cmd.Blit(pingRT.nameID, pongRT.nameID, blurMat, 0);
                        cmd.Blit(pongRT.nameID, pingRT.nameID, blurMat, 1);
                    }

                    finalBlurTexture = pingRT.nameID;
                }

                cmd.SetGlobalTexture(LiquidGlassCapture.globalTexName, tempTexture.nameID);
                cmd.SetGlobalTexture(LiquidGlassCapture.globalBlurTexName, finalBlurTexture);

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                LiquidGlassCapture.OnRenderFeatureExecute(context, ref renderingData);
            }

            public void Dispose()
            {
                tempTexture?.Release();
                pingRT?.Release();
                pongRT?.Release();

                if (blurMat != null)
                {
                    CoreUtils.Destroy(blurMat);
                    blurMat = null;
                }
            }
        }
#endif

        [SerializeField] LiquidGlassSettings settings = new LiquidGlassSettings();

#if UNITY_6000_0_OR_NEWER
        BlurRenderPass pass;
#else
        BlurRenderPassLegacy pass;
#endif

        public override void Create()
        {
#if UNITY_6000_0_OR_NEWER
            pass = new BlurRenderPass(settings);
#else
            pass = new BlurRenderPassLegacy(settings);
#endif
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
        {
            if (pass != null)
                renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                pass?.Dispose();
                pass = null;
            }
        }
    }
}
#endif
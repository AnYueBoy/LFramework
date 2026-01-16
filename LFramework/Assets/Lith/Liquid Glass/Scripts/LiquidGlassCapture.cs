using UnityEngine;
using System.Collections.Generic;


#if LLG_USE_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Linq;

#elif LLG_USE_HDRP
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Lith.LiquidGlass
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class LiquidGlassCapture : MonoBehaviour
    {
        public const string globalTexName = "_LithLiquidGlassExternalTex";
        public const string materialTexName = "_ExternalTex";
        public const string globalBlurTexName = "_LithLiquidGlassExternalBlurTex";
        public const string materialBlurTexName = "_ExternalBlurTex";
        public const string matOffsetName = "_Offset";
#if LLG_USE_URP
    public const RenderPipeline renderPipeline = RenderPipeline.URP;
#elif LLG_USE_HDRP
    public const RenderPipeline renderPipeline = RenderPipeline.HDRP;
#else
        public const RenderPipeline renderPipeline = RenderPipeline.Default;
#endif

        public enum SourceMode { Camera, RenderTexture }
        public enum RenderPipeline { Default, URP, HDRP }

        [Header("Source")]
        public SourceMode mode = SourceMode.Camera;
        public RenderTexture sourceRT; // sadece mode = RenderTexture iken

        [Header("Material Settings")]
        [Tooltip("Optional target material. Created image will be applied to selected material only")]
        public List<Material> targetMaterials;

        [Header("Downsampling")]
        [Range(1, 8)]
        public int downsample = 1;

        [Tooltip("Optional target RT (Camera mode). Leave empty = auto-create")]
        public RenderTexture targetRT;

        [Range(1, 16)]
        [Tooltip("Blur downsample factor: 1 = full res, 2 = half, etc.")]
        public int blurDownsample = 16;

        [Range(0, 6)]
        [Tooltip("Number of blur iterations")]
        public int blurIterations = 2;

        [Range(0, 4)]
        [Tooltip("Blur offset step size")]
        public float blurOffset = 0.41f;

        [Tooltip("Optional blur target RT (Camera mode). Leave empty = auto-create")]
        public RenderTexture blurRT;
        private RenderTexture pongRT;
        private Material blurMat;

        private RenderTexture downsampledRT;
        private List<Material> lastUsedMaterials = new List<Material>();
        private Camera cam;

#if LLG_USE_URP || LLG_USE_HDRP
    private static List<LiquidGlassCapture> capturerList = new List<LiquidGlassCapture>();
#endif

        void Awake()
        {
            if (mode == SourceMode.Camera)
            {
                if (cam == null)
                {
                    cam = GetComponent<Camera>();

                    if (cam == null)
                        Debug.LogError("LiquidGlassCapture in Camera mode requires a Camera component.");
                }
            }
        }

        void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += EditorClean;
#else
            Clean();
#endif

            Shader blurShader = Shader.Find("Hidden/Lith/Blur");

#if LLG_USE_URP || LLG_USE_HDRP
        if (blurShader != null)
            blurMat = CoreUtils.CreateEngineMaterial(blurShader);

        if (mode == SourceMode.Camera && !capturerList.Contains(this))
            capturerList.Add(this);
#else
            Shader.DisableKeyword("LLG_USE_URP");
            if (blurShader != null)
            {
                blurMat = new Material(blurShader);
                blurMat.hideFlags = HideFlags.HideAndDontSave;
            }
#endif
        }

        void OnValidate()
        {
            Clean();
        }
#if UNITY_EDITOR
        void Update()
        {
#if LLG_USE_URP || LLG_USE_HDRP
        if (mode == SourceMode.Camera && !capturerList.Contains(this))
            capturerList.Add(this);
        else if (mode != SourceMode.Camera && capturerList.Contains(this))
            capturerList.Remove(this);
#endif
        }
#endif
        void LateUpdate()
        {
            if (mode == SourceMode.RenderTexture)
                HandleExternalRT();
        }
        void HandleExternalRT()
        {
            if (sourceRT == null)
            {
                PushTexture(null, globalTexName, materialTexName);
                return;
            }

            int w = Mathf.Max(1, sourceRT.width / downsample);
            int h = Mathf.Max(1, sourceRT.height / downsample);

            BlitTo(ref sourceRT, ref downsampledRT, w, h, "ExternalTexRT (External)");
            PushTexture(downsampledRT, globalTexName, materialTexName);

            w = Mathf.Max(1, sourceRT.width / blurDownsample);
            h = Mathf.Max(1, sourceRT.height / blurDownsample);

            if (blurMat != null && blurIterations > 0)
                blurRT = CalculateBlur(ref sourceRT, w, h);
            else
            {
                if (blurRT != null) blurRT.Release();
                blurRT = targetRT;
            }

            PushTexture(blurRT, globalBlurTexName, materialBlurTexName);
        }
        void BlitTo(ref RenderTexture src, ref RenderTexture targetRT, int w, int h, string rtName = "")
        {
            if (targetRT == null || targetRT.width != w || targetRT.height != h)
            {
                if (targetRT != null) targetRT.Release();
                targetRT = new RenderTexture(w, h, 0);
                targetRT.name = rtName;
            }

            Graphics.Blit(src, targetRT);
        }
        RenderTexture CalculateBlur(ref RenderTexture src, int w, int h)
        {
            if (blurRT == null || blurRT.width != w || blurRT.height != h)
            {
                if (blurRT != null) blurRT.Release();
                blurRT = new RenderTexture(w, h, 0);
                blurRT.name = "ExternalBlurRT (Camera)";
            }

            if (pongRT == null || pongRT.width != w || pongRT.height != h)
            {
                if (pongRT != null) pongRT.Release();
                pongRT = new RenderTexture(w, h, 0);
                pongRT.name = "ExternalTexPongRT (Camera)";
            }

            Graphics.Blit(src, blurRT);

            for (int i = 0; i < blurIterations; i++)
            {
                float currentOffset = blurOffset * (i + 1);
                blurMat.SetFloat(matOffsetName, currentOffset);
                // Horizontal pass (Pass 0)
                Graphics.Blit(blurRT, pongRT, blurMat, 0);
                // Vertical pass (Pass 1) 
                Graphics.Blit(pongRT, blurRT, blurMat, 1);
            }

            return blurRT;
        }
#if LLG_USE_URP
    public static void OnRenderFeatureExecute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        for (int i = 0; i < capturerList.Count; i++)
            capturerList[i].OnRenderImageURP(context, ref renderingData);
    }
    private void OnRenderImageURP(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (cam == null || cam != renderingData.cameraData.camera)
            return;

        var cmd = CommandBufferPool.Get("LiquidGlassCapture");

        var colorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;

        int w = Mathf.Max(1, renderingData.cameraData.camera.pixelWidth / downsample);
        int h = Mathf.Max(1, renderingData.cameraData.camera.pixelHeight / downsample);

        if (targetRT == null || targetRT.width != w || targetRT.height != h)
        {
            if (targetRT != null) targetRT.Release();
            targetRT = new RenderTexture(w, h, 0);
            targetRT.name = "ExternalTexRT (URP)";
        }

        cmd.Blit(colorTarget, targetRT);
        PushTexture(targetRT, globalTexName, materialTexName);

        w = Mathf.Max(1, renderingData.cameraData.camera.pixelWidth / blurDownsample);
        h = Mathf.Max(1, renderingData.cameraData.camera.pixelHeight / blurDownsample);

        if (blurMat != null && blurIterations > 0)
            blurRT = CalculateBlur(cmd, colorTarget, w, h);
        else
            blurRT = targetRT;
        
        PushTexture(blurRT, globalBlurTexName, materialBlurTexName);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    RenderTexture CalculateBlur(CommandBuffer cmd, RTHandle src, int w, int h)
    {
        if (blurRT == null || blurRT.width != w || blurRT.height != h)
        {
            if (blurRT != null) blurRT.Release();
            blurRT = new RenderTexture(w, h, 0);
            blurRT.name = "ExternalTexBlurRT (Camera)";
        }

        if (pongRT == null || pongRT.width != w || pongRT.height != h)
        {
            if (pongRT != null) pongRT.Release();
            pongRT = new RenderTexture(w, h, 0);
            pongRT.name = "ExternalTexPongRT (Camera)";
        }

        cmd.Blit(src, blurRT);

        for (int i = 0; i < blurIterations; i++)
        {
            float currentOffset = blurOffset * (i + 1);
            blurMat.SetFloat(matOffsetName, currentOffset);
            // Horizontal pass (Pass 0)
            cmd.Blit(blurRT, pongRT, blurMat, 0);
            // Vertical pass (Pass 1) 
            cmd.Blit(pongRT, blurRT, blurMat, 1);
        }

        return blurRT;
    }
#elif LLG_USE_HDRP
    public static void OnRenderFeatureExecute(CustomPassContext ctx)
    {
        for (int i = 0; i < capturerList.Count; i++)
            capturerList[i].OnRenderImageHDRP(ctx);
    }
    private RTHandle targetRTHandle;
    private void OnRenderImageHDRP(CustomPassContext ctx)
    {
        if (cam == null || cam != ctx.hdCamera.camera)
            return;

        var desc = ctx.cameraColorBuffer.rt.descriptor;

        int w = Mathf.Max(1, desc.width / downsample);
        int h = Mathf.Max(1, desc.height / downsample);

        if (targetRT == null || targetRT.width != w || targetRT.height != h)
        {
            if (targetRT != null) targetRT.Release();
            targetRT = new RenderTexture(w, h, 0);
            targetRT.name = "ExternalTexRT (URP)";
        }

        if (targetRTHandle == null)
            targetRTHandle = RTHandles.Alloc(targetRT);

        var cmd = ctx.cmd;
        Blitter.BlitCameraTexture(cmd, ctx.cameraColorBuffer, targetRTHandle);

        PushTexture(targetRT, globalTexName, materialTexName);

        w = Mathf.Max(1, desc.width / blurDownsample);
        h = Mathf.Max(1, desc.height / blurDownsample);

        if (blurMat != null && blurIterations > 0)
            blurRT = CalculateBlur(cmd, targetRTHandle, w, h);
        else
            blurRT = targetRT;

        PushTexture(blurRT, globalBlurTexName, materialBlurTexName);
    }
    RenderTexture CalculateBlur(CommandBuffer cmd, RTHandle src, int w, int h)
    {
        if (blurRT == null || blurRT.width != w || blurRT.height != h)
        {
            if (blurRT != null) blurRT.Release();
            blurRT = new RenderTexture(w, h, 0);
            blurRT.name = "ExternalTexBlurRT (Camera)";
        }

        if (pongRT == null || pongRT.width != w || pongRT.height != h)
        {
            if (pongRT != null) pongRT.Release();
            pongRT = new RenderTexture(w, h, 0);
            pongRT.name = "ExternalTexPongRT (Camera)";
        }

        cmd.Blit(src, blurRT);

        for (int i = 0; i < blurIterations; i++)
        {
            float currentOffset = blurOffset * (i + 1);
            blurMat.SetFloat(matOffsetName, currentOffset);
            // Horizontal pass (Pass 0)
            cmd.Blit(blurRT, pongRT, blurMat, 0);
            // Vertical pass (Pass 1) 
            cmd.Blit(pongRT, blurRT, blurMat, 1);
        }

        return blurRT;
    }
#else
        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mode == SourceMode.Camera)
            {
                HandleCameraRT(src);
                Graphics.Blit(src, dst);
            }
            else
            {
                Graphics.Blit(src, dst);
            }
        }
        void HandleCameraRT(RenderTexture src)
        {
            int w = Mathf.Max(1, src.width / downsample);
            int h = Mathf.Max(1, src.height / downsample);

            BlitTo(ref src, ref targetRT, w, h, "ExternalTexRT (Camera)");
            PushTexture(targetRT, globalTexName, materialTexName);

            w = Mathf.Max(1, src.width / blurDownsample);
            h = Mathf.Max(1, src.height / blurDownsample);

            if (blurMat != null && blurIterations > 0)
                blurRT = CalculateBlur(ref src, w, h);
            else
                blurRT = targetRT;

            PushTexture(blurRT, globalBlurTexName, materialBlurTexName);
        }
#endif
        void PushTexture(RenderTexture rt, string globalTexName, string materialTexName)
        {
            if (targetMaterials == null || targetMaterials.Count == 0)
            {
                Shader.SetGlobalTexture(globalTexName, rt);

                if (lastUsedMaterials != null)
                {
                    for (int i = 0; i < lastUsedMaterials.Count; i++)
                    {
                        if (lastUsedMaterials[i] != null)
                            ClearKeyword(lastUsedMaterials[i]);
                    }

                    lastUsedMaterials.Clear();
                }
            }
            else
            {
                if (lastUsedMaterials.Count == 0)
                {
                    for (int i = 0; i < targetMaterials.Count; i++)
                    {
                        if (targetMaterials[i] != null)
                        {
                            AddKeyword(targetMaterials[i]);
                            targetMaterials[i].SetTexture(materialTexName, rt);
                            lastUsedMaterials.Add(targetMaterials[i]);
                        }
                    }
                }
                else
                {
                    var lastUsedMaterialsIndex = 0;
                    while (lastUsedMaterials.Count > lastUsedMaterialsIndex)
                    {
                        if (lastUsedMaterials[lastUsedMaterialsIndex] != null && !targetMaterials.Contains(lastUsedMaterials[lastUsedMaterialsIndex]))
                        {
                            ClearKeyword(lastUsedMaterials[lastUsedMaterialsIndex]);
                            lastUsedMaterials.RemoveAt(lastUsedMaterialsIndex);
                        }
                        else lastUsedMaterialsIndex++;
                    }

                    for (int i = 0; i < targetMaterials.Count; i++)
                    {
                        if (targetMaterials[i] != null)
                        {
                            if (!lastUsedMaterials.Contains(targetMaterials[i]))
                            {
                                AddKeyword(targetMaterials[i]);
                                targetMaterials[i].SetTexture(materialTexName, rt);
                                lastUsedMaterials.Add(targetMaterials[i]);
                            }
                            else
                            {
                                targetMaterials[i].SetTexture(materialTexName, rt);
                            }
                        }
                    }
                }
            }
        }

        void OnDisable()
        {
            PushTexture(null, globalTexName, materialTexName);
            Clean();

#if LLG_USE_URP || LLG_USE_HDRP
        if (capturerList.Contains(this))
            capturerList.Remove(this);
#endif
        }

        void OnDestroy()
        {
            Clean();
        }

        private void Clean()
        {
            if (downsampledRT != null) { downsampledRT.Release(); downsampledRT = null; }
            if (lastUsedMaterials != null)
            {
                for (int i = 0; i < lastUsedMaterials.Count; i++)
                {
                    if (lastUsedMaterials[i] != null)
                        ClearKeyword(lastUsedMaterials[i]);
                }

                lastUsedMaterials.Clear();
            }
            if (targetRT != null) { targetRT.Release(); targetRT = null; }
            if (blurRT != null) { blurRT.Release(); blurRT = null; }
            if (pongRT != null) { pongRT.Release(); pongRT = null; }
#if LLG_USE_HDRP
        if (targetRTHandle != null) { targetRTHandle.Release(); targetRTHandle = null; }
#endif
        }

#if UNITY_EDITOR
        void EditorClean()
        {
            if (this == null) return;
            Clean();
            EditorApplication.delayCall -= EditorClean;
        }
#endif

        void ClearKeyword(Material m) => m.DisableKeyword("USE_TEXTURE_PROPERTY");
        void AddKeyword(Material m) => m.EnableKeyword("USE_TEXTURE_PROPERTY");
    }
}
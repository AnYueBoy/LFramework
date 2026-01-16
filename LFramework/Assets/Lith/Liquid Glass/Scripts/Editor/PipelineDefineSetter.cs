using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;

namespace Lith.LiquidGlass
{
    [InitializeOnLoad]
    public static class PipelineDefineSetter
    {
        const string urpDefine = "LLG_USE_URP";
        const string hdrpDefine = "LLG_USE_HDRP";
        private static string lastPipelineType = "";

        static PipelineDefineSetter()
        {
            UpdateDefines();
            EditorApplication.update += CheckPipelineChange;
        }

        private static void CheckPipelineChange()
        {
            var asset = GraphicsSettings.defaultRenderPipeline;
            string type = asset ? asset.GetType().ToString() : "Builtin";

            if (type != lastPipelineType)
            {
                lastPipelineType = type;
                UpdateDefines();
            }
        }

        [MenuItem("Tools/Lith/Update RenderPipeline Defines")]
        public static void UpdateDefines()
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

            bool hasURP = false;
            bool hasHDRP = false;

            var asset = GraphicsSettings.defaultRenderPipeline;
            if (asset != null)
            {
                var type = asset.GetType().ToString();
                if (type.Contains("UniversalRenderPipelineAsset"))
                    hasURP = true;
                else if (type.Contains("HDRenderPipelineAsset"))
                    hasHDRP = true;
            }

            defines = defines.Replace(urpDefine, "")
                             .Replace(hdrpDefine, "")
                             .Replace(";;", ";")
                             .Trim(';', ' ');

            if (hasURP)
                defines = AppendDefine(defines, urpDefine);
            else if (hasHDRP)
                defines = AppendDefine(defines, hdrpDefine);

            PlayerSettings.SetScriptingDefineSymbols(namedTarget, defines);
        }

        private static string AppendDefine(string current, string define)
        {
            if (string.IsNullOrEmpty(current))
                return define;
            if (!current.Contains(define))
                return current + ";" + define;
            return current;
        }
    }
}
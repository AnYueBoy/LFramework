#if LLG_USE_HDRP
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Lith.LiquidGlass
{
    [InitializeOnLoad]
    public static class LiquidGlassHDRPInstaller
    {
        static LiquidGlassHDRPInstaller()
        {
            EditorApplication.delayCall += TryInstall;
            EditorSceneManager.sceneOpened += (scene, mode) => TryInstall();
        }

        private static void TryInstall()
        {
            var volume = Object.FindFirstObjectByType<CustomPassVolume>();
            if (volume != null && volume.customPasses.Exists(p => p is LiquidGlassCustomPass))
                return;

            if (!LiquidGlassEditorSettings.instance.hdrpAsked)
            {
                bool add = EditorUtility.DisplayDialog(
                    "LiquidGlass (HDRP)",
                    "LiquidGlass effect can be added automatically to your scene as a CustomPassVolume.\n\nDo you want to enable it?",
                    "Yes, enable",
                    "No"
                );

                if (!add)
                {
                    LiquidGlassEditorSettings.instance.hdrpAsked = true;
                    LiquidGlassEditorSettings.instance.Save();
                    return;
                }
            }

            EnsureCustomPass();
        }

        private static void EnsureCustomPass()
        {
            var volume = Object.FindFirstObjectByType<CustomPassVolume>();
            if (volume == null)
            {
                var go = new GameObject("LiquidGlassCustomPass");
                volume = go.AddComponent<CustomPassVolume>();
                volume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
                volume.isGlobal = true;

                EditorSceneManager.MarkSceneDirty(volume.gameObject.scene);
            }

            if (volume.customPasses.Exists(p => p is LiquidGlassCustomPass))
                return;

            var pass = new LiquidGlassCustomPass();
            volume.customPasses.Add(pass);

            EditorUtility.SetDirty(volume);
        }
    }
}
#endif
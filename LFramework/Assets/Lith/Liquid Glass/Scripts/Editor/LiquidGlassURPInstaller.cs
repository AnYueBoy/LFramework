#if LLG_USE_URP
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Lith.LiquidGlass
{
    [InitializeOnLoad]
    public static class LiquidGlassURPInstaller
    {
        static LiquidGlassURPInstaller()
        {
            EditorApplication.delayCall += TryInstall;
            EditorSceneManager.sceneOpened += (scene, mode) => TryInstall();
        }

        private static void TryInstall()
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null) return;

            var renderers = GetRendererDatas(urpAsset);
            if (renderers == null || renderers.Length == 0) return;

            int defIndex = GetDefaultRendererIndex(urpAsset);
            if (defIndex < 0 || defIndex >= renderers.Length) defIndex = 0;

            var rendererData = renderers[defIndex];
            if (rendererData == null) return;

            if (RendererHasFeature(rendererData)) return;

            if (LiquidGlassEditorSettings.instance.urpAsked) return;
            
            bool add = EditorUtility.DisplayDialog(
                "LiquidGlass (URP)",
                "LiquidGlass effect can be added automatically as a ScriptableRendererFeature to your active URP renderer.\n\nDo you want to add it now?",
                "Yes, add",
                "No"
            );

            if (!add)
            {
                LiquidGlassEditorSettings.instance.urpAsked = true;
                LiquidGlassEditorSettings.instance.Save();
                return;
            }

            AddFeature(rendererData);
            Debug.Log("<color=cyan>[LiquidGlass]</color> URP feature eklendi: " + rendererData.name);
        }

        private static ScriptableRendererData[] GetRendererDatas(UniversalRenderPipelineAsset asset)
        {
            var so = new SerializedObject(asset);
            var listProp = so.FindProperty("m_RendererDataList");
            if (listProp != null && listProp.isArray && listProp.arraySize > 0)
            {
                var arr = new ScriptableRendererData[listProp.arraySize];
                for (int i = 0; i < listProp.arraySize; i++)
                    arr[i] = listProp.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererData;
                return arr;
            }

            var singleProp = so.FindProperty("m_RendererData");
            if (singleProp != null && singleProp.objectReferenceValue != null)
                return new[] { singleProp.objectReferenceValue as ScriptableRendererData };

            var pi = asset.GetType().GetProperty("scriptableRendererData",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (pi != null)
            {
                var rd = pi.GetValue(asset) as ScriptableRendererData;
                if (rd != null) return new[] { rd };
            }
            return System.Array.Empty<ScriptableRendererData>();
        }

        private static int GetDefaultRendererIndex(UniversalRenderPipelineAsset asset)
        {
            var so = new SerializedObject(asset);
            var idx = so.FindProperty("m_DefaultRendererIndex");
            return idx != null ? idx.intValue : 0;
        }

        private static bool RendererHasFeature(ScriptableRendererData rd)
        {
            if (rd == null) return true;

            var list = GetFeatureListIfAvailable(rd);
            if (list != null)
            {
                foreach (var f in list) if (f is LiquidGlassRenderFeature) return true;
            }

            var so = new SerializedObject(rd);
            var featProp = so.FindProperty("m_RendererFeatures");
            if (featProp != null && featProp.isArray)
            {
                for (int i = 0; i < featProp.arraySize; i++)
                {
                    var el = featProp.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
                    if (el is LiquidGlassRenderFeature) return true;
                }
            }
            return false;
        }

        private static void AddFeature(ScriptableRendererData rd)
        {
            var feature = ScriptableObject.CreateInstance<LiquidGlassRenderFeature>();
            feature.name = "LiquidGlassFeature";

            AssetDatabase.AddObjectToAsset(feature, rd);
            AssetDatabase.SaveAssets();

            var list = GetFeatureListIfAvailable(rd);
            if (list != null)
            {
                list.Add(feature);
                EditorUtility.SetDirty(rd);
                return;
            }

            var so = new SerializedObject(rd);
            var featProp = so.FindProperty("m_RendererFeatures");
            if (featProp != null && featProp.isArray)
            {
                featProp.arraySize++;
                featProp.GetArrayElementAtIndex(featProp.arraySize - 1).objectReferenceValue = feature;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(rd);
            }
            else
            {
                Debug.LogWarning("[LiquidGlass] Renderer features listesine erişilemedi. URP sürümü farklı olabilir.");
            }
        }

        private static List<ScriptableRendererFeature> GetFeatureListIfAvailable(ScriptableRendererData rd)
        {
            var fi = typeof(ScriptableRendererData).GetField("rendererFeatures",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return fi?.GetValue(rd) as List<ScriptableRendererFeature>;
        }
    }
}
#endif
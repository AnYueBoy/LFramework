using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LFramework
{
    public static class BindUIProjectSetting
    {
        [SettingsProvider]
        public static SettingsProvider BindUISettingProvider()
        {
            var provider = new SettingsProvider("Project/BindUISetting", SettingsScope.Project)
            {
                label = "Bind UI Setting",
                guiHandler = (searchContext) =>
                {
                    SerializedObject setting = new SerializedObject(BindUIGenerateConfig.GetSerializedObject());
                    EditorGUILayout.PropertyField(setting.FindProperty("codeGeneratePath"),
                        new GUIContent("代码生成路径"));
                    setting.ApplyModifiedPropertiesWithoutUndo();
                },
                keywords = new[] { "Bind UI Setting" }
            };
            return provider;
        }
    }

    public class BindUIGenerateConfig : ScriptableObject
    {
        public string codeGeneratePath;

        private static BindUIGenerateConfig configInstance;

        public static BindUIGenerateConfig GetSerializedObject()
        {
            if (configInstance == null)
            {
                var tempInstance = CreateInstance<BindUIGenerateConfig>();
                var script = MonoScript.FromScriptableObject(tempInstance);
                string path = AssetDatabase.GetAssetPath(script);
                var subPath = path.Substring(0, path.LastIndexOf('/'));
                subPath += "/UIBindPathSettings.asset";
                configInstance = AssetDatabase.LoadAssetAtPath<BindUIGenerateConfig>(subPath);
                if (configInstance == null)
                {
                    AssetDatabase.CreateAsset(tempInstance, subPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                else
                {
                    DestroyImmediate(tempInstance);
                }
            }

            return configInstance;
        }
    }
}
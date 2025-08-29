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
                        new GUIContent("Code Generate Path"));
                    setting.ApplyModifiedPropertiesWithoutUndo();
                },
                keywords = new[] { "Bind UI Setting" }
            };
            return provider;
        }
    }

    public class BindUIGenerateConfig : ScriptableObject
    {
        [Header("代码生成路径")] public string codeGeneratePath;

        private static BindUIGenerateConfig configInstance;

        public static BindUIGenerateConfig GetSerializedObject()
        {
            return FindObjectOfType<BindUIGenerateConfig>();
        }
    }
}
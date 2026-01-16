using UnityEngine;
using UnityEditor;

namespace Lith.LiquidGlass
{
    [CustomEditor(typeof(LiquidGlassCapture))]
    public class LiquidGlassCaptureEditor : Editor
    {
        SerializedProperty mode;
        SerializedProperty sourceRT;
        SerializedProperty targetMaterials;
        SerializedProperty downsample;
        SerializedProperty targetRT;
        SerializedProperty blurDownsample;
        SerializedProperty blurIterations;
        SerializedProperty blurOffset;
        SerializedProperty blurRT;

        void OnEnable()
        {
            if (target == null) return;

            if (serializedObject != null)
            {
                mode = serializedObject.FindProperty("mode");
                sourceRT = serializedObject.FindProperty("sourceRT");
                targetMaterials = serializedObject.FindProperty("targetMaterials");
                downsample = serializedObject.FindProperty("downsample");
                targetRT = serializedObject.FindProperty("targetRT");
                blurDownsample = serializedObject.FindProperty("blurDownsample");
                blurIterations = serializedObject.FindProperty("blurIterations");
                blurOffset = serializedObject.FindProperty("blurOffset");
                blurRT = serializedObject.FindProperty("blurRT");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(mode);

            if ((LiquidGlassCapture.SourceMode)mode.enumValueIndex == LiquidGlassCapture.SourceMode.RenderTexture)
            {
                EditorGUILayout.PropertyField(sourceRT);
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(targetMaterials, true);

            if (targetMaterials.arraySize > 0 || (targetMaterials.arraySize == 0 && LiquidGlassCapture.renderPipeline == LiquidGlassCapture.RenderPipeline.Default))
            {
                EditorGUILayout.PropertyField(downsample);
                EditorGUILayout.PropertyField(targetRT);

                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(blurDownsample);
                EditorGUILayout.PropertyField(blurIterations);
                EditorGUILayout.PropertyField(blurOffset);
                EditorGUILayout.PropertyField(blurRT);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
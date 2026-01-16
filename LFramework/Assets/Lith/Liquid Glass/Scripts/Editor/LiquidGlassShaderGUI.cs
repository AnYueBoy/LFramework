using UnityEngine;
using UnityEditor;
using static Lith.LiquidGlass.NormalMapBaker;
using System.IO;
using UnityEngine.UI;

namespace Lith.LiquidGlass
{
    public class LiquidGlassShaderGUI : ShaderGUI
    {
        private static readonly string KW_USE_DISTORTION = "USE_DISTORTION";
        private static readonly string KW_USE_SATURATION = "USE_SATURATION";
        private static readonly string KW_USE_GLOSS = "USE_GLOSS";

        NormalMapBakerSettings normalMapBakerSettings;
        MaterialEditor materialEditor;
        MaterialProperty normalMapProperty;
        Image uiImage;
        RawImage uiRawImage;
        bool texturesChecked;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            this.materialEditor = materialEditor;
            Material targetMat = materialEditor.target as Material;

            normalMapProperty = FindProperty("_NormalMap", props);

            // Distortion / Edge
            MaterialProperty edgeWidthPx = FindProperty("_EdgeWidthPx", props);
            MaterialProperty edgeCurve = FindProperty("_EdgeCurve", props);
            MaterialProperty distortionIn = FindProperty("_DistortionInnerPx", props);
            MaterialProperty distortionOut = FindProperty("_DistortionEdgePx", props);
            MaterialProperty chromatic = FindProperty("_ChromaticAmount", props);
            MaterialProperty globalBlur = FindProperty("_GlobalBlurPercent", props);

            // Color / Post
            MaterialProperty saturation = FindProperty("_Saturation", props);
            MaterialProperty tintIntensity = FindProperty("_TintIntensity", props);

            // Gloss
            MaterialProperty glossIntensity = FindProperty("_GlossIntensity", props);
            MaterialProperty glossWidth = FindProperty("_GlossWidth", props);

            EditorGUILayout.LabelField("Blur & Color Tint", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(globalBlur, "Global Blur %");
            materialEditor.ShaderProperty(tintIntensity, "Tint Intensity");

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            bool useDistortion = targetMat.IsKeywordEnabled(KW_USE_DISTORTION);
            bool useSaturation = targetMat.IsKeywordEnabled(KW_USE_SATURATION);
            bool useGloss = targetMat.IsKeywordEnabled(KW_USE_GLOSS);

            EditorGUI.BeginChangeCheck();

            useDistortion = BoldToggle("Use Distortion", useDistortion);

            if (useDistortion)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Normal Map"), normalMapProperty);

                EditorGUILayout.Space();

                materialEditor.ShaderProperty(edgeWidthPx, "Edge Width (px)");
                materialEditor.ShaderProperty(edgeCurve, "Edge Curve");
                materialEditor.ShaderProperty(distortionIn, "Distortion Inner (px)");
                materialEditor.ShaderProperty(distortionOut, "Distortion Edge (px)");
                materialEditor.ShaderProperty(chromatic, "Chromatic Aberration");

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            useSaturation = BoldToggle("Use Saturation", useSaturation);

            if (useSaturation)
            {
                materialEditor.ShaderProperty(saturation, "Saturation");
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            useGloss = BoldToggle("Use Gloss", useGloss);

            if (useGloss)
            {
                materialEditor.ShaderProperty(glossIntensity, "Gloss Intensity");
                materialEditor.ShaderProperty(glossWidth, "Gloss Width");
                EditorGUILayout.Space();
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var t in materialEditor.targets)
                {
                    var mat = (Material)t;

                    if (useDistortion)
                        mat.EnableKeyword(KW_USE_DISTORTION);
                    else
                        mat.DisableKeyword(KW_USE_DISTORTION);

                    if (useSaturation)
                        mat.EnableKeyword(KW_USE_SATURATION);
                    else
                        mat.DisableKeyword(KW_USE_SATURATION);

                    if (useGloss)
                        mat.EnableKeyword(KW_USE_GLOSS);
                    else
                        mat.DisableKeyword(KW_USE_GLOSS);

                    EditorUtility.SetDirty(mat);
                }
            }

            EditorGUILayout.Space();

            // NORMAL MAP BAKER

            if (useDistortion)
            {
                EditorGUILayout.LabelField("Normal Map & Mask (Adjustable only in Editor)", EditorStyles.boldLabel);

                EditorGUI.BeginChangeCheck();

                var go = Selection.activeGameObject;
                var rt = go ? go.GetComponent<RectTransform>() : null;
                uiImage = go ? go.GetComponent<Image>() : null;
                uiRawImage = go ? go.GetComponent<RawImage>() : null;

                if (!MaterialUserDataStore.TryLoad(targetMat, out normalMapBakerSettings))
                {
                    if (rt == null)
                    {
                        normalMapBakerSettings = new NormalMapBakerSettings
                        {
                            mode = Mode.ProceduralRoundedRectCustom,
                            width = 1024,
                            height = 1024,
                            normalScaleFactor = 1f,
                            intensity = 16,
                            edgeWidthPx = 32,
                            edgeCurve = 4,
                            cornerRadiusPx = 128,
                            enableTop = true,
                            enableBottom = true,
                            enableRight = true,
                            enableLeft = true,
                            maskMode = MaskMode.SoftGrayscale,
                            maskGamma = 10f,
                            maskTextureType = MaskTextureType.Sprite,
                            maskScaleFactor = 1f,
                            outlineThicknessPx = 1f,
                            bakeAuto = true,
                            lastSaveDir = Path.Combine(AssetDatabase.GetAssetPath(targetMat), targetMat.name + "_textures"),
                            lastSaveName = targetMat.name
                        };
                    }
                    else
                    {
                        normalMapBakerSettings = new NormalMapBakerSettings
                        {
                            mode = Mode.ProceduralRoundedRectFromRectTransform,
                            width = (int)rt.rect.width,
                            height = (int)rt.rect.height,
                            normalScaleFactor = 1f,
                            intensity = 16,
                            edgeWidthPx = 32,
                            edgeCurve = 4,
                            cornerRadiusPx = (int)(rt.rect.width / 8f),
                            enableTop = true,
                            enableBottom = true,
                            enableRight = true,
                            enableLeft = true,
                            maskMode = MaskMode.SoftGrayscale,
                            maskGamma = 10f,
                            maskTextureType = MaskTextureType.Sprite,
                            maskScaleFactor = 1f,
                            outlineThicknessPx = 1f,
                            bakeAuto = true,
                            lastSaveDir = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(targetMat)).Replace("\\", "/"), targetMat.name + "_textures"),
                            lastSaveName = targetMat.name
                        };
                    }
                }

                if (normalMapProperty != null && normalMapProperty.textureValue != null)
                {
                    var tex = normalMapProperty.textureValue;
                    var texPath = AssetDatabase.GetAssetPath(tex);
                    var texDir = Path.GetDirectoryName(texPath).Replace("\\", "/");

                    normalMapBakerSettings.lastSaveDir = texDir;
                    normalMapBakerSettings.lastSaveName = tex.name;
                }
                else
                {
                    normalMapBakerSettings.lastSaveDir = Path.Combine(Path.GetDirectoryName(AssetDatabase.GetAssetPath(targetMat)).Replace("\\", "/"), targetMat.name + "_textures");
                    normalMapBakerSettings.lastSaveName = targetMat.name;
                }

                if (normalMapBakerSettings.mode == Mode.ProceduralRoundedRectFromRectTransform && rt != null)
                {
                    var width = (int)rt.rect.width;
                    var height = (int)rt.rect.height;
                    var enableTop = normalMapBakerSettings.enableTop;
                    var enableBottom = normalMapBakerSettings.enableBottom;
                    var enableRight = normalMapBakerSettings.enableRight;
                    var enableLeft = normalMapBakerSettings.enableLeft;

                    NormalMapBaker.OnGUI(ref normalMapBakerSettings.mode, ref width, ref height, ref normalMapBakerSettings.normalScaleFactor, ref normalMapBakerSettings.intensity,
                    ref normalMapBakerSettings.invertX, ref normalMapBakerSettings.invertY, ref normalMapBakerSettings.sourceSprite, ref normalMapBakerSettings.blurPx, ref normalMapBakerSettings.edgeWidthPx,
                    ref normalMapBakerSettings.edgeCurve, ref normalMapBakerSettings.cornerRadiusPx, ref enableTop, ref enableBottom,
                    ref enableRight, ref enableLeft, ref normalMapBakerSettings.maskScaleFactor, ref normalMapBakerSettings.maskMode,
                    ref normalMapBakerSettings.maskTextureType, ref normalMapBakerSettings.hardThreshold, ref normalMapBakerSettings.maskGamma, ref normalMapBakerSettings.invertMask,
                    ref normalMapBakerSettings.createOutline, ref normalMapBakerSettings.outlineThicknessPx, ref normalMapBakerSettings.lastSaveDir, ref normalMapBakerSettings.lastSaveName, ref normalMapBakerSettings.bakeAuto, normalMapProperty, uiImage, uiRawImage);
                }
                else
                {
                    NormalMapBaker.OnGUI(ref normalMapBakerSettings.mode, ref normalMapBakerSettings.width, ref normalMapBakerSettings.height, ref normalMapBakerSettings.normalScaleFactor, ref normalMapBakerSettings.intensity,
                    ref normalMapBakerSettings.invertX, ref normalMapBakerSettings.invertY, ref normalMapBakerSettings.sourceSprite, ref normalMapBakerSettings.blurPx, ref normalMapBakerSettings.edgeWidthPx,
                    ref normalMapBakerSettings.edgeCurve, ref normalMapBakerSettings.cornerRadiusPx, ref normalMapBakerSettings.enableTop, ref normalMapBakerSettings.enableBottom,
                    ref normalMapBakerSettings.enableRight, ref normalMapBakerSettings.enableLeft, ref normalMapBakerSettings.maskScaleFactor, ref normalMapBakerSettings.maskMode,
                    ref normalMapBakerSettings.maskTextureType, ref normalMapBakerSettings.hardThreshold, ref normalMapBakerSettings.maskGamma, ref normalMapBakerSettings.invertMask,
                    ref normalMapBakerSettings.createOutline, ref normalMapBakerSettings.outlineThicknessPx, ref normalMapBakerSettings.lastSaveDir, ref normalMapBakerSettings.lastSaveName, ref normalMapBakerSettings.bakeAuto, normalMapProperty, uiImage, uiRawImage);
                }

                if (bakeReady)
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    var fullRect = new Rect(0, 0, EditorGUIUtility.currentViewWidth, rect.yMax);

                    EditorGUI.DrawRect(fullRect, new Color(0.3f, 0.3f, 0.3f, 0.8f));

                    var style = new GUIStyle(EditorStyles.boldLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 36,
                        normal = { textColor = Color.white }
                    };

                    GUI.Label(fullRect, "Baking textures...", style);
                    bakeReadyRectDrawing = true;
                }

                EditorGUILayout.Space();

                if (EditorGUI.EndChangeCheck())
                {
                    MaterialUserDataStore.Save(targetMat, normalMapBakerSettings);

                    if (normalMapBakerSettings.bakeAuto || normalMapProperty == null || normalMapProperty.textureValue == null)
                    {
                        StartBakeAsync();
                    }
                }

                if (!texturesChecked)
                {
                    if (normalMapProperty == null || normalMapProperty.textureValue == null)
                    {
                        StartBakeAsync();
                    }

                    texturesChecked = true;
                }
            }
        }

        bool working;
        double lastChange = float.MaxValue;
        bool bakeReady = false;
        bool bakeReadyRectDrawing = false;

        void StartBakeAsync()
        {
            if (!working)
                EditorApplication.update += BakeStep;

            working = true;
            lastChange = EditorApplication.timeSinceStartup;
        }

        void BakeStep()
        {
            if (!working)
            {
                EditorApplication.update -= BakeStep;
                return;
            }

            var waitTime = 0.5f;

            if (bakeReady && (bakeReadyRectDrawing || EditorApplication.timeSinceStartup - lastChange > waitTime + 0.25f))
            {
                BakeNormalMap(normalMapBakerSettings.width, normalMapBakerSettings.height, normalMapBakerSettings.normalScaleFactor, normalMapBakerSettings.mode, normalMapBakerSettings.sourceSprite, normalMapBakerSettings.blurPx,
                     normalMapBakerSettings.edgeWidthPx, normalMapBakerSettings.edgeCurve, normalMapBakerSettings.cornerRadiusPx, normalMapBakerSettings.enableTop, normalMapBakerSettings.enableBottom,
                     normalMapBakerSettings.enableLeft, normalMapBakerSettings.enableRight, normalMapBakerSettings.maskScaleFactor, normalMapBakerSettings.maskMode, normalMapBakerSettings.maskTextureType,
                     normalMapBakerSettings.hardThreshold, normalMapBakerSettings.maskGamma, normalMapBakerSettings.invertMask, normalMapBakerSettings.intensity, normalMapBakerSettings.invertX,
                     normalMapBakerSettings.invertY, normalMapBakerSettings.createOutline, normalMapBakerSettings.outlineThicknessPx, ref normalMapBakerSettings.lastSaveDir, ref normalMapBakerSettings.lastSaveName,
                     normalMapBakerSettings.bakeAuto, normalMapProperty, uiImage, uiRawImage);

                working = false;
                bakeReady = false;
                bakeReadyRectDrawing = false;
                lastChange = float.MaxValue;
            }
            else if (EditorApplication.timeSinceStartup - lastChange > waitTime)
            {
                bakeReady = true;
                materialEditor.Repaint();
            }
        }

        static bool BoldToggle(string label, bool value)
        {
            var r = EditorGUILayout.GetControlRect();
            var fieldRect = EditorGUI.PrefixLabel(
                r, new GUIContent(label), EditorStyles.boldLabel);
            return EditorGUI.Toggle(fieldRect, value);
        }
    }
}
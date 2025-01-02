using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SmoothedNormalsUtility : EditorWindow
{
    [MenuItem("Tools/法线平滑工具")]
    private static void OpenTool()
    {
        var window = GetWindow<SmoothedNormalsUtility>(true, "法线平滑", true);
        window.minSize = new Vector2(352f, 400f);
        window.maxSize = new Vector2(352f, 5000f);
    }

    private Dictionary<Mesh, SelectedMesh> meshMap;

    private void OnSelectionChange()
    {
        meshMap = GetSelectedMeshes();
        Repaint();
    }

    private Vector2 mScroll;

    private readonly string filenameSuffix = "SmoothedMesh";

    private void OnGUI()
    {
        if (meshMap == null || meshMap.Count <= 0)
        {
            EditorGUILayout.HelpBox(
                "选择一个或多个网格创建平滑法线下的网格.\n你也可以直接在Scene中选择模型，新网格将自动分配。",
                MessageType.Info);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(true))
                GUILayout.Button("产生平滑的网格(法线)数据", GUILayout.Height(30));

            return;
        }

        GUILayout.Space(4);
        EditorGUILayout.LabelField("待处理的网格", BigHeaderLabel, GUILayout.ExpandWidth(true));
        mScroll = EditorGUILayout.BeginScrollView(mScroll);

        bool hasSkinnedMeshes = false;

        foreach (var selectedMesh in meshMap.Values)
        {
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            var label = selectedMesh.MeshName;
            if (label.Contains(filenameSuffix))
            {
                label = label.Replace(filenameSuffix, "\n" + filenameSuffix);
            }

            GUILayout.Label(label, EditorStyles.wordWrappedMiniLabel, GUILayout.Width(260));
            selectedMesh.IsSkinned = GUILayout.Toggle(selectedMesh.IsSkinned,
                new GUIContent(" Skinned", "请检查网格是否在SkinnedMeshRenderer上使用"));
            hasSkinnedMeshes |= selectedMesh.IsSkinned;
            GUILayout.Space(6);
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            SeparatorSimple();
        }

        EditorGUILayout.LabelField("选项", BigHeaderLabel, GUILayout.ExpandWidth(true));
        smoothedNormalChannel = (SmoothedNormalsChannel)EditorGUILayout.EnumPopup("存储通道", smoothedNormalChannel);

        EditorGUILayout.EndScrollView();
        GUILayout.FlexibleSpace();

        if (hasSkinnedMeshes)
        {
            EditorGUILayout.HelpBox(
                "Skin Mesh 的平滑法线仅能储存在切线数据中.",
                MessageType.Warning);
        }

        if (GUILayout.Button("产生平滑的网格(法线)数据", GUILayout.Height(30)))
        {
            try
            {
                var selection = new List<Object>();
                float progress = 1;
                float total = meshMap.Count;
                foreach (var selectedMesh in meshMap.Values)
                {
                    if (selectedMesh == null)
                        continue;

                    EditorUtility.DisplayProgressBar("稍等",
                        "正在处理网格" +
                        selectedMesh.MeshName, progress / total);
                    progress++;
                    // 产生平滑的网格资源
                    Object o = CreateSmoothedMeshAsset(selectedMesh);
                    if (o != null)
                        selection.Add(o);
                }

                Selection.objects = selection.ToArray();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }

    #region 创建网格/平滑网格

    private const string mCustomDirectoryPath = "SmoothedMeshes/";

    private SmoothedNormalsChannel smoothedNormalChannel = SmoothedNormalsChannel.VertexColors;

    private Mesh CreateSmoothedMeshAsset(SelectedMesh originalMesh)
    {
        var rootPath = Application.dataPath + "/";
        rootPath += mCustomDirectoryPath;

        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);

        var originalMeshName = originalMesh.MeshName;
        var assetPath = "Assets/" + mCustomDirectoryPath;
        var newAssetName = string.Format("{0}{1}.asset", originalMeshName,
            string.IsNullOrEmpty(filenameSuffix) ? "" : " " + filenameSuffix);
        if (originalMeshName.Contains(filenameSuffix))
        {
            newAssetName = originalMeshName + ".asset";
        }

        assetPath += newAssetName;
        var existingAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Mesh)) as Mesh;
        var assetExists = existingAsset != null && originalMesh.IsAsset;

        // 在编辑器中存在的网格资源，直接覆盖
        if (assetExists)
        {
            originalMesh.MeshComp = existingAsset;
            originalMesh.MeshName = existingAsset.name;
        }

        var channel = originalMesh.IsSkinned ? SmoothedNormalsChannel.Tangents : smoothedNormalChannel;
        Mesh newMesh = CreateSmoothedMesh(originalMesh.MeshComp, channel,
            !originalMesh.IsAsset || (originalMesh.IsAsset && assetExists));

        if (newMesh == null)
        {
            ShowNotification(new GUIContent("无法产生网格:\n" + originalMesh.MeshName));
        }
        else
        {
            if (originalMesh.AssociatedObjectList != null)
            {
                Undo.RecordObjects(originalMesh.AssociatedObjectList, "将平滑的网格分配个选择的物体");

                foreach (var o in originalMesh.AssociatedObjectList)
                {
                    if (o is SkinnedMeshRenderer)
                    {
                        (o as SkinnedMeshRenderer).sharedMesh = newMesh;
                    }
                    else if (o is MeshFilter)
                    {
                        (o as MeshFilter).sharedMesh = newMesh;
                    }
                    else
                    {
                        Debug.LogWarning("[法线平滑工具] 无法识别的关联对象: " + o +
                                         "\nType: " + o.GetType());
                    }

                    EditorUtility.SetDirty(o);
                }
            }

            if (originalMesh.IsAsset)
            {
                if (!assetExists)
                {
                    AssetDatabase.CreateAsset(newMesh, assetPath);
                }
            }
            else
                return null;
        }

        return newMesh;
    }

    private Mesh CreateSmoothedMesh(Mesh originalMesh, SmoothedNormalsChannel smoothedNormalsChannel,
        bool overwriteMesh)
    {
        if (originalMesh == null)
        {
            Debug.LogWarning("在调用创建网格方法中，原始网格数据为空");
            return null;
        }

        // 创建新网格
        var newMesh = overwriteMesh ? originalMesh : new Mesh();

        if (!overwriteMesh)
        {
            var originalAssetPath = AssetDatabase.GetAssetPath(originalMesh);
            ModelImporter originalImporter = null;
            bool restoreOptimizeGameObjects = false;

            if (!string.IsNullOrEmpty(originalAssetPath))
            {
                originalImporter = AssetImporter.GetAtPath(originalAssetPath) as ModelImporter;

                if (originalImporter != null && originalImporter.optimizeGameObjects)
                {
                    originalImporter.optimizeGameObjects = false;
                    AssetDatabase.ImportAsset(originalAssetPath, ImportAssetOptions.ForceSynchronousImport);
                    restoreOptimizeGameObjects = true;
                }
            }


            newMesh.vertices = originalMesh.vertices;
            newMesh.normals = originalMesh.normals;
            newMesh.tangents = originalMesh.tangents;
            newMesh.uv = originalMesh.uv;
            newMesh.uv2 = originalMesh.uv2;
            newMesh.uv3 = originalMesh.uv3;
            newMesh.uv4 = originalMesh.uv4;
            newMesh.colors32 = originalMesh.colors32;
            newMesh.triangles = originalMesh.triangles;
            newMesh.bindposes = originalMesh.bindposes;
            newMesh.boneWeights = originalMesh.boneWeights;

            if (originalMesh.blendShapeCount > 0)
            {
                // 复制混合形状数据
                CopyBlendShapes(originalMesh, newMesh);
            }

            newMesh.subMeshCount = originalMesh.subMeshCount;
            if (newMesh.subMeshCount > 1)
            {
                for (var i = 0; i < newMesh.subMeshCount; i++)
                {
                    newMesh.SetTriangles(originalMesh.GetTriangles(i), i);
                }
            }

            if (restoreOptimizeGameObjects)
            {
                originalImporter.optimizeGameObjects = true;
                AssetDatabase.ImportAsset(originalAssetPath, ImportAssetOptions.ForceSynchronousImport);
            }
        }


        // 计算平滑法线

        // 找到相同的位置顶点，计算它们法线的平均值

        // 顶点与法线映射
        var averageNormalsHash = new Dictionary<Vector3, Vector3>();
        for (var i = 0; i < newMesh.vertexCount; i++)
        {
            if (!averageNormalsHash.ContainsKey(newMesh.vertices[i]))
            {
                averageNormalsHash.Add(newMesh.vertices[i], newMesh.normals[i]);
            }
            else
            {
                averageNormalsHash[newMesh.vertices[i]] =
                    (averageNormalsHash[newMesh.vertices[i]] + newMesh.normals[i]).normalized;
            }
        }

        var averageNormals = new Vector3[newMesh.vertexCount];
        for (var i = 0; i < newMesh.vertexCount; i++)
        {
            // 对应顶点的平均（平滑）法线
            averageNormals[i] = averageNormalsHash[newMesh.vertices[i]];
        }

        // 存储到顶点色中

        if (smoothedNormalsChannel == SmoothedNormalsChannel.VertexColors)
        {
            // 将法线信息转换成颜色值
            var colors = new Color32[newMesh.vertexCount];
            for (var i = 0; i < newMesh.vertexCount; i++)
            {
                // 将法线范围 重映射到 0-1 之间 -> 转换成颜色
                var r = (byte)((averageNormals[i].x * 0.5f + 0.5f) * 255);
                var g = (byte)((averageNormals[i].y * 0.5f + 0.5f) * 255);
                var b = (byte)((averageNormals[i].z * 0.5f + 0.5f) * 255);

                colors[i] = new Color32(r, g, b, 255);
            }

            newMesh.colors32 = colors;
        }

        // 存储到切线信息中
        if (smoothedNormalsChannel == SmoothedNormalsChannel.Tangents)
        {
            var tangents = new Vector4[newMesh.vertexCount];
            for (var i = 0; i < newMesh.vertexCount; i++)
            {
                tangents[i] = new Vector4(averageNormals[i].x, averageNormals[i].y, averageNormals[i].z, 0f);
            }

            newMesh.tangents = tangents;
        }

        if (smoothedNormalsChannel == SmoothedNormalsChannel.UV1 ||
            smoothedNormalsChannel == SmoothedNormalsChannel.UV2 ||
            smoothedNormalsChannel == SmoothedNormalsChannel.UV3 ||
            smoothedNormalsChannel == SmoothedNormalsChannel.UV4)
        {
            // 存储到UV信息中
            newMesh.SetUVs((int)smoothedNormalChannel, new List<Vector3>(averageNormals));
        }

        return newMesh;
    }

    #endregion

    private void CopyBlendShapes(Mesh originalMesh, Mesh newMesh)
    {
        for (var i = 0; i < originalMesh.blendShapeCount; i++)
        {
            var shapeName = originalMesh.GetBlendShapeName(i);
            var frameCount = originalMesh.GetBlendShapeFrameCount(i);
            for (var j = 0; j < frameCount; j++)
            {
                var dv = new Vector3[originalMesh.vertexCount];
                var dn = new Vector3[originalMesh.vertexCount];
                var dt = new Vector3[originalMesh.vertexCount];

                var frameWeight = originalMesh.GetBlendShapeFrameWeight(i, j);
                originalMesh.GetBlendShapeFrameVertices(i, j, dv, dn, dt);
                newMesh.AddBlendShapeFrame(shapeName, frameWeight, dv, dn, dt);
            }
        }
    }

    #region 选中网格相关操作

    private Dictionary<Mesh, SelectedMesh> GetSelectedMeshes()
    {
        var meshDict = new Dictionary<Mesh, SelectedMesh>();
        foreach (var o in Selection.objects)
        {
            // 来自资源文件夹中的资源
            var isProjectAsset = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(o));

            if (o is Mesh && !meshDict.ContainsKey(o as Mesh))
            {
                if (o as Mesh != null)
                {
                    var sm = GetMeshToAdd(o as Mesh, isProjectAsset);
                    if (sm != null)
                        meshDict.Add(o as Mesh, sm);
                }
            }
            else if (o is GameObject && isProjectAsset)
            {
                var path = AssetDatabase.GetAssetPath(o);
                var allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var asset in allAssets)
                {
                    if (asset is Mesh && !meshDict.ContainsKey(asset as Mesh))
                    {
                        if (asset as Mesh != null)
                        {
                            var sm = GetMeshToAdd(asset as Mesh, isProjectAsset);
                            if (sm.MeshComp != null)
                                meshDict.Add(asset as Mesh, sm);
                        }
                    }
                }
            }

            // 来自Hierarchy的资源
            else if (o is GameObject && !isProjectAsset)
            {
                var renderers = (o as GameObject).GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var r in renderers)
                {
                    if (r.sharedMesh != null)
                    {
                        if (meshDict.ContainsKey(r.sharedMesh))
                        {
                            var sm = meshDict[r.sharedMesh];
                            sm.AddAssociatedObject(r);
                        }
                        else
                        {
                            if (r.sharedMesh.name.Contains(filenameSuffix))
                            {
                                meshDict.Add(r.sharedMesh, new SelectedMesh(r.sharedMesh, r.sharedMesh.name, false));
                            }
                            else
                            {
                                if (r.sharedMesh != null)
                                {
                                    var sm = GetMeshToAdd(r.sharedMesh, true, r);
                                    if (sm.MeshComp != null)
                                        meshDict.Add(r.sharedMesh, sm);
                                }
                            }
                        }
                    }
                }

                var mfilters = (o as GameObject).GetComponentsInChildren<MeshFilter>();
                foreach (var mf in mfilters)
                {
                    if (mf.sharedMesh != null)
                    {
                        if (meshDict.ContainsKey(mf.sharedMesh))
                        {
                            var sm = meshDict[mf.sharedMesh];
                            sm.AddAssociatedObject(mf);
                        }
                        else
                        {
                            if (mf.sharedMesh.name.Contains(filenameSuffix))
                            {
                                meshDict.Add(mf.sharedMesh, new SelectedMesh(mf.sharedMesh, mf.sharedMesh.name, false));
                            }
                            else
                            {
                                if (mf.sharedMesh != null)
                                {
                                    var sm = GetMeshToAdd(mf.sharedMesh, true, mf);
                                    if (sm.MeshComp != null)
                                        meshDict.Add(mf.sharedMesh, sm);
                                }
                            }
                        }
                    }
                }
            }
        }

        return meshDict;
    }

    private SelectedMesh GetMeshToAdd(Mesh mesh, bool isProjectAsset, Object assoObj = null)
    {
        var meshPath = AssetDatabase.GetAssetPath(mesh);
        var meshAsset = AssetDatabase.LoadAssetAtPath(meshPath, typeof(Mesh)) as Mesh;
        // 如果为空，说明不是unity的内置网格资源
        if (meshAsset == null)
        {
            return new SelectedMesh(mesh, mesh.name, isProjectAsset, assoObj);
        }

        var meshName = mesh.name;
        if (!AssetDatabase.IsMainAsset(meshAsset))
        {
            var main = AssetDatabase.LoadMainAssetAtPath(meshPath);
            meshName = main.name + " - " + meshName + "_" + mesh.GetInstanceID();
        }

        var sm = new SelectedMesh(mesh, meshName, isProjectAsset, assoObj);
        return sm;
    }

    #endregion

    #region 选中的网格数据

    private class SelectedMesh
    {
        private List<Object> _associatedObjectList = new List<Object>();

        public Mesh MeshComp;
        public string MeshName;
        public bool IsAsset;
        public bool IsSkinned;

        public Object[] AssociatedObjectList
        {
            get
            {
                if (_associatedObjectList.Count == 0)
                {
                    return null;
                }

                return _associatedObjectList.ToArray();
            }
        }

        public SelectedMesh(Mesh mesh, string meshName, bool isAsset, Object associatedObject = null,
            bool isSkinned = false)
        {
            MeshComp = mesh;
            MeshName = meshName;
            IsAsset = isAsset;
            AddAssociatedObject(associatedObject);

            IsSkinned = isSkinned;
            if (associatedObject != null && associatedObject is SkinnedMeshRenderer)
            {
                isSkinned = true;
            }
            else if (mesh != null && mesh.boneWeights != null && mesh.boneWeights.Length > 0)
            {
                isSkinned = true;
            }
        }

        public void AddAssociatedObject(Object associatedObject)
        {
            if (associatedObject != null)
            {
                _associatedObjectList.Add(associatedObject);
            }
        }
    }

    #endregion

    public enum SmoothedNormalsChannel
    {
        UV1 = 0,
        UV2,
        UV3,
        UV4,
        VertexColors,
        Tangents,
    }

    #region 样式

    private GUIStyle _bigHeaderLabel;

    private GUIStyle BigHeaderLabel
    {
        get
        {
            if (_bigHeaderLabel == null)
            {
                _bigHeaderLabel = new GUIStyle(EditorStyles.largeLabel);
                _bigHeaderLabel.fontStyle = FontStyle.Bold;
                _bigHeaderLabel.fixedHeight = 30;
                _bigHeaderLabel.normal.textColor = new Color32(250, 130, 0, 255);
            }

            return _bigHeaderLabel;
        }
    }

    private static GUIStyle _lineStyle;

    public static GUIStyle LineStyle
    {
        get
        {
            if (_lineStyle == null)
            {
                _lineStyle = new GUIStyle();
                _lineStyle.normal.background = EditorGUIUtility.whiteTexture;
                _lineStyle.stretchWidth = true;
            }

            return _lineStyle;
        }
    }

    public void SeparatorSimple()
    {
        var color = EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.65f, 0.65f, 0.65f);
        GUILine(color, 1);
        GUILayout.Space(1);
    }

    private void GUILine(Color color, float height = 2f)
    {
        var linePosition = GUILayoutUtility.GetRect(0f, float.MaxValue, height, height, LineStyle);

        if (Event.current.type == EventType.Repaint)
        {
            var orgColor = GUI.color;
            GUI.color = orgColor * color;
            LineStyle.Draw(linePosition, false, false, false, false);
            GUI.color = orgColor;
        }
    }

    #endregion
}
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LFramework
{
    [CustomEditor(typeof(UIProperty))]
    public class UIPropertyEditor : Editor
    {
        private ReorderableList bindDataList;
        private UIProperty uiProperty;

        private void OnEnable()
        {
            var data = serializedObject.FindProperty("runtimeBindData");
            bindDataList = new ReorderableList(serializedObject, data, true, true, true, true);

            bindDataList.drawHeaderCallback = DrawHeader;
            bindDataList.drawElementCallback = DrawListItems;
            bindDataList.onAddCallback = AddData;
            bindDataList.onRemoveCallback = RemoveData;
        }

        private BindData curOperateObject;
        private List<Rect> typeRectList;

        public override void OnInspectorGUI()
        {
            OnDragUpdate();

            serializedObject.Update();
            bindDataList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("生成绑定代码"))
            {
                GenerateBindCode();
            }
        }

        private void GenerateBindCode()
        {
            var context = serializedObject.targetObject as UIProperty;
            var classUIName = context.gameObject.name;
            if (!classUIName.Contains("UI"))
            {
                Debug.LogError("节点必须以UI后缀为结尾");
                return;
            }

            var classExtensionName = classUIName + "Property";
            StringBuilder sb = new StringBuilder();
            HashSet<string> allNameSpace = new HashSet<string>();
            sb.Append("//此代码由程序自动生成切勿修改\n");

            sb.Append("public partial class " + classUIName + "\n");
            sb.Append("{\n");

            int dataCount = bindDataList.count;
            for (int i = 0; i < dataCount; i++)
            {
                SerializedProperty addData =
                    bindDataList.serializedProperty.GetArrayElementAtIndex(i);

                string variableName = addData.FindPropertyRelative("variableName").stringValue;
                object referenceObject = addData.FindPropertyRelative("bindComponent").objectReferenceValue;
                // 变量名称
                string variableTypeName = referenceObject.GetType().Name;
                // 变量名称所在命名空间
                string namespaceStr = referenceObject.GetType().Namespace;
                if (!string.IsNullOrEmpty(namespaceStr))
                {
                    allNameSpace.Add("using " + namespaceStr + ";\n");
                }

                sb.Append("\t public " + variableTypeName + " " + variableName + ";\n");
            }

            sb.Append("\t  protected override void InitializeProperty()\n  \t{\n");

            for (int i = 0; i < dataCount; i++)
            {
                SerializedProperty addData =
                    bindDataList.serializedProperty.GetArrayElementAtIndex(i);

                string variableName = addData.FindPropertyRelative("variableName").stringValue;
                object referenceObject = addData.FindPropertyRelative("bindComponent").objectReferenceValue;
                // 变量名称
                string variableTypeName = referenceObject.GetType().Name;
                sb.Append($"\t  {variableName} = uiPropertyCache[\"{variableName}\"] as {variableTypeName};\n");
            }

            sb.Append("\n \t}");
            sb.Append("\n}");

            foreach (string namespaceStr in allNameSpace)
            {
                sb.Insert(0, namespaceStr);
            }

            string uiFilePath = BindUIGenerateConfig.GetSerializedObject().codeGeneratePath + classUIName + ".cs";
            if (!File.Exists(uiFilePath))
            {
                StringBuilder sbUI = new StringBuilder();
                sbUI.Append("using UnityEngine;\n");
                sbUI.Append("using UnityEngine.UI;\n");
                sbUI.Append("using LFramework;\n");

                sbUI.Append("public partial class " + classUIName + " : BaseUI\n");
                sbUI.Append("{\n");
                sbUI.Append("\n}");
                File.WriteAllText(uiFilePath, sbUI.ToString());
            }

            string extensionFilePath =
                BindUIGenerateConfig.GetSerializedObject().codeGeneratePath + classExtensionName + ".cs";
            File.WriteAllText(extensionFilePath, sb.ToString());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void OnDragUpdate()
        {
            Event e = Event.current;
            GUI.color = Color.green;
            //绘制一个监听区域
            var dragArea = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUIContent title = new GUIContent("拖动组件对象到此进行快速绑定");
            GUI.Box(dragArea, title);
            DrawTypes();

            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (curOperateObject == null)
                    {
                        curOperateObject = BuildObjectInfo(DragAndDrop.objectReferences[0] as GameObject);
                    }

                    var index = GetContainsIndex(e.mousePosition);
                    if (index <= -1)
                    {
                        break;
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (e.type == EventType.DragPerform)
                    {
                        if (curOperateObject != null)
                        {
                            // 添加数据
                            int addIndex = bindDataList.count;
                            bindDataList.serializedProperty.InsertArrayElementAtIndex(addIndex);

                            SerializedProperty addData =
                                bindDataList.serializedProperty.GetArrayElementAtIndex(addIndex);
                            addData.FindPropertyRelative("variableName").stringValue = curOperateObject.bindObject.name;
                            addData.FindPropertyRelative("bindComponent").objectReferenceValue =
                                curOperateObject.componentList[index];
                            addData.FindPropertyRelative("curBindCompIndex").intValue = index;
                            addData.FindPropertyRelative("bindObject").objectReferenceValue =
                                curOperateObject.bindObject;
                            var comList = addData.FindPropertyRelative("componentList");
                            var typeList = addData.FindPropertyRelative("typeList");
                            comList.ClearArray();
                            typeList.ClearArray();
                            for (int i = 0; i < curOperateObject.componentList.Count; i++)
                            {
                                var comp = curOperateObject.componentList[i];
                                var type = curOperateObject.typeList[i];
                                comList.InsertArrayElementAtIndex(i);
                                typeList.InsertArrayElementAtIndex(i);

                                // 获取新添加的元素引用
                                SerializedProperty compReference = comList.GetArrayElementAtIndex(i);
                                SerializedProperty typeReference = typeList.GetArrayElementAtIndex(i);

                                // 设置组件引用
                                compReference.objectReferenceValue = comp;
                                typeReference.stringValue = type;
                            }

                            serializedObject.ApplyModifiedProperties();
                            UpdateLinkReference();
                        }

                        DragAndDrop.AcceptDrag();
                    }

                    e.Use();
                    break;

                case EventType.DragExited:
                    curOperateObject = null;
                    break;

                default:
                    break;
            }

            GUI.color = Color.white;
        }

        private void DrawTypes()
        {
            if (curOperateObject == null)
            {
                return;
            }

            GUILayout.BeginVertical();
            var allTypes = curOperateObject.typeList;

            typeRectList = new List<Rect>();
            for (var i = 0; i < allTypes.Count; i++)
            {
                var r = GUILayoutUtility.GetRect(0f, 30f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                typeRectList.Add(r);
                GUI.color = r.Contains(Event.current.mousePosition) ? Color.green : Color.white;
                GUI.Box(r, allTypes[i]);
                Repaint();
            }

            GUILayout.EndVertical();
        }

        private int GetContainsIndex(Vector2 mousePos)
        {
            if (typeRectList == null || typeRectList.Count <= 0)
            {
                return -1;
            }

            for (int i = 0; i < typeRectList.Count; i++)
            {
                var rect = typeRectList[i];
                if (rect.Contains(mousePos))
                {
                    return i;
                }
            }

            return -1;
        }

        private BindData BuildObjectInfo(GameObject obj)
        {
            var info = new BindData();

            info.bindObject = obj;

            var allComponent = obj.GetComponents(typeof(Component));

            foreach (var component in allComponent)
            {
                info.typeList.Add(component.GetType().Name);
                info.componentList.Add(component);
            }

            return info;
        }

        #region ReorderableList

        private void DrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Components");
        }

        private readonly float itemWidth = 115f;
        private readonly float itemInterval = 15;

        private void DrawListItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty element = bindDataList.serializedProperty.GetArrayElementAtIndex(index);
            EditorGUI.PropertyField(
                new Rect(rect.x, rect.y, itemWidth, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("variableName"), GUIContent.none);

            var curBindComp = element.FindPropertyRelative("bindComponent");
            EditorGUI.PropertyField(
                new Rect(rect.x + itemWidth + itemInterval, rect.y, itemWidth, EditorGUIUtility.singleLineHeight),
                curBindComp, GUIContent.none);

            var curBindCompIndex = element.FindPropertyRelative("curBindCompIndex").intValue;
            var typeList = element.FindPropertyRelative("typeList");
            string[] showTypeArray = new string[typeList.arraySize];
            for (int i = 0; i < typeList.arraySize; i++)
            {
                var value = typeList.GetArrayElementAtIndex(i).stringValue;
                showTypeArray[i] = value;
            }

            var result = EditorGUI.Popup(
                new Rect(rect.x + itemWidth * 2f + itemInterval * 2f, rect.y, itemWidth,
                    EditorGUIUtility.singleLineHeight),
                curBindCompIndex, showTypeArray);

            element.FindPropertyRelative("bindComponent").objectReferenceValue =
                element.FindPropertyRelative("componentList").GetArrayElementAtIndex(result).objectReferenceValue;
            element.FindPropertyRelative("curBindCompIndex").intValue = result;
        }

        private void AddData(ReorderableList list)
        {
            // 不允许通过手动添加数组中元素来进行组件的绑定
        }

        private void RemoveData(ReorderableList list)
        {
            int removeIndex = bindDataList.count - 1;
            bindDataList.serializedProperty.DeleteArrayElementAtIndex(removeIndex);
            serializedObject.ApplyModifiedProperties();
            UpdateLinkReference();
        }

        private void UpdateLinkReference()
        {
            var runtimeComp = serializedObject.targetObject as UIProperty;
            runtimeComp.UpdateReference();
        }

        #endregion

        private void OnDestroy()
        {
            // showBindDataList = null;
        }
    }
}
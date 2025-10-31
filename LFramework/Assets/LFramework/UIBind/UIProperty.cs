using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LFramework
{
    [ExecuteAlways]
    public class UIProperty : MonoBehaviour
    {
        public List<BindData> runtimeBindData;

#if UNITY_EDITOR
        private static Texture2D linkTexture;
        private static List<BindData> editorBindDataList;

        [InitializeOnLoadMethod]
        private static void Load()
        {
            linkTexture = EditorGUIUtility.IconContent("d_UnityEditor.FindDependencies").image as Texture2D;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        }

        protected void OnEnable()
        {
            UpdateReference();
        }

        public void UpdateReference()
        {
            editorBindDataList = runtimeBindData;
        }

        private static void OnHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (editorBindDataList == null)
            {
                return;
            }

            int dataCount = editorBindDataList.Count;
            var originColor = GUI.color;

            GUI.color = Color.green;
            for (int index = 0; index < dataCount; index++)
            {
                var data = editorBindDataList[index];
                var bindObject = data.bindObject;
                if (bindObject.GetInstanceID() == instanceID)
                {
                    var r = new Rect(selectionRect);
                    r.x = 35;
                    r.width = 16;
                    r.height = 16;
                    GUI.DrawTexture(r, linkTexture);
                }
            }

            GUI.color = originColor;
        }
#endif
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LFramework
{
    [Serializable]
    public class BindData
    {
        public string variableName;
        public Component bindComponent;
#if UNITY_EDITOR
        // 绑定组件所依赖的节点
        public Object bindObject;

        public int curBindCompIndex;

        // 所有组件
        public List<Component> componentList = new();

        public List<string> typeList = new();
#endif
    }
}
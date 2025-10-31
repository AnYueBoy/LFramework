using System.Collections.Generic;
using UnityEngine;

namespace LFramework
{
    public abstract class BaseUI
    {
        private UIProperty uiProperty;
        protected Dictionary<string, Component> uiPropertyCache = new();

        public void Initialize(UIProperty uiProperty)
        {
            this.uiProperty = uiProperty;
            for (int i = 0; i < uiProperty.runtimeBindData.Count; i++)
            {
                var data = uiProperty.runtimeBindData[i];
                uiPropertyCache.Add(data.variableName, data.bindComponent);
            }

            InitializeProperty();
        }

        protected abstract void InitializeProperty();
    }
}
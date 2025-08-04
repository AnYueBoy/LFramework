using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LFramework.SortMask
{
    public class MaterialReplacements
    {
        private readonly IMaterialReplacer replacer;
        private readonly Action<Material> applyParameters;
        private readonly List<MaterialOverride> overrideList = new List<MaterialOverride>();

        public MaterialReplacements(IMaterialReplacer replacer, Action<Material> applyParameters)
        {
            this.replacer = replacer;
            this.applyParameters = applyParameters;
        }

        public Material Get(Material originalMat)
        {
            for (int i = 0; i < overrideList.Count; i++)
            {
                var entry = overrideList[i];
                // 找到原材质对应的替换材质
                if (entry.OriginalMat == originalMat)
                {
                    var existMat = entry.Get();
                    if (existMat != null)
                    {
                        existMat.CopyPropertiesFromMaterial(originalMat);
                        applyParameters.Invoke(existMat);
                    }

                    return existMat;
                }
            }

            var replacement = replacer.Replace(originalMat);
            if (replacement != null)
            {
                if (replacement == originalMat)
                {
                    throw new Exception($"IMaterialReplacer 不应该返回与原材质一样的材质");
                }

                replacement.hideFlags = HideFlags.HideAndDontSave;
                applyParameters.Invoke(replacement);
            }

            overrideList.Add(new MaterialOverride(originalMat, replacement));
            return replacement;
        }

        public void Release(Material replacement)
        {
            for (int i = 0; i < overrideList.Count; i++)
            {
                var entry = overrideList[i];
                if (entry.ReplacementMat != replacement)
                {
                    continue;
                }

                if (entry.Release())
                {
                    Object.DestroyImmediate(replacement);
                    overrideList.RemoveAt(i);
                    return;
                }
            }
        }

        public void ApplyAll()
        {
            for (int i = 0; i < overrideList.Count; i++)
            {
                var entry = overrideList[i];
                var replacement = entry.ReplacementMat;
                if (replacement != null)
                {
                    applyParameters.Invoke(replacement);
                }
            }
        }

        public void DestroyAllAndClear()
        {
            for (int i = 0; i < overrideList.Count; i++)
            {
                var entry = overrideList[i];
                Object.DestroyImmediate(entry.ReplacementMat);
                overrideList.Clear();
            }
        }

        #region 内部

        private class MaterialOverride
        {
            private int useCount;
            public Material OriginalMat { get; private set; }
            public Material ReplacementMat { get; private set; }

            public MaterialOverride(Material originalMat, Material replacementMat)
            {
                OriginalMat = originalMat;
                ReplacementMat = replacementMat;
                useCount = 1;
            }

            public Material Get()
            {
                useCount++;
                return ReplacementMat;
            }

            public bool Release()
            {
                if (useCount <= 0)
                {
                    return false;
                }

                useCount--;
                return useCount <= 0;
            }
        }

        #endregion
    }
}
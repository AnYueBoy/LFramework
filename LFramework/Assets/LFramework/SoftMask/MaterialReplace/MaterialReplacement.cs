using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace LFramework.SoftMask
{
    public class MaterialReplacement
    {
        private readonly IMaterialReplacer _materialReplacer;
        private readonly Action<Material> _applyParameters;

        private readonly List<MaterialOverride> _matOverrideList = new List<MaterialOverride>();

        public MaterialReplacement(IMaterialReplacer materialReplacer, Action<Material> applyParameters)
        {
            _materialReplacer = materialReplacer;
            _applyParameters = applyParameters;
        }

        public Material Get(Material originalMat)
        {
            for (int i = 0; i < _matOverrideList.Count; i++)
            {
                var matOverride = _matOverrideList[i];
                if (!ReferenceEquals(matOverride.originalMat, originalMat))
                {
                    continue;
                }

                var exitReplaceMat = matOverride.Get();
                if (exitReplaceMat != null)
                {
                    exitReplaceMat.CopyPropertiesFromMaterial(originalMat);
                    _applyParameters(exitReplaceMat);
                }

                return exitReplaceMat;
            }

            var replaceMat = _materialReplacer.Replace(originalMat);

            if (replaceMat != null)
            {
                Assert.AreNotEqual(originalMat, replaceMat, "替换材质不能与原材质相同");
                replaceMat.hideFlags = HideFlags.HideAndDontSave;
                _applyParameters(replaceMat);
            }

            _matOverrideList.Add(new MaterialOverride(originalMat, replaceMat));
            return replaceMat;
        }

        public void Release(Material replaceMat)
        {
            for (int i = 0; i < _matOverrideList.Count; i++)
            {
                var matOverride = _matOverrideList[i];
                if (matOverride.replaceMat == replaceMat && matOverride.Release())
                {
                    Object.DestroyImmediate(replaceMat);
                    _matOverrideList.RemoveAt(i);
                    break;
                }
            }
        }

        public void ApplyAll()
        {
            for (int i = 0; i < _matOverrideList.Count; i++)
            {
                var matOverride = _matOverrideList[i];
                if (matOverride == null)
                {
                    continue;
                }

                var replaceMat = matOverride.replaceMat;
                if (replaceMat != null)
                {
                    _applyParameters(replaceMat);
                }
            }
        }

        public void DestroyAllAndClear()
        {
            for (int i = 0; i < _matOverrideList.Count; i++)
            {
                var matOverride = _matOverrideList[i];
                if (matOverride == null)
                {
                    continue;
                }

                var replaceMat = matOverride.replaceMat;
                if (replaceMat == null)
                {
                    continue;
                }

                Object.DestroyImmediate(replaceMat);
            }

            _matOverrideList.Clear();
        }

        private class MaterialOverride
        {
            private int useCount;

            public Material originalMat { get; private set; }
            public Material replaceMat { get; private set; }

            public MaterialOverride(Material originalMat, Material replaceMat)
            {
                this.originalMat = originalMat;
                this.replaceMat = replaceMat;
                useCount = 1;
            }

            public Material Get()
            {
                useCount++;
                return replaceMat;
            }

            public bool Release()
            {
                useCount--;
                return useCount == 0;
            }
        }
    }
}
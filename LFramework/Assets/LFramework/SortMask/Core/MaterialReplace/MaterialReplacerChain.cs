using System.Collections.Generic;
using UnityEngine;

namespace LFramework.SortMask
{
    public class MaterialReplacerChain : IMaterialReplacer
    {
        private readonly List<IMaterialReplacer> replacerList;

        public MaterialReplacerChain(List<IMaterialReplacer> replacerList, IMaterialReplacer yetAnother)
        {
            this.replacerList = new List<IMaterialReplacer>(replacerList) { yetAnother };
            Initialize();
        }

        public int Order { get; private set; }

        public Material Replace(Material material)
        {
            for (int i = 0; i < replacerList.Count; i++)
            {
                var replacer = replacerList[i];
                var result = replacer.Replace(material);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void Initialize()
        {
            replacerList.Sort((a, b) => a.Order.CompareTo(b.Order));
            Order = replacerList[0].Order;
        }
    }
}
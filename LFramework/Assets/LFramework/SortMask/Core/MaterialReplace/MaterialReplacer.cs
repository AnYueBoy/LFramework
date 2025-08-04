using System;
using System.Collections.Generic;

namespace LFramework.SortMask
{
    public static class MaterialReplacer
    {
        private static List<IMaterialReplacer> globalReplacers;

        public static List<IMaterialReplacer> GlobalReplacers
        {
            get
            {
                if (globalReplacers == null)
                {
                    globalReplacers = new List<IMaterialReplacer>();
                    // 获取所有被属性标记的 IMaterialReplacer 实现类
                    RegisterAllReplacer();
                }

                return globalReplacers;
            }
        }

        private static void RegisterAllReplacer()
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < allAssemblies.Length; i++)
            {
                var assembly = allAssemblies[i];
                var types = assembly.GetTypes();
                for (int j = 0; j < types.Length; j++)
                {
                    var type = types[j];
                    if (type.IsAbstract || type.IsInterface)
                    {
                        continue;
                    }

                    if (!type.IsDefined(typeof(GlobalMaterialReplacerAttribute), false))
                    {
                        continue;
                    }

                    if (!typeof(IMaterialReplacer).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    var instance = (IMaterialReplacer)Activator.CreateInstance(type);
                    globalReplacers.Add(instance);
                }
            }
        }
    }
}
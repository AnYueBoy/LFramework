using System.Linq;
using System.Reflection;
using UnityEditor;

namespace LFramework
{
    public class InjectAnalysisEditor
    {
        [MenuItem("LFramework/生成依赖关系分析数据")]
        public static void GenerateDependency()
        {
            var assembly = Assembly.Load("Assembly-CSharp");

            // 被属性标记的类
            var markedClassArray = assembly.GetTypes()
                .Where(type => type.GetCustomAttributes(typeof(InjectAnalysis), true).Length > 0);
        }

        private static void BuildDependencyTree()
        {
            // 构建依赖树
        }
    }
}
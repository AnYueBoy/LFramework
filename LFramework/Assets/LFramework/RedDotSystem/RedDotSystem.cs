using System;
using System.Collections.Generic;

namespace LFramework.RedDotSystem
{
    public class RedDotSystem : IRedDotSystem
    {
        private static IRedDotSystem _instance;

        public static IRedDotSystem I
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RedDotSystem();
                }

                return _instance;
            }
        }

        private readonly char split = '/';
        private Dictionary<string, ITreeNode> allTreeNodeMap;

        public bool IsRunning { get; private set; }

        public Action NodeNumChangeHandler { get; }

        public ITreeNode Root { get; }

        public RedDotSystem()
        {
            allTreeNodeMap = new Dictionary<string, ITreeNode>();
            Root = new TreeNode("Root");
            dirtyNodeSet = new HashSet<ITreeNode>();
            tmpDirtyNodeList = new List<ITreeNode>();
        }

        public void Initialize()
        {
            IsRunning = true;
        }

        public void LocalUpdate()
        {
            if (!IsRunning)
            {
                return;
            }

            RefreshNodesCondition();
            RefreshDirtyNodes();
        }

        public ITreeNode GetTreeNode(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new Exception($"获取节点时 路径为空");
            }

            if (allTreeNodeMap.TryGetValue(path, out var targetNode))
            {
                return targetNode;
            }

            ITreeNode currentNode = Root;
            var pathLength = path.Length;
            var startIndex = 0;
            for (int i = 0; i < pathLength; i++)
            {
                var ch = path[i];
                if (ch != split)
                {
                    continue;
                }

                if (i == pathLength - 1)
                {
                    throw new Exception($"路径不能以分隔符结尾 {path}");
                }

                int endIndex = i - 1;
                if (endIndex < startIndex)
                {
                    throw new Exception($"路径不能以分隔符开头或路径存在连续的分隔符 {path}");
                }

                // A/B/C
                // A/B/D

                //FIXME: 此处会产生大量字符串 - 考虑使用 RangeString 进行优化
                ITreeNode child = currentNode.GetOrAddChild(path.Substring(startIndex, endIndex - startIndex));
                startIndex = i + 1;
                currentNode = child;
            }

            // 叶子节点
            ITreeNode leafNode = currentNode.GetOrAddChild(path.Substring(startIndex, pathLength - startIndex));
            allTreeNodeMap.Add(path, leafNode);
            return leafNode;
        }

        private event Action nodeNumChangeEventHandler;

        public void AddNodeNumChangeEvent(Action handler)
        {
            nodeNumChangeEventHandler += handler;
        }

        public void RemoveNodeNumChangeEvent(Action handler)
        {
            nodeNumChangeEventHandler -= handler;
        }

        public void RemoveNodeNumChangeAllEvent()
        {
            nodeNumChangeEventHandler = null;
        }

        public void NodeNumChange()
        {
            nodeNumChangeEventHandler?.Invoke();
        }

        private readonly HashSet<ITreeNode> dirtyNodeSet;
        private readonly List<ITreeNode> tmpDirtyNodeList;

        public void MarkDirtyNode(ITreeNode treeNode)
        {
            if (treeNode == null || treeNode.Path == Root.Path)
            {
                return;
            }

            dirtyNodeSet.Add(treeNode);
        }

        public void RefreshSingleNode(string path)
        {
            var node = GetTreeNode(path);
            // 只允许刷新叶子节点
            if (node == null || node.ChildCount > 0)
            {
                return;
            }

            var conditionInstance = node.TriggerCondition;
            if (conditionInstance == null)
            {
                return;
            }

            node.LeafNodeChange(conditionInstance.TriggerCondition());
        }

        public void RefreshAllNodes()
        {
            RefreshNodesCondition(true);
        }

        public void RegisterTrigger(params BaseRedDotCondition[] conditions)
        {
            for (int i = 0; i < conditions.Length; i++)
            {
                var condition = conditions[i];
                var bindNode = GetTreeNode(condition.Path);
                bindNode.TriggerCondition = condition;
            }
        }

        private void RefreshDirtyNodes()
        {
            while (dirtyNodeSet.Count > 0)
            {
                tmpDirtyNodeList.Clear();
                foreach (var node in dirtyNodeSet)
                {
                    tmpDirtyNodeList.Add(node);
                }

                dirtyNodeSet.Clear();

                for (int i = 0; i < tmpDirtyNodeList.Count; i++)
                {
                    var node = tmpDirtyNodeList[i];
                    node.NonLeafNodeChange();
                }
            }
        }

        private void RefreshNodesCondition(bool isForce = false)
        {
            foreach (var kvp in allTreeNodeMap)
            {
                var node = kvp.Value;
                // 不刷新非叶子节点的检测条件
                if (node.ChildCount > 0)
                {
                    continue;
                }

                var bindConditionInstance = node.TriggerCondition;
                // 检测条件不存在 跳过
                if (bindConditionInstance == null)
                {
                    continue;
                }

                if (!isForce && !bindConditionInstance.IsAutoCheck)
                {
                    continue;
                }

                // 叶子节点值变化
                node.LeafNodeChange(bindConditionInstance.TriggerCondition());
            }
        }
    }
}
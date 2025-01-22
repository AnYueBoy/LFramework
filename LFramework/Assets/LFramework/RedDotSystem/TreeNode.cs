using System;
using System.Collections.Generic;

namespace LFramework.RedDotSystem
{
    public class TreeNode : ITreeNode
    {
        private Dictionary<string, ITreeNode> children;

        private event Action<int> nodeValueChangedCallback;

        public ITreeNode Parent { get; }
        public int NodeValue { get; private set; }

        public int ChildCount
        {
            get
            {
                if (children == null || children.Count <= 0)
                {
                    return 0;
                }

                return children.Count;
            }
        }

        public string Path { get; }

        private ITreeNode GetChild(string key)
        {
            if (children == null)
            {
                return null;
            }

            return children.GetValueOrDefault(key);
        }

        private ITreeNode AddChild(string key)
        {
            if (children == null)
            {
                children = new Dictionary<string, ITreeNode>();
            }
            else if (children.ContainsKey(key))
            {
                throw new Exception($"子节点添加失败，不允许重复添加 {Path}");
            }

            ITreeNode child = new TreeNode(key, this);
            children.Add(key, child);
            // 节点数量变化
            RedDotSystem.I.NodeNumChange();
            return child;
        }

        public ITreeNode GetOrAddChild(string key)
        {
            ITreeNode child = GetChild(key);
            if (child == null)
            {
                child = AddChild(key);
            }

            return child;
        }

        public TreeNode(string path)
        {
            Path = path;
        }

        public TreeNode(string name, ITreeNode parent) : this(name)
        {
            Parent = parent;
        }

        public void AddNodeChangeCallback(Action<int> callback)
        {
            nodeValueChangedCallback += callback;
        }

        public void RemoveChangeCallback(Action<int> callback)
        {
            nodeValueChangedCallback -= callback;
        }

        public void RemoveAllChangeCallback()
        {
            nodeValueChangedCallback = null;
        }

        public void LeafNodeChange(int value)
        {
            if (children != null && children.Count > 0)
            {
                throw new Exception($"不允许直接改变非叶子节点的值 {Path}");
            }

            InternalChangeValue(value);
        }

        public void NonLeafNodeChange()
        {
            int sum = 0;
            if (children != null && children.Count > 0)
            {
                foreach (var kvp in children)
                {
                    var child = kvp.Value;
                    sum += child.NodeValue;
                }

                InternalChangeValue(sum);
            }
        }

        public BaseRedDotCondition TriggerCondition { get; set; }

        private void InternalChangeValue(int value)
        {
            if (NodeValue == value)
            {
                return;
            }

            NodeValue = value;
            nodeValueChangedCallback?.Invoke(value);
            RedDotSystem.I.MarkDirtyNode(this);
        }
    }
}
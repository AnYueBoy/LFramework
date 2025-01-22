using System;
using System.Collections.Generic;

namespace LFramework.RedDotSystem
{
    public interface ITreeNode
    {
        // A/B/C
        // A/B/D

        ITreeNode Parent { get; }

        int NodeValue { get; }

        int ChildCount { get; }

        string Path { get; }

        ITreeNode GetOrAddChild(string key);

        #region 节点值变化监听

        void AddNodeChangeCallback(Action<int> callback);

        void RemoveChangeCallback(Action<int> callback);

        void RemoveAllChangeCallback();

        #endregion

        #region 节点值变化

        void LeafNodeChange(int value);

        void NonLeafNodeChange();

        #endregion

        BaseRedDotCondition TriggerCondition { get; set; }
    }
}
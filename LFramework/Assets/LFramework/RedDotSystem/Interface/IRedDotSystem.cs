using System;

namespace LFramework.RedDotSystem
{
    public interface IRedDotSystem
    {
        bool IsRunning { get; }

        ITreeNode Root { get; }
        void Initialize();

        void LocalUpdate();

        ITreeNode GetTreeNode(string path);

        void AddNodeNumChangeEvent(Action handler);
        void RemoveNodeNumChangeEvent(Action handler);
        void RemoveNodeNumChangeAllEvent();

        void NodeNumChange();

        void MarkDirtyNode(ITreeNode treeNode);

        void RefreshSingleNode(string path);

        void RefreshAllNodes();


        void RegisterTrigger(params BaseRedDotCondition[] conditions);
    }
}
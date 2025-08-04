using UnityEngine;

namespace LFramework.SortMask
{
    public interface ISoftMask
    {
        bool IsAlive { get; }

        bool IsMaskingEnable { get; }

        Material GetReplacement(Material original);

        void ReleaseReplacement(Material replacement);

        void UpdateTransformChildren(Transform transform);
    }
}
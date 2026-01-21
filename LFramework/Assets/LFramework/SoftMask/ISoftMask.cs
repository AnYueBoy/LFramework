using UnityEngine;

namespace LFramework.SoftMask
{
    public interface ISoftMask
    {
        bool IsAlive { get; }

        bool IsMaskingEnable { get; }

        Material GetReplaceMat(Material originalMat);

        void ReleaseReplaceMat(Material replaceMat);
    }
}
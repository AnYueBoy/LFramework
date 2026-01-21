using UnityEngine;

namespace LFramework.SoftMask
{
    public interface IMaterialReplacer
    {
        Material Replace(Material originalMat);
    }
}
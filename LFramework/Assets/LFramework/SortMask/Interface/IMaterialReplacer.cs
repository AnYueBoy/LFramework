using UnityEngine;

namespace LFramework.SortMask
{
    public interface IMaterialReplacer
    {
        /// <summary>
        /// 确定多个IMaterialReplacer之间的调用顺序
        /// 值越小，调用时机越早。
        /// </summary>
        int Order { get; }

        /// <summary>
        /// 如果给定的材质无法替换将返回空。
        /// </summary>
        Material Replace(Material material);
    }
}
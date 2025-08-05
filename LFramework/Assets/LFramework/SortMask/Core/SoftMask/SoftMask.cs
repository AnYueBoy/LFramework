using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LFramework.SortMask
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SoftMask : UIBehaviour, ISoftMask, ICanvasRaycastFilter
    {
        #region 实现原理概述

        // SoftMask,为子节点生成了 SoftMaskable组件，这些组件会接管子节点
        // 元素的shader并替换元素在运行时的shader，这在编辑器表现上是无感的
        // SoftMaskable组件是隐藏性的，它的管理是自动进行的，当父节点上存在
        // SoftMask组件时，Maskable组件会自动创建，当SoftMask被销毁时，
        // Maskable组件也会自动销毁。
        // 替换的shader会采样Mask的贴图并与颜色缓冲区相乘。SoftMask默认替换Unity
        // 的默认UI Shader(ETC1版本)。如果SoftMask 遇到一个未知的Shader，SoftMask
        // 将什么也不做并打印警告信息。如果想在自定义的Shader上生效，参考CustomWithSoftMask
        // 来修改。
        // 所有材质替换器 IMaterialReplacer都缓存在SoftMask实例中。默认情况下，Unity绘制UI会
        // 使用很少的材质实例(每个Mask和Clipping 都会生成一个实例)。因此SoftMask创建的
        // IMaterialReplacer 相对较少。

        #endregion

        #region 注入项

        [SerializeField] private MaskSource source = MaskSource.Graphic;
        [SerializeField] private RectTransform separateMask;
        [SerializeField] private Sprite sprite;
        [SerializeField] private BorderMode spriteBorderMode = BorderMode.Simple;
        [SerializeField] private float spritePixelsPerUnitMultiplier = 1f;
        [SerializeField] private Texture texture;
        [SerializeField] private Rect textureUVRect = new Rect(0, 0, 1, 1);
        [SerializeField] private Color channelWeights = MaskChannel.Alpha;
        [SerializeField] private float raycastThreshold;
        [SerializeField] private bool invertMask;
        [SerializeField] private bool invertOutside;

        #endregion

        private readonly MaterialReplacements materials;
        private MaterialParameters parameters;
        private Rect lastMaskRect;
        private bool maskingEnable;
        private bool destroyed;
        private bool dirty;
        private readonly Queue<Transform> transSpawnMaskableQueue = new Queue<Transform>();

        private RectTransform maskTransform;
        private Graphic graphic;
        private Canvas canvas;

        public SoftMask()
        {
            var materialReplacer = new MaterialReplacerChain(
                MaterialReplacer.GlobalReplacers,);
        }
    }
}
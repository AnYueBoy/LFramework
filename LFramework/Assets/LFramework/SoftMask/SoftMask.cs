using UnityEngine;
using UnityEngine.EventSystems;

namespace LFramework.SoftMask
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class SoftMask : UIBehaviour, ISoftMask, ICanvasRaycastFilter
    {
        private MaterialParameters _matParameters;
        private readonly MaterialReplacement _materialReplacement;

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            return true;
        }

        public bool IsAlive { get; }
        public bool IsMaskingEnable { get; }

        public Material GetReplaceMat(Material originalMat)
        {
            throw new System.NotImplementedException();
        }

        public void ReleaseReplaceMat(Material replaceMat)
        {
            throw new System.NotImplementedException();
        }

        public SoftMask()
        {
            _materialReplacement = new MaterialReplacement(new MatDefaultReplacer(), MatParameterHandler);
        }

        private void MatParameterHandler(Material material)
        {
            _matParameters.Apply(material);
        }
    }
}
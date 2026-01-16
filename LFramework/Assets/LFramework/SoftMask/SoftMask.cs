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

        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            return true;
        }

        public bool IsAlive { get; }
    }
}
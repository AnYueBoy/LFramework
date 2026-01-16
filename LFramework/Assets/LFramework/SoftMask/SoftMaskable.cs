using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LFramework.SoftMask
{
    public class SoftMaskable : UIBehaviour, IMaterialModifier
    {
        public Material GetModifiedMaterial(Material baseMaterial)
        {
            return null;
        }
    }
}
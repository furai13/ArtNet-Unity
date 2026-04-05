using System;
using UnityEngine;

namespace ArtNet.Devices.Modular.Output
{
    [Serializable]
    public class MaterialSlotTarget
    {
        public enum TargetMode
        {
            AllMaterials = 0,
            SpecificIndices = 1
        }

        [SerializeField] private TargetMode mode = TargetMode.AllMaterials;
        [SerializeField] private int[] indices = Array.Empty<int>();

        public TargetMode Mode => mode;

        public int[] Indices => indices ?? Array.Empty<int>();

        public bool AppliesToAllMaterials => mode == TargetMode.AllMaterials || Indices.Length == 0;

        public void SetIndices(int[] materialIndices)
        {
            if (materialIndices == null || materialIndices.Length == 0)
            {
                mode = TargetMode.AllMaterials;
                indices = Array.Empty<int>();
                return;
            }

            mode = TargetMode.SpecificIndices;
            indices = materialIndices;
        }
    }
}

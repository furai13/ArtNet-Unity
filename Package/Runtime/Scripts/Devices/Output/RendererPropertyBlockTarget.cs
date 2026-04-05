using System;
using UnityEngine;

namespace ArtNet.Devices.Modular.Output
{
    [Serializable]
    public class RendererPropertyBlockTarget
    {
        [SerializeField] private Renderer renderer;
        [SerializeField] private MaterialSlotTarget materialSlots = new();

        public Renderer Renderer => renderer;

        public MaterialSlotTarget MaterialSlots => materialSlots;

        public void Configure(Renderer targetRenderer, int[] materialIndices)
        {
            renderer = targetRenderer;
            materialSlots ??= new MaterialSlotTarget();
            materialSlots.SetIndices(materialIndices);
        }
    }
}

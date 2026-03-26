using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class StrobeModule : DmxModuleBase
    {
        private static readonly IReadOnlyList<DmxChannelDescriptor> ChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Strobe", 0)
        };

        [SerializeField, Range(0, 255)] private int openThreshold = 15;
        [SerializeField, Min(0.1f)] private float minRateHz = 1f;
        [SerializeField, Min(0.1f)] private float maxRateHz = 20f;

        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            var value = frame.Get8(0);
            if (value <= openThreshold)
            {
                State.StrobeEnabled = false;
                State.StrobeRateHz = 0f;
                return;
            }

            State.StrobeEnabled = true;
            var normalized = Mathf.InverseLerp(openThreshold + 1, 255, value);
            State.StrobeRateHz = Mathf.Lerp(minRateHz, maxRateHz, normalized);
        }

        public override IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors()
        {
            return ChannelDescriptors;
        }
    }
}

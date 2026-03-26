using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class PanTiltModule : DmxModuleBase
    {
        [SerializeField] private bool useFineChannels = true;
        private static readonly IReadOnlyList<DmxChannelDescriptor> FineChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Pan", 0),
            new DmxChannelDescriptor("Pan Fine", 1),
            new DmxChannelDescriptor("Tilt", 2),
            new DmxChannelDescriptor("Tilt Fine", 3)
        };
        private static readonly IReadOnlyList<DmxChannelDescriptor> CoarseChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Pan", 0),
            new DmxChannelDescriptor("Tilt", 1)
        };

        public override int ChannelCount => useFineChannels ? 4 : 2;

        public override void Apply(in DmxFrame frame)
        {
            if (useFineChannels)
            {
                State.PanNormalized = frame.Get16Normalized(0);
                State.TiltNormalized = frame.Get16Normalized(2);
                return;
            }

            State.PanNormalized = frame.Get01(0);
            State.TiltNormalized = frame.Get01(1);
        }

        public override IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors()
        {
            return useFineChannels ? FineChannelDescriptors : CoarseChannelDescriptors;
        }
    }
}

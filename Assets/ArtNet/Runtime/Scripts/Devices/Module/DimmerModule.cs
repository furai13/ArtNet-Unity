using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class DimmerModule : DmxModuleBase
    {
        private static readonly IReadOnlyList<DmxChannelDescriptor> ChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Dimmer", 0)
        };

        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            State.Dimmer = frame.Get01(0);
        }

        public override IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors()
        {
            return ChannelDescriptors;
        }
    }
}

using System.Collections.Generic;

namespace ArtNet.Devices.Modular
{
    public class AngleModule : DmxModuleBase
    {
        private static readonly IReadOnlyList<DmxChannelDescriptor> ChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Angle", 0)
        };

        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            State.BeamAngleNormalized = frame.Get01(0);
        }

        public override IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors()
        {
            return ChannelDescriptors;
        }
    }
}

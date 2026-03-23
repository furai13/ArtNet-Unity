using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class PanTiltModule : DmxModuleBase
    {
        [SerializeField] private bool useFineChannels = true;

        public override int ChannelCount => useFineChannels ? 4 : 2;

        public override void Apply(in DmxFrame frame)
        {
            if (useFineChannels)
            {
                State.PanNormalized = frame.Get16Normalized(ChannelOffset);
                State.TiltNormalized = frame.Get16Normalized(ChannelOffset + 2);
                return;
            }

            State.PanNormalized = frame.Get01(ChannelOffset);
            State.TiltNormalized = frame.Get01(ChannelOffset + 1);
        }
    }
}

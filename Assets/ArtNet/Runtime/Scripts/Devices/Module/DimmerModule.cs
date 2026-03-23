namespace ArtNet.Devices.Modular
{
    public class DimmerModule : DmxModuleBase
    {
        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            State.Dimmer = frame.Get01(ChannelOffset);
        }
    }
}

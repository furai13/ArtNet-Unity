namespace ArtNet.Devices.Modular
{
    public class AngleModule : DmxModuleBase
    {
        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            State.BeamAngleNormalized = frame.Get01(0);
        }
    }
}

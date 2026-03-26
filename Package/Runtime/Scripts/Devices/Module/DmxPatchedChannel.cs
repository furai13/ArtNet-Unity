namespace ArtNet.Devices.Modular
{
    public readonly struct DmxPatchedChannel
    {
        public DmxPatchedChannel(
            DmxFixture fixture,
            DmxModuleBase module,
            DmxChannelDescriptor descriptor,
            int absoluteChannel)
        {
            Fixture = fixture;
            Module = module;
            Descriptor = descriptor;
            AbsoluteChannel = absoluteChannel;
        }

        public DmxFixture Fixture { get; }
        public DmxModuleBase Module { get; }
        public DmxChannelDescriptor Descriptor { get; }
        public int AbsoluteChannel { get; }
    }
}

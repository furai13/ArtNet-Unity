namespace ArtNet.Devices.Modular
{
    public readonly struct DmxChannelDescriptor
    {
        public DmxChannelDescriptor(string name, int relativeOffset)
        {
            Name = name;
            RelativeOffset = relativeOffset;
        }

        public string Name { get; }
        public int RelativeOffset { get; }
    }
}

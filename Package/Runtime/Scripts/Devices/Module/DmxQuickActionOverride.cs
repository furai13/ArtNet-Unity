namespace ArtNet.Devices.Modular
{
    public readonly struct DmxQuickActionOverride
    {
        public DmxQuickActionOverride(int relativeChannel, byte value)
        {
            RelativeChannel = relativeChannel;
            Value = value;
        }

        public int RelativeChannel { get; }
        public byte Value { get; }
    }
}

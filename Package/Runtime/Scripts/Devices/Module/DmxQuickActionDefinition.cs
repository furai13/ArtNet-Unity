namespace ArtNet.Devices.Modular
{
    public readonly struct DmxQuickActionDefinition
    {
        public DmxQuickActionDefinition(string id, string label)
        {
            Id = id;
            Label = label;
        }

        public string Id { get; }
        public string Label { get; }
    }
}

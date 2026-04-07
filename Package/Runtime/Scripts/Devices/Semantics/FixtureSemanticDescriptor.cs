using System;
using ArtNet.Devices.Modular;

namespace ArtNet.Devices.Semantics
{
    public readonly struct FixtureSemanticDescriptor
    {
        public FixtureSemanticDescriptor(
            FixtureSemanticBinding binding,
            DmxFixture fixture,
            FixtureSemanticProfile profile,
            string[] roles,
            string[] capabilities,
            string[] tags,
            string[] exclusionTags,
            FixtureSemanticProperty[] properties,
            float priority)
        {
            Binding = binding;
            Fixture = fixture;
            Profile = profile;
            Roles = roles ?? Array.Empty<string>();
            Capabilities = capabilities ?? Array.Empty<string>();
            Tags = tags ?? Array.Empty<string>();
            ExclusionTags = exclusionTags ?? Array.Empty<string>();
            Properties = properties ?? Array.Empty<FixtureSemanticProperty>();
            Priority = priority;
        }

        public FixtureSemanticBinding Binding { get; }
        public DmxFixture Fixture { get; }
        public FixtureSemanticProfile Profile { get; }
        public string[] Roles { get; }
        public string[] Capabilities { get; }
        public string[] Tags { get; }
        public string[] ExclusionTags { get; }
        public FixtureSemanticProperty[] Properties { get; }
        public float Priority { get; }
    }
}

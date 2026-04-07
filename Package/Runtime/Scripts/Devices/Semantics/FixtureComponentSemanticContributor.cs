using System;
using UnityEngine;

namespace ArtNet.Devices.Semantics
{
    [DisallowMultipleComponent]
    public sealed class FixtureComponentSemanticContributor : FixtureSemanticContributorBase
    {
        [Serializable]
        private struct Entry
        {
            public Component target;
            public string[] roles;
            public string[] capabilities;
            public string[] tags;
            public string[] exclusionTags;
            public FixtureSemanticProperty[] properties;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public override void Contribute(FixtureSemanticBinding binding, FixtureSemanticBuilder builder)
        {
            foreach (var entry in entries)
            {
                if (entry.target == null)
                {
                    continue;
                }

                builder.AddRoles(entry.roles);
                builder.AddCapabilities(entry.capabilities);
                builder.AddTags(entry.tags);
                builder.AddExclusionTags(entry.exclusionTags);
                builder.AddProperties(entry.properties);
            }
        }
    }
}

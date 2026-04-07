using System;
using UnityEngine;

namespace ArtNet.Devices.Semantics
{
    [DisallowMultipleComponent]
    public sealed class FixtureManualSemanticContributor : FixtureSemanticContributorBase
    {
        [SerializeField] private string[] roles = Array.Empty<string>();
        [SerializeField] private string[] capabilities = Array.Empty<string>();
        [SerializeField] private string[] tags = Array.Empty<string>();
        [SerializeField] private string[] exclusionTags = Array.Empty<string>();
        [SerializeField] private FixtureSemanticProperty[] properties = Array.Empty<FixtureSemanticProperty>();

        public override void Contribute(FixtureSemanticBinding binding, FixtureSemanticBuilder builder)
        {
            builder.AddRoles(roles);
            builder.AddCapabilities(capabilities);
            builder.AddTags(tags);
            builder.AddExclusionTags(exclusionTags);
            builder.AddProperties(properties);
        }
    }
}

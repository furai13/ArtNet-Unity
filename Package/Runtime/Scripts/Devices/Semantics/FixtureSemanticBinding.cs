using System;
using System.Collections.Generic;
using ArtNet.Devices.Modular;
using UnityEngine;

namespace ArtNet.Devices.Semantics
{
    [DisallowMultipleComponent]
    public sealed class FixtureSemanticBinding : MonoBehaviour
    {
        [SerializeField] private DmxFixture fixture;
        [SerializeField] private FixtureSemanticProfile profile;
        [SerializeField] private string[] roleOverrides = Array.Empty<string>();
        [SerializeField] private bool replaceProfileRoles;
        [SerializeField] private bool useProfilePriority = true;
        [SerializeField, Range(0f, 1f)] private float priority = 0.5f;
        [SerializeField] private bool includeLocalContributors = true;
        [SerializeField] private bool includeContributorChildren = true;
        [SerializeField] private MonoBehaviour[] explicitContributors = Array.Empty<MonoBehaviour>();
        [SerializeField] private string[] additionalCapabilities = Array.Empty<string>();
        [SerializeField] private string[] additionalTags = Array.Empty<string>();
        [SerializeField] private string[] additionalExclusionTags = Array.Empty<string>();
        [SerializeField] private FixtureSemanticProperty[] additionalProperties = Array.Empty<FixtureSemanticProperty>();

        public DmxFixture Fixture => fixture;
        public FixtureSemanticProfile Profile => profile;
        public float Priority => useProfilePriority && profile != null ? profile.DefaultPriority : priority;

        public string[] Roles
        {
            get
            {
                var builder = CreateBuilder();
                return builder.BuildRoles();
            }
        }

        public string[] Tags => CreateBuilder().BuildTags();
        public string[] ExclusionTags => CreateBuilder().BuildExclusionTags();

        public string[] GetCapabilities()
        {
            return CreateBuilder().BuildCapabilities();
        }

        public FixtureSemanticProperty[] GetProperties()
        {
            return CreateBuilder().BuildProperties();
        }

        public FixtureSemanticDescriptor BuildDescriptor()
        {
            return new FixtureSemanticDescriptor(
                this,
                fixture,
                profile,
                Roles,
                GetCapabilities(),
                Tags,
                ExclusionTags,
                GetProperties(),
                Priority);
        }

        private void Reset()
        {
            fixture = GetComponent<DmxFixture>();
            if (fixture == null)
            {
                fixture = GetComponentInParent<DmxFixture>();
            }
        }

        private FixtureSemanticBuilder CreateBuilder()
        {
            var builder = new FixtureSemanticBuilder();
            builder.AddRoles(profile != null ? profile.Roles : null);
            builder.AddCapabilities(profile != null ? profile.Capabilities : null);
            builder.AddTags(profile != null ? profile.Tags : null);
            builder.AddExclusionTags(profile != null ? profile.ExclusionTags : null);
            builder.AddProperties(profile != null ? profile.Properties : null);

            if (replaceProfileRoles)
            {
                builder.ClearRoles();
            }

            builder.AddRoles(roleOverrides);
            builder.AddCapabilities(additionalCapabilities);
            builder.AddTags(additionalTags);
            builder.AddExclusionTags(additionalExclusionTags);
            builder.AddProperties(additionalProperties);

            foreach (var contributor in GetContributors())
            {
                contributor.Contribute(this, builder);
            }

            return builder;
        }

        private IEnumerable<IFixtureSemanticContributor> GetContributors()
        {
            var contributors = new List<IFixtureSemanticContributor>();

            if (includeLocalContributors)
            {
                var localComponents = includeContributorChildren
                    ? GetComponentsInChildren<MonoBehaviour>(true)
                    : GetComponents<MonoBehaviour>();
                AddContributors(contributors, localComponents);
            }

            AddContributors(contributors, explicitContributors);
            return contributors;
        }

        private static void AddContributors(List<IFixtureSemanticContributor> contributors, IEnumerable<MonoBehaviour> components)
        {
            if (components == null)
            {
                return;
            }

            foreach (var component in components)
            {
                var contributor = component as IFixtureSemanticContributor;
                if (contributor == null)
                {
                    continue;
                }

                if (!contributors.Contains(contributor))
                {
                    contributors.Add(contributor);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArtNet.Devices.Semantics
{
    public static class FixtureSemanticRegistry
    {
        public static IReadOnlyList<FixtureSemanticBinding> FindBindings(bool includeInactive = true)
        {
            return Object.FindObjectsOfType<FixtureSemanticBinding>(includeInactive)
                .Where(binding => binding != null && binding.Fixture != null)
                .OrderBy(binding => binding.name)
                .ToArray();
        }

        public static IReadOnlyList<FixtureSemanticDescriptor> BuildDescriptors(bool includeInactive = true)
        {
            var bindings = FindBindings(includeInactive);
            var descriptors = new List<FixtureSemanticDescriptor>(bindings.Count);
            foreach (var binding in bindings)
            {
                descriptors.Add(binding.BuildDescriptor());
            }

            return descriptors;
        }

        public static IReadOnlyList<FixtureSemanticDescriptor> Find(
            Func<FixtureSemanticDescriptor, bool> predicate,
            bool includeInactive = true)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return BuildDescriptors(includeInactive)
                .Where(predicate)
                .ToArray();
        }

        public static IReadOnlyList<FixtureSemanticDescriptor> FindByRole(
            string roleId,
            bool includeInactive = true)
        {
            return Find(
                descriptor => descriptor.Roles.Contains(roleId, StringComparer.OrdinalIgnoreCase),
                includeInactive);
        }

        public static IReadOnlyList<FixtureSemanticDescriptor> FindByCapability(
            string capabilityId,
            bool includeInactive = true)
        {
            return Find(
                descriptor => descriptor.Capabilities.Contains(capabilityId, StringComparer.OrdinalIgnoreCase),
                includeInactive);
        }

        public static IReadOnlyList<FixtureSemanticDescriptor> FindByTag(
            string tag,
            bool includeInactive = true)
        {
            return Find(
                descriptor => descriptor.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase),
                includeInactive);
        }

        public static IReadOnlyList<FixtureSemanticDescriptor> FindByProperty(
            string key,
            string value = null,
            bool includeInactive = true)
        {
            return Find(
                descriptor => descriptor.Properties.Any(property =>
                    string.Equals(property.Key, key, StringComparison.OrdinalIgnoreCase) &&
                    (value == null || string.Equals(property.Value, value, StringComparison.Ordinal))),
                includeInactive);
        }
    }
}

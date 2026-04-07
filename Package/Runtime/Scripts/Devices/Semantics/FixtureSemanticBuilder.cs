using System;
using System.Collections.Generic;
using System.Linq;

namespace ArtNet.Devices.Semantics
{
    public sealed class FixtureSemanticBuilder
    {
        private readonly List<string> _roles = new List<string>();
        private readonly List<string> _capabilities = new List<string>();
        private readonly List<string> _tags = new List<string>();
        private readonly List<string> _exclusionTags = new List<string>();
        private readonly List<FixtureSemanticProperty> _properties = new List<FixtureSemanticProperty>();

        public void AddRole(string role)
        {
            AddDistinct(_roles, role);
        }

        public void AddCapability(string capability)
        {
            AddDistinct(_capabilities, capability);
        }

        public void AddTag(string tag)
        {
            AddDistinct(_tags, tag);
        }

        public void AddExclusionTag(string tag)
        {
            AddDistinct(_exclusionTags, tag);
        }

        public void AddProperty(string key, string value)
        {
            AddProperty(new FixtureSemanticProperty(key, value));
        }

        public void AddProperty(FixtureSemanticProperty property)
        {
            if (string.IsNullOrWhiteSpace(property.Key))
            {
                return;
            }

            if (!_properties.Contains(property))
            {
                _properties.Add(property);
            }
        }

        public void AddRoles(IEnumerable<string> roles)
        {
            AddDistinct(_roles, roles);
        }

        public void AddCapabilities(IEnumerable<string> capabilities)
        {
            AddDistinct(_capabilities, capabilities);
        }

        public void AddTags(IEnumerable<string> tags)
        {
            AddDistinct(_tags, tags);
        }

        public void AddExclusionTags(IEnumerable<string> tags)
        {
            AddDistinct(_exclusionTags, tags);
        }

        public void AddProperties(IEnumerable<FixtureSemanticProperty> properties)
        {
            if (properties == null)
            {
                return;
            }

            foreach (var property in properties)
            {
                AddProperty(property);
            }
        }

        public void ClearRoles()
        {
            _roles.Clear();
        }

        public string[] BuildRoles()
        {
            return _roles.ToArray();
        }

        public string[] BuildCapabilities()
        {
            return _capabilities.ToArray();
        }

        public string[] BuildTags()
        {
            return _tags.ToArray();
        }

        public string[] BuildExclusionTags()
        {
            return _exclusionTags.ToArray();
        }

        public FixtureSemanticProperty[] BuildProperties()
        {
            return _properties.ToArray();
        }

        private static void AddDistinct(List<string> values, IEnumerable<string> candidates)
        {
            if (candidates == null)
            {
                return;
            }

            foreach (var candidate in candidates)
            {
                AddDistinct(values, candidate);
            }
        }

        private static void AddDistinct(List<string> values, string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            if (!values.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                values.Add(candidate);
            }
        }
    }
}

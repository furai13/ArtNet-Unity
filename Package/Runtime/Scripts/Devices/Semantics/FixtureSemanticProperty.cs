using System;
using UnityEngine;

namespace ArtNet.Devices.Semantics
{
    [Serializable]
    public struct FixtureSemanticProperty : IEquatable<FixtureSemanticProperty>
    {
        [SerializeField] private string key;
        [SerializeField] private string value;

        public FixtureSemanticProperty(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public string Key => key ?? string.Empty;
        public string Value => value ?? string.Empty;

        public bool Equals(FixtureSemanticProperty other)
        {
            return string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is FixtureSemanticProperty other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(Key),
                StringComparer.Ordinal.GetHashCode(Value));
        }
    }
}

using System;
using UnityEngine;

namespace ArtNet.Devices.Semantics
{
    [CreateAssetMenu(
        fileName = "FixtureSemanticProfile",
        menuName = "ArtNet/Fixture Semantic Profile")]
    public sealed class FixtureSemanticProfile : ScriptableObject
    {
        [SerializeField] private string[] roles = Array.Empty<string>();
        [SerializeField] private string[] capabilities = Array.Empty<string>();
        [SerializeField] private string[] tags = Array.Empty<string>();
        [SerializeField] private string[] exclusionTags = Array.Empty<string>();
        [SerializeField] private FixtureSemanticProperty[] properties = Array.Empty<FixtureSemanticProperty>();
        [SerializeField, Range(0f, 1f)] private float defaultPriority = 0.5f;

        public string[] Roles => roles ?? Array.Empty<string>();
        public string[] Capabilities => capabilities ?? Array.Empty<string>();
        public string[] Tags => tags ?? Array.Empty<string>();
        public string[] ExclusionTags => exclusionTags ?? Array.Empty<string>();
        public FixtureSemanticProperty[] Properties => properties ?? Array.Empty<FixtureSemanticProperty>();
        public float DefaultPriority => defaultPriority;
    }
}

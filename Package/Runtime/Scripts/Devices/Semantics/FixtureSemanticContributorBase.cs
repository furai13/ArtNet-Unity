using UnityEngine;

namespace ArtNet.Devices.Semantics
{
    public abstract class FixtureSemanticContributorBase : MonoBehaviour, IFixtureSemanticContributor
    {
        public abstract void Contribute(FixtureSemanticBinding binding, FixtureSemanticBuilder builder);
    }
}

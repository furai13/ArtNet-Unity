namespace ArtNet.Devices.Semantics
{
    public interface IFixtureSemanticContributor
    {
        void Contribute(FixtureSemanticBinding binding, FixtureSemanticBuilder builder);
    }
}

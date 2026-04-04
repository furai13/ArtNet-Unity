using UnityEngine;

namespace ArtNet.Devices.Modular.Output
{
    public abstract class FixtureOutputBase : MonoBehaviour
    {
        protected DmxFixture Fixture { get; private set; }
        protected DmxFixtureState State => Fixture.State;

        internal void Initialize(DmxFixture fixture)
        {
            Fixture = fixture;
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        public abstract void Apply();

        public virtual void Tick(float deltaTime)
        {
        }
    }
}

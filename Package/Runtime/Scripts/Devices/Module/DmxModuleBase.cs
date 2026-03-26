using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public abstract class DmxModuleBase : MonoBehaviour
    {
        protected DmxFixture Fixture { get; private set; }
        protected DmxFixtureState State => Fixture.State;

        public abstract int ChannelCount { get; }

        internal void Initialize(DmxFixture fixture)
        {
            Fixture = fixture;
            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        public abstract void Apply(in DmxFrame frame);

        public virtual void Tick(float deltaTime)
        {
        }

        public abstract IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors();
    }
}

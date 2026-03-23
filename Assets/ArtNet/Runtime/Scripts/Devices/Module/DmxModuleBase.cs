using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public abstract class DmxModuleBase : MonoBehaviour
    {
        [SerializeField, Min(0)] private int channelOffset;
        [SerializeField] private bool autoAssignChannel = true;

        protected ModularDmxFixture Fixture { get; private set; }
        protected DmxFixtureState State => Fixture.State;

        public int ChannelOffset => channelOffset;
        public bool AutoAssignChannel => autoAssignChannel;
        public abstract int ChannelCount { get; }
        public virtual int ExecutionOrder => 0;

        internal void Initialize(ModularDmxFixture fixture)
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

#if UNITY_EDITOR
        internal void SetChannelOffset(int offset)
        {
            channelOffset = offset;
        }
#endif
    }
}

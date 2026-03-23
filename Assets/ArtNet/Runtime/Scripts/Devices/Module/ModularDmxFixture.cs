using System;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class ModularDmxFixture : global::ArtNet.Devices.DmxDeviceBase
    {
        [SerializeField] private bool autoAssignChannels = true;

        private byte _channelCount;
        private DmxModuleBase[] _modules = Array.Empty<DmxModuleBase>();

        public override byte ChannelNumber => _channelCount;
        public DmxFixtureState State { get; } = new();

        protected override void InitFixture()
        {
            _modules = GetComponentsInChildren<DmxModuleBase>(true);
            Array.Sort(_modules, CompareModules);

            if (autoAssignChannels)
            {
                AutoAssignModuleChannels();
            }

            var maxChannel = 0;
            foreach (var module in _modules)
            {
                module.Initialize(this);
                maxChannel = Mathf.Max(maxChannel, module.ChannelOffset + module.ChannelCount);
            }

            _channelCount = (byte)Mathf.Clamp(maxChannel, 0, 255);
        }

        protected override void UpdateProperties()
        {
            var frame = new DmxFrame(DmxData);
            foreach (var module in _modules)
            {
                module.Apply(frame);
            }
        }

        private void Update()
        {
            foreach (var module in _modules)
            {
                module.Tick(Time.deltaTime);
            }
        }

        private static int CompareModules(DmxModuleBase a, DmxModuleBase b)
        {
            var order = a.ExecutionOrder.CompareTo(b.ExecutionOrder);
            return order != 0 ? order : a.ChannelOffset.CompareTo(b.ChannelOffset);
        }

        private void AutoAssignModuleChannels()
        {
            var nextChannel = 0;
            foreach (var module in _modules)
            {
                if (!module.AutoAssignChannel)
                {
                    nextChannel = Mathf.Max(nextChannel, module.ChannelOffset + module.ChannelCount);
                    continue;
                }

#if UNITY_EDITOR
                module.SetChannelOffset(nextChannel);
#endif
                nextChannel += module.ChannelCount;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class ModularDmxFixture : global::ArtNet.Devices.DmxDeviceBase
    {
        [Serializable]
        public struct ModuleEntry
        {
            public DmxModuleBase module;
            [Min(0)] public int offset;
        }

        [SerializeField] private ModuleEntry[] modules = Array.Empty<ModuleEntry>();

        private byte _channelCount;
        private ModuleEntry[] _runtimeModules = Array.Empty<ModuleEntry>();

        public override byte ChannelNumber => _channelCount;
        public DmxFixtureState State { get; } = new();
        public ModuleEntry[] Modules => modules;

        protected override void InitFixture()
        {
            _runtimeModules = CollectValidModules();

            var maxChannel = 0;
            foreach (var entry in _runtimeModules)
            {
                entry.module.Initialize(this);
                maxChannel = Mathf.Max(maxChannel, entry.offset + entry.module.ChannelCount);
            }

            _channelCount = (byte)Mathf.Clamp(maxChannel, 0, 255);
        }

        protected override void UpdateProperties()
        {
            var frame = new DmxFrame(DmxData);
            foreach (var entry in _runtimeModules)
            {
                entry.module.Apply(frame.Slice(entry.offset));
            }
        }

        private void Update()
        {
            foreach (var entry in _runtimeModules)
            {
                entry.module.Tick(Time.deltaTime);
            }
        }

        internal ModuleEntry[] CollectModulesFromChildren()
        {
            var childModules = GetComponentsInChildren<DmxModuleBase>(true);
            Array.Sort(childModules, CompareModuleOrder);

            var collected = new ModuleEntry[childModules.Length];
            for (var i = 0; i < childModules.Length; i++)
            {
                collected[i] = new ModuleEntry
                {
                    module = childModules[i],
                    offset = 0
                };
            }

            return collected;
        }

        public void RegisterChildModules()
        {
            modules = CollectModulesFromChildren();
        }

        public ModuleEntry CreateEntry(DmxModuleBase module, int offset)
        {
            return new ModuleEntry
            {
                module = module,
                offset = Mathf.Max(0, offset)
            };
        }

        public void SetModules(ModuleEntry[] entries)
        {
            modules = entries ?? Array.Empty<ModuleEntry>();
        }

        internal ModuleEntry[] CollectValidModules()
        {
            var validEntries = new List<ModuleEntry>(modules.Length);
            foreach (var entry in modules)
            {
                if (entry.module == null)
                {
                    continue;
                }

                validEntries.Add(entry);
            }

            validEntries.Sort(CompareModuleEntries);
            return validEntries.ToArray();
        }

        public void AutoAssignModuleChannels()
        {
            var nextChannel = 0;
            for (var i = 0; i < modules.Length; i++)
            {
                if (modules[i].module == null)
                {
                    continue;
                }

                modules[i].offset = nextChannel;
                nextChannel += modules[i].module.ChannelCount;
            }
        }

        [ContextMenu("Auto Assign Channels")]
        private void AutoAssignChannelsFromContextMenu()
        {
            AutoAssignModuleChannels();
        }

        private static int CompareModuleEntries(ModuleEntry a, ModuleEntry b)
        {
            return CompareModuleOrder(a.module, b.module);
        }

        private static int CompareModuleOrder(DmxModuleBase a, DmxModuleBase b)
        {
            var order = a.ExecutionOrder.CompareTo(b.ExecutionOrder);
            if (order != 0)
            {
                return order;
            }

            return string.CompareOrdinal(a.name, b.name);
        }
    }
}

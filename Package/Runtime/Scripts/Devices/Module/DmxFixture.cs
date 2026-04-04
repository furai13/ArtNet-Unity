using System;
using System.Collections.Generic;
using System.Linq;
using ArtNet.Common;
using ArtNet.Devices.Modular.Output;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class DmxFixture : global::ArtNet.Devices.DmxDeviceBase
    {
        [Serializable]
        public struct ModuleEntry
        {
            public DmxModuleBase module;
            [Min(0)] public int offset;
        }

        [SerializeField] private string profileName;
        [SerializeField] private string groupId;
        [SerializeField] private ModuleEntry[] modules = Array.Empty<ModuleEntry>();
        [SerializeField] private FixtureOutputBase[] outputs = Array.Empty<FixtureOutputBase>();

        private byte _channelCount;
        private bool _resetRequested;

        public override byte ChannelNumber => _channelCount;
        public DmxFixtureState State { get; } = new();
        public string ProfileName => profileName;
        public string GroupId => groupId;
        public IReadOnlyList<ModuleEntry> Modules => modules;
        public IReadOnlyList<FixtureOutputBase> Outputs => outputs;
        public int ChannelFootprint => modules.Where(entry => entry.module != null)
            .Select(entry => entry.offset + entry.module.ChannelCount)
            .DefaultIfEmpty(0)
            .Max();

        protected override void InitFixture()
        {
            var maxChannel = 0;
            foreach (var entry in modules)
            {
                if (entry.module == null)
                {
                    continue;
                }

                entry.module.Initialize(this);
                maxChannel = Mathf.Max(maxChannel, entry.offset + entry.module.ChannelCount);
            }

            foreach (var output in outputs)
            {
                if (output == null)
                {
                    continue;
                }

                output.Initialize(this);
            }

            if (maxChannel > byte.MaxValue)
            {
                ArtNetLogger.LogWarn(
                    $"{name} channel footprint is {maxChannel}, but DmxDeviceBase.ChannelNumber is limited to {byte.MaxValue}. " +
                    "Modules above that range will be ignored.");
            }

            _channelCount = (byte)Mathf.Clamp(maxChannel, 0, 255);
        }

        public void ReinitializeFixture()
        {
            State.ResetToDefaults();
            InitFixture();
            DmxData = new byte[ChannelNumber];
            UpdateProperties();
        }

        protected override void UpdateProperties()
        {
            var frame = new DmxFrame(DmxData);
            _resetRequested = false;
            foreach (var entry in modules)
            {
                if (entry.module == null)
                {
                    continue;
                }

                if (entry.offset < 0 || entry.offset >= DmxData.Length)
                {
                    ArtNetLogger.LogWarn(
                        $"{name} skipped module {entry.module.GetType().Name} because offset {entry.offset} is outside the DMX buffer length {DmxData.Length}.");
                    continue;
                }

                if (entry.offset + entry.module.ChannelCount > DmxData.Length)
                {
                    ArtNetLogger.LogWarn(
                        $"{name} skipped module {entry.module.GetType().Name} because offset {entry.offset} + count {entry.module.ChannelCount} exceeds DMX buffer length {DmxData.Length}.");
                    continue;
                }

                entry.module.Apply(frame.Slice(entry.offset));
            }

            if (_resetRequested)
            {
                PerformRequestedReset();
                return;
            }

            foreach (var output in outputs)
            {
                if (output == null)
                {
                    continue;
                }

                output.Apply();
            }
        }

        private void Update()
        {
            foreach (var entry in modules)
            {
                if (entry.module == null)
                {
                    continue;
                }

                entry.module.Tick(Time.deltaTime);
            }

            foreach (var output in outputs)
            {
                if (output == null)
                {
                    continue;
                }

                output.Tick(Time.deltaTime);
            }
        }

        internal ModuleEntry[] CollectModulesFromChildren()
        {
            var childModules = GetComponentsInChildren<DmxModuleBase>(true);

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

        internal void RequestReset()
        {
            _resetRequested = true;
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

        internal FixtureOutputBase[] CollectOutputsFromChildren()
        {
            var childOutputs = GetComponentsInChildren<FixtureOutputBase>(true);
            return childOutputs;
        }

        public void RegisterChildOutputs()
        {
            outputs = CollectOutputsFromChildren();
        }

        public void SetOutputs(FixtureOutputBase[] entries)
        {
            outputs = entries ?? Array.Empty<FixtureOutputBase>();
        }

        public void SetFixtureMetadata(string newProfileName, string newGroupId)
        {
            profileName = newProfileName ?? string.Empty;
            groupId = newGroupId ?? string.Empty;
        }

        public IReadOnlyList<DmxPatchedChannel> GetPatchedChannels()
        {
            var patchedChannels = new List<DmxPatchedChannel>();
            foreach (var entry in modules)
            {
                if (entry.module == null)
                {
                    continue;
                }

                foreach (var descriptor in entry.module.GetChannelDescriptors())
                {
                    patchedChannels.Add(new DmxPatchedChannel(
                        this,
                        entry.module,
                        descriptor,
                        StartAddress + entry.offset + descriptor.RelativeOffset));
                }
            }

            return patchedChannels;
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

        private void PerformRequestedReset()
        {
            _resetRequested = false;
            ReinitializeFixture();
        }
    }
}

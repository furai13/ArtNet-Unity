using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class PanTiltModule : DmxModuleBase
    {
        [SerializeField] private bool useFineChannels = true;
        private static readonly IReadOnlyList<DmxChannelDescriptor> FineChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Pan", 0),
            new DmxChannelDescriptor("Pan Fine", 1),
            new DmxChannelDescriptor("Tilt", 2),
            new DmxChannelDescriptor("Tilt Fine", 3)
        };
        private static readonly IReadOnlyList<DmxChannelDescriptor> CoarseChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Pan", 0),
            new DmxChannelDescriptor("Tilt", 1)
        };
        private static readonly IReadOnlyList<DmxQuickActionDefinition> QuickActions = new[]
        {
            new DmxQuickActionDefinition(DmxQuickActionIds.Center, "Center")
        };

        public override int ChannelCount => useFineChannels ? 4 : 2;

        public override void Apply(in DmxFrame frame)
        {
            if (useFineChannels)
            {
                State.PanNormalized = frame.Get16Normalized(0);
                State.TiltNormalized = frame.Get16Normalized(2);
                return;
            }

            State.PanNormalized = frame.Get01(0);
            State.TiltNormalized = frame.Get01(1);
        }

        public override IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors()
        {
            return useFineChannels ? FineChannelDescriptors : CoarseChannelDescriptors;
        }

        public override IReadOnlyList<DmxQuickActionDefinition> GetQuickActionDefinitions()
        {
            return QuickActions;
        }

        public override bool TryBuildQuickAction(string actionId, List<DmxQuickActionOverride> overrides)
        {
            if (actionId != DmxQuickActionIds.Center)
            {
                return false;
            }

            if (useFineChannels)
            {
                Add16BitCenter(overrides, 0);
                Add16BitCenter(overrides, 2);
                return true;
            }

            overrides.Add(new DmxQuickActionOverride(0, 127));
            overrides.Add(new DmxQuickActionOverride(1, 127));
            return true;
        }

        private static void Add16BitCenter(List<DmxQuickActionOverride> overrides, int startChannel)
        {
            const ushort centerValue = 32768;
            overrides.Add(new DmxQuickActionOverride(startChannel, (byte)(centerValue >> 8)));
            overrides.Add(new DmxQuickActionOverride(startChannel + 1, (byte)(centerValue & 0xFF)));
        }
    }
}

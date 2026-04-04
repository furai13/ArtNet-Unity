using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class DimmerModule : DmxModuleBase
    {
        private static readonly IReadOnlyList<DmxChannelDescriptor> ChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Dimmer", 0)
        };
        private static readonly IReadOnlyList<DmxQuickActionDefinition> QuickActions = new[]
        {
            new DmxQuickActionDefinition(DmxQuickActionIds.Blackout, "Blackout"),
            new DmxQuickActionDefinition(DmxQuickActionIds.Full, "Full")
        };

        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            State.Dimmer = frame.Get01(0);
        }

        public override IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors()
        {
            return ChannelDescriptors;
        }

        public override IReadOnlyList<DmxQuickActionDefinition> GetQuickActionDefinitions()
        {
            return QuickActions;
        }

        public override bool TryBuildQuickAction(string actionId, List<DmxQuickActionOverride> overrides)
        {
            switch (actionId)
            {
                case DmxQuickActionIds.Blackout:
                    overrides.Add(new DmxQuickActionOverride(0, 0));
                    return true;
                case DmxQuickActionIds.Full:
                    overrides.Add(new DmxQuickActionOverride(0, byte.MaxValue));
                    return true;
                default:
                    return false;
            }
        }
    }
}

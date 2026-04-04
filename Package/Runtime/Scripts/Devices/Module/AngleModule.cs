using System.Collections.Generic;

namespace ArtNet.Devices.Modular
{
    public class AngleModule : DmxModuleBase
    {
        private static readonly IReadOnlyList<DmxChannelDescriptor> ChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Angle", 0)
        };
        private static readonly IReadOnlyList<DmxQuickActionDefinition> QuickActions = new[]
        {
            new DmxQuickActionDefinition(DmxQuickActionIds.Center, "Center"),
            new DmxQuickActionDefinition(DmxQuickActionIds.Full, "Full")
        };

        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            State.BeamAngleNormalized = frame.Get01(0);
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
                case DmxQuickActionIds.Center:
                    overrides.Add(new DmxQuickActionOverride(0, 127));
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

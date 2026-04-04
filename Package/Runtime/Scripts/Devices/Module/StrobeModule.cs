using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class StrobeModule : DmxModuleBase
    {
        private static readonly IReadOnlyList<DmxChannelDescriptor> ChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Strobe", 0)
        };
        private static readonly IReadOnlyList<DmxQuickActionDefinition> QuickActions = new[]
        {
            new DmxQuickActionDefinition(DmxQuickActionIds.Open, "Open"),
            new DmxQuickActionDefinition(DmxQuickActionIds.Stop, "Stop"),
            new DmxQuickActionDefinition(DmxQuickActionIds.Full, "Full")
        };

        [SerializeField, Range(0, 255)] private int openThreshold = 15;
        [SerializeField, Min(0.1f)] private float minRateHz = 1f;
        [SerializeField, Min(0.1f)] private float maxRateHz = 20f;

        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            var value = frame.Get8(0);
            if (value <= openThreshold)
            {
                State.StrobeEnabled = false;
                State.StrobeRateHz = 0f;
                return;
            }

            State.StrobeEnabled = true;
            var normalized = Mathf.InverseLerp(openThreshold + 1, 255, value);
            State.StrobeRateHz = Mathf.Lerp(minRateHz, maxRateHz, normalized);
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
                case DmxQuickActionIds.Open:
                case DmxQuickActionIds.Stop:
                    overrides.Add(new DmxQuickActionOverride(0, (byte)Mathf.Clamp(openThreshold, 0, 255)));
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

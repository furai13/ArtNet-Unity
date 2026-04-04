using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class ColorModule : DmxModuleBase
    {
        [SerializeField] private ColorBlendMode blendMode = ColorBlendMode.Overwrite;
        private static readonly IReadOnlyList<DmxChannelDescriptor> ChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Red", 0),
            new DmxChannelDescriptor("Green", 1),
            new DmxChannelDescriptor("Blue", 2)
        };
        private static readonly IReadOnlyList<DmxQuickActionDefinition> QuickActions = new[]
        {
            new DmxQuickActionDefinition(DmxQuickActionIds.Blackout, "Blackout"),
            new DmxQuickActionDefinition(DmxQuickActionIds.Full, "Full"),
            new DmxQuickActionDefinition(DmxQuickActionIds.White, "White")
        };

        public override int ChannelCount => 3;

        public override void Apply(in DmxFrame frame)
        {
            var first = frame.Get01(0);
            var second = frame.Get01(1);
            var third = frame.Get01(2);
            var color = new Color(first, second, third);

            State.Color = blendMode == ColorBlendMode.Multiply
                ? new Color(State.Color.r * color.r, State.Color.g * color.g, State.Color.b * color.b, 1f)
                : color;
        }

        public void Configure(ColorBlendMode colorBlendMode)
        {
            blendMode = colorBlendMode;
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
                    AddRgbOverrides(overrides, 0);
                    return true;
                case DmxQuickActionIds.Full:
                case DmxQuickActionIds.White:
                    AddRgbOverrides(overrides, byte.MaxValue);
                    return true;
                default:
                    return false;
            }
        }

        private static void AddRgbOverrides(List<DmxQuickActionOverride> overrides, byte value)
        {
            overrides.Add(new DmxQuickActionOverride(0, value));
            overrides.Add(new DmxQuickActionOverride(1, value));
            overrides.Add(new DmxQuickActionOverride(2, value));
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class ColorWheelModule : DmxModuleBase
    {
        [SerializeField] private ColorBlendMode blendMode = ColorBlendMode.Overwrite;
        [SerializeField] private Color[] colors =
        {
            Color.white,
            Color.red,
            Color.yellow,
            Color.green,
            Color.cyan,
            Color.blue,
            Color.magenta
        };
        private static readonly IReadOnlyList<DmxChannelDescriptor> ChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Color Wheel", 0)
        };
        private static readonly IReadOnlyList<DmxQuickActionDefinition> QuickActions = new[]
        {
            new DmxQuickActionDefinition(DmxQuickActionIds.Open, "Open"),
            new DmxQuickActionDefinition(DmxQuickActionIds.White, "White")
        };

        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            if (colors == null || colors.Length == 0)
            {
                return;
            }

            var index = Mathf.Min(colors.Length - 1, frame.Get8(0) * colors.Length / 256);
            var color = colors[index];
            State.Color = blendMode == ColorBlendMode.Multiply
                ? new Color(State.Color.r * color.r, State.Color.g * color.g, State.Color.b * color.b, 1f)
                : color;
        }

        public void Configure(Color[] wheelColors, ColorBlendMode colorBlendMode)
        {
            if (wheelColors != null && wheelColors.Length > 0)
            {
                colors = wheelColors;
            }

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
                case DmxQuickActionIds.Open:
                case DmxQuickActionIds.White:
                    if (!TryFindClosestColorIndex(Color.white, out var index))
                    {
                        return false;
                    }

                    overrides.Add(new DmxQuickActionOverride(0, EncodeColorIndex(index)));
                    return true;
                default:
                    return false;
            }
        }

        private bool TryFindClosestColorIndex(Color target, out int index)
        {
            index = -1;
            if (colors == null || colors.Length == 0)
            {
                return false;
            }

            var bestDistance = float.PositiveInfinity;
            for (var i = 0; i < colors.Length; i++)
            {
                var candidate = colors[i];
                var distance =
                    Mathf.Abs(candidate.r - target.r) +
                    Mathf.Abs(candidate.g - target.g) +
                    Mathf.Abs(candidate.b - target.b);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    index = i;
                }
            }

            return index >= 0;
        }

        private byte EncodeColorIndex(int index)
        {
            var bucketSize = 256f / colors.Length;
            var encoded = Mathf.FloorToInt(bucketSize * index + bucketSize * 0.5f);
            return (byte)Mathf.Clamp(encoded, 0, 255);
        }
    }
}

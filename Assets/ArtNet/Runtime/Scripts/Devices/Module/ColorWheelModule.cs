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
    }
}

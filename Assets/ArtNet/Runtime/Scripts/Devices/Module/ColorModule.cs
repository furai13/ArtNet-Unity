using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class ColorModule : DmxModuleBase
    {
        public enum ColorOrder
        {
            RGB,
            RBG,
            GRB,
            GBR,
            BRG,
            BGR
        }

        [SerializeField] private ColorOrder colorOrder = ColorOrder.RGB;

        public override int ChannelCount => 3;

        public override void Apply(in DmxFrame frame)
        {
            var first = frame.Get01(ChannelOffset);
            var second = frame.Get01(ChannelOffset + 1);
            var third = frame.Get01(ChannelOffset + 2);

            State.Color = colorOrder switch
            {
                ColorOrder.RGB => new Color(first, second, third, 1f),
                ColorOrder.RBG => new Color(first, third, second, 1f),
                ColorOrder.GRB => new Color(second, first, third, 1f),
                ColorOrder.GBR => new Color(third, first, second, 1f),
                ColorOrder.BRG => new Color(second, third, first, 1f),
                ColorOrder.BGR => new Color(third, second, first, 1f),
                _ => Color.white
            };
        }
    }
}

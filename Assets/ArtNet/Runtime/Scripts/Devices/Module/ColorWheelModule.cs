using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class ColorWheelModule : DmxModuleBase
    {
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

        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            if (colors == null || colors.Length == 0)
            {
                return;
            }

            var index = Mathf.Min(colors.Length - 1, frame.Get8(0) * colors.Length / 256);
            State.Color = colors[index];
        }
    }
}

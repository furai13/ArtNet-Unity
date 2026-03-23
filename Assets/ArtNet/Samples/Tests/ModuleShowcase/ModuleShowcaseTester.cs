using ArtNet.Devices.Modular;
using UnityEngine;

namespace ArtNet.Samples.Devices
{
    public class ModuleShowcaseTester : MonoBehaviour
    {
        [SerializeField] private bool autoPlay = true;
        [SerializeField, Min(0.1f)] private float demoSpeed = 1f;

        [SerializeField] private ModularDmxFixture _movingHeadFixture;
        [SerializeField] private ModularDmxFixture _colorWheelFixture;
        [SerializeField] private ModularDmxFixture _directValueFixture;

        public void Configure(
            ModularDmxFixture movingHeadFixture,
            ModularDmxFixture colorWheelFixture,
            ModularDmxFixture directValueFixture)
        {
            _movingHeadFixture = movingHeadFixture;
            _colorWheelFixture = colorWheelFixture;
            _directValueFixture = directValueFixture;
        }

        private void Update()
        {
            if (!autoPlay)
            {
                return;
            }

            var t = Time.time * demoSpeed;

            UpdateMovingHeadFixture(t);
            UpdateColorWheelFixture(t);
            UpdateDirectValueFixture(t);
        }

        private void UpdateMovingHeadFixture(float time)
        {
            if (_movingHeadFixture == null)
            {
                return;
            }

            var data = new byte[12];
            data[0] = ToByte(Mathf.Sin(time * 0.7f) * 0.5f + 0.5f);
            data[1] = ToByte(Mathf.Sin(time * 0.9f + 2f) * 0.5f + 0.5f);
            data[2] = ToByte(Mathf.Sin(time * 1.1f + 4f) * 0.5f + 0.5f);
            data[3] = 255;

            Write16(data, 4, Mathf.Sin(time * 0.35f) * 0.5f + 0.5f);
            Write16(data, 6, Mathf.Sin(time * 0.27f + 1f) * 0.5f + 0.5f);

            data[8] = ToByte(Mathf.Sin(time * 0.5f) * 0.5f + 0.5f);
            data[9] = Mathf.Sin(time * 1.7f) > 0.2f ? (byte)190 : (byte)0;

            var goboSelection = (Mathf.FloorToInt(time * 0.25f) % 3) + 1;
            data[10] = (byte)(goboSelection * 80);
            data[11] = 220;

            _movingHeadFixture.DmxUpdate(data);
        }

        private void UpdateColorWheelFixture(float time)
        {
            if (_colorWheelFixture == null)
            {
                return;
            }

            var data = new byte[2];
            data[0] = ToByte(Mathf.Repeat(time * 0.12f, 1f));
            data[1] = ToByte(Mathf.Sin(time * 1.3f) * 0.5f + 0.5f);
            _colorWheelFixture.DmxUpdate(data);
        }

        private void UpdateDirectValueFixture(float time)
        {
            if (_directValueFixture == null)
            {
                return;
            }

            var data = new byte[1];
            data[0] = ToByte(Mathf.Sin(time * 1.8f) * 0.5f + 0.5f);
            _directValueFixture.DmxUpdate(data);
        }

        private static byte ToByte(float normalized)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(normalized * 255f), 0, 255);
        }

        private static void Write16(byte[] buffer, int offset, float normalized)
        {
            var value = (ushort)Mathf.Clamp(Mathf.RoundToInt(normalized * 65535f), 0, 65535);
            buffer[offset] = (byte)(value >> 8);
            buffer[offset + 1] = (byte)(value & 0xff);
        }
    }
}

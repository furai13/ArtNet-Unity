using System;

namespace ArtNet.Devices.Modular
{
    public readonly ref struct DmxFrame
    {
        private readonly ReadOnlySpan<byte> _data;

        public DmxFrame(ReadOnlySpan<byte> data)
        {
            _data = data;
        }

        public byte Get8(int offset)
        {
            return _data[offset];
        }

        public float Get01(int offset)
        {
            return _data[offset] / 255f;
        }

        public ushort Get16(int coarseOffset)
        {
            return (ushort)((_data[coarseOffset] << 8) | _data[coarseOffset + 1]);
        }

        public float Get16Normalized(int coarseOffset)
        {
            return Get16(coarseOffset) / 65535f;
        }
    }
}

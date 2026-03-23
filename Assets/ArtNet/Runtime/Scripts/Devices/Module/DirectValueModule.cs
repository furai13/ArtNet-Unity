using System;
using UnityEngine;
using UnityEngine.Events;

namespace ArtNet.Devices.Modular
{
    [Serializable]
    public sealed class FloatEvent : UnityEvent<float>
    {
    }

    public class DirectValueModule : DmxModuleBase
    {
        public enum ReadMode
        {
            Byte,
            Word16
        }

        [SerializeField] private ReadMode readMode = ReadMode.Byte;
        [SerializeField] private Vector2 inputRange = new(0f, 255f);
        [SerializeField] private Vector2 outputRange = new(0f, 1f);
        [SerializeField] private bool invokeOnlyOnChange = true;
        [SerializeField] private FloatEvent onValueChanged;

        private bool _hasLastValue;
        private float _lastValue;

        public override int ChannelCount => readMode == ReadMode.Byte ? 1 : 2;

        public override void Apply(in DmxFrame frame)
        {
            var rawValue = readMode == ReadMode.Byte
                ? frame.Get8(ChannelOffset)
                : frame.Get16(ChannelOffset);

            var normalized = Mathf.InverseLerp(inputRange.x, inputRange.y, rawValue);
            var outputValue = Mathf.Lerp(outputRange.x, outputRange.y, normalized);

            if (invokeOnlyOnChange && _hasLastValue && Mathf.Approximately(_lastValue, outputValue))
            {
                return;
            }

            _hasLastValue = true;
            _lastValue = outputValue;
            onValueChanged?.Invoke(outputValue);
        }
    }
}

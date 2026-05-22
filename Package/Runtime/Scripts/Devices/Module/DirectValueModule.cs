using System;
using System.Collections.Generic;
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
        [SerializeField] private string plannerFunctionId = "value";
        [SerializeField] private FloatEvent onValueChanged;

        private bool _hasLastValue;
        private float _lastValue;
        private static readonly IReadOnlyList<DmxChannelDescriptor> ByteChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Value", 0)
        };
        private static readonly IReadOnlyList<DmxChannelDescriptor> WordChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Value", 0),
            new DmxChannelDescriptor("Value Fine", 1)
        };

        public override int ChannelCount => readMode == ReadMode.Byte ? 1 : 2;
        public string PlannerFunctionId =>
            string.IsNullOrWhiteSpace(plannerFunctionId) ? "value" : plannerFunctionId.Trim();

        public override void Apply(in DmxFrame frame)
        {
            var rawValue = readMode == ReadMode.Byte
                ? frame.Get8(0)
                : frame.Get16(0);

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

        public void Configure(ReadMode mode, Vector2 sourceRange, Vector2 destinationRange, bool onlyOnChange)
        {
            readMode = mode;
            inputRange = sourceRange;
            outputRange = destinationRange;
            invokeOnlyOnChange = onlyOnChange;
        }

        public void Configure(
            ReadMode mode,
            Vector2 sourceRange,
            Vector2 destinationRange,
            bool onlyOnChange,
            string functionId)
        {
            Configure(mode, sourceRange, destinationRange, onlyOnChange);
            SetPlannerFunctionId(functionId);
        }

        public void SetPlannerFunctionId(string functionId)
        {
            plannerFunctionId = string.IsNullOrWhiteSpace(functionId) ? "value" : functionId.Trim();
        }

        public void AddListener(UnityAction<float> listener)
        {
            onValueChanged ??= new FloatEvent();
            onValueChanged.AddListener(listener);
        }

        public override IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors()
        {
            return readMode == ReadMode.Byte ? ByteChannelDescriptors : WordChannelDescriptors;
        }
    }
}

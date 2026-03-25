using System.Collections.Generic;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class GoboModule : DmxModuleBase
    {
        [SerializeField, Min(1)] private int goboCount = 1;
        [SerializeField] private bool hasRotationChannel = true;
        [SerializeField, Range(0, 255)] private int openThreshold = 7;
        [SerializeField, Range(0f, 720f)] private float maxRotationSpeedDegPerSecond = 180f;
        private static readonly IReadOnlyList<DmxChannelDescriptor> SingleChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Gobo", 0)
        };
        private static readonly IReadOnlyList<DmxChannelDescriptor> DualChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Gobo", 0),
            new DmxChannelDescriptor("Gobo Rotation", 1)
        };

        public override int ChannelCount => hasRotationChannel ? 2 : 1;

        public override void Apply(in DmxFrame frame)
        {
            ApplySelection(frame);

            if (hasRotationChannel)
            {
                ApplyRotation(frame);
            }
            else
            {
                State.GoboRotate = false;
                State.GoboRotationSpeedDegPerSecond = 0f;
            }
        }

        private void ApplySelection(in DmxFrame frame)
        {
            var value = frame.Get8(0);
            if (value <= openThreshold || goboCount <= 0)
            {
                State.GoboOpen = true;
                State.GoboIndex = 0;
                return;
            }

            State.GoboOpen = false;
            var normalized = Mathf.InverseLerp(openThreshold + 1f, 255f, value);
            State.GoboIndex = Mathf.Clamp(Mathf.FloorToInt(normalized * goboCount), 0, goboCount - 1);
        }

        private void ApplyRotation(in DmxFrame frame)
        {
            var value = frame.Get8(1);
            if (value == 0 || value == 127)
            {
                State.GoboRotate = false;
                State.GoboRotationSpeedDegPerSecond = 0f;
                return;
            }

            State.GoboRotate = true;

            if (value < 127)
            {
                var normalized = Mathf.InverseLerp(1f, 126f, value);
                State.GoboRotationSpeedDegPerSecond = Mathf.Lerp(-maxRotationSpeedDegPerSecond, -1f, normalized);
                return;
            }

            var positiveNormalized = Mathf.InverseLerp(128f, 255f, value);
            State.GoboRotationSpeedDegPerSecond = Mathf.Lerp(1f, maxRotationSpeedDegPerSecond, positiveNormalized);
        }

        public void Configure(int count, bool rotationEnabled, int threshold, float maxRotationSpeed)
        {
            goboCount = Mathf.Max(1, count);
            hasRotationChannel = rotationEnabled;
            openThreshold = Mathf.Clamp(threshold, 0, 255);
            maxRotationSpeedDegPerSecond = Mathf.Max(0f, maxRotationSpeed);
        }

        public override IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors()
        {
            return hasRotationChannel ? DualChannelDescriptors : SingleChannelDescriptors;
        }
    }
}

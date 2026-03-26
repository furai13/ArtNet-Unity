using UnityEngine;

namespace ArtNet.Devices.Modular
{
    [System.Serializable]
    public sealed class DmxFixtureState
    {
        public Color Color = Color.white;
        public float Dimmer = 1f;
        public float PanNormalized = 0.5f;
        public float TiltNormalized = 0.5f;
        public float BeamAngleNormalized = 0.5f;
        public bool StrobeEnabled;
        public float StrobeRateHz;
        public bool GoboOpen = true;
        public int GoboIndex;
        public bool GoboRotate;
        public float GoboRotationSpeedDegPerSecond;
        public float GoboRotationAngle;

        public void ResetToDefaults()
        {
            Color = Color.white;
            Dimmer = 1f;
            PanNormalized = 0.5f;
            TiltNormalized = 0.5f;
            BeamAngleNormalized = 0.5f;
            StrobeEnabled = false;
            StrobeRateHz = 0f;
            GoboOpen = true;
            GoboIndex = 0;
            GoboRotate = false;
            GoboRotationSpeedDegPerSecond = 0f;
            GoboRotationAngle = 0f;
        }
    }
}

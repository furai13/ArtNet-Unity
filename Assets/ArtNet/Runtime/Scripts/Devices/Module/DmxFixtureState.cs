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
    }
}

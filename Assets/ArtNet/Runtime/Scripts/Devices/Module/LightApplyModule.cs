using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class LightApplyModule : DmxModuleBase
    {
        [Header("Light")]
        [SerializeField] private Light targetLight;
        [SerializeField, Min(0f)] private float maxIntensity = 2f;
        [SerializeField] private Vector2 spotAngleRange = new(10f, 60f);

        [Header("Pan/Tilt")]
        [SerializeField] private Transform panAxis;
        [SerializeField] private Transform tiltAxis;
        [SerializeField] private Vector2 panRange = new(-270f, 270f);
        [SerializeField] private Vector2 tiltRange = new(-135f, 135f);
        [SerializeField] private Vector3 panLocalAxis = Vector3.up;
        [SerializeField] private Vector3 tiltLocalAxis = Vector3.right;
        [SerializeField] private bool invertPan;
        [SerializeField] private bool invertTilt;

        private bool _strobeOpen = true;
        private float _strobeTimer;
        private Quaternion _panInitialRotation;
        private Quaternion _tiltInitialRotation;

        public override int ChannelCount => 0;
        public override int ExecutionOrder => 100;

        protected override void OnInitialize()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }

            if (panAxis != null)
            {
                _panInitialRotation = panAxis.localRotation;
            }

            if (tiltAxis != null)
            {
                _tiltInitialRotation = tiltAxis.localRotation;
            }
        }

        public override void Apply(in DmxFrame frame)
        {
            ApplyPanTilt();
            ApplyLight();
        }

        public override void Tick(float deltaTime)
        {
            if (targetLight == null)
            {
                return;
            }

            if (!State.StrobeEnabled || State.StrobeRateHz <= 0f)
            {
                _strobeOpen = true;
                ApplyIntensity();
                return;
            }

            _strobeTimer += deltaTime * State.StrobeRateHz;
            if (_strobeTimer < 1f)
            {
                return;
            }

            _strobeTimer -= 1f;
            _strobeOpen = !_strobeOpen;
            ApplyIntensity();
        }

        private void ApplyPanTilt()
        {
            if (panAxis != null)
            {
                var panValue = invertPan ? 1f - State.PanNormalized : State.PanNormalized;
                var panAngle = Mathf.Lerp(panRange.x, panRange.y, panValue);
                panAxis.localRotation = _panInitialRotation * Quaternion.AngleAxis(panAngle, panLocalAxis.normalized);
            }

            if (tiltAxis != null)
            {
                var tiltValue = invertTilt ? 1f - State.TiltNormalized : State.TiltNormalized;
                var tiltAngle = Mathf.Lerp(tiltRange.x, tiltRange.y, tiltValue);
                tiltAxis.localRotation = _tiltInitialRotation * Quaternion.AngleAxis(tiltAngle, tiltLocalAxis.normalized);
            }
        }

        private void ApplyLight()
        {
            if (targetLight == null)
            {
                return;
            }

            targetLight.color = State.Color;
            targetLight.spotAngle = Mathf.Lerp(spotAngleRange.x, spotAngleRange.y, State.BeamAngleNormalized);
            ApplyIntensity();
        }

        private void ApplyIntensity()
        {
            targetLight.intensity = _strobeOpen ? State.Dimmer * maxIntensity : 0f;
        }

        public void Configure(
            Light lightTarget,
            Transform panTarget,
            Transform tiltTarget,
            Vector2 lightAngleRange,
            Vector2 lightPanRange,
            Vector2 lightTiltRange,
            float intensityMax)
        {
            targetLight = lightTarget;
            panAxis = panTarget;
            tiltAxis = tiltTarget;
            spotAngleRange = lightAngleRange;
            panRange = lightPanRange;
            tiltRange = lightTiltRange;
            maxIntensity = intensityMax;
        }
    }
}

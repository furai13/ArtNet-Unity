using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public class LightOutput : FixtureOutputBase
    {
        [Header("Light")]
        [SerializeField] private Light targetLight;
        [SerializeField, Min(0f)] private float maxIntensity = 2f;
        [SerializeField] private Vector2 spotAngleRange = new(10f, 60f);
        [SerializeField, Min(0f)] private float beamAngleSpeedDegPerSecond;

        [Header("Pan/Tilt")]
        [SerializeField] private Transform panAxis;
        [SerializeField] private Transform tiltAxis;
        [SerializeField] private Vector2 panRange = new(-270f, 270f);
        [SerializeField] private Vector2 tiltRange = new(-135f, 135f);
        [SerializeField, Min(0f)] private float panSpeedDegPerSecond;
        [SerializeField, Min(0f)] private float tiltSpeedDegPerSecond;
        [SerializeField] private bool useShortestPathForPan = true;
        [SerializeField] private Vector3 panLocalAxis = Vector3.up;
        [SerializeField] private Vector3 tiltLocalAxis = Vector3.right;
        [SerializeField] private bool invertPan;
        [SerializeField] private bool invertTilt;

        private bool _strobeOpen = true;
        private float _strobeTimer;
        private Quaternion _panInitialRotation;
        private Quaternion _tiltInitialRotation;
        private float _currentPanAngle;
        private float _currentTiltAngle;
        private float _currentBeamAngle;

        protected override void OnInitialize()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }

            if (panAxis != null)
            {
                _panInitialRotation = panAxis.localRotation;
                _currentPanAngle = GetTargetPanAngle();
            }

            if (tiltAxis != null)
            {
                _tiltInitialRotation = tiltAxis.localRotation;
                _currentTiltAngle = GetTargetTiltAngle();
            }

            _currentBeamAngle = GetTargetBeamAngle();
        }

        public override void Apply()
        {
            _currentPanAngle = GetTargetPanAngle();
            _currentTiltAngle = GetTargetTiltAngle();
            _currentBeamAngle = GetTargetBeamAngle();

            ApplyPanTilt();
            ApplyLight();
        }

        public override void Tick(float deltaTime)
        {
            UpdatePanTilt(deltaTime);
            UpdateBeamAngle(deltaTime);
            ApplyPanTilt();
            ApplyBeamAngle();

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

        private void UpdatePanTilt(float deltaTime)
        {
            var targetPanAngle = GetTargetPanAngle();
            var targetTiltAngle = GetTargetTiltAngle();

            _currentPanAngle = MoveAngle(
                _currentPanAngle,
                targetPanAngle,
                panSpeedDegPerSecond,
                deltaTime,
                useShortestPathForPan);

            _currentTiltAngle = MoveAngle(
                _currentTiltAngle,
                targetTiltAngle,
                tiltSpeedDegPerSecond,
                deltaTime,
                false);
        }

        private void UpdateBeamAngle(float deltaTime)
        {
            var targetBeamAngle = GetTargetBeamAngle();
            if (beamAngleSpeedDegPerSecond <= 0f)
            {
                _currentBeamAngle = targetBeamAngle;
                return;
            }

            _currentBeamAngle = Mathf.MoveTowards(
                _currentBeamAngle,
                targetBeamAngle,
                beamAngleSpeedDegPerSecond * deltaTime);
        }

        private void ApplyPanTilt()
        {
            if (panAxis != null)
            {
                panAxis.localRotation = _panInitialRotation * Quaternion.AngleAxis(_currentPanAngle, panLocalAxis.normalized);
            }

            if (tiltAxis != null)
            {
                tiltAxis.localRotation = _tiltInitialRotation * Quaternion.AngleAxis(_currentTiltAngle, tiltLocalAxis.normalized);
            }
        }

        private void ApplyLight()
        {
            if (targetLight == null)
            {
                return;
            }

            targetLight.color = State.Color;
            ApplyBeamAngle();
            ApplyIntensity();
        }

        private void ApplyBeamAngle()
        {
            if (targetLight == null)
            {
                return;
            }

            targetLight.spotAngle = _currentBeamAngle;
        }

        private void ApplyIntensity()
        {
            targetLight.intensity = _strobeOpen ? State.Dimmer * maxIntensity : 0f;
        }

        private float GetTargetPanAngle()
        {
            var panValue = invertPan ? 1f - State.PanNormalized : State.PanNormalized;
            return Mathf.Lerp(panRange.x, panRange.y, panValue);
        }

        private float GetTargetTiltAngle()
        {
            var tiltValue = invertTilt ? 1f - State.TiltNormalized : State.TiltNormalized;
            return Mathf.Lerp(tiltRange.x, tiltRange.y, tiltValue);
        }

        private float GetTargetBeamAngle()
        {
            return Mathf.Lerp(spotAngleRange.x, spotAngleRange.y, State.BeamAngleNormalized);
        }

        private static float MoveAngle(float current, float target, float speedDegPerSecond, float deltaTime, bool shortestPath)
        {
            if (speedDegPerSecond <= 0f)
            {
                return target;
            }

            var maxDelta = speedDegPerSecond * deltaTime;
            if (shortestPath)
            {
                return Mathf.MoveTowardsAngle(current, target, maxDelta);
            }

            return Mathf.MoveTowards(current, target, maxDelta);
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

using UnityEngine;

namespace ArtNet.Devices.Modular
{
    public abstract class ColorDimmerOutputBase : FixtureOutputBase
    {
        [Header("Color / Dimmer")]
        [SerializeField] private AnimationCurve dimmerCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Source Response")]
        [SerializeField, Min(0f)] private float sourceRiseSeconds;
        [SerializeField, Min(0f)] private float sourceFallSeconds;

        [Header("Beam Response")]
        [SerializeField, Min(0f)] private float beamOnDelaySeconds;
        [SerializeField, Min(0f)] private float beamOffDelaySeconds;
        [SerializeField, Min(0f)] private float beamRiseSeconds;
        [SerializeField, Min(0f)] private float beamFallSeconds;

        private float _sourceLevel;
        private float _beamLevel;
        private bool _sourceOn;
        private bool _beamEnabled;
        private bool _pendingBeamEnabled;
        private float _beamDelayRemaining;
        private float _beamHoldLevel;
        private bool _strobeOpen = true;
        private float _strobeTimer;

        protected float EvaluatedDimmer => _strobeOpen ? _beamLevel : 0f;

        protected void InitializeColorDimmerOutput()
        {
            SnapOutputLevelsToState();
            ApplyColorDimmerOutput();
        }

        protected void UpdateColorDimmerOutput(float deltaTime)
        {
            UpdateSource(deltaTime);
            UpdateBeamDelay(deltaTime);
            UpdateBeamLevel(deltaTime);
            UpdateStrobe(deltaTime);
            ApplyColorDimmerOutput();
        }

        protected void RefreshColorDimmerOutput()
        {
            if (HasImmediateResponse())
            {
                SnapOutputLevelsToState();
            }

            ApplyColorDimmerOutput();
        }

        private void ApplyColorDimmerOutput()
        {
            ApplyColorDimmer(State.Color, EvaluatedDimmer);
        }

        protected abstract void ApplyColorDimmer(Color color, float dimmer);

        private void SnapOutputLevelsToState()
        {
            _sourceLevel = EvaluateDimmer(State.Dimmer);
            _beamLevel = _sourceLevel;
            _sourceOn = _sourceLevel > 0.0001f;
            _beamEnabled = _sourceOn;
            _pendingBeamEnabled = _beamEnabled;
            _beamDelayRemaining = 0f;
            _beamHoldLevel = _beamLevel;
            _strobeOpen = true;
            _strobeTimer = 0f;
        }

        private void UpdateSource(float deltaTime)
        {
            var target = EvaluateDimmer(State.Dimmer);
            var responseSeconds = target >= _sourceLevel ? sourceRiseSeconds : sourceFallSeconds;
            _sourceLevel = MoveTowardsNormalized(_sourceLevel, target, responseSeconds, deltaTime);

            var nextSourceOn = _sourceLevel > 0.0001f;
            if (nextSourceOn == _sourceOn)
            {
                return;
            }

            _sourceOn = nextSourceOn;
            _pendingBeamEnabled = nextSourceOn;
            _beamDelayRemaining = nextSourceOn ? beamOnDelaySeconds : beamOffDelaySeconds;
            _beamHoldLevel = nextSourceOn ? 0f : _beamLevel;

            if (_beamDelayRemaining <= 0f)
            {
                _beamEnabled = _pendingBeamEnabled;
            }
        }

        private void UpdateBeamDelay(float deltaTime)
        {
            if (_beamDelayRemaining <= 0f)
            {
                return;
            }

            _beamDelayRemaining = Mathf.Max(0f, _beamDelayRemaining - deltaTime);
            if (_beamDelayRemaining <= 0f)
            {
                _beamEnabled = _pendingBeamEnabled;
            }
        }

        private void UpdateBeamLevel(float deltaTime)
        {
            var target = _beamDelayRemaining > 0f
                ? _beamHoldLevel
                : (_beamEnabled ? _sourceLevel : 0f);
            var responseSeconds = target >= _beamLevel ? beamRiseSeconds : beamFallSeconds;
            _beamLevel = MoveTowardsNormalized(_beamLevel, target, responseSeconds, deltaTime);
        }

        private void UpdateStrobe(float deltaTime)
        {
            if (!State.StrobeEnabled || State.StrobeRateHz <= 0f)
            {
                _strobeOpen = true;
                _strobeTimer = 0f;
                return;
            }

            _strobeTimer += deltaTime * State.StrobeRateHz;
            while (_strobeTimer >= 1f)
            {
                _strobeTimer -= 1f;
                _strobeOpen = !_strobeOpen;
            }
        }

        private float EvaluateDimmer(float normalizedDimmer)
        {
            var clamped = Mathf.Clamp01(normalizedDimmer);
            var curve = dimmerCurve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
            return Mathf.Clamp01(curve.Evaluate(clamped));
        }

        private bool HasImmediateResponse()
        {
            return sourceRiseSeconds <= 0f &&
                   sourceFallSeconds <= 0f &&
                   beamOnDelaySeconds <= 0f &&
                   beamOffDelaySeconds <= 0f &&
                   beamRiseSeconds <= 0f &&
                   beamFallSeconds <= 0f;
        }

        private static float MoveTowardsNormalized(float current, float target, float responseSeconds, float deltaTime)
        {
            if (responseSeconds <= 0f)
            {
                return target;
            }

            return Mathf.MoveTowards(current, target, deltaTime / responseSeconds);
        }
    }
}

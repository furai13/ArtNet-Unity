using System;
using UnityEngine;

namespace ArtNet.Devices.Modular
{
    [Serializable]
    public struct GoboSlot
    {
        public string label;
        public Texture staticCookie;
        public Texture[] rotatedCookies;
    }

    public class GoboApplyModule : DmxModuleBase
    {
        [SerializeField] private Light targetLight;
        [SerializeField] private Texture openCookie;
        [SerializeField] private GoboSlot[] gobos;

        private Texture _lastCookie;

        public override int ChannelCount => 0;
        public override int ExecutionOrder => 110;

        protected override void OnInitialize()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }
        }

        public override void Apply(in DmxFrame frame)
        {
            ApplyCookie();
        }

        public override void Tick(float deltaTime)
        {
            if (!State.GoboOpen && State.GoboRotate && Mathf.Abs(State.GoboRotationSpeedDegPerSecond) > 0f)
            {
                State.GoboRotationAngle = Mathf.Repeat(
                    State.GoboRotationAngle + State.GoboRotationSpeedDegPerSecond * deltaTime,
                    360f);
                ApplyCookie();
            }
        }

        private void ApplyCookie()
        {
            if (targetLight == null)
            {
                return;
            }

            var cookie = ResolveCookie();
            if (ReferenceEquals(cookie, _lastCookie))
            {
                return;
            }

            targetLight.cookie = cookie;
            _lastCookie = cookie;
        }

        private Texture ResolveCookie()
        {
            if (State.GoboOpen || gobos == null || gobos.Length == 0)
            {
                return openCookie;
            }

            var goboIndex = Mathf.Clamp(State.GoboIndex, 0, gobos.Length - 1);
            var slot = gobos[goboIndex];

            if (State.GoboRotate && slot.rotatedCookies != null && slot.rotatedCookies.Length > 0)
            {
                var index = Mathf.FloorToInt(State.GoboRotationAngle / 360f * slot.rotatedCookies.Length);
                index = Mathf.Clamp(index, 0, slot.rotatedCookies.Length - 1);
                return slot.rotatedCookies[index];
            }

            if (slot.staticCookie != null)
            {
                return slot.staticCookie;
            }

            if (slot.rotatedCookies != null && slot.rotatedCookies.Length > 0)
            {
                return slot.rotatedCookies[0];
            }

            return openCookie;
        }

        public void Configure(Light lightTarget, Texture defaultCookie, GoboSlot[] slots)
        {
            targetLight = lightTarget;
            openCookie = defaultCookie;
            gobos = slots;
        }
    }
}

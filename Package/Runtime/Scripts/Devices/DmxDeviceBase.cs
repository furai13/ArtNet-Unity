using System;
using ArtNet.Common;
using UnityEngine;

namespace ArtNet.Devices
{
    public abstract class DmxDeviceBase : MonoBehaviour, IDmxDevice
    {
        [SerializeField, Range(0, 255)] private ushort universe;
        [SerializeField, Range(0, 511)] private ushort startAddress;

        protected byte[] DmxData;
        private float _lastReceivedRealtimeSinceStartup = -1f;

        public ushort Universe => universe;
        public ushort StartAddress => startAddress;
        public bool HasReceivedDmx => _lastReceivedRealtimeSinceStartup >= 0f;
        public float LastReceivedRealtimeSinceStartup => _lastReceivedRealtimeSinceStartup;
        public float SecondsSinceLastReceived =>
            HasReceivedDmx ? Time.realtimeSinceStartup - _lastReceivedRealtimeSinceStartup : -1f;

        public abstract byte ChannelNumber { get; }

        protected abstract void InitFixture();
        protected abstract void UpdateProperties();

        public void SetAddressPatch(ushort patchedUniverse, ushort patchedStartAddress)
        {
            universe = patchedUniverse;
            startAddress = patchedStartAddress;
        }

        private void Start()
        {
            EnsureInitialized();
        }


        public void DmxUpdate(ReadOnlySpan<byte> dmx)
        {
            EnsureInitialized();

            if (dmx.Length < ChannelNumber)
            {
                ArtNetLogger.LogError($"DMX data is too short. Expected {ChannelNumber} bytes, got {dmx.Length} bytes.");
                return;
            }

            var isFirstReceivedDmx = !HasReceivedDmx;
            _lastReceivedRealtimeSinceStartup = Time.realtimeSinceStartup;

            if (!isFirstReceivedDmx && dmx.SequenceEqual(DmxData.AsSpan(0, ChannelNumber))) return;
            dmx[..ChannelNumber].CopyTo(DmxData);
            //Debug.Log($"DMX data updated to {dmx.Length} bytes. Channel number: {ChannelNumber}");

            UpdateProperties();
        }

        private void EnsureInitialized()
        {
            if (DmxData != null && DmxData.Length == ChannelNumber)
            {
                return;
            }

            InitFixture();
            DmxData = new byte[ChannelNumber];
        }
    }
}

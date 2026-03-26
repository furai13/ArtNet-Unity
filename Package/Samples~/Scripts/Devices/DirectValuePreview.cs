using UnityEngine;

namespace ArtNet.Samples.Devices
{
    public class DirectValuePreview : MonoBehaviour
    {
        [SerializeField] private float currentValue;

        public float CurrentValue => currentValue;

        public void SetValue(float value)
        {
            currentValue = value;
        }
    }
}

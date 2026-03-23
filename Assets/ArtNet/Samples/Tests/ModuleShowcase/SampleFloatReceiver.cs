using UnityEngine;

namespace ArtNet.Samples.Devices
{
    public class SampleFloatReceiver : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float depth = 0.8f;
        [SerializeField] private float width = 0.8f;

        public void Configure(Transform targetTransform)
        {
            target = targetTransform;
        }

        public void SetHeight(float value)
        {
            if (target == null)
            {
                return;
            }

            var localScale = target.localScale;
            localScale.x = width;
            localScale.y = value;
            localScale.z = depth;
            target.localScale = localScale;
        }
    }
}

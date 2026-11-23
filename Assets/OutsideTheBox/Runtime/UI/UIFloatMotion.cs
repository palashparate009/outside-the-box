using UnityEngine;

namespace OutsideTheBox
{
    /// <summary>Subtle ambient motion for decorative UI, disabled by Reduce Motion.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIFloatMotion : MonoBehaviour
    {
        [SerializeField] private Vector2 amplitude = new(12f, 18f);
        [SerializeField] private float speed = 0.35f;
        [SerializeField] private float phase;
        [SerializeField] private float rotationAmplitude = 4f;
        private RectTransform rect;
        private Vector2 origin;
        private Quaternion originalRotation;

        public void Configure(Vector2 movement, float movementSpeed, float phaseOffset, float rotation)
        {
            amplitude = movement;
            speed = movementSpeed;
            phase = phaseOffset;
            rotationAmplitude = rotation;
        }

        private void Awake()
        {
            rect = (RectTransform)transform;
            origin = rect.anchoredPosition;
            originalRotation = rect.localRotation;
        }

        private void OnEnable()
        {
            if (rect == null) rect = (RectTransform)transform;
            origin = rect.anchoredPosition;
            originalRotation = rect.localRotation;
        }

        private void OnDisable()
        {
            if (rect == null) return;
            rect.anchoredPosition = origin;
            rect.localRotation = originalRotation;
        }

        private void Update()
        {
            if (rect == null) return;
            if (SaveService.Data?.reduceMotion ?? false)
            {
                rect.anchoredPosition = origin;
                rect.localRotation = originalRotation;
                return;
            }
            var configuredSpeed = LiveOpsConfigStore.Current.uiBackdropMotionSpeed;
            var time = Time.unscaledTime * Mathf.Max(0f, speed * configuredSpeed) + phase;
            rect.anchoredPosition = origin + new Vector2(Mathf.Sin(time * 0.83f), Mathf.Cos(time)) * amplitude;
            rect.localRotation = originalRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.61f) * rotationAmplitude);
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

namespace OutsideTheBox
{
    public sealed class UIJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler
    {
        private Vector3 baseScale;
        private float target = 1f;
        private float current = 1f;
        private float velocity;
        [SerializeField] private bool ambientPulse;
        [SerializeField] private bool idleHover;
        private bool pointerDown;
        private bool pointerInside;
        private float phase;
        public bool AmbientPulse { get => ambientPulse; set => ambientPulse = value; }
        public bool IdleHover { get => idleHover; set => idleHover = value; }

        private void Awake()
        {
            baseScale = transform.localScale;
            phase = Mathf.Abs(GetInstanceID() % 97) * 0.17f;
        }
        private void OnEnable()
        {
            if (baseScale == Vector3.zero) baseScale = Vector3.one;
            transform.localScale = baseScale;
            target = 1f;
            current = 1f;
            velocity = 0f;
            pointerDown = false;
        }

        private void Update()
        {
            if (SaveService.Data?.reduceMotion ?? false)
            {
                transform.localScale = baseScale;
                current = 1f;
                velocity = 0f;
                return;
            }
            if (ambientPulse)
            {
                var speed = LiveOpsConfigStore.Current.uiAmbientPulseSpeed;
                var pulse = 1f + Mathf.Sin(Time.unscaledTime * speed * Mathf.PI * 2f) * 0.018f;
                transform.localScale = baseScale * pulse;
                return;
            }
            var settings = LiveOpsConfigStore.Current;
            var frequency = settings.uiButtonSpringFrequency;
            var damping = settings.uiButtonSpringDamping;
            var delta = Mathf.Min(0.05f, Time.unscaledDeltaTime);
            var acceleration = (target - current) * frequency * frequency - 2f * damping * frequency * velocity;
            velocity += acceleration * delta;
            current += velocity * delta;
            var hover = idleHover && !pointerDown
                ? 1f + Mathf.Sin(Time.unscaledTime * settings.uiButtonHoverSpeed * Mathf.PI * 2f + phase) *
                settings.uiButtonHoverAmount
                : 1f;
            transform.localScale = baseScale * current * hover;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (ambientPulse) return;
            pointerDown = true;
            target = LiveOpsConfigStore.Current.uiButtonPressScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerDown = false;
            target = pointerInside ? 1.025f : 1f;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            if (!pointerDown && !ambientPulse) target = 1.025f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            pointerDown = false;
            target = 1f;
        }
    }

}

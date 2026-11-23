using UnityEngine;

namespace OutsideTheBox
{
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public sealed class UIScreenMotion : MonoBehaviour
    {
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private float duration;
        private float elapsed;
        private float slideDistance;
        private float startScale;
        private float overshoot;
        private bool playing;

        private void Awake()
        {
            CacheComponents();
            enabled = false;
        }

        public void PlayEnter(float animationSeconds, float distance, float scale, float bounce)
        {
            CacheComponents();
            if (rectTransform == null || canvasGroup == null) return;
            duration = Mathf.Max(0.18f, animationSeconds);
            slideDistance = Mathf.Max(120f, distance);
            startScale = Mathf.Clamp(scale, 0.85f, 0.995f);
            overshoot = Mathf.Clamp(bounce, 0f, 0.2f);
            elapsed = 0f;
            playing = true;
            rectTransform.anchoredPosition = Vector2.right * slideDistance;
            transform.localScale = Vector3.one * startScale;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            var nestedGroups = GetComponentsInChildren<CanvasGroup>(true);
            for (var i = 0; i < nestedGroups.Length; i++)
                if (nestedGroups[i] != canvasGroup) nestedGroups[i].alpha = 1f;
            enabled = true;
        }

        public void StopAndReset()
        {
            CacheComponents();
            playing = false;
            enabled = false;
            if (rectTransform == null || canvasGroup == null) return;
            rectTransform.anchoredPosition = Vector2.zero;
            transform.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void Update()
        {
            if (!playing || rectTransform == null || canvasGroup == null) return;
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var eased = 1f - Mathf.Pow(1f - t, 3f) + Mathf.Sin(t * Mathf.PI) * (1f - t) * overshoot;
            canvasGroup.alpha = Mathf.Clamp01(eased);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(Vector2.right * slideDistance,
                Vector2.zero, eased);
            transform.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, eased);
            if (t < 1f) return;
            StopAndReset();
        }

        private void OnDisable()
        {
            playing = false;
        }

        private void CacheComponents()
        {
            if (rectTransform == null) rectTransform = transform as RectTransform;
            // Resolve this every time. Prefab replacement can destroy the previously
            // cached native component while leaving its managed wrapper behind.
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
}

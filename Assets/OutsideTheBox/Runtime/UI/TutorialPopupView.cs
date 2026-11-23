using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    // All visuals and groups are authored in TutorialOverlay.prefab. Only Card
    // moves/scales; the backdrop remains a separate screen-sized input blocker.
    public sealed class TutorialPopupView : MonoBehaviour
    {
        public RectTransform card;
        public CanvasGroup inputGroup, cardGroup, backdropGroup;
        public Image backdrop;
        [Range(0f, 1f)] public float backdropOpacity = .48f;
        public float maximumWidth = 900f;
        public float preferredHeight = 720f;
        public float edgeMargin = 40f;
        private RectTransform root;
        private bool fitting;

        private void OnEnable()
        {
            ApplyBackdrop();
            FitCard();
        }
        private void OnRectTransformDimensionsChange() => FitCard();

        private void ApplyBackdrop()
        {
            // Legacy screen styling or an older prefab override must never paint
            // over the game. Only the separate, neutral shade renders outside Card.
            var rootGraphic = GetComponent<Graphic>();
            if (rootGraphic != null) rootGraphic.enabled = false;
            var gradient = GetComponent<UIGradient>();
            if (gradient != null) gradient.enabled = false;
            if (backdrop != null) backdrop.color = new Color(0f, 0f, 0f, backdropOpacity);
        }

        public void FitCard()
        {
            if (card == null || fitting) return;
            if (root == null) root = transform as RectTransform;
            if (root == null || root.rect.width <= 0f || root.rect.height <= 0f) return;
            fitting = true;
            card.anchorMin = card.anchorMax = new Vector2(.5f, .5f);
            card.pivot = new Vector2(.5f, .5f);
            card.sizeDelta = new Vector2(
                Mathf.Max(1f, Mathf.Min(maximumWidth, root.rect.width * .84f, root.rect.width - edgeMargin * 2f)),
                Mathf.Max(1f, Mathf.Min(preferredHeight, root.rect.height * .72f, root.rect.height - edgeMargin * 2f)));
            fitting = false;
        }

        public void Prepare(bool entering)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            ApplyBackdrop();
            FitCard();
            transform.localScale = Vector3.one;
            if (root != null) root.anchoredPosition = Vector2.zero;
            inputGroup.alpha = 1f;
            inputGroup.interactable = false;
            inputGroup.blocksRaycasts = true;
            cardGroup.interactable = false;
            cardGroup.blocksRaycasts = true;
            SetFrame(entering ? 0f : 1f, 1f, Vector2.zero);
        }

        public void SetFrame(float opacity, float scale, Vector2 offset)
        {
            if (card == null || cardGroup == null || backdropGroup == null) return;
            if (root == null) root = transform as RectTransform;
            var fitScale = root != null && card.rect.width > 0f && card.rect.height > 0f
                ? Mathf.Min((root.rect.width - edgeMargin) / card.rect.width,
                    (root.rect.height - edgeMargin) / card.rect.height) : 1f;
            card.localScale = Vector3.one * Mathf.Clamp(scale, .5f, Mathf.Max(.5f, fitScale));
            card.anchoredPosition = offset;
            cardGroup.alpha = Mathf.Clamp01(opacity);
            backdropGroup.alpha = Mathf.Clamp01(opacity);
        }

        public void Complete(bool visible)
        {
            SetFrame(1f, 1f, Vector2.zero);
            inputGroup.alpha = 1f;
            inputGroup.interactable = visible;
            inputGroup.blocksRaycasts = visible;
            cardGroup.interactable = visible;
            if (!visible) gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (card != null)
            {
                card.localScale = Vector3.one;
                card.anchoredPosition = Vector2.zero;
            }
        }
    }
}

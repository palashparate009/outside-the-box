using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    /// <summary>Bindings for the authored gem-store prefab, including six nested pack cards.</summary>
    public sealed class GemStoreView : MonoBehaviour
    {
        public Button openButton;
        public RectTransform content;
        public TMP_Text status;
        public Button retryButton;
        public CanvasGroup contentGroup;
        public GemPackCardView[] cards;
        private Coroutine reveal;

        public void Reveal()
        {
            if (reveal != null) StopCoroutine(reveal);
            ResetReveal();
            if (isActiveAndEnabled && !(SaveService.Data?.reduceMotion ?? false)) reveal = StartCoroutine(RevealCards());
        }

        private IEnumerator RevealCards()
        {
            var elapsed = 0f;
            while (content != null && content.gameObject.activeInHierarchy && elapsed < 0.6f)
            {
                elapsed += Time.unscaledDeltaTime;
                for (var i = 0; i < cards.Length; i++)
                {
                    var t = Mathf.Clamp01((elapsed - i * 0.045f) / 0.28f);
                    if (cards[i].revealGroup != null) cards[i].revealGroup.alpha = t;
                    if (cards[i].artwork != null) cards[i].artwork.localScale = Vector3.one * Mathf.Lerp(0.82f, 1f, 1f - Mathf.Pow(1f - t, 3f));
                }
                yield return null;
            }
            ResetReveal();
            reveal = null;
        }

        private void ResetReveal()
        {
            if (cards == null) return;
            foreach (var card in cards)
            {
                if (card == null) continue;
                if (card.revealGroup != null) card.revealGroup.alpha = 1;
                if (card.artwork != null) card.artwork.localScale = Vector3.one;
            }
        }

        private void OnDisable()
        {
            if (reveal != null) StopCoroutine(reveal);
            reveal = null;
            ResetReveal();
        }
    }
}

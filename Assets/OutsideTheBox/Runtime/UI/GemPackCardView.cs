using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    public sealed class GemPackCardView : MonoBehaviour
    {
        public Button purchaseButton;
        public TMP_Text packName;
        public TMP_Text amount;
        public TMP_Text price;
        public CanvasGroup revealGroup;
        public RectTransform artwork;
    }
}

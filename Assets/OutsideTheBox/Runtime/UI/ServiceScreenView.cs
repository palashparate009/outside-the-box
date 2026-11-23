using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    /// <summary>Inspector references for authored service screens; never constructs UI.</summary>
    public sealed class ServiceScreenView : MonoBehaviour
    {
        public CanvasGroup overlayGroup;
        public RectTransform content;
        public CanvasGroup contentGroup;
        public Button primaryButton;
        public Button secondaryButton;
        public Button backButton;
        public TMP_Text message;
        public TMP_Text status;
    }
}

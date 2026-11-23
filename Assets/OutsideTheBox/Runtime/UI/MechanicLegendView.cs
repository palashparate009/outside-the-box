using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    public sealed class MechanicLegendView : MonoBehaviour
    {
        public Image[] icons;
        public TMP_Text[] labels;
        public void SetSlot(int index, MechanicIconId id, string label, bool visible)
        {
            if (index >= icons.Length || icons[index] == null) return;
            icons[index].transform.parent.gameObject.SetActive(visible);
            if (!visible) return;
            var sprite = MechanicIconLibrary.Current?.Get(id);
            if (icons[index].sprite != sprite) icons[index].sprite = sprite;
            if (labels[index].text != label) labels[index].text = label;
        }
    }
}

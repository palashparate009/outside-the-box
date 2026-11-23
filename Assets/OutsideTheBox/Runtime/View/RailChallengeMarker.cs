using UnityEngine;

namespace OutsideTheBox
{
    // All renderers/icons are authored in RailChallengeMarker.prefab.
    public sealed class RailChallengeMarker : MonoBehaviour
    {
        public SpriteRenderer surface;
        public SpriteRenderer fill;
        public Transform[] stripes;
        public GameObject lockIcon;
        public GameObject switchIcon;
        private Vector2 size;
        private bool railDecoration;
        private SpriteRenderer mechanicIcon;

        public void SetMechanicIcon(SpriteRenderer renderer)
        {
            mechanicIcon = renderer;
            if (renderer == null) return;
            if (lockIcon != null) lockIcon.SetActive(false);
            if (switchIcon != null) switchIcon.SetActive(false);
        }

        public void Configure(Vector2 dimensions, bool pressureSwitch, bool showLock = true)
        {
            size = dimensions;
            railDecoration = !pressureSwitch && showLock;
            surface.transform.localScale = new Vector3(size.x, size.y, 1f);
            if (lockIcon != null) lockIcon.SetActive(!pressureSwitch && showLock);
            if (switchIcon != null) switchIcon.SetActive(pressureSwitch);
            if (fill != null) fill.gameObject.SetActive(!showLock && !pressureSwitch);
            for (var i = 0; i < stripes.Length; i++)
            {
                stripes[i].gameObject.SetActive(!pressureSwitch && showLock);
                stripes[i].localPosition = new Vector3((i + .5f) / stripes.Length * size.x - size.x * .5f, 0f, -.01f);
                stripes[i].localScale = new Vector3(.035f, size.y * 1.45f, 1f);
            }
            SetProgress(1f);
        }

        public void SetState(bool open)
        {
            surface.color = open ? new Color(.08f, .65f, .38f, .85f) : new Color(.85f, .15f, .16f, .9f);
            if (lockIcon != null) lockIcon.SetActive(mechanicIcon == null && railDecoration && !open);
            foreach (var stripe in stripes) if (stripe != null) stripe.gameObject.SetActive(railDecoration && !open);
        }

        public void SetProgress(float value)
        {
            if (fill == null) return;
            value = Mathf.Clamp01(value);
            fill.transform.localScale = new Vector3(size.x * value, size.y * .35f, 1f);
            fill.transform.localPosition = new Vector3(-size.x * (1f - value) * .5f, 0f, -.02f);
            fill.color = new Color(.4f, 1f, .85f, .85f);
        }
    }
}

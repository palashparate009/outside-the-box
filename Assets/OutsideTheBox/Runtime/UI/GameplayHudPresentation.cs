using TMPro;
using UnityEngine;

namespace OutsideTheBox
{
    // The layout and all graphics live in GameplayHUD.prefab. Runtime only binds
    // values and adapts the two optional controls to the mechanics in this level.
    public sealed class GameplayHudPresentation : MonoBehaviour
    {
        public TMP_Text chapterCaption;
        public RectTransform timeFill, energyFill;
        public RectTransform forceControl, echoControl;
        public RectTransform[] footerControls;
        public UIRoundRectGraphic forceFrame;
        public Color normalTimerColor = Color.white;
        public Vector2 controlY = new(.137f, .215f);
        [Min(0f)] public float minimumControlHeight = 132f;
        public Vector2 pairedForceX = new(.16f, .49f), pairedEchoX = new(.51f, .84f);
        public Vector2 singleControlX = new(.32f, .68f);
        public Vector2 footerY = new(.012f, .09f);
        public Vector2 promptY = new(.097f, .13f);
        private static GameplayHudPresentation active;
        public static float BoardAreaBottom => active != null && active.isActiveAndEnabled
            ? Mathf.Max(.225f, active.reservedBottom) : .225f;
        private RectTransform instructionCard;
        private float reservedBottom = .225f;
        private bool layingOut;
        private bool availabilitySet, lastForce, lastEcho;
        private int lastChapter = -1;
        private float lastTime = -1f, lastEnergy = -1f;
        private bool modeSet;
        private ForceMode lastMode;

        private void OnEnable()
        {
            active = this;
            RefreshLayout();
        }

        private void OnDisable()
        {
            if (active == this) active = null;
        }

        private void OnRectTransformDimensionsChange() => RefreshLayout();

        public void RefreshLayout()
        {
            if (layingOut) return;
            availabilitySet = false;
            SetControls(lastForce, lastEcho);
        }

        public void SetChapter(int chapter)
        {
            if (chapterCaption == null || lastChapter == chapter) return;
            lastChapter = chapter;
            chapterCaption.text = chapter > 0 ? $"CHAPTER {chapter:00}" : "CHALLENGE";
        }

        public void SetTime(float fraction) => SetMeter(timeFill, Mathf.Clamp01(fraction), ref lastTime);
        public void SetEnergy(float fraction) => SetMeter(energyFill, 1f - Mathf.Clamp01(fraction), ref lastEnergy);

        public void SetMode(ForceMode mode)
        {
            if (forceFrame == null || (modeSet && lastMode == mode)) return;
            modeSet = true; lastMode = mode;
            forceFrame.Configure(mode == ForceMode.Pull ? new Color(.19f, .84f, .94f)
                : new Color(1f, .76f, .25f), new Color(.72f, .94f, 1f, .7f), 2f, 25f);
        }

        public void SetControls(bool force, bool echo)
        {
            if (availabilitySet && lastForce == force && lastEcho == echo) return;
            availabilitySet = true; lastForce = force; lastEcho = echo;
            LayoutBottomRows(force, echo);
        }

        private void LayoutBottomRows(bool force, bool echo)
        {
            var root = transform as RectTransform;
            if (root == null || root.rect.height <= 0f || layingOut) return;
            layingOut = true;
            try
            {
                var height = root.rect.height;
                var gap = Mathf.Min(16f, height * .008f);
                var footerBottom = Mathf.Min(24f, height * Mathf.Max(.012f, footerY.x));
                var footerHeight = ControlHeight(height, footerY.y - footerY.x);
                if (footerControls != null)
                    foreach (var footer in footerControls)
                        if (footer != null) SetRow(footer, footer.anchorMin.x, footer.anchorMax.x, footerBottom, footerHeight);

                if (instructionCard == null) instructionCard = transform.Find("Instruction Card") as RectTransform;
                var promptBottom = footerBottom + footerHeight + gap;
                var promptHeight = Mathf.Min(height * .07f, Mathf.Max(42f, height * (promptY.y - promptY.x)));
                if (instructionCard != null)
                    SetRow(instructionCard, instructionCard.anchorMin.x, instructionCard.anchorMax.x, promptBottom, promptHeight);

                var controlsBottom = Mathf.Max(height * controlY.x, promptBottom + promptHeight + gap);
                var controlsHeight = ControlHeight(height, controlY.y - controlY.x);
                if (forceControl != null)
                {
                    var x = force && echo ? pairedForceX : singleControlX;
                    SetRow(forceControl, x.x, x.y, controlsBottom, controlsHeight);
                }
                if (echoControl != null)
                {
                    var x = force && echo ? pairedEchoX : singleControlX;
                    SetRow(echoControl, x.x, x.y, controlsBottom, controlsHeight);
                }
                var top = force || echo ? controlsBottom + controlsHeight : promptBottom + promptHeight;
                reservedBottom = Mathf.Clamp((top + gap) / height, .225f, .6f);
            }
            finally { layingOut = false; }
        }

        private float ControlHeight(float height, float span)
        {
            // Keep the touch target large on phones, while allowing the complete
            // dock to fit when a banner or multi-window view leaves very little room.
            return Mathf.Min(height * .16f, Mathf.Max(minimumControlHeight, height * Mathf.Max(.01f, span)));
        }

        private static void SetRow(RectTransform rect, float left, float right, float bottom, float height)
        {
            var x = rect.anchoredPosition.x;
            rect.anchorMin = new Vector2(left, 0f);
            rect.anchorMax = new Vector2(right, 0f);
            rect.pivot = new Vector2(rect.pivot.x, .5f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            rect.anchoredPosition = new Vector2(x, bottom + height * .5f);
        }

        private static void SetMeter(RectTransform fill, float amount, ref float previous)
        {
            if (fill == null || Mathf.Abs(previous - amount) < .005f) return;
            previous = amount;
            fill.localScale = new Vector3(amount, 1f, 1f);
        }
    }
}

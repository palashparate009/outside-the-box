using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    // Optional presentation-only prototype. It never changes a ball's physics state.
    public sealed class LevelPipeTransition : LevelPipeTransitionBase
    {
        public UIPipeGraphic pipe;
        public UIPipeGraphic glass;
        public Image ball;

        public override IEnumerator Play(bool arriving, Camera camera, SpriteRenderer source,
            Vector3 worldPoint, float worldDiameter, Func<bool> blocked, Action landed = null)
        {
            Completed = false;
            if (camera == null || source == null || group == null || pipe == null || glass == null || ball == null)
                yield break;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = false;
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, camera.WorldToScreenPoint(worldPoint), null, out var point);
            // Departure starts after pocket-drop has shrunk the world renderer.
            // Use its normal gameplay diameter for the travelling ball.
            var sourceScreen = camera.WorldToScreenPoint(worldPoint);
            var topScreen = camera.WorldToScreenPoint(worldPoint + Vector3.up * (worldDiameter * 0.5f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, sourceScreen, null, out var sourceLocal);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, topScreen, null, out var topLocal);
            var diameter = Mathf.Clamp(Vector2.Distance(sourceLocal, topLocal) * 2f, 28f, 76f);
            var width = Mathf.Clamp(diameter * 1.75f, 66f, 120f);
            var bounds = rect.rect;
            var start = arriving ? new Vector2(point.x * 0.25f, bounds.yMax + width) : point;
            var end = arriving ? point : new Vector2(bounds.center.x, bounds.yMin - width);
            var travel = Mathf.Max(width * 1.5f, Mathf.Abs(start.y - end.y) * 0.4f);
            var b = start + Vector2.down * travel;
            var c = end + Vector2.up * travel;
            pipe.SetPath(start, b, c, end, width);
            glass.SetPath(start, b, c, end, width);
            ball.sprite = source.sprite;
            var tint = source.color;
            tint.a = 1f;
            ball.color = tint;
            ball.preserveAspect = true;
            ball.gameObject.SetActive(true);
            ball.rectTransform.sizeDelta = Vector2.one * diameter;
            var duration = Mathf.Max(0.2f, arriving ? arrivalSeconds : departureSeconds);
            var fade = Mathf.Min(fadeSeconds, duration * 0.2f);
            var movement = duration - fade;
            var elapsed = 0f;
            var didLand = false;
            while (elapsed < duration)
            {
                if (blocked?.Invoke() == true) { yield return null; continue; }
                elapsed += Mathf.Min(0.05f, Time.unscaledDeltaTime);
                var t = Mathf.Clamp01(elapsed / movement);
                var eased = t * t * (3f - 2f * t);
                ball.rectTransform.anchoredPosition = UIPipeGraphic.Point(start, b, c, end, eased);
                ball.rectTransform.localScale = Vector3.one * (arriving ? 1f : Mathf.Lerp(0.38f, 1f, Mathf.Clamp01(t * 4f)));
                ball.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * -180f);
                group.alpha = Mathf.Min(Mathf.Clamp01(elapsed / fade), Mathf.Clamp01((duration - elapsed) / fade));
                if (arriving && t >= 1f && !didLand)
                {
                    didLand = true;
                    landed?.Invoke();
                    ball.gameObject.SetActive(false);
                }
                yield return null;
            }
            Completed = true;
            Hide();
        }

        public override void Hide()
        {
            if (ball != null) ball.gameObject.SetActive(false);
            base.Hide();
        }
    }
}

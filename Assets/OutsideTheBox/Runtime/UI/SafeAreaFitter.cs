using UnityEngine;

namespace OutsideTheBox
{
    public static class MobileBoardFraming
    {
        public static Rect Frame(Vector2 boardSize, float rim, float railPadding, Rect magnet)
        {
            var arena = new Rect(-boardSize * 0.5f - Vector2.one * rim, boardSize + Vector2.one * (rim * 2f));
            // Center the BOARD, not the union of the board and its current magnet.
            // Equal clearance on every side keeps both the alignment and zoom stable
            // when the magnet changes rails, without reserving an entire extra magnet.
            var horizontalClearance = Mathf.Max(arena.xMin - magnet.xMin, magnet.xMax - arena.xMax);
            var verticalClearance = Mathf.Max(arena.yMin - magnet.yMin, magnet.yMax - arena.yMax);
            var clearance = Mathf.Max(Mathf.Max(0f, railPadding - rim),
                Mathf.Max(horizontalClearance, verticalClearance));
            var size = arena.size + Vector2.one * (clearance * 2f);
            return new Rect(arena.center - size * 0.5f, size);
        }
    }

    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private Rect lastSafeArea;
        private Vector2Int lastScreen;
        private Rect lastContentArea;

        private void OnEnable() => Apply();

        private void Update()
        {
            if (lastSafeArea != Screen.safeArea || lastScreen.x != Screen.width || lastScreen.y != Screen.height ||
                lastContentArea != AvailableScreenRect()) Apply();
        }

        public static Rect AvailableScreenRect()
        {
            var ads = LiveOpsManager.Instance?.Ads;
            ads?.RefreshBannerLayoutMetrics();
            var height = ads?.BannerReservedHeightPixels ?? 0f;
            return ContentRect(Screen.safeArea, new Vector2(Screen.width, Screen.height),
                ads?.BannerAtTop == true ? height : 0f,
                ads?.BannerAtTop != true ? height : 0f, ads?.BannerRespectsSafeArea ?? true);
        }

        public static Rect ContentRect(Rect safe, Vector2 screen, float top, float bottom, bool respectSafeArea)
        {
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(Vector2.zero, screen);
            var min = respectSafeArea ? safe.yMin + bottom : Mathf.Max(safe.yMin, bottom);
            var max = respectSafeArea ? safe.yMax - top : Mathf.Min(safe.yMax, screen.y - top);
            min = Mathf.Clamp(min, safe.yMin, safe.yMax - 1f);
            max = Mathf.Clamp(max, min + 1f, safe.yMax);
            return Rect.MinMaxRect(safe.xMin, min, safe.xMax, max);
        }

        public void Apply()
        {
            var safe = AvailableScreenRect();
            var size = new Vector2(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            var rect = (RectTransform)transform;
            rect.anchorMin = safe.position / size;
            rect.anchorMax = (safe.position + safe.size) / size;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            lastSafeArea = Screen.safeArea;
            lastContentArea = safe;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
        }
    }
}

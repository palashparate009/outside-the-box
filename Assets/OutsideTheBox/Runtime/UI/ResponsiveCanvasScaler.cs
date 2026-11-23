using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    /// <summary>Keeps portrait UI readable on tall phones, tablets, foldables and multi-window sizes.</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class ResponsiveCanvasScaler : MonoBehaviour
    {
        [SerializeField] private Vector2 referenceResolution = new(1080f, 1920f);
        private CanvasScaler scaler;
        private Vector2Int lastScreenSize;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void OnEnable() => Apply();

        private void Update()
        {
            if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height) Apply();
        }

        public void Apply()
        {
            if (scaler == null) scaler = GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            var width = Mathf.Max(1f, Screen.width);
            var height = Mathf.Max(1f, Screen.height);
            var currentAspect = width / height;
            var referenceAspect = referenceResolution.x / referenceResolution.y;
            // Tall/narrow phones match width; wider tablets and foldables match height.
            scaler.matchWidthOrHeight = currentAspect <= referenceAspect ? 0f : 1f;
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        }

        public void SetReferenceResolution(Vector2 resolution)
        {
            referenceResolution = new Vector2(Mathf.Max(1f, resolution.x), Mathf.Max(1f, resolution.y));
            Apply();
        }
    }
}

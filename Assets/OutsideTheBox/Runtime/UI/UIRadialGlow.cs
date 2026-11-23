using UnityEngine;
using UnityEngine.UI;

namespace OutsideTheBox
{
    /// <summary>Soft radial decoration without a texture, hard edge, or shader dependency.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UIRadialGlow : MaskableGraphic
    {
        [SerializeField, Range(12, 64)] private int segments = 32;

        protected override void OnEnable()
        {
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }

        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var rect = GetPixelAdjustedRect();
            var center = rect.center;
            var centerColor = (Color32)color;
            var edgeColor = centerColor;
            edgeColor.a = 0;
            helper.AddVert(center, centerColor, new Vector2(0.5f, 0.5f));
            for (var i = 0; i <= segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                var position = center + new Vector2(Mathf.Cos(angle) * rect.width * 0.5f,
                    Mathf.Sin(angle) * rect.height * 0.5f);
                helper.AddVert(position, edgeColor, Vector2.zero);
                if (i > 0) helper.AddTriangle(0, i, i + 1);
            }
        }
    }
}

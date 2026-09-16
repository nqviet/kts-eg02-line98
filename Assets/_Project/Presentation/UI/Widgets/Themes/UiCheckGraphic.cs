using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>
    /// Procedural vector checkmark silhouette for status badges and selection indicators.
    /// Resolution-independent, font-atlas independent, and batches with canvas elements.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    [DisallowMultipleComponent]
    public sealed class UiCheckGraphic : MaskableGraphic
    {
        [SerializeField] private float m_Thickness = 4.5f;

        public float Thickness
        {
            get => m_Thickness;
            set
            {
                m_Thickness = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            float w = r.width;
            float h = r.height;
            float t = m_Thickness;

            // Normalized anchor points for checkmark shape:
            // p0: start of short leg (left-mid)
            // p1: vertex where legs meet (bottom-center)
            // p2: tip of long leg (top-right)
            Vector2 p0 = new Vector2(-w * 0.32f, -h * 0.04f);
            Vector2 p1 = new Vector2(-w * 0.06f, -h * 0.30f);
            Vector2 p2 = new Vector2(w * 0.34f, h * 0.28f);

            DrawSegment(vh, p0, p1, t);
            DrawSegment(vh, p1, p2, t);
        }

        private void DrawSegment(VertexHelper vh, Vector2 from, Vector2 to, float thickness)
        {
            Vector2 dir = (to - from).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * (thickness * 0.5f);

            int startIdx = vh.currentVertCount;
            UIVertex vert = UIVertex.simpleVert;
            vert.color = color;

            vert.position = from - normal; vh.AddVert(vert);
            vert.position = from + normal; vh.AddVert(vert);
            vert.position = to + normal; vh.AddVert(vert);
            vert.position = to - normal; vh.AddVert(vert);

            vh.AddTriangle(startIdx, startIdx + 1, startIdx + 2);
            vh.AddTriangle(startIdx, startIdx + 2, startIdx + 3);
        }
    }
}

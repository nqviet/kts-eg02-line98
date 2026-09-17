using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>
    /// Resolution-independent vector glyphs for the Themes surface: tab icons (three balls,
    /// 3x3 grid), four-point sparkles, the title leaf and the soft preview halo.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    [DisallowMultipleComponent]
    public sealed class UiGlyphGraphic : MaskableGraphic
    {
        public enum GlyphShape
        {
            Dots,
            Grid,
            Sparkle,
            Leaf,
            Halo,
            Star,
            Bars,
            Gamepad,
            Lock,
            Trophy
        }

        public enum GradientMode
        {
            None,
            Vertical,
            Radial
        }

        [SerializeField] private GlyphShape m_Shape = GlyphShape.Dots;
        [SerializeField] private GradientMode m_Gradient = GradientMode.None;
        [SerializeField] private Color m_GradientA = Color.white; // top (vertical) or inner (radial)
        [SerializeField] private Color m_GradientB = Color.white; // bottom (vertical) or outer (radial)
        [SerializeField, Range(8, 64)] private int m_Segments = 32;

        private Rect m_Rect;

        public GlyphShape Shape
        {
            get => m_Shape;
            set { m_Shape = value; SetVerticesDirty(); }
        }

        public void SetGradient(GradientMode mode, Color a, Color b)
        {
            m_Gradient = mode;
            m_GradientA = a;
            m_GradientB = b;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            m_Rect = rectTransform.rect;

            switch (m_Shape)
            {
                case GlyphShape.Dots: PopulateDots(vertexHelper); break;
                case GlyphShape.Grid: PopulateGrid(vertexHelper); break;
                case GlyphShape.Sparkle: PopulateSparkle(vertexHelper); break;
                case GlyphShape.Leaf: PopulateLeaf(vertexHelper); break;
                case GlyphShape.Halo: PopulateHalo(vertexHelper); break;
                case GlyphShape.Star: PopulateStar(vertexHelper); break;
                case GlyphShape.Bars: PopulateBars(vertexHelper); break;
                case GlyphShape.Gamepad: PopulateGamepad(vertexHelper); break;
                case GlyphShape.Lock: PopulateLock(vertexHelper); break;
                case GlyphShape.Trophy: PopulateTrophy(vertexHelper); break;
            }
        }

        private void PopulateDots(VertexHelper vertexHelper)
        {
            Vector2 center = m_Rect.center;
            float radius = Mathf.Min(m_Rect.height * 0.5f, m_Rect.width / 6.3f);
            float spacing = radius * 2.15f;

            for (int i = -1; i <= 1; i++)
            {
                var points = new Vector2[m_Segments];
                for (int s = 0; s < m_Segments; s++)
                {
                    float angle = s * Mathf.PI * 2f / m_Segments;
                    points[s] = center + new Vector2(i * spacing + Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                }
                AddFan(vertexHelper, center + new Vector2(i * spacing, 0f), points, 1f);
            }
        }

        private void PopulateGrid(VertexHelper vertexHelper)
        {
            const float gapRatio = 0.3f;
            float size = Mathf.Min(m_Rect.width, m_Rect.height);
            float cell = size / (3f + gapRatio * 2f);
            float pitch = cell * (1f + gapRatio);
            float corner = cell * 0.24f;
            Vector2 center = m_Rect.center;

            for (int row = -1; row <= 1; row++)
            {
                for (int column = -1; column <= 1; column++)
                {
                    Vector2 cellCenter = center + new Vector2(column * pitch, row * pitch);
                    AddFan(vertexHelper, cellCenter, RoundedRectPoints(cellCenter, cell * 0.5f, corner), 1f);
                }
            }
        }

        private void PopulateSparkle(VertexHelper vertexHelper)
        {
            // Superellipse |x|^p + |y|^p = 1 with p < 1 gives the concave four-point star.
            const float exponent = 0.62f;
            Vector2 center = m_Rect.center;
            Vector2 radius = new Vector2(m_Rect.width, m_Rect.height) * 0.5f;
            int count = m_Segments * 2;
            var points = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                points[i] = center + new Vector2(
                    Mathf.Sign(cos) * Mathf.Pow(Mathf.Abs(cos), 2f / exponent) * radius.x,
                    Mathf.Sign(sin) * Mathf.Pow(Mathf.Abs(sin), 2f / exponent) * radius.y);
            }

            AddFan(vertexHelper, center, points, 1f);
        }

        private void PopulateLeaf(VertexHelper vertexHelper)
        {
            Vector2 center = m_Rect.center;
            float halfWidth = m_Rect.width * 0.5f;
            float halfHeight = m_Rect.height * 0.5f;
            var points = new Vector2[m_Segments * 2];

            // Right edge runs tip -> stem, left edge runs stem -> tip.
            for (int i = 0; i < m_Segments; i++)
            {
                points[i] = center + LeafEdge(i / (float)m_Segments, halfWidth, halfHeight);
                Vector2 mirrored = LeafEdge(1f - i / (float)m_Segments, halfWidth, halfHeight);
                points[m_Segments + i] = center + new Vector2(-mirrored.x, mirrored.y);
            }

            AddFan(vertexHelper, center, points, 1f);
        }

        private static Vector2 LeafEdge(float t, float halfWidth, float halfHeight)
        {
            return new Vector2(
                halfWidth * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 0.85f),
                Mathf.Lerp(halfHeight, -halfHeight, t));
        }

        private void PopulateHalo(VertexHelper vertexHelper)
        {
            // Center -> bright ring near the ball silhouette -> transparent outer edge.
            Vector2 center = m_Rect.center;
            float radius = Mathf.Min(m_Rect.width, m_Rect.height) * 0.5f;
            const float ringRatio = 0.66f;
            int count = m_Segments * 2;

            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center, 0.35f);
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddVertex(vertexHelper, center + direction * radius * ringRatio, 1f);
                AddVertex(vertexHelper, center + direction * radius, 0f);
            }

            for (int i = 0; i < count; i++)
            {
                int ring = centerIndex + 1 + i * 2;
                int nextRing = centerIndex + 1 + (i + 1) % count * 2;
                vertexHelper.AddTriangle(centerIndex, ring, nextRing);
                vertexHelper.AddTriangle(ring, ring + 1, nextRing + 1);
                vertexHelper.AddTriangle(ring, nextRing + 1, nextRing);
            }
        }

        private void PopulateStar(VertexHelper vertexHelper)
        {
            // Five-point star; the visual center sits slightly below the geometric one.
            float outer = Mathf.Min(m_Rect.width, m_Rect.height) * 0.52f;
            float inner = outer * 0.47f;
            Vector2 center = m_Rect.center + new Vector2(0f, -outer * 0.06f);
            var points = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                float radius = i % 2 == 0 ? outer : inner;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            AddFan(vertexHelper, center, points, 1f);
        }

        private void PopulateBars(VertexHelper vertexHelper)
        {
            float[] heights = { 0.55f, 1f, 0.76f };
            float barWidth = m_Rect.width / 4.2f;
            float gap = (m_Rect.width - barWidth * 3f) * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                float height = m_Rect.height * heights[i];
                Vector2 center = new Vector2(m_Rect.xMin + barWidth * 0.5f + i * (barWidth + gap), m_Rect.yMin + height * 0.5f);
                AddFan(vertexHelper, center, RoundedRectPoints(center, new Vector2(barWidth, height) * 0.5f, barWidth * 0.3f), 1f);
            }
        }

        private void PopulateGamepad(VertexHelper vertexHelper)
        {
            // Body plus two rounded grips; the d-pad and buttons are child glyphs in the surface color.
            float width = m_Rect.width;
            float height = m_Rect.height;
            Vector2 bodyCenter = m_Rect.center + new Vector2(0f, height * 0.14f);
            Vector2 bodyHalf = new Vector2(width * 0.5f, height * 0.34f);
            AddFan(vertexHelper, bodyCenter, RoundedRectPoints(bodyCenter, bodyHalf, height * 0.3f), 1f);

            float gripRadius = width * 0.19f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 gripCenter = new Vector2(m_Rect.center.x + side * (width * 0.5f - gripRadius * 1.05f), m_Rect.yMin + gripRadius);
                AddFan(vertexHelper, gripCenter, CirclePoints(gripCenter, gripRadius), 1f);
                Vector2 bridgeCenter = new Vector2(gripCenter.x - side * gripRadius * 0.2f, (gripCenter.y + bodyCenter.y) * 0.5f);
                AddFan(vertexHelper, bridgeCenter, RoundedRectPoints(bridgeCenter, new Vector2(gripRadius, (bodyCenter.y - gripCenter.y) * 0.5f), 0f), 1f);
            }
        }

        private void PopulateLock(VertexHelper vertexHelper)
        {
            float width = m_Rect.width;
            float height = m_Rect.height;
            float bodyHeight = height * 0.58f;
            Vector2 bodyCenter = new Vector2(m_Rect.center.x, m_Rect.yMin + bodyHeight * 0.5f);
            AddFan(vertexHelper, bodyCenter, RoundedRectPoints(bodyCenter, new Vector2(width * 0.5f, bodyHeight * 0.5f), width * 0.14f), 1f);

            float thickness = width * 0.15f;
            float radius = width * 0.36f - thickness * 0.5f;
            Vector2 arcCenter = new Vector2(m_Rect.center.x, m_Rect.yMax - radius - thickness * 0.5f);
            AddArc(vertexHelper, arcCenter, radius, thickness, 0f, Mathf.PI);
            float legBottom = m_Rect.yMin + bodyHeight - 1f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 legCenter = new Vector2(arcCenter.x + side * radius, (arcCenter.y + legBottom) * 0.5f);
                AddFan(vertexHelper, legCenter, RoundedRectPoints(legCenter, new Vector2(thickness * 0.5f, (arcCenter.y - legBottom) * 0.5f), 0f), 1f);
            }
        }

        private void PopulateTrophy(VertexHelper vertexHelper)
        {
            float width = m_Rect.width;
            float height = m_Rect.height;
            float cupRadius = width * 0.3f;
            float rimY = m_Rect.yMax;
            float bowlY = rimY - height * 0.2f;

            // Cup: a straight upper band closed by a half ellipse.
            Vector2 bandCenter = new Vector2(m_Rect.center.x, (rimY + bowlY) * 0.5f);
            AddFan(vertexHelper, bandCenter, RoundedRectPoints(bandCenter, new Vector2(cupRadius, (rimY - bowlY) * 0.5f + 1f), 0f), 1f);
            var bowl = new Vector2[m_Segments + 1];
            Vector2 bowlCenter = new Vector2(m_Rect.center.x, bowlY);
            for (int i = 0; i <= m_Segments; i++)
            {
                float angle = Mathf.PI + i * Mathf.PI / m_Segments;
                bowl[i] = bowlCenter + new Vector2(Mathf.Cos(angle) * cupRadius, Mathf.Sin(angle) * height * 0.3f);
            }
            AddFan(vertexHelper, bowlCenter, bowl, 1f);

            float handleRadius = width * 0.14f;
            float handleThickness = width * 0.07f;
            float handleY = rimY - height * 0.2f;
            AddArc(vertexHelper, new Vector2(m_Rect.center.x - cupRadius, handleY), handleRadius, handleThickness, Mathf.PI * 0.5f, Mathf.PI * 1.6f);
            AddArc(vertexHelper, new Vector2(m_Rect.center.x + cupRadius, handleY), handleRadius, handleThickness, -Mathf.PI * 0.6f, Mathf.PI * 0.5f);

            float baseHeight = height * 0.13f;
            Vector2 baseCenter = new Vector2(m_Rect.center.x, m_Rect.yMin + baseHeight * 0.5f);
            AddFan(vertexHelper, baseCenter, RoundedRectPoints(baseCenter, new Vector2(width * 0.3f, baseHeight * 0.5f), baseHeight * 0.3f), 1f);
            float stemTop = bowlY - height * 0.28f;
            float stemBottom = m_Rect.yMin + baseHeight - 1f;
            Vector2 stemCenter = new Vector2(m_Rect.center.x, (stemTop + stemBottom) * 0.5f);
            AddFan(vertexHelper, stemCenter, RoundedRectPoints(stemCenter, new Vector2(width * 0.07f, (stemTop - stemBottom) * 0.5f), 0f), 1f);
        }

        private void AddArc(VertexHelper vertexHelper, Vector2 center, float radius, float thickness, float startAngle, float endAngle)
        {
            int start = vertexHelper.currentVertCount;
            float innerRadius = radius - thickness * 0.5f;
            float outerRadius = radius + thickness * 0.5f;
            for (int i = 0; i <= m_Segments; i++)
            {
                float angle = Mathf.Lerp(startAngle, endAngle, i / (float)m_Segments);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                AddVertex(vertexHelper, center + direction * innerRadius, 1f);
                AddVertex(vertexHelper, center + direction * outerRadius, 1f);
            }
            for (int i = 0; i < m_Segments; i++)
            {
                int index = start + i * 2;
                vertexHelper.AddTriangle(index, index + 1, index + 3);
                vertexHelper.AddTriangle(index, index + 3, index + 2);
            }
        }

        private Vector2[] CirclePoints(Vector2 center, float radius)
        {
            var points = new Vector2[m_Segments];
            for (int i = 0; i < m_Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / m_Segments;
                points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }
            return points;
        }

        private Vector2[] RoundedRectPoints(Vector2 center, float halfSize, float corner)
        {
            return RoundedRectPoints(center, new Vector2(halfSize, halfSize), corner);
        }

        private Vector2[] RoundedRectPoints(Vector2 center, Vector2 halfExtents, float corner)
        {
            const int cornerSegments = 4;
            corner = Mathf.Min(corner, Mathf.Min(halfExtents.x, halfExtents.y));
            var points = new Vector2[(cornerSegments + 1) * 4];
            Vector2 inner = halfExtents - Vector2.one * corner;
            int index = 0;

            for (int quadrant = 0; quadrant < 4; quadrant++)
            {
                Vector2 cornerCenter = center + new Vector2(
                    quadrant == 0 || quadrant == 3 ? inner.x : -inner.x,
                    quadrant < 2 ? inner.y : -inner.y);
                for (int s = 0; s <= cornerSegments; s++)
                {
                    float angle = (quadrant + s / (float)cornerSegments) * Mathf.PI * 0.5f;
                    points[index++] = cornerCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * corner;
                }
            }

            return points;
        }

        private void AddFan(VertexHelper vertexHelper, Vector2 center, Vector2[] points, float alpha)
        {
            int centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center, alpha);
            for (int i = 0; i < points.Length; i++)
            {
                AddVertex(vertexHelper, points[i], alpha);
            }

            for (int i = 0; i < points.Length; i++)
            {
                vertexHelper.AddTriangle(centerIndex, centerIndex + 1 + i, centerIndex + 1 + (i + 1) % points.Length);
            }
        }

        private void AddVertex(VertexHelper vertexHelper, Vector2 position, float alpha)
        {
            Color tint = color;
            switch (m_Gradient)
            {
                case GradientMode.Vertical:
                {
                    float t = m_Rect.height > 0f ? Mathf.InverseLerp(m_Rect.yMin, m_Rect.yMax, position.y) : 0f;
                    tint *= Color.Lerp(m_GradientB, m_GradientA, t);
                    break;
                }
                case GradientMode.Radial:
                {
                    float maxRadius = Mathf.Max(1f, Mathf.Min(m_Rect.width, m_Rect.height) * 0.5f);
                    float t = Mathf.Clamp01((position - m_Rect.center).magnitude / maxRadius);
                    tint *= Color.Lerp(m_GradientA, m_GradientB, t);
                    break;
                }
            }

            tint.a *= alpha;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = tint;
            vertex.position = position;
            vertexHelper.AddVert(vertex);
        }
    }
}

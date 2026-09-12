using System.Collections.Generic;
using UnityEngine;

namespace Line98.Design
{
    /// <summary>
    /// Procedural visual asset factory for generating runtime textures and vector sprites.
    /// Acts as the default procedural asset provider for the game's visual design.
    /// </summary>
    public static class Line98ArtFactory
    {
        private static Sprite s_RoundedSprite;
        private static Sprite s_OutlineSprite;
        private static Sprite s_GlowSprite;
        private static Sprite s_VerticalFadeSprite;
        private static Texture2D s_LandscapeTexture;
        private static readonly Dictionary<int, Sprite> s_BallSprites = new Dictionary<int, Sprite>();
        private static readonly Dictionary<Line98IconType, Sprite> s_IconSprites = new Dictionary<Line98IconType, Sprite>();

        public static Sprite GetRoundedSprite()
        {
            if (s_RoundedSprite != null)
            {
                return s_RoundedSprite;
            }

            const int size = 128;
            const float radius = 31f;
            Texture2D texture = NewTexture(size, size, "Line98 Rounded Panel");
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = RoundedRectDistance(x + 0.5f, y + 0.5f, size, size, radius);
                    float alpha = 1f - Mathf.SmoothStep(-1.5f, 1.5f, distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            s_RoundedSprite = CreateRuntimeSprite(texture, new Rect(0f, 0f, size, size), size, new Vector4(radius, radius, radius, radius));
            return s_RoundedSprite;
        }

        public static Sprite GetOutlineSprite()
        {
            if (s_OutlineSprite != null)
            {
                return s_OutlineSprite;
            }

            const int size = 128;
            const float radius = 27f;
            Texture2D texture = NewTexture(size, size, "Line98 Selection Outline");
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Abs(RoundedRectDistance(x + 0.5f, y + 0.5f, size, size, radius));
                    float alpha = 1f - Mathf.SmoothStep(1f, 3.5f, distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            s_OutlineSprite = CreateRuntimeSprite(texture, new Rect(0f, 0f, size, size), size, new Vector4(radius, radius, radius, radius));
            return s_OutlineSprite;
        }

        public static Sprite GetGlowSprite()
        {
            if (s_GlowSprite != null)
            {
                return s_GlowSprite;
            }

            const int size = 128;
            Texture2D texture = NewTexture(size, size, "Line98 Glow");
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float alpha = Mathf.Exp(-(nx * nx + ny * ny) * 2.2f) * 0.72f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            s_GlowSprite = CreateRuntimeSprite(texture, new Rect(0f, 0f, size, size), size);
            return s_GlowSprite;
        }

        public static Sprite GetVerticalFadeSprite()
        {
            if (s_VerticalFadeSprite != null)
            {
                return s_VerticalFadeSprite;
            }

            const int size = 64;
            Texture2D texture = NewTexture(2, size, "Line98 Sky Fade");
            Color32[] pixels = new Color32[size * 2];
            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                float alpha = Mathf.SmoothStep(0f, 0.78f, t) * 0.5f;
                Color color = new Color(1f, 1f, 1f, alpha);
                pixels[y * 2] = color;
                pixels[y * 2 + 1] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            s_VerticalFadeSprite = CreateRuntimeSprite(texture, new Rect(0f, 0f, 2f, size), size);
            return s_VerticalFadeSprite;
        }

        public static Texture2D GetLandscapeTexture()
        {
            if (s_LandscapeTexture != null)
            {
                return s_LandscapeTexture;
            }

            const int width = 384;
            const int height = 683;
            Texture2D texture = NewTexture(width, height, "Line98 Procedural Landscape");
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float ny = y / (float)(height - 1);
                for (int x = 0; x < width; x++)
                {
                    float nx = x / (float)(width - 1);
                    Color color = LandscapePixel(nx, ny);
                    pixels[y * width + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            s_LandscapeTexture = texture;
            return s_LandscapeTexture;
        }

        public static Sprite GetBallSprite(int colorIndex, Color baseColor)
        {
            if (s_BallSprites.TryGetValue(colorIndex, out Sprite sprite))
            {
                return sprite;
            }

            const int size = 192;
            Texture2D texture = NewTexture(size, size, "Line98 Ball " + colorIndex);
            Color32[] pixels = new Color32[size * size];
            Vector3 light = new Vector3(-0.48f, 0.58f, 0.66f).normalized;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float radiusSquared = nx * nx + ny * ny;
                    if (radiusSquared > 1.04f)
                    {
                        pixels[y * size + x] = new Color(1f, 1f, 1f, 0f);
                        continue;
                    }

                    float z = Mathf.Sqrt(Mathf.Max(0f, 1f - radiusSquared));
                    float lightAmount = Mathf.Clamp01(Vector3.Dot(new Vector3(nx, ny, z), light));
                    float edge = Mathf.Clamp01((1f - Mathf.Sqrt(radiusSquared)) * 8f);
                    float shade = 0.34f + lightAmount * 0.70f;
                    Color color = baseColor * shade;
                    float highlight = Mathf.Exp(-((nx + 0.34f) * (nx + 0.34f) * 24f + (ny - 0.38f) * (ny - 0.38f) * 34f));
                    color = Color.Lerp(color, Color.white, highlight * 0.90f);
                    float rim = Mathf.Pow(Mathf.Clamp01(1f - radiusSquared), 0.28f);
                    color = Color.Lerp(new Color(0.01f, 0.05f, 0.18f), color, rim);
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - radiusSquared) / 0.035f)) * edge;
                    pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            sprite = CreateRuntimeSprite(texture, new Rect(0f, 0f, size, size), size);
            s_BallSprites.Add(colorIndex, sprite);
            return sprite;
        }

        public static Sprite GetIconSprite(Line98IconType icon)
        {
            if (s_IconSprites.TryGetValue(icon, out Sprite sprite))
            {
                return sprite;
            }

            const int size = 160;
            Texture2D texture = NewTexture(size, size, "Line98 " + icon + " Icon");
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f);
                    float alpha = IconAlpha(icon, point);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            sprite = CreateRuntimeSprite(texture, new Rect(0f, 0f, size, size), size);
            s_IconSprites.Add(icon, sprite);
            return sprite;
        }

        private static Color LandscapePixel(float x, float y)
        {
            const float horizon = 0.31f;
            Color color;
            if (y > horizon)
            {
                float skyT = Mathf.InverseLerp(horizon, 1f, y);
                color = Color.Lerp(new Color(0.95f, 0.80f, 0.75f), new Color(0.38f, 0.72f, 0.98f), skyT);
                float cloudA = Cloud(x, y, 0.17f, 0.77f, 0.22f, 0.055f) + Cloud(x, y, 0.83f, 0.86f, 0.23f, 0.07f) + Cloud(x, y, 0.52f, 0.60f, 0.32f, 0.04f);
                color = Color.Lerp(color, Color.white, Mathf.Clamp01(cloudA) * 0.58f);
            }
            else
            {
                float waterT = Mathf.InverseLerp(0f, horizon, y);
                color = Color.Lerp(new Color(0.26f, 0.48f, 0.68f), new Color(0.78f, 0.84f, 0.91f), waterT);
                float reflection = 0.026f * Mathf.Sin((x * 27f + y * 70f) * 1.9f) * Mathf.SmoothStep(0f, 0.28f, y);
                color += new Color(reflection, reflection, reflection * 1.5f);
            }

            float farRidge = 0.41f + 0.065f * Mathf.PerlinNoise(x * 3.1f + 0.1f, 1.2f) + 0.10f * Peak(x, 0.25f, 0.16f) + 0.12f * Peak(x, 0.70f, 0.13f);
            float midRidge = 0.32f + 0.075f * Mathf.PerlinNoise(x * 4.8f + 5f, 3.4f) + 0.12f * Peak(x, 0.12f, 0.13f) + 0.10f * Peak(x, 0.80f, 0.18f);
            float nearRidge = 0.18f + 0.08f * Mathf.PerlinNoise(x * 7.6f + 8.5f, 6.8f) + 0.18f * Peak(x, 0.05f, 0.17f) + 0.19f * Peak(x, 0.95f, 0.16f);
            if (y < farRidge)
            {
                color = Color.Lerp(color, new Color(0.43f, 0.57f, 0.72f), 0.73f);
            }

            if (y < midRidge)
            {
                color = Color.Lerp(color, new Color(0.25f, 0.43f, 0.56f), 0.82f);
            }

            if (y < nearRidge)
            {
                float vegetation = 0.035f * Mathf.PerlinNoise(x * 32f, y * 25f);
                color = new Color(0.08f + vegetation, 0.25f + vegetation * 2f, 0.24f + vegetation, 1f);
            }

            float glow = Mathf.Exp(-((x - 0.52f) * (x - 0.52f) * 28f + (y - 0.31f) * (y - 0.31f) * 38f));
            color = Color.Lerp(color, new Color(1f, 0.87f, 0.67f), glow * 0.28f);
            return color;
        }

        private static float Cloud(float x, float y, float cx, float cy, float width, float height)
        {
            float dx = (x - cx) / width;
            float dy = (y - cy) / height;
            return Mathf.Exp(-(dx * dx + dy * dy) * 3.8f);
        }

        private static float Peak(float value, float center, float width)
        {
            float t = (value - center) / width;
            return Mathf.Exp(-t * t * 2f);
        }

        private static float IconAlpha(Line98IconType icon, Vector2 point)
        {
            switch (icon)
            {
                case Line98IconType.Gear:
                    return GearAlpha(point);
                case Line98IconType.Bars:
                    return BarsAlpha(point);
                case Line98IconType.Crown:
                    return CrownAlpha(point);
                case Line98IconType.Undo:
                    return TurnAlpha(point, false);
                case Line98IconType.Restart:
                    return TurnAlpha(point, true);
                case Line98IconType.Dpad:
                    return DpadAlpha(point);
                case Line98IconType.Arrow:
                    return TriangleAlpha(point, new Vector2(-0.68f, -0.46f), new Vector2(0.68f, -0.46f), new Vector2(0f, 0.68f));
                default:
                    return 0f;
            }
        }

        private static float GearAlpha(Vector2 point)
        {
            float radius = point.magnitude;
            float angle = Mathf.Atan2(point.y, point.x);
            float teeth = 0.58f + Mathf.Max(0f, Mathf.Cos(angle * 8f)) * 0.16f;
            bool filled = radius > 0.22f && radius < teeth;
            return filled ? 1f : 0f;
        }

        private static float BarsAlpha(Vector2 point)
        {
            bool left = RoundedBox(point, new Vector2(-0.52f, -0.43f), new Vector2(-0.23f, 0.16f), 0.10f);
            bool middle = RoundedBox(point, new Vector2(-0.10f, -0.43f), new Vector2(0.20f, 0.53f), 0.10f);
            bool right = RoundedBox(point, new Vector2(0.34f, -0.43f), new Vector2(0.64f, 0.79f), 0.10f);
            return left || middle || right ? 1f : 0f;
        }

        private static float CrownAlpha(Vector2 point)
        {
            Vector2[] crown =
            {
                new Vector2(-0.65f, -0.42f),
                new Vector2(-0.55f, 0.50f),
                new Vector2(-0.18f, 0.08f),
                new Vector2(0f, 0.70f),
                new Vector2(0.20f, 0.08f),
                new Vector2(0.58f, 0.50f),
                new Vector2(0.67f, -0.42f),
            };
            return PointInPolygon(point, crown) || RoundedBox(point, new Vector2(-0.68f, -0.57f), new Vector2(0.70f, -0.33f), 0.08f) ? 1f : 0f;
        }

        private static float TurnAlpha(Vector2 point, bool clockwise)
        {
            Vector2 adjusted = clockwise ? point : new Vector2(-point.x, point.y);
            float radius = adjusted.magnitude;
            float angle = Mathf.Atan2(adjusted.y, adjusted.x);
            bool arc = radius > 0.38f && radius < 0.62f && !(angle > 0.45f && angle < 1.25f);
            bool arrow = TriangleAlpha(adjusted, new Vector2(0.22f, 0.76f), new Vector2(0.78f, 0.66f), new Vector2(0.56f, 0.22f)) > 0f;
            return arc || arrow ? 1f : 0f;
        }

        private static float DpadAlpha(Vector2 point)
        {
            bool up = TriangleAlpha(point, new Vector2(-0.18f, 0.06f), new Vector2(0.18f, 0.06f), new Vector2(0f, 0.50f)) > 0f;
            bool down = TriangleAlpha(point, new Vector2(-0.18f, -0.06f), new Vector2(0.18f, -0.06f), new Vector2(0f, -0.50f)) > 0f;
            bool left = TriangleAlpha(point, new Vector2(-0.06f, -0.18f), new Vector2(-0.06f, 0.18f), new Vector2(-0.50f, 0f)) > 0f;
            bool right = TriangleAlpha(point, new Vector2(0.06f, -0.18f), new Vector2(0.06f, 0.18f), new Vector2(0.50f, 0f)) > 0f;
            return up || down || left || right ? 1f : 0f;
        }

        private static bool RoundedBox(Vector2 point, Vector2 min, Vector2 max, float radius)
        {
            Vector2 center = (min + max) * 0.5f;
            Vector2 halfSize = (max - min) * 0.5f;
            Vector2 delta = new Vector2(Mathf.Abs(point.x - center.x), Mathf.Abs(point.y - center.y)) - halfSize + Vector2.one * radius;
            return Mathf.Max(delta.x, delta.y) < 0f || Mathf.Min(delta.magnitude, 0f) + Mathf.Max(delta.x, delta.y) <= 0f;
        }

        private static float TriangleAlpha(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float sign1 = Sign(point, a, b);
            float sign2 = Sign(point, b, c);
            float sign3 = Sign(point, c, a);
            bool hasNegative = sign1 < 0f || sign2 < 0f || sign3 < 0f;
            bool hasPositive = sign1 > 0f || sign2 > 0f || sign3 > 0f;
            return hasNegative && hasPositive ? 0f : 1f;
        }

        private static float Sign(Vector2 point, Vector2 a, Vector2 b)
        {
            return (point.x - b.x) * (a.y - b.y) - (a.x - b.x) * (point.y - b.y);
        }

        private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                bool crosses = (a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (crosses)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static float RoundedRectDistance(float x, float y, float width, float height, float radius)
        {
            Vector2 point = new Vector2(x - width * 0.5f, y - height * 0.5f);
            Vector2 halfSize = new Vector2(width, height) * 0.5f - Vector2.one * radius;
            Vector2 delta = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - halfSize;
            return Mathf.Min(Mathf.Max(delta.x, delta.y), 0f) + new Vector2(Mathf.Max(delta.x, 0f), Mathf.Max(delta.y, 0f)).magnitude - radius;
        }

        private static Sprite CreateRuntimeSprite(Texture2D texture, Rect rect, float pixelsPerUnit, Vector4 border = default)
        {
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        private static Texture2D NewTexture(int width, int height, string name)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            return texture;
        }
    }
}

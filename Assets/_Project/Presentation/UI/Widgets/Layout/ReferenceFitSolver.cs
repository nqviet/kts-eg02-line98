using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Pure solver that fits an authored reference composition inside a host rect with one
    /// uniform scale, honouring the device safe area. Used by full-page popups and the main menu.
    /// </summary>
    public static class ReferenceFitSolver
    {
        public readonly struct FitResult
        {
            public readonly float Scale;
            public readonly Vector2 Offset;
            public readonly bool IsValid;

            public FitResult(float scale, Vector2 offset)
            {
                Scale = scale;
                Offset = offset;
                IsValid = true;
            }
        }

        /// <summary>
        /// Converts the screen safe area into insets expressed in host units.
        /// The host is expected to cover the whole screen (a stretched canvas child).
        /// </summary>
        public static RectOffsetF GetSafeInsets(Vector2 hostSize, Rect safeArea, int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return default;
            }

            float left = Mathf.Clamp(safeArea.xMin, 0f, screenWidth) / screenWidth * hostSize.x;
            float right = Mathf.Clamp(screenWidth - safeArea.xMax, 0f, screenWidth) / screenWidth * hostSize.x;
            float bottom = Mathf.Clamp(safeArea.yMin, 0f, screenHeight) / screenHeight * hostSize.y;
            float top = Mathf.Clamp(screenHeight - safeArea.yMax, 0f, screenHeight) / screenHeight * hostSize.y;
            return new RectOffsetF(left, right, top, bottom);
        }

        /// <summary>
        /// Returns the uniform scale and centre offset (host units, centre anchored) that place the
        /// reference size inside the host rect minus the insets.
        /// </summary>
        public static FitResult Solve(Vector2 hostSize, RectOffsetF insets, Vector2 referenceSize, float minScale, float maxScale)
        {
            if (hostSize.x <= 1f || hostSize.y <= 1f || referenceSize.x <= 0f || referenceSize.y <= 0f)
            {
                return default;
            }

            float availableWidth = Mathf.Max(1f, hostSize.x - insets.Left - insets.Right);
            float availableHeight = Mathf.Max(1f, hostSize.y - insets.Top - insets.Bottom);
            float scale = Mathf.Min(availableWidth / referenceSize.x, availableHeight / referenceSize.y);
            scale = Mathf.Clamp(scale, Mathf.Max(0.01f, minScale), Mathf.Max(minScale, maxScale));

            Vector2 offset = new Vector2(
                (insets.Left - insets.Right) * 0.5f,
                (insets.Bottom - insets.Top) * 0.5f);
            return new FitResult(scale, offset);
        }
    }

    /// <summary>Float insets in canvas units.</summary>
    public readonly struct RectOffsetF
    {
        public readonly float Left;
        public readonly float Right;
        public readonly float Top;
        public readonly float Bottom;

        public RectOffsetF(float left, float right, float top, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }
    }
}

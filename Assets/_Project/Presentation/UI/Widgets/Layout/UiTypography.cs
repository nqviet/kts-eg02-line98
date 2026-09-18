using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Minimum readable text sizes for the responsive UI.
    /// <para>
    /// Floors are expressed in canvas units at the 1080-wide reference, which is also the unit
    /// TMP_Text.fontSize uses here. On a 3x-density phone 1080 canvas units span 360dp, so
    /// dividing a floor by 3 gives its dp value -- 30 units is 10dp, the Material minimum for
    /// body copy. Because the responsive pass multiplies authored sizes by a layout scale that
    /// drops below 1 on short or wide windows, a text that must stay readable needs a floor as
    /// well as a scale, or it shrinks below legibility on exactly the devices that need it most.
    /// </para>
    /// <para>
    /// These floors are applied per text, not globally -- see <see cref="UiMinFontSize"/>. A blanket
    /// floor would raise every string authored below <see cref="MinBody"/> up to it at scale 1,
    /// which reflows the whole UI rather than fixing a readability bug.
    /// </para>
    /// </summary>
    public static class UiTypography
    {
        /// <summary>10dp at 3x. Absolute floor -- no text may render smaller than this.</summary>
        public const float MinBody = 30f;

        /// <summary>12dp at 3x. For short, load-bearing strings: counters, badges, captions.</summary>
        public const float MinCaption = 36f;

        /// <summary>Titles stay clearly dominant even on the smallest window.</summary>
        public const float MinTitle = 44f;

        /// <summary>
        /// Scales an authored font size, never returning less than <paramref name="minFontSize"/>.
        /// A non-positive floor falls back to <see cref="MinBody"/> so an unconfigured text is
        /// still protected.
        /// </summary>
        public static float ResolveFontSize(float authoredSize, float scale, float minFontSize)
        {
            float floor = minFontSize > 0f ? minFontSize : MinBody;

            // A zero or negative authored size means "not authored"; leave it alone rather than
            // inflating a text the designer deliberately collapsed.
            if (authoredSize <= 0f) return authoredSize;

            return Mathf.Max(authoredSize * scale, floor);
        }
    }
}

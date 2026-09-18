using TMPro;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Gives one text a minimum font size, so the responsive scale pass cannot shrink it below
    /// legibility. Floors are opt-in: attach this to short, load-bearing strings -- counters,
    /// badges, captions -- and leave the rest of the UI at its authored size.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class UiMinFontSize : MonoBehaviour
    {
        [Tooltip("Floor in canvas units at the 1080 reference. Divide by 3 for dp at 3x density.")]
        [SerializeField] private float m_MinFontSize = UiTypography.MinCaption;

        public float MinFontSize => m_MinFontSize;

        public void SetMinFontSize(float minFontSize)
        {
            m_MinFontSize = minFontSize;
        }
    }
}

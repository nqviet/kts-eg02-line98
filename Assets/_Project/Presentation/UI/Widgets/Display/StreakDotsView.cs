using System;
using UnityEngine;
using UnityEngine.UI;
using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Displays 7 sequential daily streak indicator dots on the MainMenu streak card.
    /// Slots up to min(streak, 7) are illuminated with StreakDotOn, and the rest with StreakDotOff.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StreakDotsView : MonoBehaviour
    {
        [SerializeField] private Image[] m_DotImages = Array.Empty<Image>();
        [SerializeField] private Color m_DefaultOnColor = new Color(0.247f, 0.725f, 0.502f, 1f); // #3FB980
        [SerializeField] private Color m_DefaultOffColor = new Color(0.85f, 0.88f, 0.92f, 0.5f);

        private int m_CurrentStreak;
        private UiThemeSO m_CurrentTheme;

        public int CurrentStreak => m_CurrentStreak;
        public Image[] DotImages => m_DotImages;

        public void Configure(Image[] dots)
        {
            m_DotImages = dots;
            Refresh();
        }

        public void SetStreak(int streak, UiThemeSO theme = null)
        {
            m_CurrentStreak = Mathf.Max(0, streak);
            if (theme != null)
            {
                m_CurrentTheme = theme;
            }
            Refresh();
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null) return;
            m_CurrentTheme = theme;
            Refresh();
        }

        public void Refresh()
        {
            if (m_DotImages == null || m_DotImages.Length == 0)
            {
                m_DotImages = GetComponentsInChildren<Image>(true);
            }

            Color onColor = m_CurrentTheme != null ? m_CurrentTheme.StreakDotOn : m_DefaultOnColor;
            Color offColor = m_CurrentTheme != null ? m_CurrentTheme.StreakDotOff : m_DefaultOffColor;

            int filledCount = Mathf.Min(m_CurrentStreak, m_DotImages.Length);

            for (int i = 0; i < m_DotImages.Length; i++)
            {
                if (m_DotImages[i] != null)
                {
                    m_DotImages[i].color = i < filledCount ? onColor : offColor;
                }
            }
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Presenter/View-binder for the MainMenu screen: binds Best Score and Daily Streak
    /// to the UI header cards, controls StreakDotsView, and responds to theme updates.
    /// Hydrated by AppRoot without violating Presentation -> Services asmdef isolation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private UiValueCard m_BestCard;
        [SerializeField] private TMP_Text m_StreakText;
        [SerializeField] private StreakDotsView m_StreakDotsView;

        [Header("Theme")]
        [SerializeField] private UiThemeSO m_Theme;

        private int m_BestScore;
        private int m_CurrentStreak;

        public UiValueCard BestCard => m_BestCard;
        public TMP_Text StreakText => m_StreakText;
        public StreakDotsView StreakDotsView => m_StreakDotsView;
        public int BestScore => m_BestScore;
        public int CurrentStreak => m_CurrentStreak;

        public void Configure(UiValueCard bestCard, TMP_Text streakText, StreakDotsView dotsView)
        {
            m_BestCard = bestCard;
            m_StreakText = streakText;
            m_StreakDotsView = dotsView;
        }

        public void SetStats(int bestScore, int streak, UiThemeSO theme = null)
        {
            m_BestScore = Mathf.Max(0, bestScore);
            m_CurrentStreak = Mathf.Max(0, streak);
            if (theme != null)
            {
                m_Theme = theme;
            }

            Refresh();
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null) return;
            m_Theme = theme;
            m_StreakDotsView?.ApplyTheme(m_Theme);
        }

        public void Refresh()
        {
            if (m_BestCard != null)
            {
                m_BestCard.SetValue(m_BestScore, false);
            }

            if (m_StreakText != null)
            {
                m_StreakText.text = $"{m_CurrentStreak} DAY STREAK";
            }

            if (m_StreakDotsView != null)
            {
                m_StreakDotsView.SetStreak(m_CurrentStreak, m_Theme);
            }
        }
    }
}

using System;
using TMPro;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Displays player performance statistics (Games Played, Best Score, Total Lines, Average Score).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatisticsPopup : UiPopupBase
    {
        [Header("Statistic Fields")]
        [SerializeField] private TMP_Text m_GamesPlayedText;
        [SerializeField] private TMP_Text m_BestScoreText;
        [SerializeField] private TMP_Text m_TotalLinesText;
        [SerializeField] private TMP_Text m_AverageScoreText;

        public void Populate(int gamesPlayed, int bestScore, int totalLines, int avgScore)
        {
            if (m_GamesPlayedText != null) m_GamesPlayedText.text = gamesPlayed.ToString();
            if (m_BestScoreText != null) m_BestScoreText.text = bestScore.ToString("D5");
            if (m_TotalLinesText != null) m_TotalLinesText.text = totalLines.ToString();
            if (m_AverageScoreText != null) m_AverageScoreText.text = avgScore.ToString();
        }
    }
}

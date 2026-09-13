using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>
    /// Game Over popup presenting final score, best score, lines cleared, and revive / new game options.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameOverPopup : PopupView
    {
        [Header("Game Over Fields")]
        [SerializeField] private TMP_Text m_FinalScoreText;
        [SerializeField] private TMP_Text m_BestScoreText;
        [SerializeField] private TMP_Text m_LinesClearedText;
        [SerializeField] private Button m_ContinueButton;
        [SerializeField] private Button m_NewGameButton;

        public Button ContinueButton => m_ContinueButton;
        public Button NewGameButton => m_NewGameButton;

        public void Populate(int finalScore, int bestScore, int linesCleared, bool canContinue)
        {
            if (m_FinalScoreText != null) m_FinalScoreText.text = finalScore.ToString("D5");
            if (m_BestScoreText != null) m_BestScoreText.text = bestScore.ToString("D5");
            if (m_LinesClearedText != null) m_LinesClearedText.text = linesCleared.ToString();

            if (m_ContinueButton != null)
            {
                m_ContinueButton.gameObject.SetActive(canContinue);
            }
        }
    }
}

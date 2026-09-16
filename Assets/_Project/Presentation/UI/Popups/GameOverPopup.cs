using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Line98.Core;

namespace Line98.Presentation
{
    /// <summary>
    /// Game Over popup presenting final score, best score, lines cleared, longest line,
    /// total moves, and revive / new game options per GDD [P1.7].
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameOverPopup : UiPopupBase
    {
        [Header("Game Over Fields")]
        [SerializeField] private TMP_Text m_FinalScoreText;
        [SerializeField] private TMP_Text m_BestScoreText;
        [SerializeField] private TMP_Text m_LinesClearedText;
        [SerializeField] private TMP_Text m_LongestLineText;
        [SerializeField] private TMP_Text m_TotalMovesText;
        [SerializeField] private Button m_ContinueButton;
        [SerializeField] private Button m_NewGameButton;
        [SerializeField] private GameObject m_NewBestBadge;

        public TMP_Text FinalScoreText => m_FinalScoreText;
        public TMP_Text BestScoreText => m_BestScoreText;
        public TMP_Text LinesClearedText => m_LinesClearedText;
        public TMP_Text LongestLineText => m_LongestLineText;
        public TMP_Text TotalMovesText => m_TotalMovesText;

        public Button ContinueButton => m_ContinueButton != null ? m_ContinueButton : PrimaryButton;
        public Button NewGameButton => m_NewGameButton != null ? m_NewGameButton : SecondaryButton;
        public override bool UsesFullLayoutHeight => true;

        protected override void Awake()
        {
            base.Awake();
            if (m_ContinueButton == null && PrimaryButton != null)
            {
                m_ContinueButton = PrimaryButton;
            }
            if (m_NewGameButton == null && SecondaryButton != null)
            {
                m_NewGameButton = SecondaryButton;
            }
        }

        public void Populate(in SessionSummary summary)
        {
            Populate(
                summary.FinalScore,
                summary.BestScore,
                summary.LinesCleared,
                summary.LongestLine,
                summary.TotalMoves,
                summary.CanContinue);
        }

        public void Populate(
            int finalScore,
            int bestScore,
            int linesCleared,
            int longestLine,
            int totalMoves,
            bool canContinue)
        {
            if (m_FinalScoreText != null) m_FinalScoreText.text = finalScore.ToString("N0", CultureInfo.InvariantCulture);
            if (m_BestScoreText != null) m_BestScoreText.text = bestScore.ToString("N0", CultureInfo.InvariantCulture);
            if (m_LinesClearedText != null) m_LinesClearedText.text = linesCleared.ToString();
            if (m_LongestLineText != null) m_LongestLineText.text = longestLine.ToString();
            if (m_TotalMovesText != null) m_TotalMovesText.text = totalMoves.ToString();

            var continueBtn = ContinueButton;
            if (continueBtn != null)
            {
                continueBtn.gameObject.SetActive(canContinue);
            }

            if (m_NewBestBadge != null)
            {
                m_NewBestBadge.SetActive(finalScore > 0 && finalScore >= bestScore);
            }
        }

        public void Populate(int finalScore, int bestScore, int linesCleared, bool canContinue)
        {
            Populate(finalScore, bestScore, linesCleared, 0, 0, canContinue);
        }
    }
}

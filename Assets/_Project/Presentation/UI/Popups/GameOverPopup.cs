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
        [Tooltip("The \"WATCH A SHORT AD\" caption. A sibling of the Continue button, so it has to be hidden alongside it.")]
        [SerializeField] private GameObject m_AdCaption;

        private const string AdCaptionObjectName = "AdCaption";

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
            EnsureAdCaption();
        }

        /// <summary>
        /// Resolves the caption by name when the prefab has not wired it. Runs lazily as well as in
        /// Awake, because a runtime-built hierarchy gets its children after the component exists.
        /// </summary>
        private void EnsureAdCaption()
        {
            if (m_AdCaption != null) return;

            Transform caption = FindDescendant(transform, AdCaptionObjectName);
            if (caption != null) m_AdCaption = caption.gameObject;
        }

        /// <summary>Test and authoring seam for hierarchies built without the prefab.</summary>
        public void BindGameOverFields(Button continueButton, Button newGameButton)
        {
            if (continueButton != null) m_ContinueButton = continueButton;
            if (newGameButton != null) m_NewGameButton = newGameButton;
            EnsureAdCaption();
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name) return child;

                Transform found = FindDescendant(child, name);
                if (found != null) return found;
            }

            return null;
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

            EnsureAdCaption();

            var continueBtn = ContinueButton;
            if (continueBtn != null)
            {
                continueBtn.gameObject.SetActive(canContinue);
            }

            // The caption sits beside the button, not under it, so it needs hiding in its own right
            // or the player is told to "watch a short ad" with nothing to tap.
            if (m_AdCaption != null)
            {
                m_AdCaption.SetActive(canContinue);
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

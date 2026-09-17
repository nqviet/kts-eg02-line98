using System;
using TMPro;
using UnityEngine;
using Line98.Core;
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
        [SerializeField] private UnityEngine.UI.Image[] m_ShowcaseGems = Array.Empty<UnityEngine.UI.Image>();

        [Header("Resume")]
        [Tooltip("PLAY button label; switches to CONTINUE when a Classic game can be resumed. Resolved at runtime when empty.")]
        [SerializeField] private TMP_Text m_PlayLabel;

        [Header("Theme")]
        [SerializeField] private UiThemeSO m_Theme;
        private BallThemeSO m_BallTheme;

        private int m_BestScore;
        private int m_CurrentStreak;
        private bool m_HasResumeOffer;
        private int m_ResumeScore;
        private int m_ResumeMoveCount;
        private string m_PlayLabelDefaultText;
        private bool m_PlayLabelDefaultAutoSize;
        private float m_PlayLabelDefaultFontSize;
        private bool m_HasCapturedPlayLabel;

        public UiValueCard BestCard => m_BestCard;
        public TMP_Text StreakText => m_StreakText;
        public StreakDotsView StreakDotsView => m_StreakDotsView;
        public UnityEngine.UI.Image[] ShowcaseGems => m_ShowcaseGems;
        public BallThemeSO BallTheme => m_BallTheme;
        public int BestScore => m_BestScore;
        public int CurrentStreak => m_CurrentStreak;
        public bool HasResumeOffer => m_HasResumeOffer;
        public TMP_Text PlayLabel => m_PlayLabel;

        public void Configure(UiValueCard bestCard, TMP_Text streakText, StreakDotsView dotsView)
        {
            m_BestCard = bestCard;
            m_StreakText = streakText;
            m_StreakDotsView = dotsView;
        }

        public void ConfigureShowcaseGems(UnityEngine.UI.Image[] gems)
        {
            m_ShowcaseGems = gems ?? Array.Empty<UnityEngine.UI.Image>();
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

        /// <summary>Makes PLAY context-sensitive: it reads CONTINUE with progress while a Classic game is resumable.</summary>
        public void SetResumeOffer(bool available, int score, int moveCount)
        {
            m_HasResumeOffer = available;
            m_ResumeScore = Mathf.Max(0, score);
            m_ResumeMoveCount = Mathf.Max(0, moveCount);
            RefreshPlayLabel();
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null) return;
            m_Theme = theme;
            m_StreakDotsView?.ApplyTheme(m_Theme);
            RefreshShowcaseGems();
        }

        public void ApplyBallTheme(BallThemeSO ballTheme)
        {
            m_BallTheme = ballTheme;
            RefreshShowcaseGems();
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

            RefreshShowcaseGems();
            RefreshPlayLabel();
        }

        private void RefreshPlayLabel()
        {
            if (!TryCapturePlayLabel()) return;

            if (m_HasResumeOffer)
            {
                string moves = m_ResumeMoveCount == 1 ? "MOVE" : "MOVES";
                m_PlayLabel.enableAutoSizing = true;
                m_PlayLabel.fontSizeMin = 24f;
                m_PlayLabel.fontSizeMax = m_PlayLabelDefaultFontSize;
                m_PlayLabel.text = $"CONTINUE\n<size=45%>{m_ResumeScore:N0} PTS - {m_ResumeMoveCount} {moves}</size>";
            }
            else
            {
                m_PlayLabel.enableAutoSizing = m_PlayLabelDefaultAutoSize;
                m_PlayLabel.fontSize = m_PlayLabelDefaultFontSize;
                m_PlayLabel.text = m_PlayLabelDefaultText;
            }
        }

        private bool TryCapturePlayLabel()
        {
            if (m_PlayLabel == null)
            {
                UiNavigationButton[] buttons = GetComponentsInChildren<UiNavigationButton>(true);
                for (int i = 0; i < buttons.Length && m_PlayLabel == null; i++)
                {
                    if (buttons[i].Destination != UiNavigationButton.UiDestination.Game) continue;

                    Transform label = buttons[i].transform.Find("Content/Label");
                    m_PlayLabel = label != null ? label.GetComponent<TMP_Text>() : buttons[i].GetComponentInChildren<TMP_Text>(true);
                }
            }

            if (m_PlayLabel == null) return false;

            if (!m_HasCapturedPlayLabel)
            {
                m_PlayLabelDefaultText = m_PlayLabel.text;
                m_PlayLabelDefaultAutoSize = m_PlayLabel.enableAutoSizing;
                m_PlayLabelDefaultFontSize = m_PlayLabel.fontSize;
                m_HasCapturedPlayLabel = true;
            }

            return true;
        }

        private void RefreshShowcaseGems()
        {
            var spriteSet = m_BallTheme?.PreviewSpriteSet ?? m_Theme?.PreviewSpriteSet;
            if (spriteSet == null || m_ShowcaseGems == null)
            {
                return;
            }

            BallColor[] colors =
            {
                BallColor.Red,
                BallColor.Orange,
                BallColor.Yellow,
                BallColor.Green,
                BallColor.Cyan,
                BallColor.Purple,
                BallColor.Blue
            };

            int count = Mathf.Min(colors.Length, m_ShowcaseGems.Length);
            for (int i = 0; i < count; i++)
            {
                if (m_ShowcaseGems[i] != null)
                {
                    m_ShowcaseGems[i].sprite = spriteSet.GetSprite(colors[i]);
                }
            }
        }
    }
}

using System;
using TMPro;
using Line98.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>
    /// Combined Your Progress surface: the pinned seven statistics and achievement progress.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StatisticsPopup : UiPopupBase
    {
        [Header("Tabs")]
        [SerializeField] private Button m_StatisticsTabButton;
        [SerializeField] private Button m_AchievementsTabButton;
        [SerializeField] private Image m_StatisticsTabSurface;
        [SerializeField] private Image m_AchievementsTabSurface;
        [SerializeField] private RectTransform m_StatisticsSection;
        [SerializeField] private RectTransform m_AchievementsSection;

        [Header("Statistic Fields")]
        [SerializeField] private TMP_Text m_GamesPlayedText;
        [SerializeField] private TMP_Text m_BestScoreText;
        [SerializeField] private TMP_Text m_TotalScoreText;
        [SerializeField] private TMP_Text m_TotalLinesText;
        [SerializeField] private TMP_Text m_LongestLineText;
        [SerializeField] private TMP_Text m_HighestComboText;
        [SerializeField] private TMP_Text m_CurrentStreakText;
        [SerializeField] private Image[] m_HeroBalls = Array.Empty<Image>();

        [Header("Achievements")]
        [SerializeField] private TMP_Text m_AchievementSummaryText;
        [SerializeField] private Image m_AchievementSummaryFill;
        [SerializeField] private RectTransform[] m_AchievementRows = Array.Empty<RectTransform>();
        [SerializeField] private Image[] m_AchievementIcons = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] m_AchievementNames = Array.Empty<TMP_Text>();
        [SerializeField] private UiCheckGraphic[] m_AchievementChecks = Array.Empty<UiCheckGraphic>();
        [SerializeField] private Image[] m_AchievementProgressFills = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] m_AchievementProgressTexts = Array.Empty<TMP_Text>();

        private ProgressPopupPayload m_Payload;
        private UiThemeSO m_Theme;
        private bool m_ShowingAchievements;

        public override bool UsesFullLayoutHeight => true;
        public override bool UsesDimScrim => false;

        public override void ApplyResponsiveLayout(float layoutWidth, float middleHeight)
        {
            if (ModalContainer == null) return;
            const float referenceWidth = 940f;
            const float referenceHeight = 1670f;
            float scale = Mathf.Clamp(Mathf.Min(
                (layoutWidth - 24f) / referenceWidth,
                (middleHeight - 24f) / referenceHeight), 0.45f, 1f);
            ModalContainer.anchorMin = new Vector2(0.5f, 0.5f);
            ModalContainer.anchorMax = new Vector2(0.5f, 0.5f);
            ModalContainer.pivot = new Vector2(0.5f, 0.5f);
            ModalContainer.sizeDelta = new Vector2(referenceWidth, referenceHeight);
            ModalContainer.anchoredPosition = Vector2.zero;
            SetModalRestScale(Vector3.one * scale);
        }

        protected override void Awake()
        {
            base.Awake();
            if (m_StatisticsTabButton != null) m_StatisticsTabButton.onClick.AddListener(ShowStatistics);
            if (m_AchievementsTabButton != null) m_AchievementsTabButton.onClick.AddListener(ShowAchievements);
        }

        protected override void OnDestroy()
        {
            if (m_StatisticsTabButton != null) m_StatisticsTabButton.onClick.RemoveListener(ShowStatistics);
            if (m_AchievementsTabButton != null) m_AchievementsTabButton.onClick.RemoveListener(ShowAchievements);
            base.OnDestroy();
        }

        public void Populate(int gamesPlayed, int bestScore, int totalLines, int avgScore)
        {
            Populate(new ProgressPopupPayload
            {
                GamesPlayed = gamesPlayed,
                BestScore = bestScore,
                TotalLinesCleared = totalLines,
                TotalScore = avgScore
            });
        }

        public void Populate(ProgressPopupPayload payload)
        {
            m_Payload = payload ?? new ProgressPopupPayload();
            if (m_GamesPlayedText != null) m_GamesPlayedText.text = m_Payload.GamesPlayed.ToString("N0");
            if (m_BestScoreText != null) m_BestScoreText.text = m_Payload.BestScore.ToString("N0");
            if (m_TotalScoreText != null) m_TotalScoreText.text = m_Payload.TotalScore.ToString("N0");
            if (m_TotalLinesText != null) m_TotalLinesText.text = m_Payload.TotalLinesCleared.ToString("N0");
            if (m_LongestLineText != null) m_LongestLineText.text = m_Payload.LongestLine.ToString("N0");
            if (m_HighestComboText != null) m_HighestComboText.text = m_Payload.HighestCombo.ToString("N0");
            if (m_CurrentStreakText != null) m_CurrentStreakText.text = $"{m_Payload.CurrentStreak} DAYS";
            if (m_AchievementSummaryText != null) m_AchievementSummaryText.text = $"{m_Payload.AchievementsUnlocked}/{m_Payload.AchievementsTotal}";
            if (m_AchievementSummaryFill != null) m_AchievementSummaryFill.fillAmount = Mathf.Clamp01(m_Payload.AchievementRatio);
            RefreshAchievementRows();
            SetTab(m_ShowingAchievements);
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null) return;
            m_Theme = theme;
            UiThemeApplier[] appliers = GetComponentsInChildren<UiThemeApplier>(true);
            for (int i = 0; i < appliers.Length; i++) appliers[i].Apply(theme);
            ApplyHeroBallTheme();
            SetTab(m_ShowingAchievements);
            RefreshAchievementRows();
        }

        public void ShowStatistics() => SetTab(false);
        public void ShowAchievements() => SetTab(true);

        private void SetTab(bool achievements)
        {
            m_ShowingAchievements = achievements;
            if (m_StatisticsSection != null) m_StatisticsSection.gameObject.SetActive(!achievements);
            if (m_AchievementsSection != null)
            {
                m_AchievementsSection.gameObject.SetActive(achievements);
            }

            Color active = m_Theme != null ? m_Theme.SurfacePrimary : new Color(0.15f, 0.66f, 0.33f, 1f);
            Color inactive = m_Theme != null ? m_Theme.SurfaceSecondary : new Color(0.96f, 0.95f, 0.91f, 1f);
            if (m_StatisticsTabSurface != null) m_StatisticsTabSurface.color = achievements ? inactive : active;
            if (m_AchievementsTabSurface != null) m_AchievementsTabSurface.color = achievements ? active : inactive;
            RefreshAchievementRows();
        }

        private void ApplyHeroBallTheme()
        {
            UiPreviewSpriteSetSO sprites = m_Theme != null ? m_Theme.PreviewSpriteSet : null;
            for (int i = 0; i < m_HeroBalls.Length; i++)
            {
                if (m_HeroBalls[i] == null) continue;
                m_HeroBalls[i].sprite = sprites != null ? sprites.GetSpriteByIndex(i) : null;
                m_HeroBalls[i].preserveAspect = true;
            }
        }

        private void RefreshAchievementRows()
        {
            AchievementPopupRow[] entries = m_Payload?.Achievements ?? Array.Empty<AchievementPopupRow>();
            int visibleLimit = m_ShowingAchievements ? m_AchievementRows.Length : Mathf.Min(3, m_AchievementRows.Length);
            for (int i = 0; i < m_AchievementRows.Length; i++)
            {
                bool visible = i < visibleLimit && i < entries.Length;
                if (m_AchievementRows[i] != null) m_AchievementRows[i].gameObject.SetActive(visible);
                if (!visible) continue;

                AchievementPopupRow row = entries[i];
                if (i < m_AchievementNames.Length && m_AchievementNames[i] != null) m_AchievementNames[i].text = row.DisplayName;
                if (i < m_AchievementIcons.Length && m_AchievementIcons[i] != null)
                {
                    m_AchievementIcons[i].sprite = row.Icon;
                    m_AchievementIcons[i].preserveAspect = true;
                    m_AchievementIcons[i].color = row.Icon != null ? Color.white : m_Theme != null ? m_Theme.Crown : Color.yellow;
                }
                if (i < m_AchievementChecks.Length && m_AchievementChecks[i] != null)
                {
                    m_AchievementChecks[i].gameObject.SetActive(row.Unlocked);
                    m_AchievementChecks[i].color = Color.white;
                }
                if (i < m_AchievementProgressFills.Length && m_AchievementProgressFills[i] != null)
                {
                    m_AchievementProgressFills[i].transform.parent.gameObject.SetActive(!row.Unlocked);
                    m_AchievementProgressFills[i].fillAmount = row.Normalized;
                }
                if (i < m_AchievementProgressTexts.Length && m_AchievementProgressTexts[i] != null)
                {
                    m_AchievementProgressTexts[i].gameObject.SetActive(!row.Unlocked);
                    m_AchievementProgressTexts[i].text = $"{row.Current:N0}/{row.Threshold:N0}";
                }
            }
        }
    }
}

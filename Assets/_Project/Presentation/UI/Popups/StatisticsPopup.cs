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
        private const int k_PreviewRowCount = 3;
        private const float k_RowTop = 189f;
        private const float k_RowPitch = 121f;

        [Header("Tabs")]
        [SerializeField] private Button m_StatisticsTabButton;
        [SerializeField] private Button m_AchievementsTabButton;
        [SerializeField] private Image m_StatisticsTabSurface;
        [SerializeField] private Image m_AchievementsTabSurface;
        [SerializeField] private TMP_Text m_StatisticsTabLabel;
        [SerializeField] private TMP_Text m_AchievementsTabLabel;
        [SerializeField] private Graphic m_StatisticsTabIcon;
        [SerializeField] private Graphic m_AchievementsTabIcon;
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
        [SerializeField] private Image[] m_LineBalls = Array.Empty<Image>();

        [Header("Achievements")]
        [SerializeField] private RectTransform m_AchievementsCard;
        [SerializeField] private float m_PreviewCardTop = -143f;
        [SerializeField] private float m_PreviewCardHeight = 540f;
        [SerializeField] private float m_FullCardTop = 558f;
        [SerializeField] private Button m_ViewAllButton;
        [SerializeField] private TMP_Text m_AchievementSummaryText;
        [SerializeField] private Image m_AchievementSummaryFill;
        [SerializeField] private RectTransform[] m_AchievementRows = Array.Empty<RectTransform>();
        [SerializeField] private Image[] m_AchievementIcons = Array.Empty<Image>();
        [SerializeField] private Graphic[] m_AchievementTrophies = Array.Empty<Graphic>();
        [SerializeField] private GameObject[] m_AchievementLocks = Array.Empty<GameObject>();
        [SerializeField] private TMP_Text[] m_AchievementNames = Array.Empty<TMP_Text>();
        [SerializeField] private Image[] m_AchievementBadges = Array.Empty<Image>();
        [SerializeField] private UiCheckGraphic[] m_AchievementChecks = Array.Empty<UiCheckGraphic>();
        [SerializeField] private Image[] m_AchievementProgressFills = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] m_AchievementProgressTexts = Array.Empty<TMP_Text>();

        [Header("Crystal Styling")]
        [SerializeField] private RawImage m_Backdrop;
        [SerializeField] private Texture2D m_CrystalBackdropTexture;
        [SerializeField] private Image m_BackdropWash;
        [SerializeField] private Color m_CrystalBackdropWash = new Color(0.98f, 0.97f, 0.93f, 0.3f);
        [SerializeField] private TMP_Text m_HeaderTitle;
        [SerializeField] private Image[] m_CardSurfaces = Array.Empty<Image>();
        [SerializeField] private Color m_CrystalCardTint = Color.white;
        [SerializeField] private Sprite m_RowFeaturedSprite;
        [SerializeField] private Sprite m_RowUnlockedSprite;
        [SerializeField] private Sprite m_BadgeLockedSprite;
        [SerializeField] private Color m_BestScoreTop = new Color(1f, 0.85f, 0.32f, 1f);
        [SerializeField] private Color m_BestScoreBottom = new Color(0.9f, 0.52f, 0.08f, 1f);

        private ProgressPopupPayload m_Payload;
        private UiThemeSO m_Theme;
        private bool m_ShowingAchievements;

        public override bool UsesFullLayoutHeight => true;
        public override bool UsesDimScrim => false;

        private bool IsCrystal => m_Theme != null && m_Theme.CardBackgroundSprite != null;

        // The 940x1670 composition is a full page fitted into the safe area.
        protected override Vector2 ReferenceLayoutSize => new Vector2(940f, 1670f);

        protected override void Awake()
        {
            base.Awake();
            if (m_StatisticsTabButton != null) m_StatisticsTabButton.onClick.AddListener(ShowStatistics);
            if (m_AchievementsTabButton != null) m_AchievementsTabButton.onClick.AddListener(ShowAchievements);
            if (m_ViewAllButton != null) m_ViewAllButton.onClick.AddListener(ShowAchievements);
        }

        protected override void OnDestroy()
        {
            if (m_StatisticsTabButton != null) m_StatisticsTabButton.onClick.RemoveListener(ShowStatistics);
            if (m_AchievementsTabButton != null) m_AchievementsTabButton.onClick.RemoveListener(ShowAchievements);
            if (m_ViewAllButton != null) m_ViewAllButton.onClick.RemoveListener(ShowAchievements);
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
            SetBar(m_AchievementSummaryFill, m_Payload.AchievementRatio);
            SetTab(m_ShowingAchievements);
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null) return;
            m_Theme = theme;
            UiThemeApplier[] appliers = GetComponentsInChildren<UiThemeApplier>(true);
            for (int i = 0; i < appliers.Length; i++) appliers[i].Apply(theme);
            ApplyBallSprites();
            ApplySurfaceStyle();
            SetTab(m_ShowingAchievements);
        }

        public void ShowStatistics() => SetTab(false);
        public void ShowAchievements() => SetTab(true);

        private void SetTab(bool achievements)
        {
            m_ShowingAchievements = achievements;
            if (m_StatisticsSection != null) m_StatisticsSection.gameObject.SetActive(!achievements);
            if (m_AchievementsSection != null) m_AchievementsSection.gameObject.SetActive(true);
            if (m_ViewAllButton != null) m_ViewAllButton.gameObject.SetActive(!achievements);

            StyleTab(m_StatisticsTabSurface, m_StatisticsTabLabel, m_StatisticsTabIcon, !achievements);
            StyleTab(m_AchievementsTabSurface, m_AchievementsTabLabel, m_AchievementsTabIcon, achievements);
            LayoutAchievementsCard();
            RefreshAchievementRows();
        }

        private void StyleTab(Image surface, TMP_Text label, Graphic icon, bool active)
        {
            Color navy = m_Theme != null ? m_Theme.BrandNavy : new Color(0.04f, 0.11f, 0.25f, 1f);
            if (surface != null)
            {
                Sprite activeSprite = m_Theme != null ? m_Theme.ButtonCapsulePrimary : null;
                Sprite inactiveSprite = m_Theme != null ? m_Theme.ButtonCapsuleSprite : null;
                surface.sprite = active ? activeSprite : inactiveSprite;
                surface.type = Image.Type.Sliced;
                surface.color = IsCrystal
                    ? Color.white
                    : active
                        ? m_Theme != null ? m_Theme.StreakDotOn : new Color(0.15f, 0.66f, 0.33f, 1f)
                        : m_Theme != null ? m_Theme.SurfaceSecondary : new Color(0.96f, 0.95f, 0.91f, 1f);
            }
            Color ink = active ? Color.white : navy;
            if (label != null) label.color = ink;
            if (icon != null) icon.color = ink;
        }

        private void LayoutAchievementsCard()
        {
            if (m_AchievementsCard == null) return;
            int entryCount = m_Payload?.Achievements?.Length ?? 0;
            int rows = Mathf.Max(1, Mathf.Min(entryCount, m_AchievementRows.Length));
            float top = m_ShowingAchievements ? m_FullCardTop : m_PreviewCardTop;
            float height = m_ShowingAchievements ? k_RowTop + k_RowPitch * (rows - 1) + 86f : m_PreviewCardHeight;
            m_AchievementsCard.anchoredPosition = new Vector2(m_AchievementsCard.anchoredPosition.x, top);
            m_AchievementsCard.sizeDelta = new Vector2(m_AchievementsCard.sizeDelta.x, height);
            for (int i = 0; i < m_AchievementRows.Length; i++)
            {
                if (m_AchievementRows[i] == null) continue;
                m_AchievementRows[i].anchoredPosition = new Vector2(m_AchievementRows[i].anchoredPosition.x, -k_RowTop - k_RowPitch * i);
            }
        }

        private void ApplySurfaceStyle()
        {
            bool crystal = IsCrystal;
            if (m_Backdrop != null)
            {
                m_Backdrop.texture = crystal ? m_CrystalBackdropTexture : null;
                m_Backdrop.color = crystal ? Color.white : m_Theme != null ? m_Theme.LightScrim : Color.clear;
            }
            if (m_BackdropWash != null)
            {
                m_BackdropWash.color = m_CrystalBackdropWash;
                m_BackdropWash.enabled = crystal;
            }

            if (m_HeaderTitle != null)
            {
                m_HeaderTitle.enableVertexGradient = crystal;
                if (crystal)
                {
                    Color top = new Color(0.118f, 0.290f, 0.549f, 1f);
                    m_HeaderTitle.colorGradient = new VertexGradient(top, top, m_Theme.BrandNavy, m_Theme.BrandNavy);
                    m_HeaderTitle.color = Color.white;
                }
            }
            if (m_BestScoreText != null)
            {
                m_BestScoreText.enableVertexGradient = crystal;
                if (crystal)
                {
                    m_BestScoreText.colorGradient = new VertexGradient(m_BestScoreTop, m_BestScoreTop, m_BestScoreBottom, m_BestScoreBottom);
                    m_BestScoreText.color = Color.white;
                }
            }
            if (!crystal) return;

            for (int i = 0; i < m_CardSurfaces.Length; i++)
            {
                if (m_CardSurfaces[i] != null) m_CardSurfaces[i].color = m_CrystalCardTint;
            }
        }

        private void ApplyBallSprites()
        {
            UiPreviewSpriteSetSO sprites = m_Theme != null ? m_Theme.PreviewSpriteSet : null;
            for (int i = 0; i < m_HeroBalls.Length; i++)
            {
                if (m_HeroBalls[i] == null) continue;
                m_HeroBalls[i].sprite = sprites != null ? sprites.GetSpriteByIndex(i) : null;
                m_HeroBalls[i].preserveAspect = true;
            }
            int[] lineIndices = { 4, 3, 6 };
            for (int i = 0; i < m_LineBalls.Length; i++)
            {
                if (m_LineBalls[i] == null) continue;
                m_LineBalls[i].sprite = sprites != null ? sprites.GetSpriteByIndex(lineIndices[Mathf.Min(i, lineIndices.Length - 1)]) : null;
                m_LineBalls[i].preserveAspect = true;
            }
        }

        private void RefreshAchievementRows()
        {
            AchievementPopupRow[] entries = m_Payload?.Achievements ?? Array.Empty<AchievementPopupRow>();
            int visibleLimit = m_ShowingAchievements ? m_AchievementRows.Length : Mathf.Min(k_PreviewRowCount, m_AchievementRows.Length);
            bool crystal = IsCrystal;
            Color unlockedBadge = m_Theme != null ? m_Theme.StreakDotOn : new Color(0.15f, 0.72f, 0.36f, 1f);
            bool featuredUsed = false;

            for (int i = 0; i < m_AchievementRows.Length; i++)
            {
                bool visible = i < visibleLimit && i < entries.Length;
                if (m_AchievementRows[i] != null) m_AchievementRows[i].gameObject.SetActive(visible);
                if (!visible) continue;

                AchievementPopupRow row = entries[i];
                bool featured = row.Unlocked && !featuredUsed;
                if (featured) featuredUsed = true;

                Image surface = m_AchievementRows[i] != null ? m_AchievementRows[i].GetComponent<Image>() : null;
                if (surface != null)
                {
                    if (crystal)
                    {
                        surface.sprite = featured ? m_RowFeaturedSprite : row.Unlocked ? m_RowUnlockedSprite : m_Theme.CardBackgroundSprite;
                        surface.color = Color.white;
                    }
                    else if (m_Theme != null)
                    {
                        surface.sprite = featured ? m_Theme.CardGoldSprite : m_Theme.CardBackgroundSprite;
                        surface.color = featured ? m_Theme.SurfaceCardGold : m_Theme.SurfaceSecondary;
                    }
                }

                if (i < m_AchievementNames.Length && m_AchievementNames[i] != null)
                {
                    TMP_Text nameText = m_AchievementNames[i];
                    nameText.text = row.DisplayName;
                    Vector2 position = nameText.rectTransform.anchoredPosition;
                    nameText.rectTransform.anchoredPosition = new Vector2(position.x, row.Unlocked ? 0f : 17f);
                }

                bool hasIcon = row.Icon != null;
                if (i < m_AchievementIcons.Length && m_AchievementIcons[i] != null)
                {
                    m_AchievementIcons[i].gameObject.SetActive(hasIcon);
                    m_AchievementIcons[i].sprite = row.Icon;
                    m_AchievementIcons[i].preserveAspect = true;
                    m_AchievementIcons[i].color = Color.white;
                }
                if (i < m_AchievementTrophies.Length && m_AchievementTrophies[i] != null)
                {
                    m_AchievementTrophies[i].gameObject.SetActive(!hasIcon && row.Unlocked);
                }
                if (i < m_AchievementLocks.Length && m_AchievementLocks[i] != null)
                {
                    m_AchievementLocks[i].SetActive(!hasIcon && !row.Unlocked);
                }

                if (i < m_AchievementBadges.Length && m_AchievementBadges[i] != null)
                {
                    Image badge = m_AchievementBadges[i];
                    bool greyBadge = !row.Unlocked && crystal && m_BadgeLockedSprite != null;
                    badge.sprite = greyBadge ? m_BadgeLockedSprite : m_Theme != null ? m_Theme.ButtonCircleSprite : badge.sprite;
                    badge.color = row.Unlocked ? unlockedBadge : greyBadge ? Color.white : m_Theme != null ? m_Theme.StreakDotOff : Color.grey;
                }
                if (i < m_AchievementChecks.Length && m_AchievementChecks[i] != null)
                {
                    m_AchievementChecks[i].gameObject.SetActive(row.Unlocked);
                    m_AchievementChecks[i].color = Color.white;
                }
                if (i < m_AchievementProgressFills.Length && m_AchievementProgressFills[i] != null)
                {
                    m_AchievementProgressFills[i].transform.parent.gameObject.SetActive(!row.Unlocked);
                    SetBar(m_AchievementProgressFills[i], row.Normalized);
                }
                if (i < m_AchievementProgressTexts.Length && m_AchievementProgressTexts[i] != null)
                {
                    m_AchievementProgressTexts[i].gameObject.SetActive(!row.Unlocked);
                    m_AchievementProgressTexts[i].text = $"{row.Current:N0}/{row.Threshold:N0}";
                }
            }
        }

        private static void SetBar(Image fill, float normalized)
        {
            if (fill == null) return;
            // Sliced capsule fills keep their rounded caps, so they are sized by anchors rather than fillAmount.
            float value = Mathf.Clamp01(normalized);
            RectTransform rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(value, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            fill.enabled = value > 0f;
        }
    }
}

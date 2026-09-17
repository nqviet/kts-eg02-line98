using System;
using TMPro;
using Line98.Core;
using Line98.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    [DisallowMultipleComponent]
    public sealed class DailyChallengePopup : UiPopupBase
    {
        [Header("Daily Header")]
        [SerializeField] private TMP_Text m_DateText;
        [SerializeField] private TMP_Text m_StreakText;
        [SerializeField] private TMP_Text m_BestText;
        [SerializeField] private TMP_Text m_TodayValueText;
        [SerializeField] private TMP_Text m_TodayStatusText;

        [Header("Week Strip")]
        [SerializeField] private TMP_Text[] m_WeekdayTexts = Array.Empty<TMP_Text>();
        [SerializeField] private Image[] m_DayBadges = Array.Empty<Image>();
        [SerializeField] private UiCheckGraphic[] m_DayChecks = Array.Empty<UiCheckGraphic>();
        [SerializeField] private UiGlyphGraphic[] m_DayGlows = Array.Empty<UiGlyphGraphic>();

        [Header("Board Preview")]
        [SerializeField] private Image[] m_BoardBalls = Array.Empty<Image>();
        [SerializeField] private Image[] m_MissionBalls = Array.Empty<Image>();

        [Header("Actions")]
        [SerializeField] private Button m_PlayButton;
        [SerializeField] private Button m_HowToPlayButton;

        [Header("Crystal Styling")]
        [SerializeField] private RawImage m_Backdrop;
        [SerializeField] private Texture2D m_CrystalBackdropTexture;
        [SerializeField] private Image m_BackdropWash;
        [SerializeField] private Color m_CrystalBackdropWash = new Color(0.98f, 0.97f, 0.93f, 0.3f);
        [SerializeField] private TMP_Text m_HeaderTitle;
        [SerializeField] private Image[] m_CardSurfaces = Array.Empty<Image>();
        [SerializeField] private Image[] m_BoardCells = Array.Empty<Image>();
        [SerializeField] private Color m_CrystalCardTint = Color.white;
        [SerializeField] private Color m_CrystalCellTint = new Color(0.9f, 0.91f, 0.88f, 0.75f);

        private UiThemeSO m_UiTheme;
        private BallThemeSO m_BallTheme;
        private DailyPopupPayload m_Payload;

        public override bool UsesFullLayoutHeight => true;
        public override bool UsesDimScrim => false;
        public event Action OnPlayRequested;
        public event Action OnHowToPlayRequested;

        // The 940x1670 composition is a full page fitted into the safe area.
        protected override Vector2 ReferenceLayoutSize => new Vector2(940f, 1670f);

        protected override void Awake()
        {
            base.Awake();
            if (m_PlayButton != null) m_PlayButton.onClick.AddListener(HandlePlay);
            if (m_HowToPlayButton != null) m_HowToPlayButton.onClick.AddListener(HandleHowToPlay);
        }

        protected override void OnDestroy()
        {
            if (m_PlayButton != null) m_PlayButton.onClick.RemoveListener(HandlePlay);
            if (m_HowToPlayButton != null) m_HowToPlayButton.onClick.RemoveListener(HandleHowToPlay);
            base.OnDestroy();
        }

        public void Populate(DailyPopupPayload payload)
        {
            m_Payload = payload ?? new DailyPopupPayload();
            if (m_DateText != null) m_DateText.text = m_Payload.Date.ToString("MMMM d").ToUpperInvariant();
            if (m_StreakText != null) m_StreakText.text = $"{m_Payload.CurrentStreak} DAY STREAK";
            if (m_BestText != null) m_BestText.text = m_Payload.BestScore.ToString("N0");

            if (m_TodayValueText != null)
            {
                m_TodayValueText.text = m_Payload.PlayedToday ? m_Payload.TodayScore.ToString("N0") : "—";
            }
            if (m_TodayStatusText != null)
            {
                m_TodayStatusText.text = m_Payload.CompletedToday ? "COMPLETED" : m_Payload.PlayedToday ? "PLAYED" : "NOT PLAYED";
            }
            if (m_PlayButton != null) m_PlayButton.interactable = m_Payload.CanPlayToday;

            PopulateWeek();
            PopulateBoard();
            PopulateMissionBalls();
        }

        public void ApplyTheme(UiThemeSO uiTheme, BallThemeSO ballTheme = null)
        {
            if (uiTheme != null) m_UiTheme = uiTheme;
            if (ballTheme != null) m_BallTheme = ballTheme;

            UiThemeApplier[] appliers = GetComponentsInChildren<UiThemeApplier>(true);
            for (int i = 0; i < appliers.Length; i++) appliers[i].Apply(m_UiTheme);
            ApplySurfaceStyle();

            PopulateWeek();
            PopulateBoard();
            PopulateMissionBalls();
        }

        private void ApplySurfaceStyle()
        {
            bool crystal = m_UiTheme != null && m_UiTheme.CardBackgroundSprite != null;

            if (m_Backdrop != null)
            {
                m_Backdrop.texture = crystal ? m_CrystalBackdropTexture : null;
                m_Backdrop.color = crystal ? Color.white : m_UiTheme != null ? m_UiTheme.LightScrim : Color.clear;
            }

            if (m_BackdropWash != null)
            {
                m_BackdropWash.color = m_CrystalBackdropWash;
                m_BackdropWash.enabled = crystal;
            }

            if (!crystal)
            {
                if (m_HeaderTitle != null) m_HeaderTitle.enableVertexGradient = false;
                return;
            }

            for (int i = 0; i < m_CardSurfaces.Length; i++)
            {
                if (m_CardSurfaces[i] != null) m_CardSurfaces[i].color = m_CrystalCardTint;
            }
            for (int i = 0; i < m_BoardCells.Length; i++)
            {
                if (m_BoardCells[i] != null) m_BoardCells[i].color = m_CrystalCellTint;
            }
            if (m_HeaderTitle != null)
            {
                Color top = new Color(0.118f, 0.290f, 0.549f, 1f);
                m_HeaderTitle.enableVertexGradient = true;
                m_HeaderTitle.colorGradient = new VertexGradient(top, top, m_UiTheme.BrandNavy, m_UiTheme.BrandNavy);
                m_HeaderTitle.color = Color.white;
            }
        }

        private void PopulateWeek()
        {
            DailyDayVisual[] week = m_Payload?.Week ?? Array.Empty<DailyDayVisual>();
            int count = Mathf.Min(7, Mathf.Min(m_DayBadges.Length, m_WeekdayTexts.Length));
            for (int i = 0; i < count; i++)
            {
                DailyDayVisual cell = i < week.Length ? week[i] : default;
                if (m_WeekdayTexts[i] != null)
                {
                    m_WeekdayTexts[i].text = cell.Date == default ? "—" : cell.Date.ToString("ddd").Substring(0, 1).ToUpperInvariant();
                }

                bool complete = cell.State == DailyDayVisualState.Completed || cell.State == DailyDayVisualState.TodayCompleted;
                Color fill = m_UiTheme != null
                    ? complete ? m_UiTheme.StreakDotOn : m_UiTheme.StreakDotOff
                    : complete ? new Color(0.15f, 0.72f, 0.36f, 1f) : new Color(0.81f, 0.84f, 0.86f, 1f);
                if (cell.State == DailyDayVisualState.TodayCompleted && m_UiTheme != null) fill = m_UiTheme.ButtonGold;
                if (m_DayBadges[i] != null) m_DayBadges[i].color = fill;
                if (i < m_DayChecks.Length && m_DayChecks[i] != null)
                {
                    m_DayChecks[i].gameObject.SetActive(complete);
                    m_DayChecks[i].color = Color.white;
                }
                if (i < m_DayGlows.Length && m_DayGlows[i] != null)
                {
                    m_DayGlows[i].gameObject.SetActive(cell.IsToday && complete);
                }
            }
        }

        private void PopulateBoard()
        {
            BallColor[] board = m_Payload?.Board ?? Array.Empty<BallColor>();
            UiPreviewSpriteSetSO sprites = m_BallTheme != null ? m_BallTheme.PreviewSpriteSet : m_UiTheme != null ? m_UiTheme.PreviewSpriteSet : null;
            for (int i = 0; i < m_BoardBalls.Length; i++)
            {
                Image image = m_BoardBalls[i];
                if (image == null) continue;
                BallColor color = i < board.Length ? board[i] : BallColor.None;
                bool occupied = color != BallColor.None;
                image.gameObject.SetActive(occupied);
                if (!occupied) continue;

                Sprite sprite = sprites != null ? sprites.GetSprite(color) : null;
                image.sprite = sprite;
                image.preserveAspect = true;
                image.color = sprite != null ? Color.white : ResolveBallColor(color);
            }
        }

        private void PopulateMissionBalls()
        {
            UiPreviewSpriteSetSO sprites = m_BallTheme != null ? m_BallTheme.PreviewSpriteSet : m_UiTheme != null ? m_UiTheme.PreviewSpriteSet : null;
            int[] spriteIndices = { 4, 3, 5 };
            for (int i = 0; i < m_MissionBalls.Length; i++)
            {
                Image image = m_MissionBalls[i];
                if (image == null) continue;
                image.sprite = sprites != null ? sprites.GetSpriteByIndex(spriteIndices[Mathf.Min(i, spriteIndices.Length - 1)]) : null;
                image.preserveAspect = true;
            }
        }

        private Color ResolveBallColor(BallColor color)
        {
            if (m_UiTheme == null) return Color.white;
            return color switch
            {
                BallColor.Red => m_UiTheme.BallRed,
                BallColor.Orange => m_UiTheme.BallOrange,
                BallColor.Yellow => m_UiTheme.BallYellow,
                BallColor.Green => m_UiTheme.BallGreen,
                BallColor.Cyan => m_UiTheme.BallCyan,
                BallColor.Purple => m_UiTheme.BallPurple,
                BallColor.Blue => m_UiTheme.BallBlue,
                _ => Color.clear
            };
        }

        private void HandlePlay()
        {
            if (m_Payload != null && m_Payload.CanPlayToday) OnPlayRequested?.Invoke();
        }

        private void HandleHowToPlay() => OnHowToPlayRequested?.Invoke();
    }
}

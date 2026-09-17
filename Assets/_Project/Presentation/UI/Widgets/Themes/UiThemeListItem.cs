using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Line98.Data;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Item card in the theme list:
    /// - Displays theme title and status badge (DEFAULT / SELECT)
    /// - 4 thumbnail swatches (3 ball sprites + 1 effect glyph)
    /// - Active green border when previewed
    /// - Tapping card invokes OnCardClicked (preview only)
    /// - Tapping SELECT invokes OnSelectClicked (applies theme)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiThemeListItem : MonoBehaviour
    {
        [Header("Card Controls")]
        [SerializeField] private Button m_CardButton;
        [SerializeField] private Image m_ActiveBorder;
        [SerializeField] private TMP_Text m_ItemNameLabel;

        [Header("Status Badge / Button")]
        [SerializeField] private Button m_StatusButton;
        [SerializeField] private Image m_StatusBadgeBg;
        [SerializeField] private Outline m_StatusBadgeOutline;
        [SerializeField] private GameObject m_StatusCheckIcon;
        [SerializeField] private TMP_Text m_StatusLabel;
        [SerializeField] private Sprite m_AppliedBadgeSprite;
        [SerializeField] private Sprite m_SelectBadgeSprite;

        [Header("Swatches")]
        [SerializeField] private RectTransform m_ThumbRow;
        [SerializeField] private Image[] m_SwatchSlots = new Image[4];

        [Header("Board Swatch")]
        [SerializeField] private RectTransform m_BoardSwatch;
        [SerializeField] private Image m_BoardSwatchPlate;
        [SerializeField] private Image[] m_BoardSwatchCells = new Image[9];
        [SerializeField] private TMP_Text m_ItemSubLabel;

        [Header("Styling")]
        [SerializeField] private Color m_DefaultBgColor = Color.white;
        [SerializeField] private Color m_DefaultTextColor = Color.white;
        [SerializeField] private Color m_SelectBgColor = Color.white;
        [SerializeField] private Color m_SelectTextColor = new Color(0.075f, 0.157f, 0.31f, 1f);  // Navy #13284F
        [SerializeField] private Color m_SelectBorderColor = new Color(0.353f, 0.651f, 0.863f, 1f); // Sky #5AA6DC

        private string m_PartId;
        private ThemeCategory m_Category = ThemeCategory.Ball;
        private bool m_IsActive;
        private bool m_IsApplied;
        private TweenRunner m_TweenRunner;

        public event Action<string> OnCardClicked;
        public event Action<string> OnSelectClicked;

        public string PartId => m_PartId;
        public ThemeCategory Category => m_Category;
        public bool IsActive => m_IsActive;
        public bool IsApplied => m_IsApplied;
        public bool IsBorderVisible => m_ActiveBorder != null && m_ActiveBorder.enabled;
        public Button CardButton => m_CardButton;
        public Button StatusButton => m_StatusButton;
        public TMP_Text StatusLabel => m_StatusLabel;
        public RectTransform ThumbRow => m_ThumbRow;
        public RectTransform BoardSwatch => m_BoardSwatch;
        public Image BoardSwatchPlate => m_BoardSwatchPlate;
        public Image[] BoardSwatchCells => m_BoardSwatchCells;
        public TMP_Text ItemSubLabel => m_ItemSubLabel;

        private void Awake()
        {
            EnsureControls();
        }

        private void OnDestroy()
        {
            if (m_CardButton != null)
            {
                m_CardButton.onClick.RemoveListener(HandleCardClicked);
            }

            if (m_StatusButton != null)
            {
                m_StatusButton.onClick.RemoveListener(HandleStatusClicked);
            }
        }

        private void EnsureControls()
        {
            if (m_CardButton == null)
            {
                m_CardButton = GetComponent<Button>();
            }

            if (m_StatusButton == null)
            {
                var badge = transform.Find("StatusBadge");
                if (badge != null)
                {
                    m_StatusButton = badge.GetComponent<Button>();
                }
            }

            if (m_StatusLabel == null)
            {
                var badge = transform.Find("StatusBadge");
                if (badge != null)
                {
                    m_StatusLabel = badge.GetComponentInChildren<TMP_Text>(true);
                }
            }

            if (m_ActiveBorder == null)
            {
                var border = transform.Find("ActiveBorder");
                if (border != null)
                {
                    m_ActiveBorder = border.GetComponent<Image>();
                }
            }

            if (m_ThumbRow == null)
            {
                var thumb = transform.Find("ThumbRow");
                if (thumb != null) m_ThumbRow = thumb as RectTransform;
            }

            if (m_BoardSwatch == null)
            {
                var swatch = transform.Find("BoardSwatch");
                if (swatch != null) m_BoardSwatch = swatch as RectTransform;
            }

            if (m_BoardSwatch != null)
            {
                if (m_BoardSwatchPlate == null)
                {
                    var plate = m_BoardSwatch.Find("Plate");
                    if (plate != null) m_BoardSwatchPlate = plate.GetComponent<Image>();
                }

                if (m_BoardSwatchCells == null || m_BoardSwatchCells.Length == 0)
                {
                    var plate = m_BoardSwatch.Find("Plate");
                    var cellsRoot = plate != null ? plate.Find("Cells") : m_BoardSwatch.Find("Cells");
                    if (cellsRoot != null)
                    {
                        m_BoardSwatchCells = cellsRoot.GetComponentsInChildren<Image>(true);
                    }
                }
            }

            if (m_ItemSubLabel == null)
            {
                var sub = transform.Find("Label_ItemSub");
                if (sub != null) m_ItemSubLabel = sub.GetComponent<TMP_Text>();
            }

            if (m_CardButton != null)
            {
                m_CardButton.onClick.RemoveListener(HandleCardClicked);
                m_CardButton.onClick.AddListener(HandleCardClicked);
            }

            if (m_StatusButton != null)
            {
                m_StatusButton.onClick.RemoveListener(HandleStatusClicked);
                m_StatusButton.onClick.AddListener(HandleStatusClicked);
            }
        }

        private UiThemeSO m_CurrentTheme;

        public void Initialize(TweenRunner tweenRunner)
        {
            m_TweenRunner = tweenRunner;
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null) return;
            m_CurrentTheme = theme;

            m_AppliedBadgeSprite = theme.ButtonCapsulePrimary;
            m_SelectBadgeSprite = theme.ButtonCapsuleSprite;

            if (m_AppliedBadgeSprite != null)
            {
                m_DefaultBgColor = Color.white;
                m_DefaultTextColor = Color.white;
            }
            else
            {
                m_DefaultBgColor = theme.SelectedBadgeFill;
                m_DefaultTextColor = theme.SelectedBadgeInk;
            }

            m_SelectBgColor = m_SelectBadgeSprite != null ? Color.white : theme.PanelButton;
            m_SelectTextColor = theme.BrandNavy;
            m_SelectBorderColor = theme.ActiveCardBorder;

            if (m_ActiveBorder != null)
            {
                m_ActiveBorder.color = theme.ActiveCardBorder;
                m_ActiveBorder.sprite = theme.CardBackgroundSprite;
                m_ActiveBorder.material = theme.CardBackgroundSprite == null ? theme.SurfaceMaterialCard : null;
            }

            if (m_ItemNameLabel != null)
            {
                m_ItemNameLabel.color = theme.BrandNavy;
            }

            if (m_ItemSubLabel != null)
            {
                m_ItemSubLabel.color = theme.BrandNavy;
            }

            var appliers = GetComponentsInChildren<UiThemeApplier>(true);
            for (int i = 0; i < appliers.Length; i++)
            {
                appliers[i].Apply(theme);
            }

            SetIsApplied(m_IsApplied, punch: false);
        }

        public void Bind(
            ThemeItemModel model,
            bool isActive,
            bool isApplied,
            TweenRunner tweenRunner = null)
        {
            ThemeCategory inferredCat = (model.BoardMaterials != null && model.BoardMaterials.Length > 0)
                ? ThemeCategory.Board
                : ThemeCategory.Ball;
            Bind(model, inferredCat, isActive, isApplied, tweenRunner);
        }

        public void Bind(
            ThemeItemModel model,
            ThemeCategory category,
            bool isActive,
            bool isApplied,
            TweenRunner tweenRunner = null)
        {
            EnsureControls();
            if (tweenRunner != null) m_TweenRunner = tweenRunner;
            m_PartId = model.PartId;
            m_Category = category;

            if (m_ItemNameLabel != null)
            {
                m_ItemNameLabel.text = !string.IsNullOrEmpty(model.DisplayName)
                    ? model.DisplayName.ToUpperInvariant()
                    : m_PartId.ToUpperInvariant();
            }

            bool isBoard = category == ThemeCategory.Board;
            if (m_ThumbRow != null) m_ThumbRow.gameObject.SetActive(!isBoard);
            if (m_BoardSwatch != null) m_BoardSwatch.gameObject.SetActive(isBoard);
            if (m_ItemSubLabel != null)
            {
                m_ItemSubLabel.gameObject.SetActive(isBoard);
                m_ItemSubLabel.text = "UI follows this style";
            }

            if (isBoard)
            {
                Material frameMat = (model.BoardMaterials != null && model.BoardMaterials.Length > 0)
                    ? model.BoardMaterials[0]
                    : model.Bundle?.BoardTheme?.BoardFrameMaterial;

                Material cellMat = (model.BoardMaterials != null && model.BoardMaterials.Length > 1)
                    ? model.BoardMaterials[1]
                    : model.Bundle?.BoardTheme?.BoardCellMaterial;

                if (m_BoardSwatchPlate != null)
                {
                    m_BoardSwatchPlate.material = frameMat;
                    m_BoardSwatchPlate.color = Color.white;
                }

                if (m_BoardSwatchCells != null)
                {
                    for (int i = 0; i < m_BoardSwatchCells.Length; i++)
                    {
                        var cellImg = m_BoardSwatchCells[i];
                        if (cellImg != null)
                        {
                            cellImg.material = cellMat;
                            cellImg.color = Color.white;
                        }
                    }
                }
            }
            else
            {
                // Bind 3 swatches
                for (int i = 0; i < 3 && i < m_SwatchSlots.Length; i++)
                {
                    var slot = m_SwatchSlots[i];
                    if (slot == null) continue;

                    Sprite sp = (model.Swatches != null && i < model.Swatches.Length) ? model.Swatches[i] : null;
                    slot.sprite = sp;
                    slot.enabled = sp != null;
                    slot.preserveAspect = true;
                }

                // Bind 4th slot (effect glyph)
                if (m_SwatchSlots.Length > 3 && m_SwatchSlots[3] != null)
                {
                    m_SwatchSlots[3].sprite = model.EffectGlyph;
                    m_SwatchSlots[3].enabled = model.EffectGlyph != null;
                    m_SwatchSlots[3].preserveAspect = true;
                }
            }

            SetIsActive(isActive);
            SetIsApplied(isApplied, punch: false);
        }

        public void SetIsActive(bool isActive)
        {
            m_IsActive = isActive;
            RefreshActiveBorder();
        }

        public void SetIsApplied(bool isApplied, bool punch = false)
        {
            m_IsApplied = isApplied;
            RefreshActiveBorder();

            if (m_StatusCheckIcon != null)
            {
                m_StatusCheckIcon.SetActive(isApplied);
            }

            if (m_StatusBadgeBg != null)
            {
                Sprite badgeSprite = isApplied ? m_AppliedBadgeSprite : m_SelectBadgeSprite;
                m_StatusBadgeBg.sprite = badgeSprite;
                m_StatusBadgeBg.material = badgeSprite == null ? (m_CurrentTheme != null ? m_CurrentTheme.SurfaceMaterialButton : null) : null;
                m_StatusBadgeBg.color = isApplied ? m_DefaultBgColor : m_SelectBgColor;
            }

            if (m_StatusBadgeOutline != null)
            {
                m_StatusBadgeOutline.enabled = !isApplied;
                m_StatusBadgeOutline.effectColor = m_SelectBorderColor;
            }

            if (m_StatusLabel != null)
            {
                string appliedLabel = m_Category == ThemeCategory.Board ? "IN USE" : "DEFAULT";
                m_StatusLabel.text = isApplied ? appliedLabel : "SELECT";
                m_StatusLabel.color = isApplied ? m_DefaultTextColor : m_SelectTextColor;
            }

            if (punch && m_TweenRunner != null && m_StatusButton != null)
            {
                PunchStatusPill();
            }
        }

        /// <summary>
        /// The rim marks the focused (previewed) card only; applied state is
        /// conveyed by the DEFAULT badge, so it must not keep a card outlined.
        /// </summary>
        private void RefreshActiveBorder()
        {
            if (m_ActiveBorder != null)
            {
                m_ActiveBorder.enabled = m_IsActive;
            }
        }

        private void HandleCardClicked()
        {
            if (string.IsNullOrEmpty(m_PartId)) return;
            OnCardClicked?.Invoke(m_PartId);
        }

        private void HandleStatusClicked()
        {
            if (string.IsNullOrEmpty(m_PartId)) return;
            OnSelectClicked?.Invoke(m_PartId);
        }

        private void PunchStatusPill()
        {
            var rt = m_StatusButton.transform as RectTransform;
            if (rt == null) return;

            m_TweenRunner.CancelByOwner(rt);
            rt.localScale = Vector3.one;

            Tween punchIn = new Tween
            {
                From = 1.0f,
                To = 1.08f,
                Duration = 0.12f,
                Ease = Easing.OutBack,
                Owner = rt,
                OnUpdate = val =>
                {
                    if (rt != null) rt.localScale = new Vector3(val, val, 1f);
                },
                OnComplete = () =>
                {
                    if (rt == null) return;
                    Tween punchOut = new Tween
                    {
                        From = 1.08f,
                        To = 1.0f,
                        Duration = 0.14f,
                        Ease = Easing.OutCubic,
                        Owner = rt,
                        OnUpdate = v =>
                        {
                            if (rt != null) rt.localScale = new Vector3(v, v, 1f);
                        }
                    };
                    m_TweenRunner.Play(in punchOut);
                }
            };
            m_TweenRunner.Play(in punchIn);
        }
    }
}

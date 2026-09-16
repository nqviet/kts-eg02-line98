using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField] private Image[] m_SwatchSlots = new Image[4];

        [Header("Styling")]
        [SerializeField] private Color m_DefaultBgColor = Color.white;
        [SerializeField] private Color m_DefaultTextColor = Color.white;
        [SerializeField] private Color m_SelectBgColor = Color.white;
        [SerializeField] private Color m_SelectTextColor = new Color(0.075f, 0.157f, 0.31f, 1f);  // Navy #13284F
        [SerializeField] private Color m_SelectBorderColor = new Color(0.353f, 0.651f, 0.863f, 1f); // Sky #5AA6DC

        private string m_PartId;
        private bool m_IsActive;
        private bool m_IsApplied;
        private TweenRunner m_TweenRunner;

        public event Action<string> OnCardClicked;
        public event Action<string> OnSelectClicked;

        public string PartId => m_PartId;
        public bool IsActive => m_IsActive;
        public bool IsApplied => m_IsApplied;
        public Button CardButton => m_CardButton;
        public Button StatusButton => m_StatusButton;

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

        public void Initialize(TweenRunner tweenRunner)
        {
            m_TweenRunner = tweenRunner;
        }

        public void Bind(
            ThemeItemModel model,
            bool isActive,
            bool isApplied,
            TweenRunner tweenRunner = null)
        {
            EnsureControls();
            if (tweenRunner != null) m_TweenRunner = tweenRunner;
            m_PartId = model.PartId;

            if (m_ItemNameLabel != null)
            {
                m_ItemNameLabel.text = !string.IsNullOrEmpty(model.DisplayName)
                    ? model.DisplayName.ToUpperInvariant()
                    : m_PartId.ToUpperInvariant();
            }

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

            SetIsActive(isActive);
            SetIsApplied(isApplied, punch: false);
        }

        public void SetIsActive(bool isActive)
        {
            m_IsActive = isActive;
            if (m_ActiveBorder != null)
            {
                m_ActiveBorder.enabled = m_IsActive || m_IsApplied;
            }
        }

        public void SetIsApplied(bool isApplied, bool punch = false)
        {
            m_IsApplied = isApplied;

            if (m_ActiveBorder != null)
            {
                m_ActiveBorder.enabled = m_IsActive || m_IsApplied;
            }

            if (m_StatusCheckIcon != null)
            {
                m_StatusCheckIcon.SetActive(isApplied);
            }

            if (m_StatusBadgeBg != null)
            {
                Sprite badgeSprite = isApplied ? m_AppliedBadgeSprite : m_SelectBadgeSprite;
                if (badgeSprite != null)
                {
                    m_StatusBadgeBg.sprite = badgeSprite;
                }
                m_StatusBadgeBg.color = isApplied ? m_DefaultBgColor : m_SelectBgColor;
            }

            if (m_StatusBadgeOutline != null)
            {
                m_StatusBadgeOutline.enabled = !isApplied;
                m_StatusBadgeOutline.effectColor = m_SelectBorderColor;
            }

            if (m_StatusLabel != null)
            {
                m_StatusLabel.text = isApplied ? "DEFAULT" : "SELECT";
                m_StatusLabel.color = isApplied ? m_DefaultTextColor : m_SelectTextColor;
            }

            if (punch && m_TweenRunner != null && m_StatusButton != null)
            {
                PunchStatusPill();
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

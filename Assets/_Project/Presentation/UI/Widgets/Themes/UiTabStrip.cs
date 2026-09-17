using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Line98.Data;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Tab strip for the theme selection popup matching themes_selection_UI_crystal.png.
    /// Provides 3 tabs: BALLS, BOARD, EFFECTS with active pill highlight styling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiTabStrip : MonoBehaviour
    {
        [System.Serializable]
        public struct TabSegment
        {
            public ThemeCategory Category;
            public Button Button;
            public Image BackgroundPill;
            public TMP_Text Label;
            public Graphic Icon;
        }

        [Header("Segments")]
        [SerializeField] private TabSegment[] m_Segments = Array.Empty<TabSegment>();

        [Header("Colors")]
        [SerializeField] private Color m_ActivePillColor = Color.white; // sp_btn_capsule_green is already green
        [SerializeField] private Color m_InactivePillColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color m_ActiveTextColor = Color.white;
        [SerializeField] private Color m_InactiveTextColor = new Color(0f, 0.063f, 0.251f, 1f); // #001040
        [SerializeField] private Color m_ActiveIconColor = Color.white;
        [SerializeField] private Color m_InactiveIconColor = new Color(0f, 0.063f, 0.251f, 1f);

        private ThemeCategory m_ActiveCategory = ThemeCategory.Ball;
        private TweenRunner m_TweenRunner;

        public event Action<ThemeCategory> OnTabSelected;
        public ThemeCategory ActiveCategory => m_ActiveCategory;

        private void Awake()
        {
            for (int i = 0; i < m_Segments.Length; i++)
            {
                var segment = m_Segments[i];
                if (segment.Button != null)
                {
                    var cat = segment.Category;
                    segment.Button.onClick.AddListener(() => HandleTabClicked(cat));
                }
            }
        }

        public void Initialize(TweenRunner tweenRunner)
        {
            m_TweenRunner = tweenRunner;
            RefreshVisuals(animate: false);
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null) return;

            m_ActivePillColor = theme.ButtonCapsulePrimary != null ? Color.white : theme.SurfacePrimary;
            m_InactivePillColor = Color.clear;
            m_ActiveTextColor = theme.InkButtonPrimary;
            m_InactiveTextColor = theme.BrandNavy;
            m_ActiveIconColor = theme.InkButtonPrimary;
            m_InactiveIconColor = theme.BrandNavy;

            for (int i = 0; i < m_Segments.Length; i++)
            {
                var seg = m_Segments[i];
                if (seg.BackgroundPill != null)
                {
                    seg.BackgroundPill.sprite = theme.ButtonCapsulePrimary;
                    seg.BackgroundPill.material = theme.ButtonCapsulePrimary == null ? theme.SurfaceMaterialButton : null;
                }
            }

            RefreshVisuals(animate: false);
        }

        public void SetActive(ThemeCategory category, bool notify = false)
        {
            if (m_ActiveCategory == category && notify == false)
            {
                return;
            }

            m_ActiveCategory = category;
            RefreshVisuals(animate: true);

            if (notify)
            {
                OnTabSelected?.Invoke(m_ActiveCategory);
            }
        }

        private void HandleTabClicked(ThemeCategory category)
        {
            if (m_ActiveCategory == category) return;
            SetActive(category, notify: true);
        }

        private void RefreshVisuals(bool animate)
        {
            for (int i = 0; i < m_Segments.Length; i++)
            {
                var seg = m_Segments[i];
                bool isActive = seg.Category == m_ActiveCategory;

                Color targetPill = isActive ? m_ActivePillColor : m_InactivePillColor;
                Color targetText = isActive ? m_ActiveTextColor : m_InactiveTextColor;
                Color targetIcon = isActive ? m_ActiveIconColor : m_InactiveIconColor;

                if (seg.BackgroundPill != null)
                {
                    seg.BackgroundPill.color = targetPill;
                }

                if (seg.Label != null)
                {
                    seg.Label.color = targetText;
                }

                if (seg.Icon != null)
                {
                    seg.Icon.color = targetIcon;
                }
            }
        }
    }
}

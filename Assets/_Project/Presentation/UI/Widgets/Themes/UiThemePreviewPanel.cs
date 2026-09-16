using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Line98.Data;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Preview card displaying the selected/active theme bundle:
    /// - Uppercased theme name
    /// - 5x5 recessed cell board mock
    /// - 7 diamond-flower ball slots with center warm glow and sparkle accents
    /// - SELECTED badge
    /// - Outward 22ms staggered pop animation on theme preview swap
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiThemePreviewPanel : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text m_ThemeNameLabel;

        [Header("Board Mock & Slots")]
        [SerializeField] private GameObject m_BoardMockRoot;
        [SerializeField] private UiPreviewSlot[] m_Slots = new UiPreviewSlot[7];
        [SerializeField] private Graphic m_PreviewGlow;
        [SerializeField] private GameObject m_SelectedBadge;

        [Header("Empty State")]
        [SerializeField] private GameObject m_EmptyStateRoot;
        [SerializeField] private TMP_Text m_EmptyStateLabel;

        private TweenRunner m_TweenRunner;

        public TMP_Text ThemeNameLabel => m_ThemeNameLabel;
        public string ThemeName => m_ThemeNameLabel != null ? m_ThemeNameLabel.text : string.Empty;
        public GameObject SelectedBadge => m_SelectedBadge;
        public UiPreviewSlot[] Slots => m_Slots;
        public GameObject BoardMockRoot => m_BoardMockRoot;
        public GameObject EmptyStateRoot => m_EmptyStateRoot;

        public void Initialize(TweenRunner tweenRunner)
        {
            m_TweenRunner = tweenRunner;
        }

        public void Show(
            string themeName,
            Sprite[] sprites,
            bool isApplied,
            bool animate = true)
        {
            if (m_BoardMockRoot != null) m_BoardMockRoot.SetActive(true);
            if (m_EmptyStateRoot != null) m_EmptyStateRoot.SetActive(false);

            if (m_ThemeNameLabel != null)
            {
                m_ThemeNameLabel.text = !string.IsNullOrEmpty(themeName) ? themeName.ToUpperInvariant() : string.Empty;
            }

            if (m_SelectedBadge != null)
            {
                m_SelectedBadge.SetActive(isApplied);
            }

            if (m_PreviewGlow != null)
            {
                m_PreviewGlow.enabled = true;
            }

            // Populate slots
            for (int i = 0; i < m_Slots.Length; i++)
            {
                var slot = m_Slots[i];
                if (slot == null) continue;

                Sprite sp = (sprites != null && i < sprites.Length) ? sprites[i] : null;
                slot.SetSprite(sp, 1f);

                if (animate && m_TweenRunner != null && sp != null)
                {
                    AnimateSlot(slot, i);
                }
            }
        }

        public void ShowEmpty(ThemeCategory category)
        {
            if (m_BoardMockRoot != null) m_BoardMockRoot.SetActive(false);
            if (m_EmptyStateRoot != null) m_EmptyStateRoot.SetActive(true);

            if (m_ThemeNameLabel != null)
            {
                m_ThemeNameLabel.text = category switch
                {
                    ThemeCategory.Board => "BOARD THEMES",
                    ThemeCategory.ClearEffect => "EFFECT THEMES",
                    _ => "THEMES"
                };
            }

            if (m_EmptyStateLabel != null)
            {
                m_EmptyStateLabel.text = "Coming soon";
            }

            if (m_SelectedBadge != null)
            {
                m_SelectedBadge.SetActive(false);
            }
        }

        private void AnimateSlot(UiPreviewSlot slot, int slotIndex)
        {
            // Center is slot 3 -> ring 0 (0 ms delay)
            // Slots 1, 2, 4, 5 -> ring 1 (22 ms delay)
            // Slots 0, 6 -> ring 2 (44 ms delay)
            float delaySeconds = slotIndex switch
            {
                3 => 0.0f,
                1 or 2 or 4 or 5 => 0.022f,
                _ => 0.044f
            };

            var rt = slot.transform as RectTransform;
            if (rt == null) return;

            m_TweenRunner.CancelByOwner(rt);
            rt.localScale = new Vector3(0.75f, 0.75f, 1f);

            Tween tween = new Tween
            {
                From = 0.75f,
                To = 1.0f,
                Duration = 0.22f,
                Ease = Easing.OutBack,
                Owner = rt,
                OnUpdate = val =>
                {
                    if (rt != null) rt.localScale = new Vector3(val, val, 1f);
                }
            };

            if (delaySeconds > 0.001f)
            {
                // Delayed play: simple small delay
                Tween delayTween = new Tween
                {
                    From = 0f,
                    To = 1f,
                    Duration = delaySeconds,
                    Owner = rt,
                    OnComplete = () =>
                    {
                        if (rt != null) m_TweenRunner.Play(in tween);
                    }
                };
                m_TweenRunner.Play(in delayTween);
            }
            else
            {
                m_TweenRunner.Play(in tween);
            }
        }
    }
}

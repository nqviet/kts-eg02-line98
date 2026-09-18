using TMPro;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Reusable setting row with optional subtitle, toggle, or action button.</summary>
    [DisallowMultipleComponent]
    public sealed class UiSettingRow : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image m_IconImage;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_Text m_SubtitleText;
        [SerializeField] private UiToggle m_Toggle;
        [SerializeField] private UnityEngine.UI.Button m_ActionButton;
        [SerializeField] private UnityEngine.UI.Image m_ActionIcon;

        public UiToggle Toggle => m_Toggle;
        public UnityEngine.UI.Button ActionButton => m_ActionButton;
        public UnityEngine.UI.Image ActionIcon => m_ActionIcon;

        /// <summary>Test and authoring seam for rows built without the prefab.</summary>
        public void BindActionButton(UnityEngine.UI.Button actionButton)
        {
            m_ActionButton = actionButton;
        }

        public void Configure(Sprite icon, string title, string subtitle = null)
        {
            if (m_IconImage != null && icon != null) m_IconImage.sprite = icon;
            if (m_TitleText != null) m_TitleText.text = title ?? string.Empty;
            SetSubtitle(subtitle);
        }

        /// <summary>Replaces the subtitle without disturbing the authored icon or title.</summary>
        public void SetSubtitle(string subtitle)
        {
            if (m_SubtitleText == null) return;

            bool hasSubtitle = !string.IsNullOrEmpty(subtitle);
            m_SubtitleText.gameObject.SetActive(hasSubtitle);
            if (hasSubtitle) m_SubtitleText.text = subtitle;
        }

        /// <summary>
        /// Grays out the whole row and kills its action button, so a feature that does not work yet
        /// cannot be tapped at all. The row stays visible and legible, just plainly unavailable.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (m_ActionButton != null) m_ActionButton.interactable = interactable;
            if (m_Toggle != null) m_Toggle.SetInteractable(interactable);
            UiDimState.Apply(gameObject, interactable);
        }

        /// <summary>Compatibility helper for legacy toggle rows.</summary>
        public void Set(string label, bool value)
        {
            Configure(null, label);
            m_Toggle?.SetState(value, notify: false, animate: false);
        }
    }
}

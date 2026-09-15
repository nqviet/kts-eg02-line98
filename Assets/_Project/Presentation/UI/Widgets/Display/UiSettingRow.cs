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

        public void Configure(Sprite icon, string title, string subtitle = null)
        {
            if (m_IconImage != null && icon != null) m_IconImage.sprite = icon;
            if (m_TitleText != null) m_TitleText.text = title ?? string.Empty;
            if (m_SubtitleText != null)
            {
                bool hasSubtitle = !string.IsNullOrEmpty(subtitle);
                m_SubtitleText.gameObject.SetActive(hasSubtitle);
                if (hasSubtitle) m_SubtitleText.text = subtitle;
            }
        }

        /// <summary>Compatibility helper for legacy toggle rows.</summary>
        public void Set(string label, bool value)
        {
            Configure(null, label);
            m_Toggle?.SetState(value, notify: false, animate: false);
        }
    }
}

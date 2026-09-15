using TMPro;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Semantic wrapper for a titled, themeable settings-card group.</summary>
    [DisallowMultipleComponent]
    public sealed class UiCardGroup : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_HeaderText;
        [SerializeField] private UnityEngine.UI.Image m_CardImage;

        public void SetHeader(string header)
        {
            if (m_HeaderText != null) m_HeaderText.text = header ?? string.Empty;
        }

        public void ApplyTheme(Line98.Data.UiThemeSO theme)
        {
            if (theme == null || m_CardImage == null) return;
            if (theme.CardBackgroundSprite != null) m_CardImage.sprite = theme.CardBackgroundSprite;
            m_CardImage.color = theme.PanelCard;
        }
    }
}

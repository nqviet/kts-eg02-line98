using TMPro;
using Line98.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>Applies named UiThemeSO tokens to prefab-owned graphic and TMP targets.</summary>
    [DisallowMultipleComponent]
    public sealed class UiThemeApplier : MonoBehaviour
    {
        public enum ColorToken
        {
            PanelCard,
            PanelButton,
            PanelTray,
            PanelShadow,
            InkValue,
            InkLabel,
            InkIcon,
            BadgeFill,
            BadgeNumeral,
            Crown,
            BrandNavy,
            BrandBlue
        }

        public enum FontSizeToken
        {
            None,
            Label,
            Score,
            Best,
            Button,
            Badge,
            Title,
            Body
        }

        [SerializeField] private ColorToken m_ColorToken = ColorToken.PanelCard;
        [SerializeField] private Graphic[] m_ColorTargets;
        [SerializeField] private FontSizeToken m_FontSizeToken = FontSizeToken.None;
        [SerializeField] private TMP_Text[] m_FontTargets;

        public void Configure(ColorToken colorToken, Graphic[] colorTargets, FontSizeToken fontSizeToken, TMP_Text[] fontTargets)
        {
            m_ColorToken = colorToken;
            m_ColorTargets = colorTargets ?? System.Array.Empty<Graphic>();
            m_FontSizeToken = fontSizeToken;
            m_FontTargets = fontTargets ?? System.Array.Empty<TMP_Text>();
        }

        public void Apply(UiThemeSO theme)
        {
            if (theme == null)
            {
                return;
            }

            Color color = ResolveColor(theme, m_ColorToken);
            if (m_ColorTargets == null) m_ColorTargets = System.Array.Empty<Graphic>();
            if (m_FontTargets == null) m_FontTargets = System.Array.Empty<TMP_Text>();

            for (int i = 0; i < m_ColorTargets.Length; i++)
            {
                if (m_ColorTargets[i] != null) m_ColorTargets[i].color = color;
            }

            if (m_FontSizeToken == FontSizeToken.None)
            {
                return;
            }

            float fontSize = ResolveFontSize(theme, m_FontSizeToken);
            for (int i = 0; i < m_FontTargets.Length; i++)
            {
                if (m_FontTargets[i] != null) m_FontTargets[i].fontSize = fontSize;
            }
        }

        private static Color ResolveColor(UiThemeSO theme, ColorToken token)
        {
            return token switch
            {
                ColorToken.PanelButton => theme.PanelButton,
                ColorToken.PanelTray => theme.PanelTray,
                ColorToken.PanelShadow => theme.PanelShadow,
                ColorToken.InkValue => theme.InkValue,
                ColorToken.InkLabel => theme.InkLabel,
                ColorToken.InkIcon => theme.InkIcon,
                ColorToken.BadgeFill => theme.BadgeFill,
                ColorToken.BadgeNumeral => theme.BadgeNumeral,
                ColorToken.Crown => theme.Crown,
                ColorToken.BrandNavy => theme.BrandNavy,
                ColorToken.BrandBlue => theme.BrandBlue,
                _ => theme.PanelCard
            };
        }

        private static float ResolveFontSize(UiThemeSO theme, FontSizeToken token)
        {
            return token switch
            {
                FontSizeToken.Label => theme.LabelFontSize,
                FontSizeToken.Score => theme.ScoreFontSize,
                FontSizeToken.Best => theme.BestFontSize,
                FontSizeToken.Button => theme.ButtonFontSize,
                FontSizeToken.Badge => theme.BadgeFontSize,
                FontSizeToken.Title => theme.TitleFontSize,
                FontSizeToken.Body => theme.BodyFontSize,
                _ => 0f
            };
        }
    }
}

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
            BrandBlue,
            SurfaceCardGold,
            SurfaceCardBorder,
            SurfaceGoldBorder,
            ToggleTrackActive,
            ToggleTrackInactive,
            ToggleThumb,
            InkRowTitle,
            InkSublabel,
            DividerHairline,
            ButtonGold,
            SurfacePrimary,
            SurfaceSecondary,
            SurfaceTertiaryZen,
            InkButtonPrimary,
            StreakDotOn,
            StreakDotOff,
            SelectedBadgeFill,
            SelectedBadgeInk,
            ActiveCardBorder,
            LightScrim,
            PanelCell
        }

        public enum SpriteToken
        {
            None,
            CardBackground,
            CardGold,
            ButtonCapsule,
            ButtonCapsulePrimary,
            ButtonCircle,
            ToggleTrackOn,
            ToggleTrackOff,
            ToggleThumb,
            IconPlay,
            IconCalendar,
            IconLotus,
            IconChart,
            IconGear,
            IconFlame,
            IconCrown
        }

        public enum MaterialToken
        {
            None,
            SurfaceMaterialCard,
            SurfaceMaterialButton
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
        [SerializeField] private SpriteToken m_SpriteToken = SpriteToken.None;
        [SerializeField] private UnityEngine.UI.Image[] m_SpriteTargets;
        [SerializeField] private MaterialToken m_MaterialToken = MaterialToken.None;
        [SerializeField] private Graphic[] m_MaterialTargets;

        public void Configure(ColorToken colorToken, Graphic[] colorTargets, FontSizeToken fontSizeToken, TMP_Text[] fontTargets)
        {
            m_ColorToken = colorToken;
            m_ColorTargets = colorTargets ?? System.Array.Empty<Graphic>();
            m_FontSizeToken = fontSizeToken;
            m_FontTargets = fontTargets ?? System.Array.Empty<TMP_Text>();
        }

        public void ConfigureSprite(SpriteToken spriteToken, UnityEngine.UI.Image[] spriteTargets)
        {
            m_SpriteToken = spriteToken;
            m_SpriteTargets = spriteTargets ?? System.Array.Empty<UnityEngine.UI.Image>();
        }

        public ColorToken CurrentColorToken => m_ColorToken;
        public SpriteToken CurrentSpriteToken => m_SpriteToken;
        public MaterialToken CurrentMaterialToken => m_MaterialToken;
        public Graphic[] ColorTargets => m_ColorTargets ?? System.Array.Empty<Graphic>();
        public UnityEngine.UI.Image[] SpriteTargets => m_SpriteTargets ?? System.Array.Empty<UnityEngine.UI.Image>();
        public Graphic[] MaterialTargets => m_MaterialTargets ?? System.Array.Empty<Graphic>();

        public void ConfigureMaterial(MaterialToken materialToken, Graphic[] materialTargets)
        {
            m_MaterialToken = materialToken;
            m_MaterialTargets = materialTargets ?? System.Array.Empty<Graphic>();
        }

        public static bool IsSurfaceToken(SpriteToken token)
        {
            return token switch
            {
                SpriteToken.CardBackground or
                SpriteToken.CardGold or
                SpriteToken.ButtonCapsule or
                SpriteToken.ButtonCapsulePrimary or
                SpriteToken.ButtonCircle or
                SpriteToken.ToggleTrackOn or
                SpriteToken.ToggleTrackOff or
                SpriteToken.ToggleThumb => true,
                _ => false
            };
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
            if (m_SpriteTargets == null) m_SpriteTargets = System.Array.Empty<UnityEngine.UI.Image>();
            if (m_MaterialTargets == null) m_MaterialTargets = System.Array.Empty<Graphic>();

            for (int i = 0; i < m_ColorTargets.Length; i++)
            {
                if (m_ColorTargets[i] != null) m_ColorTargets[i].color = color;
            }

            if (m_FontSizeToken != FontSizeToken.None)
            {
                float fontSize = ResolveFontSize(theme, m_FontSizeToken);
                for (int i = 0; i < m_FontTargets.Length; i++)
                {
                    if (m_FontTargets[i] != null) m_FontTargets[i].fontSize = fontSize;
                }
            }

            Sprite sprite = ResolveSprite(theme, m_SpriteToken);
            Material material = ResolveMaterial(theme, m_MaterialToken);
            bool isSurface = IsSurfaceToken(m_SpriteToken);

            if (m_SpriteToken != SpriteToken.None)
            {
                for (int i = 0; i < m_SpriteTargets.Length; i++)
                {
                    var img = m_SpriteTargets[i];
                    if (img == null) continue;

                    if (isSurface)
                    {
                        if (sprite != null)
                        {
                            img.sprite = sprite;
                            img.material = null; // D18: Sprite xor Material
                        }
                        else
                        {
                            img.sprite = null;
                            img.material = material;
                        }
                    }
                    else
                    {
                        if (sprite != null)
                        {
                            img.sprite = sprite;
                            img.material = null;
                        }
                    }
                }
            }

            if (m_MaterialToken != MaterialToken.None)
            {
                for (int i = 0; i < m_MaterialTargets.Length; i++)
                {
                    var matTarget = m_MaterialTargets[i];
                    if (matTarget == null) continue;

                    if (sprite != null && isSurface)
                    {
                        matTarget.material = null;
                        if (matTarget is UnityEngine.UI.Image img)
                        {
                            img.sprite = sprite;
                        }
                    }
                    else if (material != null)
                    {
                        matTarget.material = material;
                        if (matTarget is UnityEngine.UI.Image img && (isSurface || sprite == null))
                        {
                            img.sprite = null; // D18: Material xor Sprite
                        }
                    }
                    else
                    {
                        matTarget.material = null;
                        if (matTarget is UnityEngine.UI.Image img && isSurface)
                        {
                            img.sprite = null;
                        }
                    }
                }
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
                ColorToken.SurfaceCardGold => theme.SurfaceCardGold,
                ColorToken.SurfaceCardBorder => theme.SurfaceCardBorder,
                ColorToken.SurfaceGoldBorder => theme.SurfaceGoldBorder,
                ColorToken.ToggleTrackActive => theme.ToggleTrackActive,
                ColorToken.ToggleTrackInactive => theme.ToggleTrackInactive,
                ColorToken.ToggleThumb => theme.ToggleThumb,
                ColorToken.InkRowTitle => theme.InkRowTitle,
                ColorToken.InkSublabel => theme.InkSublabel,
                ColorToken.DividerHairline => theme.DividerHairline,
                ColorToken.ButtonGold => theme.ButtonGold,
                ColorToken.SurfacePrimary => theme.SurfacePrimary,
                ColorToken.SurfaceSecondary => theme.SurfaceSecondary,
                ColorToken.SurfaceTertiaryZen => theme.SurfaceTertiaryZen,
                ColorToken.InkButtonPrimary => theme.InkButtonPrimary,
                ColorToken.StreakDotOn => theme.StreakDotOn,
                ColorToken.StreakDotOff => theme.StreakDotOff,
                ColorToken.SelectedBadgeFill => theme.SelectedBadgeFill,
                ColorToken.SelectedBadgeInk => theme.SelectedBadgeInk,
                ColorToken.ActiveCardBorder => theme.ActiveCardBorder,
                ColorToken.LightScrim => theme.LightScrim,
                ColorToken.PanelCell => theme.PanelCell,
                _ => theme.PanelCard
            };
        }

        private static Sprite ResolveSprite(UiThemeSO theme, SpriteToken token)
        {
            return token switch
            {
                SpriteToken.CardBackground => theme.CardBackgroundSprite,
                SpriteToken.CardGold => theme.CardGoldSprite,
                SpriteToken.ButtonCapsule => theme.ButtonCapsuleSprite,
                SpriteToken.ButtonCapsulePrimary => theme.ButtonCapsulePrimary,
                SpriteToken.ButtonCircle => theme.ButtonCircleSprite,
                SpriteToken.ToggleTrackOn => theme.ToggleTrackOnSprite,
                SpriteToken.ToggleTrackOff => theme.ToggleTrackOffSprite,
                SpriteToken.ToggleThumb => theme.ToggleThumbSprite,
                SpriteToken.IconPlay => theme.IconPlay,
                SpriteToken.IconCalendar => theme.IconCalendar,
                SpriteToken.IconLotus => theme.IconLotus,
                SpriteToken.IconChart => theme.IconChart,
                SpriteToken.IconGear => theme.IconGear,
                SpriteToken.IconFlame => theme.IconFlame,
                SpriteToken.IconCrown => theme.IconCrown,
                _ => null
            };
        }

        private static Material ResolveMaterial(UiThemeSO theme, MaterialToken token)
        {
            return token switch
            {
                MaterialToken.SurfaceMaterialCard => theme.SurfaceMaterialCard,
                MaterialToken.SurfaceMaterialButton => theme.SurfaceMaterialButton,
                _ => null
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

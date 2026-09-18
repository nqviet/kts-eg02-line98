using UnityEngine;

namespace Line98.Data
{
    /// <summary>
    /// Design tokens for LINE 98 UI theme matching main_scene_mockup.png specifications.
    /// Defines panel tints, ink colors, ball preview colors, and typography metrics.
    /// </summary>
    [CreateAssetMenu(fileName = "UiTheme_Default", menuName = "Line98/Definitions/UI Theme")]
    public class UiThemeSO : ScriptableObject
    {
        [Header("Theme Identity")]
        [SerializeField] private string m_ThemeId = "default";
        [SerializeField] private string m_DisplayName = "Default";
        [SerializeField] private Sprite m_Thumbnail;
        [SerializeField] private bool m_UnlockedByDefault = true;
        [SerializeField] private UiPreviewSpriteSetSO m_PreviewSpriteSet;

        [Header("Panels (Frosted Family)")]
        [SerializeField] private Color m_PanelCard = new Color(0.918f, 0.949f, 1f, 0.40f);      // #EAF2FF @ 0.40
        [SerializeField] private Color m_PanelButton = new Color(0.918f, 0.949f, 1f, 0.65f);    // #EAF2FF @ 0.65
        [SerializeField] private Color m_PanelTray = new Color(0.804f, 0.855f, 0.910f, 0.78f);   // (205,218,232) @ 0.78
        [SerializeField] private Color m_PanelCell = new Color(0.761f, 0.816f, 0.886f, 0.88f);   // composite #C2D0E2
        [SerializeField] private Color m_PanelFrame = new Color(0.969f, 0.980f, 0.988f, 1f);    // #F7FAFC
        [SerializeField] private Color m_PanelShadow = new Color(0.051f, 0.106f, 0.165f, 0.20f); // #0D1B2A @ 0.20
        [SerializeField] private Color m_Scrim = new Color(0.039f, 0.078f, 0.157f, 0.61f);       // (10,20,40) @ 0.61

        [Header("Ink")]
        [SerializeField] private Color m_InkValue = new Color(0.039f, 0.063f, 0.200f, 1f);       // #0A1033
        [SerializeField] private Color m_InkLabel = new Color(0.278f, 0.388f, 0.569f, 1f);       // #476391
        [SerializeField] private Color m_InkIcon = new Color(0.125f, 0.188f, 0.314f, 1f);        // #203050
        [SerializeField] private Color m_BadgeFill = new Color(0.878f, 0.157f, 0.157f, 1f);      // #E02828
        [SerializeField] private Color m_BadgeNumeral = Color.white;                              // #FFFFFF
        [SerializeField] private Color m_Crown = new Color(0.973f, 0.722f, 0.063f, 1f);          // #F8B810
        [SerializeField] private Color m_BrandNavy = new Color(0f, 0.063f, 0.251f, 1f);          // #001040
        [SerializeField] private Color m_BrandBlue = new Color(0f, 0.282f, 0.941f, 1f);          // #0048F0

        [Header("Ball Face Colors")]
        [SerializeField] private Color m_BallRed = new Color(0.992f, 0.133f, 0.169f, 1f);       // #FD222B
        [SerializeField] private Color m_BallOrange = new Color(0.988f, 0.412f, 0.110f, 1f);    // #FC691C
        [SerializeField] private Color m_BallYellow = new Color(0.996f, 0.855f, 0.110f, 1f);    // #FEDA1C
        [SerializeField] private Color m_BallGreen = new Color(0.012f, 0.659f, 0.259f, 1f);     // #03A842
        [SerializeField] private Color m_BallCyan = new Color(0.133f, 0.761f, 0.980f, 1f);      // #22C2FA
        [SerializeField] private Color m_BallPurple = new Color(0.749f, 0.075f, 0.933f, 1f);    // #BF13EE
        [SerializeField] private Color m_BallBlue = new Color(0f, 0.306f, 0.992f, 1f);          // #004EFD

        [Header("Typography Metrics")]
        [SerializeField] private float m_LabelFontSize = 26f;
        [SerializeField] private float m_ScoreFontSize = 68f;
        [SerializeField] private float m_BestFontSize = 57f;
        [SerializeField] private float m_ButtonFontSize = 27f;
        [SerializeField] private float m_BadgeFontSize = 22f;
        [SerializeField] private float m_TitleFontSize = 36f;
        [SerializeField] private float m_BodyFontSize = 26f;

        [Header("Crystal Surface Tokens")]
        [SerializeField] private Color m_SurfaceCardGold = new Color(1f, 0.973f, 0.906f, 1f);          // #FFF8E7
        [SerializeField] private Color m_SurfaceCardBorder = new Color(0.910f, 0.886f, 0.824f, 1f);      // #E8E2D2
        [SerializeField] private Color m_SurfaceGoldBorder = new Color(0.918f, 0.765f, 0.322f, 1f);      // #EAC352
        [SerializeField] private Color m_ToggleTrackActive = new Color(0.153f, 0.722f, 0.373f, 1f);      // #27B85F
        [SerializeField] private Color m_ToggleTrackInactive = new Color(0.816f, 0.808f, 0.780f, 1f);    // #D0CEC7
        [SerializeField] private Color m_ToggleThumb = Color.white;
        [SerializeField] private Color m_InkRowTitle = new Color(0.055f, 0.122f, 0.267f, 1f);            // #0E1F44
        [SerializeField] private Color m_InkSublabel = new Color(0.424f, 0.494f, 0.584f, 1f);            // #6C7E95
        [SerializeField] private Color m_DividerHairline = new Color(0.871f, 0.851f, 0.796f, 0.6f);      // #DED9CB
        [SerializeField] private Color m_ButtonGold = new Color(0.961f, 0.780f, 0.306f, 1f);              // #F5C74E

        [Header("Menu & Surface Colors")]
        [SerializeField] private Color m_SurfacePrimary = new Color(0.145f, 0.655f, 0.329f, 1f);     // #25A754
        [SerializeField] private Color m_SurfaceSecondary = new Color(0.969f, 0.961f, 0.933f, 1f);   // #F7F5EE
        [SerializeField] private Color m_SurfaceTertiaryZen = new Color(0.839f, 0.910f, 0.969f, 1f); // #D6E8F7
        [SerializeField] private Color m_InkButtonPrimary = Color.white;
        [SerializeField] private Color m_StreakDotOn = new Color(0.145f, 0.722f, 0.365f, 1f);        // #25B85D
        [SerializeField] private Color m_StreakDotOff = new Color(0.812f, 0.839f, 0.863f, 1f);       // #CFD6DC

        [Header("Theme Selection Surface Tokens")]
        [SerializeField] private Color m_SelectedBadgeFill = new Color(0.875f, 0.965f, 0.898f, 1f); // #DFF6E5
        [SerializeField] private Color m_SelectedBadgeInk = new Color(0.118f, 0.478f, 0.239f, 1f);  // #1E7A3D
        [SerializeField] private Color m_ActiveCardBorder = new Color(0.184f, 0.733f, 0.380f, 1f);  // #2FBB61
        [SerializeField] private Color m_LightScrim = new Color(0.96f, 0.97f, 0.98f, 0.75f);

        [Header("Crystal UI Sprite Tokens")]
        [SerializeField] private Sprite m_CardBackgroundSprite;
        [SerializeField] private Sprite m_CardGoldSprite;
        [SerializeField] private Sprite m_ButtonCapsuleSprite;
        [SerializeField] private Sprite m_ButtonCapsulePrimary;
        [SerializeField] private Sprite m_ButtonCircleSprite;
        [SerializeField] private Sprite m_ToggleTrackOnSprite;
        [SerializeField] private Sprite m_ToggleTrackOffSprite;
        [SerializeField] private Sprite m_ToggleThumbSprite;
        [SerializeField] private Sprite m_IconPlay;
        [SerializeField] private Sprite m_IconCalendar;
        [SerializeField] private Sprite m_IconLotus;
        [SerializeField] private Sprite m_IconChart;
        [SerializeField] private Sprite m_IconGear;
        [SerializeField] private Sprite m_IconFlame;
        [SerializeField] private Sprite m_IconCrown;

        [Header("Classic UI Material Tokens (D18)")]
        [SerializeField] private Material m_SurfaceMaterialCard;
        [SerializeField] private Material m_SurfaceMaterialButton;

        public string ThemeId => m_ThemeId;
        public string DisplayName => m_DisplayName;
        public Sprite Thumbnail => m_Thumbnail;
        public bool UnlockedByDefault => m_UnlockedByDefault;
        public UiPreviewSpriteSetSO PreviewSpriteSet => m_PreviewSpriteSet;

        public Color PanelCard => m_PanelCard;
        public Color PanelButton => m_PanelButton;
        public Color PanelTray => m_PanelTray;
        public Color PanelCell => m_PanelCell;
        public Color PanelFrame => m_PanelFrame;
        public Color PanelShadow => m_PanelShadow;
        public Color Scrim => m_Scrim;

        public Color InkValue => m_InkValue;
        public Color InkLabel => m_InkLabel;
        public Color InkIcon => m_InkIcon;
        public Color BadgeFill => m_BadgeFill;
        public Color BadgeNumeral => m_BadgeNumeral;
        public Color Crown => m_Crown;
        public Color BrandNavy => m_BrandNavy;
        public Color BrandBlue => m_BrandBlue;

        public Color BallRed => m_BallRed;
        public Color BallOrange => m_BallOrange;
        public Color BallYellow => m_BallYellow;
        public Color BallGreen => m_BallGreen;
        public Color BallCyan => m_BallCyan;
        public Color BallPurple => m_BallPurple;
        public Color BallBlue => m_BallBlue;

        public float LabelFontSize => m_LabelFontSize;
        public float ScoreFontSize => m_ScoreFontSize;
        public float BestFontSize => m_BestFontSize;
        public float ButtonFontSize => m_ButtonFontSize;
        public float BadgeFontSize => m_BadgeFontSize;
        public float TitleFontSize => m_TitleFontSize;
        public float BodyFontSize => m_BodyFontSize;

        public Color SurfaceCardGold => m_SurfaceCardGold;
        public Color SurfaceCardBorder => m_SurfaceCardBorder;
        public Color SurfaceGoldBorder => m_SurfaceGoldBorder;
        public Color ToggleTrackActive => m_ToggleTrackActive;
        public Color ToggleTrackInactive => m_ToggleTrackInactive;
        public Color ToggleThumb => m_ToggleThumb;
        public Color InkRowTitle => m_InkRowTitle;
        public Color InkSublabel => m_InkSublabel;
        public Color DividerHairline => m_DividerHairline;
        public Color ButtonGold => m_ButtonGold;

        public Color SurfacePrimary => m_SurfacePrimary;
        public Color SurfaceSecondary => m_SurfaceSecondary;
        public Color SurfaceTertiaryZen => m_SurfaceTertiaryZen;
        public Color InkButtonPrimary => m_InkButtonPrimary;
        public Color StreakDotOn => m_StreakDotOn;
        public Color StreakDotOff => m_StreakDotOff;

        public Color SelectedBadgeFill => m_SelectedBadgeFill.a > 0.001f ? m_SelectedBadgeFill : new Color(0.875f, 0.965f, 0.898f, 1f);
        public Color SelectedBadgeInk => m_SelectedBadgeInk.a > 0.001f ? m_SelectedBadgeInk : new Color(0.118f, 0.478f, 0.239f, 1f);
        public Color ActiveCardBorder => m_ActiveCardBorder.a > 0.001f ? m_ActiveCardBorder : new Color(0.184f, 0.733f, 0.380f, 1f);
        public Color LightScrim => m_LightScrim.a > 0.001f ? m_LightScrim : new Color(0.96f, 0.97f, 0.98f, 0.75f);

        public Sprite CardBackgroundSprite => m_CardBackgroundSprite;
        public Sprite CardGoldSprite => m_CardGoldSprite;
        public Sprite ButtonCapsuleSprite => m_ButtonCapsuleSprite;
        public Sprite ButtonCapsulePrimary => m_ButtonCapsulePrimary;
        public Sprite ButtonCircleSprite => m_ButtonCircleSprite;
        public Sprite ToggleTrackOnSprite => m_ToggleTrackOnSprite;
        public Sprite ToggleTrackOffSprite => m_ToggleTrackOffSprite;
        public Sprite ToggleThumbSprite => m_ToggleThumbSprite;
        public Sprite IconPlay => m_IconPlay;
        public Sprite IconCalendar => m_IconCalendar;
        public Sprite IconLotus => m_IconLotus;
        public Sprite IconChart => m_IconChart;
        public Sprite IconGear => m_IconGear;
        public Sprite IconFlame => m_IconFlame;
        public Sprite IconCrown => m_IconCrown;

        public Material SurfaceMaterialCard => m_SurfaceMaterialCard;
        public Material SurfaceMaterialButton => m_SurfaceMaterialButton;
    }
}

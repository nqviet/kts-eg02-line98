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
        [SerializeField] private float m_BadgeFontSize = 26f;
        [SerializeField] private float m_TitleFontSize = 36f;
        [SerializeField] private float m_BodyFontSize = 26f;

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
    }
}

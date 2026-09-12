using System;
using TMPro;
using UnityEngine;

namespace Line98.Design
{
    /// <summary>
    /// ScriptableObject defining all visual design tokens, palettes, layouts, metrics,
    /// and visual asset bindings for the Line 98 game.
    /// Allows artists and designers to alter the look and feel without modifying behaviours.
    /// </summary>
    [CreateAssetMenu(fileName = "Line98Theme", menuName = "Line98/Theme Data")]
    public class Line98ThemeData : ScriptableObject
    {
        private static TMP_FontAsset s_DefaultFont;

        [Header("Dimensions & Metrics")]
        [SerializeField] private float m_DesignWidth = 768f;
        [SerializeField] private float m_DesignHeight = 1366f;
        [SerializeField] private int m_BoardSize = 9;
        [SerializeField] private float m_CellSize = 72f;
        [SerializeField] private float m_CellSpacing = 3f;
        [SerializeField] private float m_PulseSpeed = 7f;
        [SerializeField] private float m_PulseAmplitude = 0.025f;
        [SerializeField] private float m_ToastDuration = 1.35f;
        [SerializeField] private float m_ToastFadeDuration = 0.28f;

        [Header("Ball Visuals")]
        [SerializeField]
        private Color[] m_BallColors =
        {
            new Color(0.96f, 0.08f, 0.12f),
            new Color(0.03f, 0.27f, 0.94f),
            new Color(1.00f, 0.76f, 0.00f),
            new Color(0.01f, 0.58f, 0.25f),
            new Color(0.70f, 0.05f, 0.85f),
            new Color(0.00f, 0.66f, 0.92f),
            new Color(1.00f, 0.28f, 0.02f),
        };

        [SerializeField] private Sprite[] m_CustomBallSprites;

        [Header("Scene & Environment")]
        [SerializeField] private Color m_CameraBackgroundColor = new Color(0.66f, 0.84f, 0.98f);
        [SerializeField] private Color m_SkyWashColor = new Color(0.87f, 0.95f, 1f, 0.34f);
        [SerializeField] private Texture2D m_CustomLandscapeTexture;
        [SerializeField] private Sprite m_CustomSkyWashSprite;

        [Header("UI Panels & Frames")]
        [SerializeField] private Color m_PanelBackgroundColor = new Color(0.86f, 0.93f, 1f, 0.80f);
        [SerializeField] private Color m_PanelShadowColor = new Color(0.05f, 0.14f, 0.34f, 0.17f);
        [SerializeField] private Color m_PanelOutlineColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color m_BoardFrameColor = new Color(0.91f, 0.97f, 1f, 0.80f);
        [SerializeField] private Color m_BoardFrameOutlineColor = new Color(1f, 1f, 1f, 0.60f);
        [SerializeField] private Color m_CellNormalColor = new Color(0.76f, 0.84f, 0.93f, 0.78f);
        [SerializeField] private Color m_SelectionOutlineColor = new Color(0.19f, 0.90f, 1f, 0.96f);
        [SerializeField] private float m_SelectionGlowAlpha = 0.48f;

        [Header("Typography & Text Colors")]
        [SerializeField] private TMP_FontAsset m_FontAsset;
        [SerializeField] private Color m_TextPrimaryColor = new Color(0.02f, 0.08f, 0.25f);
        [SerializeField] private Color m_TextSecondaryColor = new Color(0.27f, 0.38f, 0.62f);
        [SerializeField] private Color m_TextShadowColor = new Color(0.91f, 0.98f, 1f, 0.65f);
        [SerializeField] private Color m_TitleColor = new Color(0.02f, 0.09f, 0.28f);
        [SerializeField] private Color m_NumberTitleColor = new Color(0.03f, 0.31f, 0.98f);
        [SerializeField] private Color m_SubtitleColor = new Color(0.22f, 0.34f, 0.59f);

        [Header("Controls & Buttons")]
        [SerializeField] private Color m_ButtonNormalTint = new Color(0.88f, 0.94f, 1f, 0.88f);
        [SerializeField] private Color m_IconTint = new Color(0.11f, 0.20f, 0.39f);
        [SerializeField] private Color m_CrownTint = new Color(1f, 0.66f, 0.0f);
        [SerializeField] private Color m_DpadBackgroundColor = new Color(0.85f, 0.94f, 1f, 0.96f);
        [SerializeField] private Color m_DpadOutlineColor = new Color(0.22f, 0.87f, 1f, 0.95f);
        [SerializeField] private Color m_ArrowTint = new Color(0.02f, 0.35f, 0.94f, 1f);

        [Header("Modal Dialogs")]
        [SerializeField] private Color m_ModalDimmerColor = new Color(0.02f, 0.08f, 0.24f, 0.42f);
        [SerializeField] private Color m_ModalDialogColor = new Color(0.91f, 0.97f, 1f, 0.98f);
        [SerializeField] private Color m_ModalPrimaryButtonColor = new Color(0.22f, 0.53f, 0.96f, 1f);
        [SerializeField] private Color m_ModalSecondaryButtonColor = new Color(0.84f, 0.90f, 0.99f, 1f);

        [Header("Custom UI Sprites")]
        [SerializeField] private Sprite m_CustomRoundedSprite;
        [SerializeField] private Sprite m_CustomOutlineSprite;
        [SerializeField] private Sprite m_CustomGlowSprite;
        [SerializeField] private Sprite m_BoardFrameSprite;
        [SerializeField] private Sprite m_CellSprite;
        [SerializeField] private Sprite m_TrayNextSprite;
        [SerializeField] private Sprite m_ButtonSquareSprite;
        [SerializeField] private Sprite m_UndoButtonSprite;
        [SerializeField] private Sprite m_NewGameButtonSprite;
        [SerializeField] private Sprite m_DpadSprite;
        [SerializeField] private Sprite m_IconGearSprite;
        [SerializeField] private Sprite m_IconBarsSprite;
        [SerializeField] private Sprite m_IconCrownSprite;
        [SerializeField] private Sprite m_LogoQuadSprite;
        [SerializeField] private Sprite m_LogoTextSprite;

        // Metrics properties
        public float DesignWidth => m_DesignWidth;
        public float DesignHeight => m_DesignHeight;
        public int BoardSize => m_BoardSize;
        public float CellSize => m_CellSize;
        public float CellSpacing => m_CellSpacing;
        public float PulseSpeed => m_PulseSpeed;
        public float PulseAmplitude => m_PulseAmplitude;
        public float ToastDuration => m_ToastDuration;
        public float ToastFadeDuration => m_ToastFadeDuration;

        // Ball palette properties
        public int TotalBallColors => m_BallColors != null ? m_BallColors.Length : 0;
        public Color[] BallColors => m_BallColors;

        // Colors properties
        public Color CameraBackgroundColor => m_CameraBackgroundColor;
        public Color SkyWashColor => m_SkyWashColor;
        public Color PanelBackgroundColor => m_PanelBackgroundColor;
        public Color PanelShadowColor => m_PanelShadowColor;
        public Color PanelOutlineColor => m_PanelOutlineColor;
        public Color BoardFrameColor => m_BoardFrameColor;
        public Color BoardFrameOutlineColor => m_BoardFrameOutlineColor;
        public Color CellNormalColor => m_CellNormalColor;
        public Color SelectionOutlineColor => m_SelectionOutlineColor;
        public float SelectionGlowAlpha => m_SelectionGlowAlpha;
        public Color TextPrimaryColor => m_TextPrimaryColor;
        public Color TextSecondaryColor => m_TextSecondaryColor;
        public Color TextShadowColor => m_TextShadowColor;
        public Color TitleColor => m_TitleColor;
        public Color NumberTitleColor => m_NumberTitleColor;
        public Color SubtitleColor => m_SubtitleColor;
        public Color ButtonNormalTint => m_ButtonNormalTint;
        public Color IconTint => m_IconTint;
        public Color CrownTint => m_CrownTint;
        public Color DpadBackgroundColor => m_DpadBackgroundColor;
        public Color DpadOutlineColor => m_DpadOutlineColor;
        public Color ArrowTint => m_ArrowTint;
        public Color ModalDimmerColor => m_ModalDimmerColor;
        public Color ModalDialogColor => m_ModalDialogColor;
        public Color ModalPrimaryButtonColor => m_ModalPrimaryButtonColor;
        public Color ModalSecondaryButtonColor => m_ModalSecondaryButtonColor;

        public Color GetBallColor(int index)
        {
            if (m_BallColors == null || m_BallColors.Length == 0)
            {
                return Color.white;
            }

            int safeIndex = Mathf.Clamp(index, 0, m_BallColors.Length - 1);
            return m_BallColors[safeIndex];
        }

        public Sprite GetBallSprite(int index)
        {
            if (m_CustomBallSprites != null && index >= 0 && index < m_CustomBallSprites.Length && m_CustomBallSprites[index] != null)
            {
                return m_CustomBallSprites[index];
            }

            return Line98ArtFactory.GetBallSprite(index, GetBallColor(index));
        }

        public Sprite GetIconSprite(Line98IconType icon)
        {
            switch (icon)
            {
                case Line98IconType.Gear when m_IconGearSprite != null:
                    return m_IconGearSprite;
                case Line98IconType.Bars when m_IconBarsSprite != null:
                    return m_IconBarsSprite;
                case Line98IconType.Crown when m_IconCrownSprite != null:
                    return m_IconCrownSprite;
                case Line98IconType.Dpad when m_DpadSprite != null:
                    return m_DpadSprite;
                default:
                    return Line98ArtFactory.GetIconSprite(icon);
            }
        }

        public Sprite GetRoundedSprite()
        {
            return m_CustomRoundedSprite != null ? m_CustomRoundedSprite : Line98ArtFactory.GetRoundedSprite();
        }

        public Sprite GetBoardFrameSprite()
        {
            return m_BoardFrameSprite != null ? m_BoardFrameSprite : GetRoundedSprite();
        }

        public Sprite GetCellSprite()
        {
            return m_CellSprite != null ? m_CellSprite : GetRoundedSprite();
        }

        public Sprite GetTrayNextSprite()
        {
            return m_TrayNextSprite != null ? m_TrayNextSprite : GetRoundedSprite();
        }

        public Sprite GetButtonSquareSprite()
        {
            return m_ButtonSquareSprite != null ? m_ButtonSquareSprite : GetRoundedSprite();
        }

        public Sprite GetUndoButtonSprite()
        {
            return m_UndoButtonSprite != null ? m_UndoButtonSprite : GetRoundedSprite();
        }

        public Sprite GetNewGameButtonSprite()
        {
            return m_NewGameButtonSprite != null ? m_NewGameButtonSprite : GetRoundedSprite();
        }

        public Sprite GetDpadSprite()
        {
            return m_DpadSprite != null ? m_DpadSprite : GetRoundedSprite();
        }

        public Sprite GetLogoQuadSprite()
        {
            return m_LogoQuadSprite;
        }

        public Sprite GetLogoTextSprite()
        {
            return m_LogoTextSprite;
        }

        public Sprite GetOutlineSprite()
        {
            return m_CustomOutlineSprite != null ? m_CustomOutlineSprite : Line98ArtFactory.GetOutlineSprite();
        }

        public Sprite GetGlowSprite()
        {
            return m_CustomGlowSprite != null ? m_CustomGlowSprite : Line98ArtFactory.GetGlowSprite();
        }

        public Sprite GetVerticalFadeSprite()
        {
            return m_CustomSkyWashSprite != null ? m_CustomSkyWashSprite : Line98ArtFactory.GetVerticalFadeSprite();
        }

        public Texture2D GetLandscapeTexture()
        {
            return m_CustomLandscapeTexture != null ? m_CustomLandscapeTexture : Line98ArtFactory.GetLandscapeTexture();
        }

        public TMP_FontAsset GetFont()
        {
            if (m_FontAsset != null)
            {
                return m_FontAsset;
            }

            if (s_DefaultFont != null)
            {
                return s_DefaultFont;
            }

            s_DefaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            return s_DefaultFont;
        }

        public static Line98ThemeData CreateDefault()
        {
            Line98ThemeData instance = CreateInstance<Line98ThemeData>();
            instance.name = "Default Line98 Theme";
            return instance;
        }
    }
}

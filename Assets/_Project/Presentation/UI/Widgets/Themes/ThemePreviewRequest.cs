using System;
using UnityEngine;
using Line98.Data;

namespace Line98.Presentation
{
    public enum PreviewEmphasis : byte
    {
        Gems,
        Board
    }

    /// <summary>
    /// Payload request for UiThemePreviewPanel describing which assets to show on the preview card
    /// and which axis to emphasize (Gems for BALLS tab, Board for BOARD tab).
    /// </summary>
    public struct ThemePreviewRequest
    {
        public string DisplayName;
        public BoardThemeSO BoardTheme;
        public BallThemeSO BallTheme;
        public UiThemeSO UiTheme;
        public Sprite[] GemSprites;
        public PreviewEmphasis Emphasis;
        public bool IsApplied;
        public bool Animate;

        public BoardThemeSO Board
        {
            readonly get => BoardTheme;
            set => BoardTheme = value;
        }

        public BallThemeSO Ball
        {
            readonly get => BallTheme;
            set => BallTheme = value;
        }

        public ThemePreviewRequest(
            string displayName,
            BoardThemeSO board,
            BallThemeSO ball,
            Sprite[] gemSprites,
            PreviewEmphasis emphasis,
            bool isApplied,
            bool animate = true,
            UiThemeSO uiTheme = null)
        {
            DisplayName = displayName ?? string.Empty;
            BoardTheme = board;
            BallTheme = ball;
            UiTheme = uiTheme;
            GemSprites = gemSprites ?? Array.Empty<Sprite>();
            Emphasis = emphasis;
            IsApplied = isApplied;
            Animate = animate;
        }
    }
}

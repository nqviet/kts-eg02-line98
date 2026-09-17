using System;
using Line98.Data;

namespace Line98.Services
{
    /// <summary>
    /// Extension of frozen IThemeProvider exposing the three independent cosmetic axes
    /// (Ball, Board, ClearEffect), the board-derived UI theme, persistence, and change notifications.
    /// </summary>
    public interface ICosmeticService : IThemeProvider
    {
        BallThemeSO ActiveBallTheme { get; }
        BoardThemeSO ActiveBoardTheme { get; }
        UiThemeSO ActiveUiTheme { get; }
        string ActiveUiThemeId { get; }
        ClearEffectSO ActiveClearEffect { get; }
        string ActiveClearEffectThemeId { get; }
        event Action<ThemeChange> OnThemeChanged;

        void SetClearEffectTheme(string clearEffectId);
        void ResetToDefault();

        ThemeSelection GetSelection();
        bool IsPartAvailable(string partId);
        void ReportPartSwapped(ThemeCategory category, string partId);
    }
}

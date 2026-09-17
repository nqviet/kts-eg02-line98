using System;
using Line98.Data;

namespace Line98.Services
{
    /// <summary>
    /// Extension of frozen IThemeProvider providing bundle selection, UI/Clear theme management,
    /// persistence, and change notifications without modifying IThemeProvider.
    /// </summary>
    public interface ICosmeticService : IThemeProvider
    {
        string ActiveThemeId { get; }
        ThemeDefinitionSO ActiveTheme { get; }
        BallThemeSO ActiveBallTheme { get; }
        string ActiveClearEffectThemeId { get; }
        string BallOverrideId { get; }
        string BoardOverrideId { get; }
        event Action<ThemeChange> OnThemeChanged;

        void SetTheme(string themeId);
        void SetUiTheme(string uiThemeId);
        void SetClearEffectTheme(string clearEffectId);
        void ResetCategoryOverride(ThemeCategory category);
        void ResetToDefault();
    }
}

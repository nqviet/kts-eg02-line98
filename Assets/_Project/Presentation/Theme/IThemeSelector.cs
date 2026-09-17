using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Presentation-owned contract for requesting theme changes from UI surfaces (e.g. CosmeticsPopup)
    /// without creating an asmdef dependency from Presentation to Services.
    /// </summary>
    public interface IThemeSelector
    {
        // Bundle-level selection is the player-facing authority.
        string ActiveThemeId { get; }
        void RequestTheme(string bundleThemeId);

        // Category-level APIs remain available for explicit future mixing affordances.
        BallThemeSO ActiveBallTheme { get; }
        string ActiveBallThemeId { get; }
        string ActiveBoardThemeId { get; }
        string ActiveClearEffectThemeId { get; }
        void RequestTheme(ThemeCategory category, string partThemeId);
    }
}

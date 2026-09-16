using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Presentation-owned contract for requesting theme changes from UI surfaces (e.g. CosmeticsPopup)
    /// without creating an asmdef dependency from Presentation to Services.
    /// </summary>
    public interface IThemeSelector
    {
        // v1 — bundle-level, kept for the Settings quick path and existing tests
        void RequestTheme(string bundleThemeId);

        // v2 — per-category tab support
        string ActiveBallThemeId { get; }
        string ActiveBoardThemeId { get; }
        string ActiveClearEffectThemeId { get; }
        void RequestTheme(ThemeCategory category, string partThemeId);
    }
}

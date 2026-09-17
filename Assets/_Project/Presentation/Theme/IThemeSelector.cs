using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Presentation-owned contract for requesting theme changes from UI surfaces (e.g. CosmeticsPopup)
    /// without creating an asmdef dependency from Presentation to Services.
    /// Selection is per part: Ball, Board and ClearEffect are independent axes; UI is derived from the board.
    /// </summary>
    public interface IThemeSelector
    {
        BallThemeSO ActiveBallTheme { get; }
        string ActiveBallThemeId { get; }
        string ActiveBoardThemeId { get; }
        UiThemeSO ActiveUiTheme { get; }        // derived from the board selection
        string ActiveUiThemeId { get; }
        string ActiveClearEffectThemeId { get; }

        /// <summary>Ball | Board | ClearEffect. Ui is not selectable and is ignored with a warning.</summary>
        void RequestTheme(ThemeCategory category, string partThemeId);
    }
}

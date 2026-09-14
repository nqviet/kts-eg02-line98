namespace Line98.Services
{
    /// <summary>
    /// Frozen interface contract for visual theme management per GDD P0.1 / P3.9.
    /// Allows swapping cosmetic ball and board themes without coupling presentation to gameplay.
    /// </summary>
    public interface IThemeProvider
    {
        string ActiveBallThemeId { get; }
        string ActiveBoardThemeId { get; }
        void SetBallTheme(string themeId);
        void SetBoardTheme(string themeId);
    }
}

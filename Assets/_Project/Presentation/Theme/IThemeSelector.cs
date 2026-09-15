namespace Line98.Presentation
{
    /// <summary>
    /// Presentation-owned contract for requesting theme changes from UI surfaces (e.g. CosmeticsPopup)
    /// without creating an asmdef dependency from Presentation to Services.
    /// </summary>
    public interface IThemeSelector
    {
        void RequestTheme(string themeId);
    }
}

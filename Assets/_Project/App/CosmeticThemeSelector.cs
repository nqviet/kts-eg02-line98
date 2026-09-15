using Line98.Presentation;
using Line98.Services;

namespace Line98.App
{
    /// <summary>
    /// Presentation-to-Services bridge adapting IThemeSelector requests to ICosmeticService.
    /// Enforces C4: breaks direct asmdef coupling between Line98.Presentation and Line98.Services.
    /// </summary>
    public sealed class CosmeticThemeSelector : IThemeSelector
    {
        private readonly ICosmeticService m_CosmeticService;

        public CosmeticThemeSelector(ICosmeticService cosmeticService)
        {
            m_CosmeticService = cosmeticService;
        }

        public void RequestTheme(string themeId)
        {
            m_CosmeticService?.SetTheme(themeId);
        }
    }
}

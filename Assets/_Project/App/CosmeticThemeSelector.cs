using Line98.Data;
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

        public string ActiveBallThemeId => m_CosmeticService?.ActiveBallThemeId ?? ThemeIds.Classic;
        public string ActiveBoardThemeId => m_CosmeticService?.ActiveBoardThemeId ?? ThemeIds.Classic;
        public string ActiveClearEffectThemeId => m_CosmeticService?.ActiveClearEffectThemeId ?? ThemeIds.Classic;

        public void RequestTheme(string themeId)
        {
            m_CosmeticService?.SetTheme(themeId);
        }

        public void RequestTheme(Line98.Data.ThemeCategory category, string partThemeId)
        {
            if (m_CosmeticService == null) return;
            switch (category)
            {
                case Line98.Data.ThemeCategory.Ball:
                    m_CosmeticService.SetBallTheme(partThemeId);
                    break;
                case Line98.Data.ThemeCategory.Board:
                    m_CosmeticService.SetBoardTheme(partThemeId);
                    break;
                case Line98.Data.ThemeCategory.Ui:
                    m_CosmeticService.SetUiTheme(partThemeId);
                    break;
                case Line98.Data.ThemeCategory.ClearEffect:
                    m_CosmeticService.SetClearEffectTheme(partThemeId);
                    break;
            }
        }
    }
}

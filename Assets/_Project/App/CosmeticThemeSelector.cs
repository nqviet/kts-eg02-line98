using UnityEngine;
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

        public BallThemeSO ActiveBallTheme => m_CosmeticService?.ActiveBallTheme;
        public string ActiveBallThemeId => m_CosmeticService?.ActiveBallThemeId ?? ThemeIds.Classic;
        public string ActiveBoardThemeId => m_CosmeticService?.ActiveBoardThemeId ?? ThemeIds.Classic;
        public UiThemeSO ActiveUiTheme => m_CosmeticService?.ActiveUiTheme;
        public string ActiveUiThemeId => m_CosmeticService?.ActiveUiThemeId ?? ThemeIds.Default;
        public string ActiveClearEffectThemeId => m_CosmeticService?.ActiveClearEffectThemeId ?? ThemeIds.Classic;

        public void RequestTheme(ThemeCategory category, string partThemeId)
        {
            if (m_CosmeticService == null) return;
            switch (category)
            {
                case ThemeCategory.Ball:
                    m_CosmeticService.SetBallTheme(partThemeId);
                    break;
                case ThemeCategory.Board:
                    m_CosmeticService.SetBoardTheme(partThemeId);
                    break;
                case ThemeCategory.ClearEffect:
                    m_CosmeticService.SetClearEffectTheme(partThemeId);
                    break;
                case ThemeCategory.Ui:
                    Debug.LogWarning($"[CosmeticThemeSelector] Ignored UI theme request '{partThemeId}': UI follows the selected board.");
                    break;
            }
        }
    }
}

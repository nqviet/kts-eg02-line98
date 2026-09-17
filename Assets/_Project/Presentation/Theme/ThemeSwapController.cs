using UnityEngine;
using Line98.Data;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Orchestrates live runtime cosmetic theme swaps across board, balls, UI, and clear effects.
    /// Fixed execution order per architecture: Board -> Balls -> UI -> Clear.
    /// </summary>
    public sealed class ThemeSwapController
    {
        private readonly BoardView m_BoardView;
        private readonly BallViewManager m_BallManager;
        private readonly UiShell m_UiShell;
        private readonly HudPresenter m_HudPresenter;
        private readonly BoardAnimator m_BoardAnimator;

        public ThemeSwapController(
            BoardView boardView,
            BallViewManager ballManager,
            UiShell uiShell,
            HudPresenter hudPresenter,
            BoardAnimator boardAnimator)
        {
            m_BoardView = boardView;
            m_BallManager = ballManager;
            m_UiShell = uiShell;
            m_HudPresenter = hudPresenter;
            m_BoardAnimator = boardAnimator;
        }

        public void ApplyBallTheme(BallThemeSO ballTheme)
        {
            if (ballTheme == null) return;
            if (m_BallManager != null)
            {
                m_BallManager.ApplyTheme(ballTheme);
            }
            if (m_HudPresenter != null)
            {
                m_HudPresenter.ApplyTheme(null, ballTheme);
            }
        }

        public void ApplyBoardTheme(BoardThemeSO boardTheme)
        {
            if (boardTheme == null || m_BoardView == null) return;
            m_BoardView.ApplyTheme(boardTheme);
        }

        public void ApplyClearEffect(ClearEffectSO clearEffect)
        {
            if (clearEffect == null || m_BoardAnimator == null) return;
            m_BoardAnimator.ApplyClearEffect(clearEffect);
        }

        public void ApplyUiTheme(UiThemeSO uiTheme, string themeId)
        {
            m_UiShell?.ApplyTheme(uiTheme, themeId);
            if (uiTheme != null)
            {
                m_HudPresenter?.ApplyTheme(uiTheme, null);
            }
        }

        public void ApplyTheme(ThemeDefinitionSO theme, BallThemeSO ballTheme = null)
        {
            if (theme == null) return;

            // 1. Board
            if (theme.BoardTheme != null)
            {
                ApplyBoardTheme(theme.BoardTheme);
            }

            // 2. Balls (effective ball theme wins over the bundle's)
            ApplyBallTheme(ballTheme != null ? ballTheme : theme.BallTheme);

            // 3. UI
            ApplyUiTheme(theme.UiTheme, theme.ThemeId);

            // 4. Clear effect
            if (theme.ClearEffect != null)
            {
                ApplyClearEffect(theme.ClearEffect);
            }
        }
    }
}

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

        public void ApplyTheme(ThemeDefinitionSO theme)
        {
            if (theme == null) return;

            // 1. Board
            if (theme.BoardTheme != null && m_BoardView != null)
            {
                m_BoardView.ApplyTheme(theme.BoardTheme);
            }

            // 2. Balls
            if (theme.BallTheme != null && m_BallManager != null)
            {
                m_BallManager.ApplyTheme(theme.BallTheme);
            }

            // 3. UI
            if (theme.UiTheme != null)
            {
                m_UiShell?.ApplyTheme(theme.UiTheme, theme.ThemeId);
                m_HudPresenter?.ApplyTheme(theme.UiTheme, theme.BallTheme);
            }
            else
            {
                m_UiShell?.ApplyTheme(null, theme.ThemeId);
                if (theme.BallTheme != null)
                {
                    m_HudPresenter?.ApplyTheme(null, theme.BallTheme);
                }
            }

            // 4. Clear effect
            if (theme.ClearEffect != null && m_BoardAnimator != null)
            {
                m_BoardAnimator.ApplyClearEffect(theme.ClearEffect);
            }
        }
    }
}

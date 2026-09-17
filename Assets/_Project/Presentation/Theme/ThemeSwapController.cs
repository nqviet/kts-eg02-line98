using UnityEngine;
using Line98.Data;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Orchestrates live runtime cosmetic theme swaps across board, balls, UI, and clear effects.
    /// Fixed execution order per architecture: Board -> UI -> Balls -> Clear.
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

        public void ApplyUiTheme(UiThemeSO uiTheme)
        {
            if (uiTheme == null) return;
            m_UiShell?.ApplyTheme(uiTheme);
            m_HudPresenter?.ApplyTheme(uiTheme, null);
        }

        /// <summary>Applies every resolved part. Board and UI go together so they never visibly diverge.</summary>
        public void ApplySelection(ThemeSelection selection)
        {
            ApplyBoardTheme(selection.Board);
            ApplyUiTheme(selection.Ui);
            ApplyBallTheme(selection.Ball);
            ApplyClearEffect(selection.ClearEffect);
        }
    }
}

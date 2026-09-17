using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>Fully resolved cosmetic selection: one asset per presentation surface.</summary>
    public readonly struct ThemeSelection
    {
        public readonly BallThemeSO Ball;
        public readonly BoardThemeSO Board;
        public readonly UiThemeSO Ui;
        public readonly ClearEffectSO ClearEffect;

        public ThemeSelection(BallThemeSO ball, BoardThemeSO board, UiThemeSO ui, ClearEffectSO clearEffect)
        {
            Ball = ball;
            Board = board;
            Ui = ui;
            ClearEffect = clearEffect;
        }
    }
}

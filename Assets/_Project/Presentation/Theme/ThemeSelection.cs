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

        public static implicit operator Line98.Data.ThemeSelection(ThemeSelection s)
            => new Line98.Data.ThemeSelection(s.Ball, s.Board, s.Ui, s.ClearEffect);

        public static implicit operator ThemeSelection(Line98.Data.ThemeSelection s)
            => new ThemeSelection(s.Ball, s.Board, s.Ui, s.ClearEffect);
    }
}

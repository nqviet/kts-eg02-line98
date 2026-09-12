using System;

namespace Line98.Core
{
    /// <summary>
    /// Serializable memento capturing full game state.
    /// Used by UndoService and SaveService. Captures exact RNG state to guarantee
    /// non-visual real undo per GDD §12.
    /// </summary>
    [Serializable]
    public sealed class GameSnapshot
    {
        public byte[] BoardCells { get; set; }
        public BallColor[] PreviewQueue { get; set; }
        public XorShift128 RngState { get; set; }
        public int Score { get; set; }
        public int MoveCount { get; set; }
        public int LinesCleared { get; set; }
        public int SelectedIndex { get; set; } = -1;

        public GameSnapshot()
        {
            BoardCells = new byte[BoardModel.CellCount];
            PreviewQueue = new BallColor[3];
        }

        public static GameSnapshot Capture(
            BoardModel board,
            PreviewQueue previewQueue,
            in XorShift128 rng,
            int score,
            int moveCount,
            int linesCleared,
            int selectedIndex = -1)
        {
            var snapshot = new GameSnapshot
            {
                BoardCells = board.ExportCells(),
                PreviewQueue = previewQueue.ToArray(),
                RngState = rng,
                Score = score,
                MoveCount = moveCount,
                LinesCleared = linesCleared,
                SelectedIndex = selectedIndex
            };
            return snapshot;
        }

        public void RestoreTo(
            BoardModel board,
            PreviewQueue previewQueue,
            out XorShift128 rng,
            out int score,
            out int moveCount,
            out int linesCleared,
            out int selectedIndex)
        {
            board.ImportCells(BoardCells);
            previewQueue.CopyFrom(PreviewQueue);
            rng = RngState;
            score = Score;
            moveCount = MoveCount;
            linesCleared = LinesCleared;
            selectedIndex = SelectedIndex;
        }
    }
}

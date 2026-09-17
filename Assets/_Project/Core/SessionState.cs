namespace Line98.Core
{
    public readonly struct SessionState
    {
        public readonly byte[] BoardCells;     // 81
        public readonly BallColor[] Preview;   // 3
        public readonly XorShift128 Rng;
        public readonly int Score;
        public readonly int MoveCount;
        public readonly int LinesCleared;
        public readonly int LongestLine;
        public readonly int FreeUndosRemaining;
        public readonly int RewardedUndosUsed;
        public readonly int SelectedIndex;     // -1 => none
        public readonly GamePhase Phase;
        public readonly string ModeId;
        public readonly string DailySeedDate;
        public readonly int DailySeedVersion;

        public SessionState(
            byte[] boardCells,
            BallColor[] preview,
            in XorShift128 rng,
            int score,
            int moveCount,
            int linesCleared,
            int longestLine,
            int freeUndosRemaining,
            int rewardedUndosUsed,
            int selectedIndex,
            GamePhase phase,
            string modeId,
            string dailySeedDate = null,
            int dailySeedVersion = 1)
        {
            BoardCells = boardCells;
            Preview = preview;
            Rng = rng;
            Score = score;
            MoveCount = moveCount;
            LinesCleared = linesCleared;
            LongestLine = longestLine;
            FreeUndosRemaining = freeUndosRemaining;
            RewardedUndosUsed = rewardedUndosUsed;
            SelectedIndex = selectedIndex;
            Phase = phase;
            ModeId = modeId;
            DailySeedDate = dailySeedDate;
            DailySeedVersion = dailySeedVersion;
        }
    }
}

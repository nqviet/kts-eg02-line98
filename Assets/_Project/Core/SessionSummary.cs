using System;

namespace Line98.Core
{
    /// <summary>
    /// Immutable payload carrying end-of-session statistics per GDD [P1.7].
    /// </summary>
    [Serializable]
    public readonly struct SessionSummary
    {
        public int FinalScore { get; }
        public int BestScore { get; }
        public int LongestLine { get; }
        public int LinesCleared { get; }
        public int TotalMoves { get; }
        public bool CanContinue { get; }

        public SessionSummary(
            int finalScore,
            int bestScore,
            int longestLine,
            int linesCleared,
            int totalMoves,
            bool canContinue)
        {
            FinalScore = finalScore;
            BestScore = bestScore;
            LongestLine = longestLine;
            LinesCleared = linesCleared;
            TotalMoves = totalMoves;
            CanContinue = canContinue;
        }
    }
}

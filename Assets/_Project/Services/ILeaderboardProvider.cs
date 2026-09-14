using System;

namespace Line98.Services
{
    /// <summary>
    /// Frozen interface contract for leaderboard operations per GDD P0.1 / P3.5.
    /// In V1, operates as a NullLeaderboard null object fallback without blocking offline play.
    /// </summary>
    public interface ILeaderboardProvider
    {
        bool IsAvailable { get; }
        void SubmitScore(string leaderboardId, int score, Action<bool> onComplete);
    }

    public sealed class NullLeaderboard : ILeaderboardProvider
    {
        public bool IsAvailable => false;

        public void SubmitScore(string leaderboardId, int score, Action<bool> onComplete)
        {
            onComplete?.Invoke(false);
        }
    }
}

using System;
using System.Collections.Generic;

namespace Line98.Services
{
    public interface IAchievementService
    {
        event Action<string> OnAchievementUnlocked;
        bool IsUnlocked(string achievementId);
        bool TryUnlock(string achievementId);
        void CheckScore(int score);
        void CheckLine(int length);
        void CheckMoves(int moves);
        void CheckStreak(int streak);
    }

    public sealed class AchievementService : IAchievementService
    {
        private readonly HashSet<string> m_UnlockedAchievements = new HashSet<string>();

        public event Action<string> OnAchievementUnlocked;

        public bool IsUnlocked(string achievementId) => m_UnlockedAchievements.Contains(achievementId);

        public bool TryUnlock(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId) || m_UnlockedAchievements.Contains(achievementId))
            {
                return false;
            }

            m_UnlockedAchievements.Add(achievementId);
            OnAchievementUnlocked?.Invoke(achievementId);
            return true;
        }

        public void CheckScore(int score)
        {
            if (score >= 1000) TryUnlock("score_1000");
            if (score >= 10000) TryUnlock("score_10000");
            if (score >= 50000) TryUnlock("score_50000");
        }

        public void CheckLine(int length)
        {
            if (length >= 5) TryUnlock("first_line");
            if (length >= 7) TryUnlock("long_shot_7");
            if (length >= 9) TryUnlock("perfect_9");
        }

        public void CheckMoves(int moves)
        {
            if (moves >= 100) TryUnlock("moves_100");
        }

        public void CheckStreak(int streak)
        {
            if (streak >= 7) TryUnlock("streak_7");
            if (streak >= 30) TryUnlock("streak_30");
        }
    }
}

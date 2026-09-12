using System;

namespace Line98.Services
{
    [Serializable]
    public sealed class PlayerStats
    {
        public int GamesPlayed;
        public int GamesCompleted;
        public int BestScore;
        public int TotalScore;
        public int TotalLinesCleared;
        public int LongestLine;
        public int TotalMoves;
        public int HighestCombo;
        public int CurrentDailyStreak;
        public int LongestDailyStreak;
    }

    public sealed class StatisticsService
    {
        private readonly PlayerStats m_Stats;

        public PlayerStats Stats => m_Stats;

        public StatisticsService(PlayerStats stats = null)
        {
            m_Stats = stats ?? new PlayerStats();
        }

        public void RecordMove()
        {
            m_Stats.TotalMoves++;
        }

        public void RecordGameStart()
        {
            m_Stats.GamesPlayed++;
        }

        public void RecordGameOver(int finalScore)
        {
            m_Stats.GamesCompleted++;
            m_Stats.TotalScore += finalScore;
            if (finalScore > m_Stats.BestScore)
            {
                m_Stats.BestScore = finalScore;
            }
        }

        public void RecordLineClear(int lineLength, int comboCount, int scoreEarned)
        {
            m_Stats.TotalLinesCleared++;
            if (lineLength > m_Stats.LongestLine)
            {
                m_Stats.LongestLine = lineLength;
            }
            if (comboCount > m_Stats.HighestCombo)
            {
                m_Stats.HighestCombo = comboCount;
            }
        }
    }
}

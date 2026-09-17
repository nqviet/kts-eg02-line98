using System;
using Line98.Core;

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

        public int AverageScore => GamesCompleted > 0 ? TotalScore / GamesCompleted : 0;

        public PlayerStats Clone()
        {
            return new PlayerStats
            {
                GamesPlayed = this.GamesPlayed,
                GamesCompleted = this.GamesCompleted,
                BestScore = this.BestScore,
                TotalScore = this.TotalScore,
                TotalLinesCleared = this.TotalLinesCleared,
                LongestLine = this.LongestLine,
                TotalMoves = this.TotalMoves,
                HighestCombo = this.HighestCombo,
                CurrentDailyStreak = this.CurrentDailyStreak,
                LongestDailyStreak = this.LongestDailyStreak
            };
        }
    }

    public readonly struct StatisticsReadModel
    {
        // Pinned display set (7 cards in Your Progress popup mockup)
        public readonly int BestScore;
        public readonly int GamesPlayed;
        public readonly int TotalScore;
        public readonly int TotalLinesCleared;
        public readonly int LongestLine;
        public readonly int HighestCombo;
        public readonly int CurrentStreak;

        // Retained per GDD §18 but not shown in 7-card mockup
        public readonly int GamesCompleted;
        public readonly int TotalMoves;
        public readonly int AverageScore;
        public readonly int LongestStreak;

        public StatisticsReadModel(
            int bestScore,
            int gamesPlayed,
            int totalScore,
            int totalLinesCleared,
            int longestLine,
            int highestCombo,
            int currentStreak,
            int gamesCompleted,
            int totalMoves,
            int averageScore,
            int longestStreak)
        {
            BestScore = bestScore;
            GamesPlayed = gamesPlayed;
            TotalScore = totalScore;
            TotalLinesCleared = totalLinesCleared;
            LongestLine = longestLine;
            HighestCombo = highestCombo;
            CurrentStreak = currentStreak;
            GamesCompleted = gamesCompleted;
            TotalMoves = totalMoves;
            AverageScore = averageScore;
            LongestStreak = longestStreak;
        }
    }

    public sealed class StatisticsService
    {
        private readonly PlayerStats m_Stats;

        public PlayerStats Stats => m_Stats;

        public int BestScore => m_Stats.BestScore;
        public int GamesPlayed => m_Stats.GamesPlayed;
        public int GamesCompleted => m_Stats.GamesCompleted;
        public int TotalScore => m_Stats.TotalScore;
        public int TotalLinesCleared => m_Stats.TotalLinesCleared;
        public int LongestLine => m_Stats.LongestLine;
        public int TotalMoves => m_Stats.TotalMoves;
        public int HighestCombo => m_Stats.HighestCombo;
        public int CurrentStreak => m_Stats.CurrentDailyStreak;
        public int LongestStreak => m_Stats.LongestDailyStreak;
        public int AverageScore => m_Stats.AverageScore;

        public StatisticsService(PlayerStats stats = null)
        {
            m_Stats = stats != null ? stats.Clone() : new PlayerStats();
        }

        public StatsSave Snapshot()
        {
            return new StatsSave
            {
                GamesPlayed = m_Stats.GamesPlayed,
                GamesCompleted = m_Stats.GamesCompleted,
                BestScore = m_Stats.BestScore,
                TotalScore = m_Stats.TotalScore,
                TotalLinesCleared = m_Stats.TotalLinesCleared,
                LongestLine = m_Stats.LongestLine,
                TotalMoves = m_Stats.TotalMoves,
                HighestCombo = m_Stats.HighestCombo,
                CurrentStreak = m_Stats.CurrentDailyStreak,
                LongestStreak = m_Stats.LongestDailyStreak
            };
        }

        public void LoadFrom(PlayerStats stats)
        {
            if (stats == null) return;
            m_Stats.GamesPlayed = stats.GamesPlayed;
            m_Stats.GamesCompleted = stats.GamesCompleted;
            m_Stats.BestScore = stats.BestScore;
            m_Stats.TotalScore = stats.TotalScore;
            m_Stats.TotalLinesCleared = stats.TotalLinesCleared;
            m_Stats.LongestLine = stats.LongestLine;
            m_Stats.TotalMoves = stats.TotalMoves;
            m_Stats.HighestCombo = stats.HighestCombo;
            m_Stats.CurrentDailyStreak = stats.CurrentDailyStreak;
            m_Stats.LongestDailyStreak = stats.LongestDailyStreak;
        }

        public void LoadFrom(StatsSave save)
        {
            if (save == null) return;
            m_Stats.GamesPlayed = save.GamesPlayed;
            m_Stats.GamesCompleted = save.GamesCompleted;
            m_Stats.BestScore = save.BestScore;
            m_Stats.TotalScore = save.TotalScore;
            m_Stats.TotalLinesCleared = save.TotalLinesCleared;
            m_Stats.LongestLine = save.LongestLine;
            m_Stats.TotalMoves = save.TotalMoves;
            m_Stats.HighestCombo = save.HighestCombo;
            m_Stats.CurrentDailyStreak = save.CurrentStreak;
            m_Stats.LongestDailyStreak = save.LongestStreak;
        }

        public void RecordMove() => m_Stats.TotalMoves++;

        public void RecordScore(int score)
        {
            if (score > m_Stats.BestScore) m_Stats.BestScore = score;
        }

        public void RecordLineClear(int longestRun, int runCount, int scoreDelta)
        {
            m_Stats.TotalLinesCleared += runCount;
            if (longestRun > m_Stats.LongestLine) m_Stats.LongestLine = longestRun;
            int combo = Math.Max(0, runCount - 1);
            if (combo > m_Stats.HighestCombo) m_Stats.HighestCombo = combo;
        }

        public void RecordGameOver(int finalScore, int longestLine = 0)
        {
            m_Stats.GamesCompleted++;
            m_Stats.TotalScore += finalScore;
            if (finalScore > m_Stats.BestScore) m_Stats.BestScore = finalScore;
            if (longestLine > m_Stats.LongestLine) m_Stats.LongestLine = longestLine;
        }

        public void RecordDailyCompletion()
        {
        }

        public void Reset()
        {
            m_Stats.GamesPlayed = 0;
            m_Stats.GamesCompleted = 0;
            m_Stats.BestScore = 0;
            m_Stats.TotalScore = 0;
            m_Stats.TotalLinesCleared = 0;
            m_Stats.LongestLine = 0;
            m_Stats.TotalMoves = 0;
            m_Stats.HighestCombo = 0;
            m_Stats.CurrentDailyStreak = 0;
            m_Stats.LongestDailyStreak = 0;
        }

        public ProgressMetrics BuildMetrics(in SessionContext live, int derivedCurrentStreak = 0, int derivedLongestStreak = 0)
        {
            int streak = derivedCurrentStreak > 0 ? derivedCurrentStreak : m_Stats.CurrentDailyStreak;
            int longest = derivedLongestStreak > 0 ? derivedLongestStreak : m_Stats.LongestDailyStreak;

            return new ProgressMetrics(
                careerBestScore: m_Stats.BestScore,
                careerTotalScore: m_Stats.TotalScore,
                careerGamesPlayed: m_Stats.GamesPlayed,
                careerGamesCompleted: m_Stats.GamesCompleted,
                careerLinesCleared: m_Stats.TotalLinesCleared,
                careerLongestLine: m_Stats.LongestLine,
                careerHighestCombo: m_Stats.HighestCombo,
                careerMoves: m_Stats.TotalMoves,
                bestSessionScore: live.BestSessionScore,
                bestSessionLineLength: live.BestSessionLineLength,
                bestSessionCombo: live.BestSessionCombo,
                bestSessionMoves: live.BestSessionMoves,
                currentStreak: streak,
                longestStreak: longest,
                liveScore: live.LiveScore,
                liveMoves: live.LiveMoves,
                liveLineLength: live.LiveLineLength);
        }

        public StatisticsReadModel BuildReadModel(in SessionContext live, int derivedCurrentStreak = 0)
        {
            int streak = derivedCurrentStreak > 0 ? derivedCurrentStreak : m_Stats.CurrentDailyStreak;
            int best = Math.Max(m_Stats.BestScore, live.LiveScore);

            return new StatisticsReadModel(
                bestScore: best,
                gamesPlayed: m_Stats.GamesPlayed,
                totalScore: m_Stats.TotalScore,
                totalLinesCleared: m_Stats.TotalLinesCleared,
                longestLine: Math.Max(m_Stats.LongestLine, live.LiveLineLength),
                highestCombo: m_Stats.HighestCombo,
                currentStreak: streak,
                gamesCompleted: m_Stats.GamesCompleted,
                totalMoves: m_Stats.TotalMoves,
                averageScore: m_Stats.AverageScore,
                longestStreak: m_Stats.LongestDailyStreak);
        }

        public void RecordGameStart(string modeId = "classic", bool recordsStatistics = true)
        {
            if (!recordsStatistics) return;
            m_Stats.GamesPlayed++;
        }

        public void OnMoveCommitted(in MoveResult result, in SessionContext ctx, bool recordsStatistics = true)
        {
            if (!recordsStatistics) return;

            m_Stats.TotalMoves++;

            if (!result.Cleared.IsEmpty)
            {
                m_Stats.TotalLinesCleared += result.Cleared.RunCount;

                if (result.Cleared.LongestRun > m_Stats.LongestLine)
                {
                    m_Stats.LongestLine = result.Cleared.LongestRun;
                }

                int combo = Math.Max(0, result.Cleared.RunCount - 1);
                if (combo > m_Stats.HighestCombo)
                {
                    m_Stats.HighestCombo = combo;
                }
            }
        }

        public void RecordGameOver(in SessionSummary summary, in SessionContext ctx, bool recordsStatistics = true)
        {
            if (!recordsStatistics) return;

            m_Stats.GamesCompleted++;
            m_Stats.TotalScore += summary.FinalScore;

            if (summary.FinalScore > m_Stats.BestScore)
            {
                m_Stats.BestScore = summary.FinalScore;
            }

            if (summary.LongestLine > m_Stats.LongestLine)
            {
                m_Stats.LongestLine = summary.LongestLine;
            }
        }

        public void RecordDailyCompletion(bool extended, int streak, int longest)
        {
            m_Stats.CurrentDailyStreak = streak;
            if (longest > m_Stats.LongestDailyStreak)
            {
                m_Stats.LongestDailyStreak = longest;
            }
        }
    }
}

using System;

namespace Line98.Core
{
    public readonly struct ProgressMetrics
    {
        // ---- career / high-water (from StatisticsService) ----
        public readonly int CareerBestScore;
        public readonly int CareerTotalScore;
        public readonly int CareerGamesPlayed;
        public readonly int CareerGamesCompleted;
        public readonly int CareerLinesCleared;
        public readonly int CareerLongestLine;
        public readonly int CareerHighestCombo;
        public readonly int CareerMoves;

        // ---- best-session values (for session-scoped achievement progress) ----
        public readonly int BestSessionScore;
        public readonly int BestSessionLineLength;
        public readonly int BestSessionCombo;
        public readonly int BestSessionMoves;

        // ---- streaks (derived from the completion log) ----
        public readonly int CurrentStreak;
        public readonly int LongestStreak;

        // ---- live session (0 when no run is active) ----
        public readonly int LiveScore;
        public readonly int LiveMoves;
        public readonly int LiveLineLength;

        public readonly int BestSessionLinesCleared;
        public readonly int LiveLinesCleared;

        public ProgressMetrics(
            int careerBestScore,
            int careerTotalScore,
            int careerGamesPlayed,
            int careerGamesCompleted,
            int careerLinesCleared,
            int careerLongestLine,
            int careerHighestCombo,
            int careerMoves,
            int bestSessionScore,
            int bestSessionLineLength,
            int bestSessionCombo,
            int bestSessionMoves,
            int currentStreak,
            int longestStreak,
            int liveScore,
            int liveMoves,
            int liveLineLength,
            int bestSessionLinesCleared = 0,
            int liveLinesCleared = 0)
        {
            CareerBestScore = careerBestScore;
            CareerTotalScore = careerTotalScore;
            CareerGamesPlayed = careerGamesPlayed;
            CareerGamesCompleted = careerGamesCompleted;
            CareerLinesCleared = careerLinesCleared;
            CareerLongestLine = careerLongestLine;
            CareerHighestCombo = careerHighestCombo;
            CareerMoves = careerMoves;
            BestSessionScore = bestSessionScore;
            BestSessionLineLength = bestSessionLineLength;
            BestSessionCombo = bestSessionCombo;
            BestSessionMoves = bestSessionMoves;
            CurrentStreak = currentStreak;
            LongestStreak = longestStreak;
            LiveScore = liveScore;
            LiveMoves = liveMoves;
            LiveLineLength = liveLineLength;
            BestSessionLinesCleared = bestSessionLinesCleared;
            LiveLinesCleared = liveLinesCleared;
        }

        public ProgressMetrics(
            int scoreSession,
            int scoreCareer,
            int gamesPlayedCareer,
            int totalScoreCareer,
            int linesClearedSession,
            int linesClearedCareer,
            int longestLineSession,
            int longestLineCareer,
            float comboSession,
            float comboCareer,
            int movesSession,
            int movesCareer,
            int dailyStreakCurrent,
            int dailyStreakCareer)
            : this(
                careerBestScore: scoreCareer,
                careerTotalScore: totalScoreCareer,
                careerGamesPlayed: gamesPlayedCareer,
                careerGamesCompleted: gamesPlayedCareer,
                careerLinesCleared: linesClearedCareer,
                careerLongestLine: longestLineCareer,
                careerHighestCombo: (int)comboCareer,
                careerMoves: movesCareer,
                bestSessionScore: scoreSession,
                bestSessionLineLength: longestLineSession,
                bestSessionCombo: (int)comboSession,
                bestSessionMoves: movesSession,
                currentStreak: dailyStreakCurrent,
                longestStreak: dailyStreakCareer,
                liveScore: scoreSession,
                liveMoves: movesSession,
                liveLineLength: longestLineSession,
                bestSessionLinesCleared: linesClearedSession,
                liveLinesCleared: linesClearedSession)
        {
        }

        public int ValueOf(AchievementMetric metric, AchievementScope scope)
        {
            if (scope == AchievementScope.Career)
            {
                switch (metric)
                {
                    case AchievementMetric.Score: return CareerBestScore > 0 ? CareerBestScore : CareerTotalScore;
                    case AchievementMetric.LineLength: return CareerLongestLine;
                    case AchievementMetric.Combo: return CareerHighestCombo;
                    case AchievementMetric.LinesClearedTotal: return CareerLinesCleared;
                    case AchievementMetric.Moves: return CareerMoves;
                    case AchievementMetric.DailyStreak: return LongestStreak;
                    case AchievementMetric.GamesCompleted: return CareerGamesPlayed > 0 ? CareerGamesPlayed : CareerGamesCompleted;
                    default: return 0;
                }
            }
            else // Session
            {
                switch (metric)
                {
                    case AchievementMetric.Score: return Math.Max(BestSessionScore, LiveScore);
                    case AchievementMetric.LineLength: return Math.Max(BestSessionLineLength, LiveLineLength);
                    case AchievementMetric.Combo: return BestSessionCombo;
                    case AchievementMetric.LinesClearedTotal: return Math.Max(BestSessionLinesCleared, LiveLinesCleared);
                    case AchievementMetric.Moves: return Math.Max(BestSessionMoves, LiveMoves);
                    case AchievementMetric.DailyStreak: return CurrentStreak;
                    case AchievementMetric.GamesCompleted: return CareerGamesCompleted;
                    default: return 0;
                }
            }
        }
    }

    public readonly struct SessionContext
    {
        public readonly int LiveScore;
        public readonly int LiveMoves;
        public readonly int LiveLineLength;
        public readonly int BestSessionScore;
        public readonly int BestSessionLineLength;
        public readonly int BestSessionCombo;
        public readonly int BestSessionMoves;

        public SessionContext(
            int liveScore = 0,
            int liveMoves = 0,
            int liveLineLength = 0,
            int bestSessionScore = 0,
            int bestSessionLineLength = 0,
            int bestSessionCombo = 0,
            int bestSessionMoves = 0)
        {
            LiveScore = liveScore;
            LiveMoves = liveMoves;
            LiveLineLength = liveLineLength;
            BestSessionScore = bestSessionScore;
            BestSessionLineLength = bestSessionLineLength;
            BestSessionCombo = bestSessionCombo;
            BestSessionMoves = bestSessionMoves;
        }

        public static SessionContext Empty => new SessionContext(0, 0, 0, 0, 0, 0, 0);
    }
}

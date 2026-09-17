namespace Line98.Core
{
    public enum AchievementMetric : byte
    {
        Score,
        LineLength,
        Combo,
        LinesClearedTotal,
        Moves,
        DailyStreak,
        GamesCompleted,
        LinesCleared = LinesClearedTotal,
        LongestLine = LineLength,
        GamesPlayed = GamesCompleted
    }

    public enum AchievementScope : byte
    {
        Session,
        Career
    }
}

using Line98.Core;
using Line98.Services;

namespace Line98.App
{
    public readonly struct ProgressReadModel
    {
        public readonly StatisticsReadModel Statistics;
        public readonly AchievementReadModel Achievements;
        public readonly ProgressMetrics Metrics;

        public ProgressReadModel(in StatisticsReadModel statistics, in AchievementReadModel achievements, in ProgressMetrics metrics)
        {
            Statistics = statistics;
            Achievements = achievements;
            Metrics = metrics;
        }
    }
}

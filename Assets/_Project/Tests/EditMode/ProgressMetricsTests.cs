using Line98.Core;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class ProgressMetricsTests
    {
        [Test]
        public void ValueOf_ReturnsCorrectValueForEachMetricAndScope()
        {
            var metrics = new ProgressMetrics(
                scoreSession: 150,
                scoreCareer: 1500,
                gamesPlayedCareer: 5,
                totalScoreCareer: 10000,
                linesClearedSession: 3,
                linesClearedCareer: 30,
                longestLineSession: 5,
                longestLineCareer: 8,
                comboSession: 2f,
                comboCareer: 4f,
                movesSession: 12,
                movesCareer: 120,
                dailyStreakCurrent: 3,
                dailyStreakCareer: 7);

            // Score
            Assert.AreEqual(150, metrics.ValueOf(AchievementMetric.Score, AchievementScope.Session));
            Assert.AreEqual(1500, metrics.ValueOf(AchievementMetric.Score, AchievementScope.Career));

            // Moves
            Assert.AreEqual(12, metrics.ValueOf(AchievementMetric.Moves, AchievementScope.Session));
            Assert.AreEqual(120, metrics.ValueOf(AchievementMetric.Moves, AchievementScope.Career));

            // LinesCleared
            Assert.AreEqual(3, metrics.ValueOf(AchievementMetric.LinesCleared, AchievementScope.Session));
            Assert.AreEqual(30, metrics.ValueOf(AchievementMetric.LinesCleared, AchievementScope.Career));

            // LongestLine
            Assert.AreEqual(5, metrics.ValueOf(AchievementMetric.LongestLine, AchievementScope.Session));
            Assert.AreEqual(8, metrics.ValueOf(AchievementMetric.LongestLine, AchievementScope.Career));

            // Combo
            Assert.AreEqual(2, metrics.ValueOf(AchievementMetric.Combo, AchievementScope.Session));
            Assert.AreEqual(4, metrics.ValueOf(AchievementMetric.Combo, AchievementScope.Career));

            // DailyStreak
            Assert.AreEqual(3, metrics.ValueOf(AchievementMetric.DailyStreak, AchievementScope.Session));
            Assert.AreEqual(7, metrics.ValueOf(AchievementMetric.DailyStreak, AchievementScope.Career));

            // GamesPlayed
            Assert.AreEqual(5, metrics.ValueOf(AchievementMetric.GamesPlayed, AchievementScope.Career));
        }
    }
}

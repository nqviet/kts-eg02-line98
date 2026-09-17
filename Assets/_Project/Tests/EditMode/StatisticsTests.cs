using Line98.Core;
using Line98.Gameplay;
using Line98.Services;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class StatisticsTests
    {
        [Test]
        public void ReadModel_DisplaysSevenCoreMockupStats()
        {
            var stats = new StatisticsService();
            stats.RecordGameStart();
            stats.RecordScore(1200);
            stats.RecordLineClear(longestRun: 6, runCount: 2, scoreDelta: 1200);
            stats.RecordMove();
            stats.RecordMove();
            stats.RecordGameOver(1200);

            var readModel = stats.BuildReadModel(SessionContext.Empty, derivedCurrentStreak: 5);

            // 7 displayed stats
            Assert.AreEqual(1200, readModel.BestScore);
            Assert.AreEqual(1, readModel.GamesPlayed);
            Assert.AreEqual(1200, readModel.TotalScore);
            Assert.AreEqual(2, readModel.TotalLinesCleared);
            Assert.AreEqual(6, readModel.LongestLine);
            Assert.AreEqual(1, readModel.HighestCombo);
            Assert.AreEqual(5, readModel.CurrentStreak);

            // Hidden but tracked stats
            Assert.AreEqual(1, readModel.GamesCompleted);
            Assert.AreEqual(2, readModel.TotalMoves);
            Assert.AreEqual(1200, readModel.AverageScore);
        }

        [Test]
        public void AverageScore_IsDerivedCorrectly()
        {
            var stats = new StatisticsService();

            // Game 1: 1000
            stats.RecordGameStart();
            stats.RecordGameOver(1000);

            // Game 2: 3000
            stats.RecordGameStart();
            stats.RecordGameOver(3000);

            Assert.AreEqual(2, stats.GamesPlayed);
            Assert.AreEqual(4000, stats.TotalScore);
            Assert.AreEqual(2000, stats.AverageScore);
        }

        [Test]
        public void ZenMode_DoesNotRecordCareerStats()
        {
            var stats = new StatisticsService();
            var zen = new ZenMode();

            Assert.IsFalse(zen.RecordsStatistics);

            // When a mode has RecordsStatistics == false, callers suppress recording
            int initialGames = stats.GamesPlayed;
            int initialBest = stats.BestScore;

            if (zen.RecordsStatistics)
            {
                stats.RecordGameStart();
                stats.RecordScore(9999);
                stats.RecordGameOver(9999);
            }

            Assert.AreEqual(initialGames, stats.GamesPlayed);
            Assert.AreEqual(initialBest, stats.BestScore);
        }

        [Test]
        public void Snapshot_IsDetachedAndRoundTrips()
        {
            var stats = new StatisticsService();
            stats.RecordGameStart();
            stats.RecordScore(500);
            stats.RecordLineClear(5, 1, 500);
            stats.RecordGameOver(500);

            StatsSave save = stats.Snapshot();
            Assert.AreEqual(500, save.BestScore);
            Assert.AreEqual(1, save.GamesPlayed);
            Assert.AreEqual(1, save.GamesCompleted);
            Assert.AreEqual(500, save.TotalScore);

            var restored = new StatisticsService();
            restored.LoadFrom(save);

            Assert.AreEqual(500, restored.BestScore);
            Assert.AreEqual(1, restored.GamesPlayed);
            Assert.AreEqual(1, restored.GamesCompleted);
            Assert.AreEqual(500, restored.TotalScore);
        }
    }
}

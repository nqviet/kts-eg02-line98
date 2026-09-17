using System.Collections.Generic;
using Line98.Core;
using Line98.Data;
using Line98.Services;
using NUnit.Framework;
using UnityEngine;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class AchievementServiceTests
    {
        private AchievementCatalogSO m_Catalog;

        [SetUp]
        public void SetUp()
        {
            m_Catalog = ScriptableObject.CreateInstance<AchievementCatalogSO>();

            var items = new List<AchievementSO>
            {
                CreateAchievement("first_line", AchievementMetric.LinesCleared, 1, AchievementScope.Career),
                CreateAchievement("first_5_line", AchievementMetric.LongestLine, 5, AchievementScope.Session),
                CreateAchievement("long_shot_7", AchievementMetric.LongestLine, 7, AchievementScope.Session),
                CreateAchievement("perfect_9", AchievementMetric.LongestLine, 9, AchievementScope.Session),
                CreateAchievement("score_1000", AchievementMetric.Score, 1000, AchievementScope.Session),
                CreateAchievement("score_10000", AchievementMetric.Score, 10000, AchievementScope.Session),
                CreateAchievement("score_50000", AchievementMetric.Score, 50000, AchievementScope.Session),
                CreateAchievement("moves_100", AchievementMetric.Moves, 100, AchievementScope.Career),
                CreateAchievement("streak_7", AchievementMetric.DailyStreak, 7, AchievementScope.Career),
                CreateAchievement("streak_30", AchievementMetric.DailyStreak, 30, AchievementScope.Career)
            };

            m_Catalog.InitializeRuntime(items);
        }

        private static AchievementSO CreateAchievement(string id, AchievementMetric metric, int threshold, AchievementScope scope)
        {
            var so = ScriptableObject.CreateInstance<AchievementSO>();
            so.InitializeRuntime(id, metric, scope, threshold, id, id);
            return so;
        }

        [Test]
        public void UnlockAchievement_EmitsEventAndPersists()
        {
            var service = new AchievementService(m_Catalog);
            string unlockedId = null;
            service.OnAchievementUnlocked += id => unlockedId = id;

            bool result = service.TryUnlock("score_1000");

            Assert.IsTrue(result);
            Assert.AreEqual("score_1000", unlockedId);
            Assert.IsTrue(service.IsUnlocked("score_1000"));

            // Duplicate unlock should return false and not fire event again
            unlockedId = null;
            bool duplicate = service.TryUnlock("score_1000");
            Assert.IsFalse(duplicate);
            Assert.IsNull(unlockedId);
        }

        [Test]
        public void EvaluateAll_UnlocksMatchingThresholds()
        {
            var service = new AchievementService(m_Catalog);
            var unlockedEvents = new List<string>();
            service.OnAchievementUnlocked += id => unlockedEvents.Add(id);

            // Metrics: Score 1500, LinesCleared 2, LongestLine 5
            var metrics = new ProgressMetrics(
                scoreSession: 1500,
                scoreCareer: 1500,
                gamesPlayedCareer: 1,
                totalScoreCareer: 1500,
                linesClearedSession: 2,
                linesClearedCareer: 2,
                longestLineSession: 5,
                longestLineCareer: 5,
                comboSession: 1f,
                comboCareer: 1f,
                movesSession: 20,
                movesCareer: 20,
                dailyStreakCurrent: 0,
                dailyStreakCareer: 0);

            var report = service.EvaluateAll(in metrics);

            // Should unlock: first_line (thresh 1), first_5_line (thresh 5), score_1000 (thresh 1000)
            Assert.AreEqual(3, report.Count);
            CollectionAssert.Contains(unlockedEvents, "first_line");
            CollectionAssert.Contains(unlockedEvents, "first_5_line");
            CollectionAssert.Contains(unlockedEvents, "score_1000");
        }

        [Test]
        public void BuildReadModel_CalculatesRatioAndOrderedProgress()
        {
            var service = new AchievementService(m_Catalog);
            service.TryUnlock("first_line");
            service.TryUnlock("score_1000");

            var metrics = new ProgressMetrics(
                scoreSession: 2000,
                scoreCareer: 2000,
                gamesPlayedCareer: 1,
                totalScoreCareer: 2000,
                linesClearedSession: 3,
                linesClearedCareer: 3,
                longestLineSession: 5,
                longestLineCareer: 5,
                comboSession: 1f,
                comboCareer: 1f,
                movesSession: 10,
                movesCareer: 10,
                dailyStreakCurrent: 5,
                dailyStreakCareer: 5);

            AchievementReadModel readModel = service.BuildReadModel(in metrics, featuredCount: 3);

            Assert.AreEqual(2, readModel.Unlocked);
            Assert.AreEqual(10, readModel.Total);
            Assert.AreEqual(0.2f, readModel.Ratio, 0.001f);
            Assert.AreEqual(3, readModel.FeaturedCount);
            Assert.AreEqual(10, readModel.Ordered.Length);

            // First items in Ordered should be the unlocked ones (most recent first)
            Assert.IsTrue(readModel.Ordered[0].Unlocked);
            Assert.AreEqual("score_1000", readModel.Ordered[0].Id);
            Assert.IsTrue(readModel.Ordered[1].Unlocked);
            Assert.AreEqual("first_line", readModel.Ordered[1].Id);

            // Subsequent items should be locked, sorted by highest normalized progress
            Assert.IsFalse(readModel.Ordered[2].Unlocked);
        }

        [Test]
        public void Unlocks_PersistAcrossSaveLoadRoundTrip()
        {
            var service1 = new AchievementService(m_Catalog);
            service1.TryUnlock("first_line");
            service1.TryUnlock("moves_100");

            List<string> savedIds = service1.SaveState();
            CollectionAssert.AreEquivalent(new[] { "first_line", "moves_100" }, savedIds);

            var service2 = new AchievementService(m_Catalog);
            service2.LoadState(savedIds);

            Assert.IsTrue(service2.IsUnlocked("first_line"));
            Assert.IsTrue(service2.IsUnlocked("moves_100"));
            Assert.IsFalse(service2.IsUnlocked("score_1000"));
        }
    }
}

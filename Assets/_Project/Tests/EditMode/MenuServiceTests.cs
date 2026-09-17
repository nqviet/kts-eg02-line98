using Line98.App;
using Line98.Services;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class MenuServiceTests
    {
        [Test]
        public void MenuSnapshot_ReflectsStatsAndDaily()
        {
            var stats = new StatisticsService();
            stats.RecordGameStart();
            stats.RecordGameOver(2500);

            var daily = new DailyChallengeService();
            var menuService = new MenuService(stats, daily);

            MenuSnapshot snapshot = menuService.GetSnapshot();
            Assert.AreEqual(2500, snapshot.BestScore);
            Assert.AreEqual(0, snapshot.CurrentDailyStreak);
            Assert.IsFalse(snapshot.DailyCompletedToday);
            Assert.IsFalse(snapshot.HasResumableGame);
        }

        [Test]
        public void MenuSnapshot_ResumeOfferedOnlyWhenDecisionIsOffer()
        {
            var menuService = new MenuService(null, null);

            // Default: None
            Assert.IsFalse(menuService.GetSnapshot().HasResumableGame);

            // Set Offer
            menuService.SetResumeDecision(ResumeDecision.Offer("Classic", 1200, 30));
            MenuSnapshot offerSnapshot = menuService.GetSnapshot();
            Assert.IsTrue(offerSnapshot.HasResumableGame);
            Assert.AreEqual("Classic", offerSnapshot.ResumeModeId);
            Assert.AreEqual(1200, offerSnapshot.ResumeScore);
            Assert.AreEqual(30, offerSnapshot.ResumeMoveCount);

            // Set Expired
            menuService.SetResumeDecision(ResumeDecision.Expired("daily", "2026-09-16"));
            MenuSnapshot expiredSnapshot = menuService.GetSnapshot();
            Assert.IsFalse(expiredSnapshot.HasResumableGame);
        }

        [Test]
        public void SetResumeDecision_FiresOnMenuStateChanged()
        {
            var menuService = new MenuService(null, null);
            bool fired = false;
            menuService.OnMenuStateChanged += () => fired = true;

            menuService.SetResumeDecision(ResumeDecision.Offer("Classic", 100, 5));
            Assert.IsTrue(fired);
        }
    }
}

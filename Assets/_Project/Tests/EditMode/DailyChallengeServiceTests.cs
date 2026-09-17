using System;
using Line98.Core;
using Line98.Gameplay;
using Line98.Services;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class DailyChallengeServiceTests
    {
        [Test]
        public void SameDate_ProducesIdenticalSeed()
        {
            var mode1 = new DailyChallengeMode("2026-09-13");
            var mode2 = new DailyChallengeMode("2026-09-13");

            Assert.AreEqual(mode1.GenerateSeed(), mode2.GenerateSeed());
        }

        [Test]
        public void DifferentDate_ProducesDifferentSeed()
        {
            var mode1 = new DailyChallengeMode("2026-09-13");
            var mode2 = new DailyChallengeMode("2026-09-14");

            Assert.AreNotEqual(mode1.GenerateSeed(), mode2.GenerateSeed());
        }

        [Test]
        public void PreviewParity_MatchesInitialLayoutBuilder()
        {
            var fixedDate = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
            var dateProvider = new FixedDateProvider(fixedDate, seedVersion: 1);
            var dailyService = new DailyChallengeService(provider: dateProvider);

            DailyPreview preview = dailyService.GetPreview();
            Assert.IsNotNull(preview.BoardCells);
            Assert.AreEqual(BoardModel.CellCount, preview.BoardCells.Length);
            Assert.IsNotNull(preview.PreviewQueue);
            Assert.AreEqual(3, preview.PreviewQueue.Length);

            // Compare with InitialLayoutBuilder using the exact same seed
            uint seed = dailyService.TodaySeed;
            var expectedBoard = new BoardModel();
            var expectedQueue = new PreviewQueue(3);
            InitialLayoutBuilder.Build(seed, SpawnRules.Default, expectedBoard, expectedQueue, 3);

            CollectionAssert.AreEqual(expectedBoard.ExportCells(), preview.BoardCells);
            CollectionAssert.AreEqual(expectedQueue.ToArray(), preview.PreviewQueue);
        }

        [Test]
        public void WeekStrip_MondayFirst_HasCorrectSevenCells()
        {
            // 2026-09-17 is a Thursday
            var fixedDate = new DateTime(2026, 9, 17, 10, 0, 0, DateTimeKind.Utc);
            var dateProvider = new FixedDateProvider(fixedDate);
            var dailyService = new DailyChallengeService(provider: dateProvider);

            DailyReadModel readModel = dailyService.BuildReadModel();
            Assert.AreEqual(7, readModel.Week.Length);
            Assert.AreEqual(DayOfWeek.Monday, readModel.WeekStart);

            // Check day order: Mon, Tue, Wed, Thu, Fri, Sat, Sun
            Assert.AreEqual(DayOfWeek.Monday, readModel.Week[0].DayOfWeek);
            Assert.AreEqual(DayOfWeek.Tuesday, readModel.Week[1].DayOfWeek);
            Assert.AreEqual(DayOfWeek.Wednesday, readModel.Week[2].DayOfWeek);
            Assert.AreEqual(DayOfWeek.Thursday, readModel.Week[3].DayOfWeek);
            Assert.AreEqual(DayOfWeek.Friday, readModel.Week[4].DayOfWeek);
            Assert.AreEqual(DayOfWeek.Saturday, readModel.Week[5].DayOfWeek);
            Assert.AreEqual(DayOfWeek.Sunday, readModel.Week[6].DayOfWeek);

            // Thursday is Today (not yet completed)
            Assert.IsTrue(readModel.Week[3].IsToday);
            Assert.AreEqual(DailyCellState.TodayUnplayed, readModel.Week[3].State);

            // Friday, Saturday, Sunday are in Future
            Assert.AreEqual(DailyCellState.Future, readModel.Week[4].State);
            Assert.AreEqual(DailyCellState.Future, readModel.Week[5].State);
            Assert.AreEqual(DailyCellState.Future, readModel.Week[6].State);
        }

        [Test]
        public void StreakFormula_ConsecutiveDays_ExtendsStreak()
        {
            var day1 = new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc);
            var provider = new FixedDateProvider(day1);
            var dailySave = new DailySave();
            var dailyService = new DailyChallengeService(provider: provider, save: dailySave);

            // Day 1 completion
            var res1 = dailyService.CompleteToday(1000, 5, 20);
            Assert.AreEqual(1, res1.CurrentStreak);
            Assert.AreEqual(1, dailyService.CurrentStreak);

            // Advance to Day 2 (gap == 1)
            provider.SetUtcNow(new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc));
            var res2 = dailyService.CompleteToday(1500, 7, 25);
            Assert.AreEqual(2, res2.CurrentStreak);
            Assert.AreEqual(2, dailyService.CurrentStreak);
            Assert.IsTrue(res2.StreakExtended);
        }

        [Test]
        public void StreakFormula_GapGreaterOrEqualThree_ResetsStreak()
        {
            var day1 = new DateTime(2026, 9, 10, 12, 0, 0, DateTimeKind.Utc);
            var provider = new FixedDateProvider(day1);
            var dailySave = new DailySave();
            var dailyService = new DailyChallengeService(provider: provider, save: dailySave);

            dailyService.CompleteToday(1000, 5, 20);
            Assert.AreEqual(1, dailyService.CurrentStreak);

            // Advance by 3 days (gap >= 3)
            provider.SetUtcNow(new DateTime(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc));
            var res = dailyService.CompleteToday(2000, 8, 30);
            Assert.AreEqual(1, res.CurrentStreak);
            Assert.AreEqual(1, dailyService.CurrentStreak);
        }

        [Test]
        public void OneRunPolicy_DeniesSecondScoredRun_AllowsPracticeWithoutOverwrite()
        {
            var fixedDate = new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Utc);
            var provider = new FixedDateProvider(fixedDate);
            var dailySave = new DailySave();
            var dailyService = new DailyChallengeService(provider: provider, save: dailySave);

            // First run is scored
            var result1 = dailyService.CompleteToday(5000, 10, 40, scoredRun: true);
            Assert.IsTrue(result1.FirstCompletionToday);
            Assert.AreEqual(1, result1.CurrentStreak);
            Assert.AreEqual(5000, dailyService.TodayScore);

            // Second run on same day (practice run)
            var result2 = dailyService.CompleteToday(9999, 20, 60, scoredRun: false);
            Assert.IsFalse(result2.FirstCompletionToday);
            Assert.IsFalse(result2.StreakExtended);
            Assert.AreEqual(5000, dailyService.TodayScore, "Practice run must not overwrite today's score");
            Assert.AreEqual(1, dailyService.CurrentStreak, "Practice run must not advance streak");
        }
    }
}

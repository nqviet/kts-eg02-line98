using System.Collections.Generic;
using Line98.Core;
using Line98.Data;
using Line98.Gameplay;
using Line98.Presentation.Animation;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class FeedbackTierSelectionTests
    {
        private FeedbackRules m_Rules;
        private BoardAnimator m_BoardAnimator;

        [SetUp]
        public void SetUp()
        {
            m_Rules = FeedbackRules.Default;
            // BoardAnimator with null dependencies for headless evaluation
            m_BoardAnimator = new BoardAnimator(null, null, null, null, m_Rules);
        }

        [Test]
        public void Single5Run_SelectsTier1()
        {
            var positions = new[]
            {
                new GridPos(0, 0), new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0), new GridPos(4, 0)
            };
            var group = new ClearGroup(positions, longestRun: 5, runCount: 1, BallColor.Red);

            AssertTier(group, expectedTier: 1);
        }

        [Test]
        public void FivePlusFiveCross_9DedupedCells_SelectsTier1()
        {
            // Cross centered at (2, 2) sharing one cell:
            // H run: (0,2), (1,2), (2,2), (3,2), (4,2) -> length 5
            // V run: (2,0), (2,1), (2,2), (2,3), (2,4) -> length 5
            // Total deduped positions = 9 cells, but longestRun is 5!
            var positions = new[]
            {
                new GridPos(0, 2), new GridPos(1, 2), new GridPos(2, 2), new GridPos(3, 2), new GridPos(4, 2),
                new GridPos(2, 0), new GridPos(2, 1), new GridPos(2, 3), new GridPos(2, 4)
            };
            var group = new ClearGroup(positions, longestRun: 5, runCount: 2, BallColor.Red);

            // GDD §8 mandate: tier is based on line length (longest run), not cell count.
            // 9 deduped cells must NOT trigger Tier 4 (Perfect Line) because neither line has length >= 9.
            AssertTier(group, expectedTier: 1);
        }

        [Test]
        public void Single7Run_SelectsTier2()
        {
            var positions = new[]
            {
                new GridPos(0, 0), new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0),
                new GridPos(4, 0), new GridPos(5, 0), new GridPos(6, 0)
            };
            var group = new ClearGroup(positions, longestRun: 7, runCount: 1, BallColor.Red);

            AssertTier(group, expectedTier: 2);
        }

        [Test]
        public void Single8Run_SelectsTier3()
        {
            var positions = new[]
            {
                new GridPos(0, 0), new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0),
                new GridPos(4, 0), new GridPos(5, 0), new GridPos(6, 0), new GridPos(7, 0)
            };
            var group = new ClearGroup(positions, longestRun: 8, runCount: 1, BallColor.Red);

            AssertTier(group, expectedTier: 3);
        }

        [Test]
        public void Single9Run_SelectsTier4()
        {
            var positions = new[]
            {
                new GridPos(0, 0), new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0),
                new GridPos(4, 0), new GridPos(5, 0), new GridPos(6, 0), new GridPos(7, 0), new GridPos(8, 0)
            };
            var group = new ClearGroup(positions, longestRun: 9, runCount: 1, BallColor.Red);

            AssertTier(group, expectedTier: 4);
        }

        [Test]
        public void Two9Runs_SelectsTier4()
        {
            var positions = new List<GridPos>();
            for (int x = 0; x < 9; x++) positions.Add(new GridPos(x, 0));
            for (int y = 1; y < 9; y++) positions.Add(new GridPos(0, y));

            var group = new ClearGroup(positions.ToArray(), longestRun: 9, runCount: 2, BallColor.Red);

            AssertTier(group, expectedTier: 4);
        }

        private void AssertTier(ClearGroup group, int expectedTier)
        {
            FeedbackCue directCue = FeedbackDirector.Evaluate(group, m_Rules);
            Assert.AreEqual(expectedTier, directCue.Tier, "FeedbackDirector must produce expected tier");

            m_BoardAnimator.AnimateClear(group, group.Positions[0], null);
            Assert.AreEqual(expectedTier, m_BoardAnimator.ActiveTierRule.Tier, "BoardAnimator.ActiveTierRule.Tier must match FeedbackDirector and expected tier");
        }
    }
}

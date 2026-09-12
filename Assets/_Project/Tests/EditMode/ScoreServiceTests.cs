using Line98.Core;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class ScoreServiceTests
    {
        private ScoreRules m_Rules;

        [SetUp]
        public void SetUp()
        {
            m_Rules = ScoreRules.Default;
        }

        [TestCase(5, 100)]
        [TestCase(6, 180)]
        [TestCase(7, 300)]
        [TestCase(8, 500)]
        [TestCase(9, 800)]
        public void BaseScores_MatchGddTable(int length, int expectedScore)
        {
            var positions = new GridPos[length];
            var group = new ClearGroup(positions, length, 1, BallColor.Red);

            int score = ScoreEvaluator.Evaluate(group, m_Rules);

            Assert.AreEqual(expectedScore, score);
        }

        [Test]
        public void MultipleRuns_ApplyComboMultiplier()
        {
            // Two 5-ball lines intersecting: base 100 * (1 + 0.25 * (2 - 1)) = 125
            var positions = new GridPos[9];
            var group = new ClearGroup(positions, 5, 2, BallColor.Red);

            int score = ScoreEvaluator.Evaluate(group, m_Rules);

            Assert.AreEqual(125, score);
        }
    }
}

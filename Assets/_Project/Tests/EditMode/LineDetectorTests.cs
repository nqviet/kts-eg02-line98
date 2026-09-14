using Line98.Core;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class LineDetectorTests
    {
        private BoardModel m_Board;

        [SetUp]
        public void SetUp()
        {
            m_Board = new BoardModel();
        }

        [Test]
        public void HorizontalLineOf5_Detected()
        {
            for (int x = 0; x < 5; x++)
            {
                m_Board.Set(new GridPos(x, 2), BallColor.Red);
            }

            bool detected = LineDetector.TryBuildClearGroup(m_Board, new GridPos(2, 2), out ClearGroup group);

            Assert.IsTrue(detected);
            Assert.AreEqual(5, group.Count);
            Assert.AreEqual(5, group.LongestRun);
            Assert.AreEqual(1, group.RunCount);
            Assert.AreEqual(BallColor.Red, group.ClearedColor);
        }

        [Test]
        public void VerticalLineOf5_Detected()
        {
            for (int y = 2; y <= 6; y++)
            {
                m_Board.Set(new GridPos(3, y), BallColor.Cyan);
            }

            bool detected = LineDetector.TryBuildClearGroup(m_Board, new GridPos(3, 4), out ClearGroup group);

            Assert.IsTrue(detected);
            Assert.AreEqual(5, group.Count);
            Assert.AreEqual(5, group.LongestRun);
            Assert.AreEqual(1, group.RunCount);
        }

        [Test]
        public void DiagonalAscendingLineOf5_Detected()
        {
            for (int i = 0; i < 5; i++)
            {
                m_Board.Set(new GridPos(i, i), BallColor.Green);
            }

            bool detected = LineDetector.TryBuildClearGroup(m_Board, new GridPos(2, 2), out ClearGroup group);

            Assert.IsTrue(detected);
            Assert.AreEqual(5, group.Count);
            Assert.AreEqual(5, group.LongestRun);
        }

        [Test]
        public void DiagonalDescendingLineOf5_Detected()
        {
            for (int i = 0; i < 5; i++)
            {
                m_Board.Set(new GridPos(i, 4 - i), BallColor.Yellow);
            }

            bool detected = LineDetector.TryBuildClearGroup(m_Board, new GridPos(2, 2), out ClearGroup group);

            Assert.IsTrue(detected);
            Assert.AreEqual(5, group.Count);
        }

        [Test]
        public void LineOf4_NotDetected()
        {
            for (int x = 0; x < 4; x++)
            {
                m_Board.Set(new GridPos(x, 0), BallColor.Purple);
            }

            bool detected = LineDetector.TryBuildClearGroup(m_Board, new GridPos(1, 0), out ClearGroup group);

            Assert.IsFalse(detected);
            Assert.AreEqual(0, group.Count);
        }

        [Test]
        public void IntersectingCross_ProducesDedupedUnion()
        {
            // Center at (4,4)
            GridPos center = new GridPos(4, 4);

            // Horizontal line through (4,4): x from 2 to 6
            for (int x = 2; x <= 6; x++)
            {
                m_Board.Set(new GridPos(x, 4), BallColor.Red);
            }

            // Vertical line through (4,4): y from 2 to 6
            for (int y = 2; y <= 6; y++)
            {
                m_Board.Set(new GridPos(4, y), BallColor.Red);
            }

            bool detected = LineDetector.TryBuildClearGroup(m_Board, center, out ClearGroup group);

            Assert.IsTrue(detected);
            Assert.AreEqual(2, group.RunCount);
            Assert.AreEqual(5, group.LongestRun);
            // 5 horizontal + 5 vertical - 1 shared intersection = exactly 9 unique cells
            Assert.AreEqual(9, group.Count);
        }

        [TestCase(LineAxis.Horizontal)]
        [TestCase(LineAxis.Vertical)]
        [TestCase(LineAxis.DiagonalAscending)]
        [TestCase(LineAxis.DiagonalDescending)]
        public void Exactly6_AllAxes_Detected_WithLongestRun6(LineAxis axis)
        {
            GridPos origin = new GridPos(3, 3);
            for (int step = -2; step <= 3; step++) // 6 balls
            {
                int x = 3, y = 3;
                switch (axis)
                {
                    case LineAxis.Horizontal: x += step; break;
                    case LineAxis.Vertical: y += step; break;
                    case LineAxis.DiagonalAscending: x += step; y += step; break;
                    case LineAxis.DiagonalDescending: x += step; y -= step; break;
                }
                m_Board.Set(new GridPos(x, y), BallColor.Blue);
            }

            bool detected = LineDetector.TryBuildClearGroup(m_Board, origin, out ClearGroup group);
            Assert.IsTrue(detected);
            Assert.AreEqual(6, group.Count);
            Assert.AreEqual(6, group.LongestRun);
            Assert.AreEqual(1, group.RunCount);
        }

        [TestCase(LineAxis.Horizontal)]
        [TestCase(LineAxis.Vertical)]
        [TestCase(LineAxis.DiagonalAscending)]
        [TestCase(LineAxis.DiagonalDescending)]
        public void Exactly7_AllAxes_Detected_WithLongestRun7(LineAxis axis)
        {
            GridPos origin = new GridPos(3, 3);
            for (int step = -3; step <= 3; step++) // 7 balls
            {
                int x = 3, y = 3;
                switch (axis)
                {
                    case LineAxis.Horizontal: x += step; break;
                    case LineAxis.Vertical: y += step; break;
                    case LineAxis.DiagonalAscending: x += step; y += step; break;
                    case LineAxis.DiagonalDescending: x += step; y -= step; break;
                }
                m_Board.Set(new GridPos(x, y), BallColor.Cyan);
            }

            bool detected = LineDetector.TryBuildClearGroup(m_Board, origin, out ClearGroup group);
            Assert.IsTrue(detected);
            Assert.AreEqual(7, group.Count);
            Assert.AreEqual(7, group.LongestRun);
            Assert.AreEqual(1, group.RunCount);
        }

        [Test]
        public void Exactly8_Horizontal_Detected_WithLongestRun8()
        {
            for (int x = 0; x < 8; x++)
            {
                m_Board.Set(new GridPos(x, 5), BallColor.Orange);
            }

            bool detected = LineDetector.TryBuildClearGroup(m_Board, new GridPos(4, 5), out ClearGroup group);
            Assert.IsTrue(detected);
            Assert.AreEqual(8, group.Count);
            Assert.AreEqual(8, group.LongestRun);
            Assert.AreEqual(1, group.RunCount);
        }

        [Test]
        public void Exactly9_FullRow_Detected_WithLongestRun9()
        {
            for (int x = 0; x < 9; x++)
            {
                m_Board.Set(new GridPos(x, 4), BallColor.Yellow);
            }

            bool detected = LineDetector.TryBuildClearGroup(m_Board, new GridPos(4, 4), out ClearGroup group);
            Assert.IsTrue(detected);
            Assert.AreEqual(9, group.Count);
            Assert.AreEqual(9, group.LongestRun);
            Assert.AreEqual(1, group.RunCount);
        }

        [Test]
        public void TwoSeparateSimultaneousLines_NoSharedCell_Detected()
        {
            // Row 0: 6 Red balls
            for (int x = 0; x < 6; x++)
            {
                m_Board.Set(new GridPos(x, 0), BallColor.Red);
            }

            // Row 8: 6 Red balls
            for (int x = 0; x < 6; x++)
            {
                m_Board.Set(new GridPos(x, 8), BallColor.Red);
            }

            bool detected = LineDetector.TryBuildAllClearGroups(m_Board, out ClearGroup group);
            Assert.IsTrue(detected);
            Assert.AreEqual(2, group.RunCount);
            Assert.AreEqual(6, group.LongestRun);
            Assert.AreEqual(12, group.Count); // 6 + 6 with zero shared cells = exactly 12
        }

        [Test]
        public void ThreeSimultaneousLines_IntersectingAtOrigin_ProducesDedupedUnion()
        {
            GridPos center = new GridPos(4, 4);

            // Horizontal through (4,4): x from 2 to 6 (5 balls)
            for (int x = 2; x <= 6; x++) m_Board.Set(new GridPos(x, 4), BallColor.Purple);
            // Vertical through (4,4): y from 2 to 6 (5 balls)
            for (int y = 2; y <= 6; y++) m_Board.Set(new GridPos(4, y), BallColor.Purple);
            // Diagonal ascending through (4,4): step from -2 to 2 (5 balls)
            for (int step = -2; step <= 2; step++) m_Board.Set(new GridPos(4 + step, 4 + step), BallColor.Purple);

            bool detected = LineDetector.TryBuildClearGroup(m_Board, center, out ClearGroup group);
            Assert.IsTrue(detected);
            Assert.AreEqual(3, group.RunCount);
            Assert.AreEqual(5, group.LongestRun);
            // 5 + 4 + 4 = exactly 13 unique cells
            Assert.AreEqual(13, group.Count);
        }

        [Test]
        public void IntersectingCross_Scoring_AwardsDistinctRunsAndDeduplicatesSharedCell()
        {
            GridPos center = new GridPos(4, 4);
            for (int x = 2; x <= 6; x++) m_Board.Set(new GridPos(x, 4), BallColor.Red);
            for (int y = 2; y <= 6; y++) m_Board.Set(new GridPos(4, y), BallColor.Red);

            Assert.IsTrue(LineDetector.TryBuildClearGroup(m_Board, center, out ClearGroup group));
            Assert.AreEqual(9, group.Count); // 5 + 5 - 1 shared cell = 9
            Assert.AreEqual(2, group.RunCount);

            // Score: 100 * (1 + 0.25 * (2 - 1)) = 125
            int score = ScoreEvaluator.Evaluate(group, ScoreRules.Default);
            Assert.AreEqual(125, score);
        }
    }
}

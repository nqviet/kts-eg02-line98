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
    }
}

using System.Collections.Generic;
using Line98.Core;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class PathfinderTests
    {
        private BoardModel m_Board;
        private List<GridPos> m_Path;

        [SetUp]
        public void SetUp()
        {
            m_Board = new BoardModel();
            m_Path = new List<GridPos>();
        }

        [Test]
        public void DirectPath_OnEmptyBoard_Succeeds()
        {
            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(0, 3);
            m_Board.Set(from, BallColor.Red);

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);

            Assert.IsTrue(found);
            Assert.AreEqual(4, m_Path.Count);
            Assert.AreEqual(from, m_Path[0]);
            Assert.AreEqual(to, m_Path[m_Path.Count - 1]);
        }

        [Test]
        public void BlockedPath_ReturnsFalse()
        {
            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(2, 0);
            m_Board.Set(from, BallColor.Red);

            // Create an impenetrable wall around (0,0)
            m_Board.Set(new GridPos(1, 0), BallColor.Cyan);
            m_Board.Set(new GridPos(0, 1), BallColor.Cyan);

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);

            Assert.IsFalse(found);
            Assert.AreEqual(0, m_Path.Count);
        }

        [Test]
        public void NavigatesAroundObstacles()
        {
            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(2, 0);
            m_Board.Set(from, BallColor.Red);

            // Block direct horizontal path at (1,0)
            m_Board.Set(new GridPos(1, 0), BallColor.Green);

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);

            Assert.IsTrue(found);
            Assert.AreEqual(from, m_Path[0]);
            Assert.AreEqual(to, m_Path[m_Path.Count - 1]);
            // Path must detour through (0,1) -> (1,1) -> (2,1) -> (2,0)
            Assert.IsTrue(m_Path.Count >= 5);
        }

        [Test]
        public void DestinationOccupied_ReturnsFalse()
        {
            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(1, 1);
            m_Board.Set(from, BallColor.Red);
            m_Board.Set(to, BallColor.Yellow); // Target already occupied

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);

            Assert.IsFalse(found);
        }
    }
}

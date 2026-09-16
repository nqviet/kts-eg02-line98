using System;
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
            m_Path = new List<GridPos>(BoardModel.CellCount);
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

        [Test]
        public void FromAndToAllFourCorners_ReturnsContiguousPath()
        {
            GridPos c00 = new GridPos(0, 0);
            GridPos c88 = new GridPos(8, 8);
            GridPos c80 = new GridPos(8, 0);
            GridPos c08 = new GridPos(0, 8);

            // (0,0) <-> (8,8)
            m_Board = new BoardModel();
            m_Board.Set(c00, BallColor.Red);
            Assert.IsTrue(Pathfinder.TryFindPath(m_Board, c00, c88, m_Path));
            AssertContiguousPath(m_Path, c00, c88);

            m_Board = new BoardModel();
            m_Board.Set(c88, BallColor.Cyan);
            Assert.IsTrue(Pathfinder.TryFindPath(m_Board, c88, c00, m_Path));
            AssertContiguousPath(m_Path, c88, c00);

            // (8,0) <-> (0,8)
            m_Board = new BoardModel();
            m_Board.Set(c80, BallColor.Green);
            Assert.IsTrue(Pathfinder.TryFindPath(m_Board, c80, c08, m_Path));
            AssertContiguousPath(m_Path, c80, c08);

            m_Board = new BoardModel();
            m_Board.Set(c08, BallColor.Orange);
            Assert.IsTrue(Pathfinder.TryFindPath(m_Board, c08, c80, m_Path));
            AssertContiguousPath(m_Path, c08, c80);
        }

        [Test]
        public void MultipleObstacles_ForcesDetour()
        {
            GridPos from = new GridPos(4, 0);
            GridPos to = new GridPos(4, 4);
            m_Board.Set(from, BallColor.Red);

            // Wall along Y=2 from X=1 to X=8, leaving only X=0 open
            for (int x = 1; x <= 8; x++)
            {
                m_Board.Set(new GridPos(x, 2), BallColor.Purple);
            }

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);
            Assert.IsTrue(found);
            AssertContiguousPath(m_Path, from, to);

            // Verify that the path passed through the gap at (0,2)
            bool usedGap = false;
            foreach (GridPos p in m_Path)
            {
                if (p.X == 0 && p.Y == 2)
                {
                    usedGap = true;
                    break;
                }
            }
            Assert.IsTrue(usedGap, "Path must pass through the only open gap at (0,2)");
        }

        [Test]
        public void UnreachableDestination_Pocket()
        {
            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(8, 8);
            m_Board.Set(from, BallColor.Red);

            // Enclose (8,8) with walls at (7,8) and (8,7)
            m_Board.Set(new GridPos(7, 8), BallColor.Yellow);
            m_Board.Set(new GridPos(8, 7), BallColor.Yellow);

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);
            Assert.IsFalse(found);
            Assert.AreEqual(0, m_Path.Count);
        }

        [Test]
        public void SnakeMaze_SingleFileCorridor_ReturnsExactLength()
        {
            // Build a snake maze on an empty board:
            // Row 0 open except end
            // We fill rows 1, 3, 5, 7 with barriers leaving alternating ends open
            for (int x = 0; x < 8; x++)
            {
                m_Board.Set(new GridPos(x, 1), BallColor.Red);
            } // (8,1) is open
            for (int x = 1; x < 9; x++)
            {
                m_Board.Set(new GridPos(x, 3), BallColor.Red);
            } // (0,3) is open
            for (int x = 0; x < 8; x++)
            {
                m_Board.Set(new GridPos(x, 5), BallColor.Red);
            } // (8,5) is open
            for (int x = 1; x < 9; x++)
            {
                m_Board.Set(new GridPos(x, 7), BallColor.Red);
            } // (0,7) is open

            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(0, 8);
            m_Board.Set(from, BallColor.Cyan);

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);
            Assert.IsTrue(found);
            AssertContiguousPath(m_Path, from, to);

            // Length calculation:
            // Row 0: (0,0) to (8,0) = 9 cells
            // Move down: (8,1) = 1 cell
            // Row 2: (8,2) to (0,2) = 9 cells
            // Move down: (0,3) = 1 cell
            // Row 4: (0,4) to (8,4) = 9 cells
            // Move down: (8,5) = 1 cell
            // Row 6: (8,6) to (0,6) = 9 cells
            // Move down: (0,7) = 1 cell
            // Move to (0,8) = 1 cell
            // Total = 9 + 1 + 9 + 1 + 9 + 1 + 9 + 1 + 1 = 41 cells
            Assert.AreEqual(41, m_Path.Count);
        }

        [Test]
        public void UnobstructedPath_LengthEqualsManhattanDistancePlusOne()
        {
            GridPos from = new GridPos(1, 2);
            GridPos to = new GridPos(6, 7);
            m_Board.Set(from, BallColor.Yellow);

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);
            Assert.IsTrue(found);

            int manhattan = Math.Abs(to.X - from.X) + Math.Abs(to.Y - from.Y);
            Assert.AreEqual(manhattan + 1, m_Path.Count);
        }

        [Test]
        public void PathHugsWall_StaysInBounds()
        {
            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(0, 4);
            m_Board.Set(from, BallColor.Red);

            // Put a barrier along X=0 for Y=1..3
            m_Board.Set(new GridPos(0, 1), BallColor.Cyan);
            m_Board.Set(new GridPos(0, 2), BallColor.Cyan);
            m_Board.Set(new GridPos(0, 3), BallColor.Cyan);

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);
            Assert.IsTrue(found);
            AssertContiguousPath(m_Path, from, to);

            foreach (GridPos p in m_Path)
            {
                Assert.IsTrue(p.X < BoardModel.Size);
                Assert.IsTrue(p.Y < BoardModel.Size);
            }
        }

        [Test]
        public void SelfPath_FromEqualsTo_ReturnsFalse()
        {
            // Destination must be empty while source must contain a ball.
            // If from == to, a cell cannot be both occupied and empty.
            GridPos pos = new GridPos(3, 3);
            m_Board.Set(pos, BallColor.Red);

            bool found = Pathfinder.TryFindPath(m_Board, pos, pos, m_Path);
            Assert.IsFalse(found);
            Assert.AreEqual(0, m_Path.Count);
        }

        [Test]
        public void SourceEmpty_ReturnsFalse()
        {
            GridPos from = new GridPos(1, 1);
            GridPos to = new GridPos(2, 2);
            // Board has no ball at from

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);
            Assert.IsFalse(found);
            Assert.AreEqual(0, m_Path.Count);
        }

        [Test]
        public void OutOfBounds_ReturnsFalse()
        {
            GridPos valid = new GridPos(0, 0);
            GridPos invalid = new GridPos(10, 10);
            m_Board.Set(valid, BallColor.Red);

            Assert.IsFalse(Pathfinder.TryFindPath(m_Board, valid, invalid, m_Path));
            Assert.IsFalse(Pathfinder.TryFindPath(m_Board, invalid, valid, m_Path));
            Assert.IsFalse(Pathfinder.TryFindPath(null, valid, new GridPos(1, 1), m_Path));
        }

        [Test]
        public void ReturnedPath_IsContiguousAndTraversable()
        {
            GridPos from = new GridPos(1, 1);
            GridPos to = new GridPos(7, 7);
            m_Board.Set(from, BallColor.Red);

            // Add scattered obstacles
            m_Board.Set(new GridPos(3, 3), BallColor.Green);
            m_Board.Set(new GridPos(4, 4), BallColor.Green);
            m_Board.Set(new GridPos(5, 5), BallColor.Green);

            bool found = Pathfinder.TryFindPath(m_Board, from, to, m_Path);
            Assert.IsTrue(found);
            AssertContiguousPath(m_Path, from, to);

            // Intermediate cells must all be empty on the board
            for (int i = 1; i < m_Path.Count; i++)
            {
                Assert.IsTrue(m_Board.IsEmpty(m_Path[i]));
            }
        }

        [Test]
        public void TryFindPath_PreSizedList_DoesNotAllocate()
        {
            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(8, 8);
            m_Board.Set(from, BallColor.Red);

            // Warm-up
            Pathfinder.TryFindPath(m_Board, from, to, m_Path);

            long before = GC.GetAllocatedBytesForCurrentThread();
            Pathfinder.TryFindPath(m_Board, from, to, m_Path);
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.AreEqual(0, after - before, "Pathfinder.TryFindPath must not allocate GC memory when pre-sized list is used");
        }

        private void AssertContiguousPath(List<GridPos> path, GridPos expectedStart, GridPos expectedEnd)
        {
            Assert.IsNotNull(path);
            Assert.IsTrue(path.Count >= 2);
            Assert.AreEqual(expectedStart, path[0]);
            Assert.AreEqual(expectedEnd, path[path.Count - 1]);

            for (int i = 0; i < path.Count - 1; i++)
            {
                GridPos current = path[i];
                GridPos next = path[i + 1];
                int dx = Math.Abs(next.X - current.X);
                int dy = Math.Abs(next.Y - current.Y);
                Assert.AreEqual(1, dx + dy, $"Cells at index {i} ({current}) and {i + 1} ({next}) must be 4-directionally adjacent");
            }
        }
    }
}

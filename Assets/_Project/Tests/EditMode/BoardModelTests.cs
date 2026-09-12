using Line98.Core;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class BoardModelTests
    {
        private BoardModel m_Board;

        [SetUp]
        public void SetUp()
        {
            m_Board = new BoardModel();
        }

        [Test]
        public void NewBoard_IsEmpty()
        {
            Assert.AreEqual(81, m_Board.EmptyCount);
            Assert.AreEqual(0, m_Board.OccupiedCount);
            Assert.IsTrue(m_Board.IsEmptyBoard);
            Assert.IsFalse(m_Board.IsFull);
        }

        [Test]
        public void SetBall_UpdatesStateAndCount()
        {
            GridPos pos = new GridPos(4, 4);
            m_Board.Set(pos, BallColor.Red);

            Assert.IsFalse(m_Board.IsEmpty(pos));
            Assert.AreEqual(BallColor.Red, m_Board.ColorAt(pos));
            Assert.AreEqual(80, m_Board.EmptyCount);
            Assert.AreEqual(1, m_Board.OccupiedCount);
        }

        [Test]
        public void ClearBall_RestoresEmptyState()
        {
            GridPos pos = new GridPos(0, 0);
            m_Board.Set(pos, BallColor.Cyan);
            m_Board.Clear(pos);

            Assert.IsTrue(m_Board.IsEmpty(pos));
            Assert.AreEqual(BallColor.None, m_Board.ColorAt(pos));
            Assert.AreEqual(81, m_Board.EmptyCount);
        }

        [Test]
        public void FullBoard_ReportsIsFull()
        {
            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    m_Board.Set(new GridPos(x, y), BallColor.Yellow);
                }
            }

            Assert.IsTrue(m_Board.IsFull);
            Assert.AreEqual(0, m_Board.EmptyCount);
            Assert.AreEqual(81, m_Board.OccupiedCount);
        }

        [Test]
        public void ExportAndImport_PreservesAllCells()
        {
            m_Board.Set(new GridPos(1, 2), BallColor.Green);
            m_Board.Set(new GridPos(7, 8), BallColor.Purple);

            byte[] exported = m_Board.ExportCells();
            var newBoard = new BoardModel();
            newBoard.ImportCells(exported);

            Assert.AreEqual(BallColor.Green, newBoard.ColorAt(new GridPos(1, 2)));
            Assert.AreEqual(BallColor.Purple, newBoard.ColorAt(new GridPos(7, 8)));
            Assert.AreEqual(m_Board.EmptyCount, newBoard.EmptyCount);
        }
    }
}

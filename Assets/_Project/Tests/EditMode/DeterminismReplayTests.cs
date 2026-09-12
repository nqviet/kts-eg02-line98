using Line98.Core;
using Line98.Gameplay;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class DeterminismReplayTests
    {
        [Test]
        public void ReplayingSameMovesWithSameSeed_ProducesIdenticalState()
        {
            uint seed = 0x5EED1234;

            var session1 = new GameSession();
            session1.StartNewGame(seed);

            var session2 = new GameSession();
            session2.StartNewGame(seed);

            // Execute 3 identical moves on both sessions
            for (int step = 0; step < 3; step++)
            {
                // Find a legal move on session1
                bool moveFound = FindLegalMove(session1.Board, out GridPos from, out GridPos to);
                if (!moveFound) break;

                session1.TrySelect(from);
                bool res1 = session1.ExecuteMove(to);

                session2.TrySelect(from);
                bool res2 = session2.ExecuteMove(to);

                Assert.AreEqual(res1, res2);
                Assert.AreEqual(session1.Score, session2.Score);
                Assert.AreEqual(session1.MoveCount, session2.MoveCount);
                Assert.AreEqual(session1.Rng.S0, session2.Rng.S0);
                Assert.AreEqual(session1.Rng.S1, session2.Rng.S1);

                byte[] board1 = session1.Board.ExportCells();
                byte[] board2 = session2.Board.ExportCells();
                CollectionAssert.AreEqual(board1, board2);
            }
        }

        private static bool FindLegalMove(BoardModel board, out GridPos from, out GridPos to)
        {
            from = default;
            to = default;

            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos p = new GridPos(x, y);
                    if (!board.IsEmpty(p))
                    {
                        // Check neighboring cell
                        if (x < BoardModel.Size - 1 && board.IsEmpty(new GridPos(x + 1, y)))
                        {
                            from = p;
                            to = new GridPos(x + 1, y);
                            return true;
                        }
                        if (y < BoardModel.Size - 1 && board.IsEmpty(new GridPos(x, y + 1)))
                        {
                            from = p;
                            to = new GridPos(x, y + 1);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}

using Line98.Core;
using Line98.Gameplay;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class UndoServiceTests
    {
        [Test]
        public void UndoMove_RestoresBoardAndRngState()
        {
            var session = new GameSession();
            session.StartNewGame(9999U);

            int initialScore = session.Score;
            int initialMoves = session.MoveCount;
            XorShift128 initialRng = session.Rng;
            byte[] initialBoardCells = session.Board.ExportCells();

            // Find an occupied cell and an adjacent empty cell
            GridPos from = default;
            GridPos to = default;
            bool found = false;

            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos p = new GridPos(x, y);
                    if (!session.Board.IsEmpty(p))
                    {
                        if (x < BoardModel.Size - 1 && session.Board.IsEmpty(new GridPos(x + 1, y)))
                        {
                            from = p;
                            to = new GridPos(x + 1, y);
                            found = true;
                            break;
                        }
                    }
                }
                if (found) break;
            }

            Assert.IsTrue(found, "Should find a legal adjacent move on initial board");

            session.TrySelect(from);
            bool executed = session.ExecuteMove(to);
            Assert.IsTrue(executed);
            Assert.AreEqual(initialMoves + 1, session.MoveCount);

            // Now Undo
            bool undone = session.TryUndo();
            Assert.IsTrue(undone);
            Assert.AreEqual(initialMoves, session.MoveCount);
            Assert.AreEqual(initialScore, session.Score);
            Assert.AreEqual(initialRng.S0, session.Rng.S0);
            Assert.AreEqual(initialRng.S1, session.Rng.S1);

            byte[] restoredCells = session.Board.ExportCells();
            CollectionAssert.AreEqual(initialBoardCells, restoredCells);
        }

        [Test]
        public void DailyChallenge_DisablesUndo()
        {
            var session = new GameSession(new DailyChallengeMode("2026-09-13"));
            session.StartNewGame();

            Assert.IsFalse(session.Mode.UndoAllowed);
            Assert.IsFalse(session.TryUndo());
        }
    }
}

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

        [Test]
        public void FreeAllowance_ExhaustionAndRewardGating()
        {
            var nullGate = new NullAdGate();
            var undoService = new UndoService(UndoRules.Default, nullGate);
            var session = new GameSession(new ClassicMode(), undoService: undoService);
            session.StartNewGame(1111U);

            Assert.AreEqual(3, session.FreeUndosRemaining);

            // Make 3 moves and undos
            for (int i = 0; i < 3; i++)
            {
                MakeOneValidMove(session);
                Assert.IsTrue(session.TryUndo(), $"Undo {i + 1} should succeed");
                Assert.AreEqual(2 - i, session.FreeUndosRemaining);
            }

            Assert.AreEqual(0, session.FreeUndosRemaining);

            // 4th move and undo should be denied because NullAdGate provides no rewarded ad
            MakeOneValidMove(session);
            bool fourthUndo = session.TryUndo();
            Assert.IsFalse(fourthUndo, "4th undo without ad gate must be denied");
        }

        [Test]
        public void RewardedUndo_SucceedsWithAlwaysGrantGate()
        {
            var grantGate = new AlwaysGrantAdGate();
            var undoService = new UndoService(UndoRules.Default, grantGate);
            var session = new GameSession(new ClassicMode(), undoService: undoService);
            session.StartNewGame(2222U);

            // Exhaust 3 free undos
            for (int i = 0; i < 3; i++)
            {
                MakeOneValidMove(session);
                Assert.IsTrue(session.TryUndo());
            }

            Assert.AreEqual(0, session.FreeUndosRemaining);

            // 4th undo uses rewarded ad through AlwaysGrantAdGate
            MakeOneValidMove(session);
            bool fourthUndo = session.TryUndo();
            Assert.IsTrue(fourthUndo, "4th undo with AlwaysGrantAdGate must succeed via rewarded path");
        }

        [Test]
        public void ZenMode_UnlimitedUndos()
        {
            var session = new GameSession(new ZenMode());
            session.StartNewGame(3333U);

            Assert.IsTrue(session.Mode.UnlimitedUndo);

            // Make 5 moves and undos in Zen mode
            for (int i = 0; i < 5; i++)
            {
                MakeOneValidMove(session);
                Assert.IsTrue(session.TryUndo());
            }

            // Free undos counter is not consumed in Zen mode
            Assert.AreEqual(3, session.FreeUndosRemaining);
        }

        private static void MakeOneValidMove(GameSession session)
        {
            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos p = new GridPos(x, y);
                    if (!session.Board.IsEmpty(p))
                    {
                        if (x < BoardModel.Size - 1 && session.Board.IsEmpty(new GridPos(x + 1, y)))
                        {
                            session.TrySelect(p);
                            if (session.ExecuteMove(new GridPos(x + 1, y)))
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}

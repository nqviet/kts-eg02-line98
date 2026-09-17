using Line98.Core;
using Line98.Gameplay;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class ReviveServiceTests
    {
        [Test]
        public void WorksWithAlwaysGrantGate_FreesAtLeastThreeCells()
        {
            var grantGate = new AlwaysGrantAdGate();
            var reviveService = new ReviveService(ReviveRules.Default, grantGate);
            var session = new GameSession();
            session.StartNewGame(1234U);

            // Fill board to force game over
            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                session.Board.Set(i, BallColor.Red);
            }

            var plan = new MovePlan { Outcome = MoveOutcome.GameOver, IsGameOver = true };
            session.Commit(plan);
            Assert.AreEqual(GamePhase.GameOver, session.Phase);

            ContinueResult result = reviveService.Apply(session, session.Mode);
            Assert.IsTrue(result.Applied);
            Assert.GreaterOrEqual(result.FreedCells, 3);
            Assert.AreEqual(GamePhase.Playing, session.Phase);
        }

        [Test]
        public void DeniedWhenGateDenies()
        {
            var nullGate = new NullAdGate();
            var reviveService = new ReviveService(ReviveRules.Default, nullGate);
            var session = new GameSession();
            session.StartNewGame(5678U);

            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                session.Board.Set(i, BallColor.Blue);
            }

            var plan = new MovePlan { Outcome = MoveOutcome.GameOver, IsGameOver = true };
            session.Commit(plan);

            ContinueResult result = reviveService.Apply(session, session.Mode);
            Assert.IsFalse(result.Applied);
            Assert.AreEqual(GamePhase.GameOver, session.Phase);
        }

        [Test]
        public void Continue_DoesNotAdvanceRng()
        {
            var grantGate = new AlwaysGrantAdGate();
            var reviveService = new ReviveService(ReviveRules.Default, grantGate);
            var session = new GameSession();
            session.StartNewGame(9999U);

            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                session.Board.Set(i, BallColor.Green);
            }

            var plan = new MovePlan { Outcome = MoveOutcome.GameOver, IsGameOver = true };
            session.Commit(plan);

            XorShift128 rngBefore = session.Rng;
            ContinueResult result = reviveService.Apply(session, session.Mode);

            Assert.IsTrue(result.Applied);
            Assert.AreEqual(rngBefore.S0, session.Rng.S0);
            Assert.AreEqual(rngBefore.S1, session.Rng.S1);
            Assert.AreEqual(rngBefore.S2, session.Rng.S2);
            Assert.AreEqual(rngBefore.S3, session.Rng.S3);
        }

        [Test]
        public void Continue_SecondTimeInSameRun_Denied()
        {
            var grantGate = new AlwaysGrantAdGate();
            var reviveService = new ReviveService(ReviveRules.Default, grantGate);
            var session = new GameSession();
            session.StartNewGame(4321U);

            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                session.Board.Set(i, BallColor.Yellow);
            }

            var plan = new MovePlan { Outcome = MoveOutcome.GameOver, IsGameOver = true };
            session.Commit(plan);

            // First continue succeeds
            ContinueResult result1 = reviveService.Apply(session, session.Mode);
            Assert.IsTrue(result1.Applied);

            // Force game over again
            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                session.Board.Set(i, BallColor.Yellow);
            }
            session.Commit(plan);

            // Second continue in same run must be denied
            ContinueResult result2 = reviveService.Apply(session, session.Mode);
            Assert.IsFalse(result2.Applied);
        }

        [Test]
        public void NewGame_SucceedsWhenGateAlwaysDenies()
        {
            var session = new GameSession();
            session.StartNewGame(7777U);

            Assert.AreEqual(GamePhase.Playing, session.Phase);
            Assert.AreEqual(3, session.Board.OccupiedCount);
        }
    }
}

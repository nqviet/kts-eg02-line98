using System.Collections.Generic;
using Line98.App;
using Line98.Core;
using Line98.Gameplay;
using Line98.Services;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    /// <summary>
    /// Regression coverage for New Game reset (P1.7), full Undo restoration (P3.3)
    /// and mid-game resume (P3.1 / P3.6).
    /// </summary>
    [TestFixture]
    public class SessionReplaceTests
    {
        [Test]
        public void Undo_CountIsDecrementedBeforeStateRestoredObserversRun()
        {
            var session = new GameSession();
            session.StartNewGame(4242U);
            MakeOneValidMove(session);

            int observed = -1;
            session.OnStateRestored += _ => observed = session.FreeUndosRemaining;

            Assert.IsTrue(session.TryUndo());
            Assert.AreEqual(2, observed);
            Assert.AreEqual(2, session.FreeUndosRemaining);
        }

        [Test]
        public void Undo_PersistsDecrementedCount()
        {
            var session = new GameSession();
            session.StartNewGame(4242U);
            MakeOneValidMove(session);

            SessionSave saved = null;
            session.OnStateRestored += _ => saved = SessionSaveMapper.ToSave(session.CaptureState(), 0);

            Assert.IsTrue(session.TryUndo());
            Assert.IsNotNull(saved);
            Assert.AreEqual(2, saved.FreeUndosRemaining);
        }

        [Test]
        public void Undo_FailedRestore_RefundsFreeUndo()
        {
            var service = new UndoService();
            var ctx = new UndoAvailability(GamePhase.Playing, true, true);

            UndoResult result = service.Request(ctx, () => null);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(3, service.FreeUndosRemaining);
        }

        [Test]
        public void OnSessionReplaced_FiresWithMatchingReason()
        {
            var session = new GameSession();
            var reasons = new List<SessionReplaceReason>();
            session.OnSessionReplaced += reasons.Add;

            session.StartNewGame(7U);
            MakeOneValidMove(session);
            Assert.IsTrue(session.TryUndo());
            SessionState state = session.CaptureState();
            Assert.IsTrue(session.TryRestore(in state));

            CollectionAssert.AreEqual(
                new[] { SessionReplaceReason.NewGame, SessionReplaceReason.Undo, SessionReplaceReason.Restore },
                reasons);
        }

        [Test]
        public void NewGame_AfterMove_ResetsAllState()
        {
            var session = new GameSession();
            session.StartNewGame(1234U);
            MakeOneValidMove(session);
            Assert.IsTrue(session.TryUndo());
            MakeOneValidMove(session);

            session.StartNewGame(99U);

            var reference = new GameSession();
            reference.StartNewGame(99U);

            CollectionAssert.AreEqual(reference.Board.ExportCells(), session.Board.ExportCells());
            Assert.AreEqual(3, BoardModel.CellCount - session.Board.EmptyCount);
            Assert.AreEqual(0, session.Score);
            Assert.AreEqual(0, session.MoveCount);
            Assert.AreEqual(0, session.LinesCleared);
            Assert.AreEqual(3, session.FreeUndosRemaining);
            Assert.IsFalse(session.HasSnapshotStack);
            Assert.AreEqual(reference.Rng.S0, session.Rng.S0);
            Assert.AreEqual(reference.Rng.S3, session.Rng.S3);
        }

        [Test]
        public void Resume_RoundTripThroughSave_RestoresSession()
        {
            var session = new GameSession();
            session.StartNewGame(555U);
            MakeOneValidMove(session);
            MakeOneValidMove(session);
            Assert.IsTrue(session.TryUndo());

            SessionSave save = SessionSaveMapper.ToSave(session.CaptureState(), 1);
            Assert.IsTrue(SessionSaveMapper.ToState(save, out SessionState state));

            var manager = new GameManager(new GameSession());
            Assert.IsTrue(manager.TryResumeGame(in state));
            GameSession resumed = manager.ActiveSession;

            CollectionAssert.AreEqual(session.Board.ExportCells(), resumed.Board.ExportCells());
            CollectionAssert.AreEqual(session.PreviewQueue.ToArray(), resumed.PreviewQueue.ToArray());
            Assert.AreEqual(session.Rng.S0, resumed.Rng.S0);
            Assert.AreEqual(session.Rng.S1, resumed.Rng.S1);
            Assert.AreEqual(session.Rng.S2, resumed.Rng.S2);
            Assert.AreEqual(session.Rng.S3, resumed.Rng.S3);
            Assert.AreEqual(session.Score, resumed.Score);
            Assert.AreEqual(session.MoveCount, resumed.MoveCount);
            Assert.AreEqual(session.LinesCleared, resumed.LinesCleared);
            Assert.AreEqual(2, resumed.FreeUndosRemaining);
            Assert.AreEqual(GamePhase.Playing, resumed.Phase);
            Assert.AreEqual("classic", resumed.Mode.ModeId);
        }

        [Test]
        public void GameModeFactory_ForwardsDailySeedVersion()
        {
            var fromFactory = (DailyChallengeMode)GameModeFactory.Create("daily", new ModeSpec(dailyDate: "2026-09-18", dailySeedVersion: 2));
            var direct = new DailyChallengeMode("2026-09-18", seedVersion: 2);

            Assert.AreEqual(2, fromFactory.SeedVersion);
            Assert.AreEqual(direct.GenerateSeed(), fromFactory.GenerateSeed());
        }

        [Test]
        public void Router_CountsNewGamesButNotResume()
        {
            var session = new GameSession();
            var stats = new StatisticsService();
            session.StartNewGame(1U);

            using (var router = new SessionEventRouter(session, stats, null, save: new SaveService(new InMemorySaveBackend())))
            {
                Assert.AreEqual(0, stats.GamesPlayed, "Attaching a router must not count a game start");

                session.StartNewGame(2U);
                Assert.AreEqual(1, stats.GamesPlayed, "In-game New Game counts as a start");

                SessionState state = session.CaptureState();
                Assert.IsTrue(session.TryRestore(in state));
                Assert.AreEqual(1, stats.GamesPlayed, "Resume must not count as a start");
            }
        }

        [Test]
        public void Router_PersistsNewGame()
        {
            var session = new GameSession();
            var save = new SaveService(new InMemorySaveBackend());
            session.StartNewGame(1U);
            MakeOneValidMove(session);

            using (new SessionEventRouter(session, new StatisticsService(), null, save: save))
            {
                session.StartNewGame(2U);
            }

            SessionSave persisted = save.LoadGame().Session;
            Assert.IsNotNull(persisted);
            Assert.AreEqual(0, persisted.MoveCount);
            CollectionAssert.AreEqual(session.Board.ExportCells(), persisted.BoardCells);
        }

        [Test]
        public void ResumeEvaluator_OffersClassicAndExpiresStaleDaily()
        {
            var session = new GameSession();
            session.StartNewGame(3U);
            var classic = new SaveData { Session = SessionSaveMapper.ToSave(session.CaptureState(), 0) };
            Assert.AreEqual(ResumeDecisionKind.Offer, ResumeEvaluator.Evaluate(classic, "2026-09-18").Kind);

            var daily = new GameSession(new DailyChallengeMode("2026-09-17"));
            daily.StartNewGame();
            var stale = new SaveData { Session = SessionSaveMapper.ToSave(daily.CaptureState(), 0) };
            Assert.AreEqual(ResumeDecisionKind.Expired, ResumeEvaluator.Evaluate(stale, "2026-09-18").Kind);
        }

        private static void MakeOneValidMove(GameSession session)
        {
            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos p = new GridPos(x, y);
                    if (!session.Board.IsEmpty(p) && x < BoardModel.Size - 1 && session.Board.IsEmpty(new GridPos(x + 1, y)))
                    {
                        session.TrySelect(p);
                        if (session.ExecuteMove(new GridPos(x + 1, y)))
                        {
                            return;
                        }
                    }
                }
            }

            Assert.Fail("No valid move found");
        }
    }
}

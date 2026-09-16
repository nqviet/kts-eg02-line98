using System;
using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Line98.App;
using Line98.Core;
using Line98.Gameplay;
using Line98.Presentation;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class GameOverPayloadTests
    {
        [Test]
        public void SessionSummary_CarriesAllGddListedStats()
        {
            var summary = new SessionSummary(
                finalScore: 1250,
                bestScore: 2000,
                longestLine: 7,
                linesCleared: 8,
                totalMoves: 34,
                canContinue: true);

            Assert.AreEqual(1250, summary.FinalScore);
            Assert.AreEqual(2000, summary.BestScore);
            Assert.AreEqual(7, summary.LongestLine);
            Assert.AreEqual(8, summary.LinesCleared);
            Assert.AreEqual(34, summary.TotalMoves);
            Assert.IsTrue(summary.CanContinue);
        }

        [Test]
        public void GameSnapshot_CapturesAndRestores_LongestLine()
        {
            var board = new BoardModel();
            var preview = new PreviewQueue(3);
            var rng = new XorShift128(12345);

            board.Set(new GridPos(0, 0), BallColor.Red);
            preview.Populate(ref rng, 5);

            var snapshot = GameSnapshot.Capture(
                board,
                preview,
                in rng,
                score: 500,
                moveCount: 12,
                linesCleared: 3,
                longestLine: 6,
                selectedIndex: 0);

            Assert.AreEqual(6, snapshot.LongestLine);

            var restoredBoard = new BoardModel();
            var restoredPreview = new PreviewQueue(3);

            snapshot.RestoreTo(
                restoredBoard,
                restoredPreview,
                out XorShift128 outRng,
                out int outScore,
                out int outMoveCount,
                out int outLinesCleared,
                out int outLongestLine,
                out int outSelectedIndex);

            Assert.AreEqual(6, outLongestLine);
            Assert.AreEqual(500, outScore);
            Assert.AreEqual(12, outMoveCount);
            Assert.AreEqual(3, outLinesCleared);
        }

        [Test]
        public void GameSession_TracksLongestLine_AcrossMixedSequence()
        {
            var session = new GameSession();
            session.StartNewGame(4242U);

            Assert.AreEqual(0, session.LongestLine);

            // Directly simulate clear commits with varying run lengths
            // First clear: length 5
            var positions5 = new[]
            {
                new GridPos(0, 0), new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0), new GridPos(4, 0)
            };
            var clear5 = new ClearGroup(positions5, 5, 1, BallColor.Red);
            var plan5 = new MovePlan
            {
                From = new GridPos(0, 1),
                To = new GridPos(4, 0),
                Cleared = clear5,
                ScoreDelta = 100,
                PostMoveRng = session.Rng,
                Outcome = MoveOutcome.Cleared
            };
            // Put ball to move
            session.Board.Set(new GridPos(0, 1), BallColor.Red);

            session.Commit(plan5);
            Assert.AreEqual(5, session.LongestLine);

            // Second clear: length 7
            var positions7 = new[]
            {
                new GridPos(0, 2), new GridPos(1, 2), new GridPos(2, 2), new GridPos(3, 2), new GridPos(4, 2), new GridPos(5, 2), new GridPos(6, 2)
            };
            var clear7 = new ClearGroup(positions7, 7, 1, BallColor.Blue);
            var plan7 = new MovePlan
            {
                From = new GridPos(0, 3),
                To = new GridPos(6, 2),
                Cleared = clear7,
                ScoreDelta = 300,
                PostMoveRng = session.Rng,
                Outcome = MoveOutcome.Cleared
            };
            session.Board.Set(new GridPos(0, 3), BallColor.Blue);

            session.Commit(plan7);
            Assert.AreEqual(7, session.LongestLine, "LongestLine should increase to 7");

            // Third clear: length 5 (should not decrease LongestLine)
            var positions5b = new[]
            {
                new GridPos(0, 4), new GridPos(1, 4), new GridPos(2, 4), new GridPos(3, 4), new GridPos(4, 4)
            };
            var clear5b = new ClearGroup(positions5b, 5, 1, BallColor.Green);
            var plan5b = new MovePlan
            {
                From = new GridPos(0, 5),
                To = new GridPos(4, 4),
                Cleared = clear5b,
                ScoreDelta = 100,
                PostMoveRng = session.Rng,
                Outcome = MoveOutcome.Cleared
            };
            session.Board.Set(new GridPos(0, 5), BallColor.Green);

            session.Commit(plan5b);
            Assert.AreEqual(7, session.LongestLine, "LongestLine should retain maximum of 7");
        }

        [Test]
        public void GameSession_Undo_RevertsLongestLineToPreMoveState()
        {
            var session = new GameSession();
            session.StartNewGame(1234U);

            // Set up a board state where a move can be made via TrySelect & ExecuteMove
            session.Board.Reset();
            // 4 balls in a row at (0,0)..(3,0)
            session.Board.Set(new GridPos(0, 0), BallColor.Red);
            session.Board.Set(new GridPos(1, 0), BallColor.Red);
            session.Board.Set(new GridPos(2, 0), BallColor.Red);
            session.Board.Set(new GridPos(3, 0), BallColor.Red);
            // 5th ball at (4, 1) can move to (4, 0) completing 5-line
            session.Board.Set(new GridPos(4, 1), BallColor.Red);
            // Extra ball far away so the board is not completely empty after clear
            session.Board.Set(new GridPos(8, 8), BallColor.Blue);

            Assert.AreEqual(0, session.LongestLine);

            Assert.IsTrue(session.TrySelect(new GridPos(4, 1)));
            Assert.IsTrue(session.ExecuteMove(new GridPos(4, 0)));

            Assert.AreEqual(5, session.LongestLine);
            Assert.AreEqual(1, session.LinesCleared);

            // Now Undo
            Assert.IsTrue(session.TryUndo());
            Assert.AreEqual(0, session.LongestLine, "Undo must revert LongestLine to state before the move");
            Assert.AreEqual(0, session.LinesCleared);
        }

        [Test]
        public void GameSession_OnGameOver_PassesCompleteSessionSummary()
        {
            var session = new GameSession();
            session.StartNewGame(9999U);
            session.SetBestScore(5000);

            SessionSummary receivedSummary = default;
            bool eventFired = false;
            session.OnGameOver += summary =>
            {
                eventFired = true;
                receivedSummary = summary;
            };

            // Trigger game over commit
            var plan = new MovePlan
            {
                From = new GridPos(0, 0),
                To = new GridPos(1, 0),
                IsGameOver = true,
                Outcome = MoveOutcome.GameOver,
                PostMoveRng = session.Rng
            };
            session.Board.Set(new GridPos(0, 0), BallColor.Yellow);

            session.Commit(plan);

            Assert.IsTrue(eventFired, "OnGameOver event should be fired");
            Assert.AreEqual(GamePhase.GameOver, session.Phase);
            Assert.AreEqual(session.Score, receivedSummary.FinalScore);
            Assert.AreEqual(5000, receivedSummary.BestScore);
            Assert.AreEqual(session.LongestLine, receivedSummary.LongestLine);
            Assert.AreEqual(session.LinesCleared, receivedSummary.LinesCleared);
            Assert.AreEqual(session.MoveCount, receivedSummary.TotalMoves);
            Assert.IsTrue(receivedSummary.CanContinue);
        }

        [Test]
        public void GameOverPopup_Populate_BindsAllFiveStatsAndContinueButton()
        {
            PopupFixture fixture = CreatePopupFixture("TestGameOverPopup");

            var summary = new SessionSummary(
                finalScore: 450,
                bestScore: 1200,
                longestLine: 6,
                linesCleared: 4,
                totalMoves: 18,
                canContinue: true);

            fixture.Popup.Populate(summary);

            Assert.AreEqual("450", fixture.FinalScore.text);
            Assert.AreEqual("1,200", fixture.BestScore.text);
            Assert.AreEqual("4", fixture.Lines.text);
            Assert.AreEqual("6", fixture.Longest.text);
            Assert.AreEqual("18", fixture.Moves.text);
            Assert.IsTrue(fixture.ContinueButton.gameObject.activeSelf);

            // Also assert New Game button is available without ad gate
            Assert.IsNotNull(fixture.Popup.NewGameButton);
            Assert.IsTrue(fixture.Popup.NewGameButton.gameObject.activeSelf);

            UnityEngine.Object.DestroyImmediate(fixture.Root);
        }

        [Test]
        public void DebugHud_MockSummaries_ExerciseEveryGameOverPopupBranch()
        {
            Assert.IsNotNull(DebugHud.MockSummaries, "The popup debug hub must expose mock summaries.");
            Assert.GreaterOrEqual(DebugHud.MockSummaries.Count, 2, "The hub needs an alternate fixture to cycle between.");

            for (int i = 0; i < DebugHud.MockSummaries.Count; i++)
            {
                SessionSummary summary = DebugHud.MockSummaries[i];
                PopupFixture fixture = CreatePopupFixture($"TestMockFixture{i}");

                Assert.DoesNotThrow(() => fixture.Popup.Populate(summary), $"Fixture {i} must populate without throwing.");

                Assert.AreEqual(
                    summary.FinalScore.ToString("N0", CultureInfo.InvariantCulture),
                    fixture.FinalScore.text,
                    $"Fixture {i}: final score must use N0 formatting.");
                Assert.AreEqual(
                    summary.BestScore.ToString("N0", CultureInfo.InvariantCulture),
                    fixture.BestScore.text,
                    $"Fixture {i}: best score must use N0 formatting.");
                Assert.AreEqual(
                    summary.LinesCleared.ToString(),
                    fixture.Lines.text,
                    $"Fixture {i}: lines cleared must not be swapped with the longest line.");
                Assert.AreEqual(
                    summary.LongestLine.ToString(),
                    fixture.Longest.text,
                    $"Fixture {i}: longest line must not be swapped with lines cleared.");
                Assert.AreEqual(summary.TotalMoves.ToString(), fixture.Moves.text, $"Fixture {i}: total moves must bind.");

                Assert.AreEqual(
                    summary.CanContinue,
                    fixture.ContinueButton.gameObject.activeSelf,
                    $"Fixture {i}: continue visibility must follow CanContinue.");

                bool expectsNewBest = summary.FinalScore > 0 && summary.FinalScore >= summary.BestScore;
                Assert.AreEqual(
                    expectsNewBest,
                    fixture.NewBestBadge.activeSelf,
                    $"Fixture {i}: new-best badge must follow finalScore >= bestScore.");

                UnityEngine.Object.DestroyImmediate(fixture.Root);
            }
        }

        private sealed class PopupFixture
        {
            public GameObject Root;
            public GameOverPopup Popup;
            public TextMeshProUGUI FinalScore;
            public TextMeshProUGUI BestScore;
            public TextMeshProUGUI Lines;
            public TextMeshProUGUI Longest;
            public TextMeshProUGUI Moves;
            public Button ContinueButton;
            public Button NewGameButton;
            public GameObject NewBestBadge;
        }

        /// <summary>
        /// Builds a GameOverPopup with every serialized reference bound the way the
        /// Popup_GameOver prefab binds them, so Populate can be asserted in isolation.
        /// </summary>
        private static PopupFixture CreatePopupFixture(string name)
        {
            var fixture = new PopupFixture { Root = new GameObject(name) };
            fixture.Popup = fixture.Root.AddComponent<GameOverPopup>();
            fixture.FinalScore = CreateChildText(fixture.Root, "FinalScore");
            fixture.BestScore = CreateChildText(fixture.Root, "BestScore");
            fixture.Lines = CreateChildText(fixture.Root, "Lines");
            fixture.Longest = CreateChildText(fixture.Root, "Longest");
            fixture.Moves = CreateChildText(fixture.Root, "Moves");
            fixture.ContinueButton = CreateChildButton(fixture.Root, "BtnContinue");
            fixture.NewGameButton = CreateChildButton(fixture.Root, "BtnNewGame");
            fixture.NewBestBadge = new GameObject("NewBestBadge");
            fixture.NewBestBadge.transform.SetParent(fixture.Root.transform);

            SetPrivateField(fixture.Popup, "m_FinalScoreText", fixture.FinalScore);
            SetPrivateField(fixture.Popup, "m_BestScoreText", fixture.BestScore);
            SetPrivateField(fixture.Popup, "m_LinesClearedText", fixture.Lines);
            SetPrivateField(fixture.Popup, "m_LongestLineText", fixture.Longest);
            SetPrivateField(fixture.Popup, "m_TotalMovesText", fixture.Moves);
            SetPrivateField(fixture.Popup, "m_ContinueButton", fixture.ContinueButton);
            SetPrivateField(fixture.Popup, "m_NewGameButton", fixture.NewGameButton);
            SetPrivateField(fixture.Popup, "m_NewBestBadge", fixture.NewBestBadge);

            return fixture;
        }

        private static TextMeshProUGUI CreateChildText(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform);
            return go.GetComponent<TextMeshProUGUI>();
        }

        private static Button CreateChildButton(GameObject parent, string name)
        {
            var go = new GameObject(name, typeof(Button));
            go.transform.SetParent(parent.transform);
            return go.GetComponent<Button>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            typeof(GameOverPopup)
                .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(target, value);
        }
    }
}

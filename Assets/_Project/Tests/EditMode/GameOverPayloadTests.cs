using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
            var go = new GameObject("TestGameOverPopup");
            var popup = go.AddComponent<GameOverPopup>();

            var finalScoreGo = new GameObject("FinalScore", typeof(TextMeshProUGUI));
            finalScoreGo.transform.SetParent(go.transform);
            var finalScoreText = finalScoreGo.GetComponent<TextMeshProUGUI>();

            var bestScoreGo = new GameObject("BestScore", typeof(TextMeshProUGUI));
            bestScoreGo.transform.SetParent(go.transform);
            var bestScoreText = bestScoreGo.GetComponent<TextMeshProUGUI>();

            var linesGo = new GameObject("Lines", typeof(TextMeshProUGUI));
            linesGo.transform.SetParent(go.transform);
            var linesText = linesGo.GetComponent<TextMeshProUGUI>();

            var longestGo = new GameObject("Longest", typeof(TextMeshProUGUI));
            longestGo.transform.SetParent(go.transform);
            var longestText = longestGo.GetComponent<TextMeshProUGUI>();

            var movesGo = new GameObject("Moves", typeof(TextMeshProUGUI));
            movesGo.transform.SetParent(go.transform);
            var movesText = movesGo.GetComponent<TextMeshProUGUI>();

            var continueBtnGo = new GameObject("BtnContinue", typeof(Button));
            continueBtnGo.transform.SetParent(go.transform);
            var continueBtn = continueBtnGo.GetComponent<Button>();

            var newGameBtnGo = new GameObject("BtnNewGame", typeof(Button));
            newGameBtnGo.transform.SetParent(go.transform);
            var newGameBtn = newGameBtnGo.GetComponent<Button>();

            // Set private fields via reflection
            typeof(GameOverPopup).GetField("m_FinalScoreText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(popup, finalScoreText);
            typeof(GameOverPopup).GetField("m_BestScoreText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(popup, bestScoreText);
            typeof(GameOverPopup).GetField("m_LinesClearedText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(popup, linesText);
            typeof(GameOverPopup).GetField("m_LongestLineText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(popup, longestText);
            typeof(GameOverPopup).GetField("m_TotalMovesText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(popup, movesText);
            typeof(GameOverPopup).GetField("m_ContinueButton", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(popup, continueBtn);
            typeof(GameOverPopup).GetField("m_NewGameButton", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(popup, newGameBtn);

            var summary = new SessionSummary(
                finalScore: 450,
                bestScore: 1200,
                longestLine: 6,
                linesCleared: 4,
                totalMoves: 18,
                canContinue: true);

            popup.Populate(summary);

            Assert.AreEqual("450", finalScoreText.text);
            Assert.AreEqual("1,200", bestScoreText.text);
            Assert.AreEqual("4", linesText.text);
            Assert.AreEqual("6", longestText.text);
            Assert.AreEqual("18", movesText.text);
            Assert.IsTrue(continueBtn.gameObject.activeSelf);

            // Also assert New Game button is available without ad gate
            Assert.IsNotNull(popup.NewGameButton);
            Assert.IsTrue(popup.NewGameButton.gameObject.activeSelf);

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}

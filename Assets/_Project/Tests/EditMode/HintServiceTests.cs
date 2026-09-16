using System.Collections.Generic;
using Line98.Core;
using Line98.Data;
using Line98.Gameplay;
using Line98.Presentation;
using Line98.Presentation.Animation;
using NUnit.Framework;
using UnityEngine;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class HintServiceTests
    {
        private BoardModel m_Board;
        private PreviewQueue m_Queue;
        private ScoreRules m_Rules;
        private List<GridPos> m_PathOut;

        [SetUp]
        public void SetUp()
        {
            m_Board = new BoardModel();
            m_Queue = new PreviewQueue();
            m_Rules = ScoreRules.Default;
            m_PathOut = new List<GridPos>(BoardModel.CellCount);
        }

        [Test]
        public void HintPathReturned_OrderedSourceToDestination_MatchesPathfinder()
        {
            // Set up a board where moving (2,4) to (0,4) completes a 5-line of Red
            m_Board.Set(new GridPos(0, 0), BallColor.Red);
            m_Board.Set(new GridPos(0, 1), BallColor.Red);
            m_Board.Set(new GridPos(0, 2), BallColor.Red);
            m_Board.Set(new GridPos(0, 3), BallColor.Red);
            // 5th Red ball is at (2, 4)
            m_Board.Set(new GridPos(2, 4), BallColor.Red);

            bool found = HintService.TryFindBestMove(m_Board, m_Queue, m_Rules, out GridPos from, out GridPos to, m_PathOut);
            Assert.IsTrue(found, "Hint should find a legal move");
            Assert.AreEqual(new GridPos(2, 4), from);
            Assert.AreEqual(new GridPos(0, 4), to);

            // Path must be returned, start at 'from', end at 'to'
            Assert.IsNotNull(m_PathOut);
            Assert.IsTrue(m_PathOut.Count >= 2);
            Assert.AreEqual(from, m_PathOut[0]);
            Assert.AreEqual(to, m_PathOut[m_PathOut.Count - 1]);

            // Verify path matches a fresh Pathfinder query
            var freshPath = new List<GridPos>(BoardModel.CellCount);
            Assert.IsTrue(Pathfinder.TryFindPath(m_Board, from, to, freshPath));
            Assert.AreEqual(freshPath.Count, m_PathOut.Count);
            for (int i = 0; i < freshPath.Count; i++)
            {
                Assert.AreEqual(freshPath[i], m_PathOut[i]);
            }
        }

        [Test]
        public void Hint_NeverMutatesBoard()
        {
            m_Board.Set(new GridPos(2, 2), BallColor.Yellow);
            m_Board.Set(new GridPos(3, 3), BallColor.Blue);

            // Take board snapshot
            var colorsBefore = new BallColor[BoardModel.CellCount];
            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                colorsBefore[i] = m_Board.ColorAt(i);
            }

            bool found = HintService.TryFindBestMove(m_Board, m_Queue, m_Rules, out GridPos from, out GridPos to, m_PathOut);
            Assert.IsTrue(found);

            // GDD §13 requirement: "Never automatically execute" — board must remain completely unchanged
            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                Assert.AreEqual(colorsBefore[i], m_Board.ColorAt(i), $"Cell {i} was mutated by HintService!");
            }
        }

        [Test]
        public void GameSession_RequestHint_ReturnsPath()
        {
            var session = new GameSession();
            session.StartNewGame();

            var path = new List<GridPos>();
            bool requested = session.RequestHint(out GridPos from, out GridPos to, path);

            if (requested)
            {
                Assert.IsTrue(from.IsValid);
                Assert.IsTrue(to.IsValid);
                Assert.IsFalse(session.Board.IsEmpty(from));
                Assert.IsTrue(session.Board.IsEmpty(to));
                Assert.IsTrue(path.Count >= 2);
                Assert.AreEqual(from, path[0]);
                Assert.AreEqual(to, path[path.Count - 1]);
            }
        }

        [Test]
        public void PathPreviewView_ShowAndHide_TogglesVisibilityCleanly()
        {
            var rootGo = new GameObject("TestRoot");
            var boardGo = new GameObject("BoardView");
            boardGo.transform.SetParent(rootGo.transform);
            var boardView = boardGo.AddComponent<BoardView>();
            boardView.Initialize();

            var preview = new PathPreviewView(boardView, null, null, rootGo.transform);
            Assert.IsFalse(preview.IsVisible);

            var path = new List<GridPos>
            {
                new GridPos(0, 0),
                new GridPos(0, 1),
                new GridPos(0, 2)
            };

            object owner = new object();
            preview.ShowPath(path, 1.0f, owner);

            Assert.IsTrue(preview.IsVisible);
            Assert.IsTrue(preview.LineRenderer.enabled);
            Assert.AreEqual(3, preview.LineRenderer.positionCount);
            Assert.IsTrue(preview.DestRingObject.activeSelf);

            // Cancel by wrong owner should do nothing
            preview.CancelByOwner(new object());
            Assert.IsTrue(preview.IsVisible);

            // Cancel by actual owner should hide
            preview.CancelByOwner(owner);
            Assert.IsFalse(preview.IsVisible);
            Assert.IsFalse(preview.LineRenderer.enabled);
            Assert.IsFalse(preview.DestRingObject.activeSelf);

            Object.DestroyImmediate(rootGo);
        }
    }
}

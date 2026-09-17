using System.Collections.Generic;
using Line98.Core;
using Line98.Data;
using Line98.Gameplay;
using Line98.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public sealed class InputRouterTests
    {
        private GameObject m_Root;
        private InputRouter m_InputRouter;
        private GameSession m_Session;

        [SetUp]
        public void SetUp()
        {
            m_Root = new GameObject("InputRouterTests");
            m_InputRouter = m_Root.AddComponent<InputRouter>();
            m_Session = new GameSession();
            m_Session.StartNewGame(0x1234ABCD);
            m_InputRouter.Initialize(null, null, m_Session);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Root != null)
            {
                Object.DestroyImmediate(m_Root);
            }
        }

        [Test]
        public void HandleCellTapped_AfterHintSelection_MovesToDifferentLegalDestination()
        {
            Assert.IsTrue(m_Session.RequestHint(out GridPos hintedFrom, out GridPos hintedTo));
            Assert.IsTrue(m_Session.TrySelect(hintedFrom));

            GridPos clickedTo = FindDifferentLegalDestination(hintedFrom, hintedTo);
            BallColor movingColor = m_Session.Board.ColorAt(hintedFrom);

            m_InputRouter.HandleCellTapped(clickedTo);

            Assert.AreNotEqual(hintedTo, clickedTo);
            Assert.AreEqual(1, m_Session.MoveCount);
            Assert.AreEqual(movingColor, m_Session.Board.ColorAt(clickedTo));
        }

        private GridPos FindDifferentLegalDestination(GridPos from, GridPos hintedTo)
        {
            var path = new List<GridPos>(BoardModel.CellCount);
            for (int index = 0; index < BoardModel.CellCount; index++)
            {
                GridPos candidate = GridPos.FromIndex(index);
                if (candidate != hintedTo && m_Session.Board.IsEmpty(candidate) &&
                    Pathfinder.TryFindPath(m_Session.Board, from, candidate, path))
                {
                    return candidate;
                }
            }

            Assert.Fail("Expected a legal destination different from the hinted position.");
            return default;
        }
    }
}

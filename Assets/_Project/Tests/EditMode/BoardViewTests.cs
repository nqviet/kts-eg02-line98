using NUnit.Framework;
using UnityEngine;
using Line98.Core;
using Line98.Presentation;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class BoardViewTests
    {
        private GameObject m_BoardGo;
        private BoardView m_BoardView;

        [SetUp]
        public void SetUp()
        {
            m_BoardGo = new GameObject("TestBoardView");
            m_BoardView = m_BoardGo.AddComponent<BoardView>();
            m_BoardView.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_BoardGo != null)
            {
                Object.DestroyImmediate(m_BoardGo);
            }
        }

        [Test]
        public void All81GridPositions_RoundTripThroughWorldToGridAccurately()
        {
            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos original = new GridPos(x, y);
                    Vector3 worldPos = m_BoardView.GridToWorld(original);

                    bool mapped = m_BoardView.WorldToGrid(worldPos, out GridPos resolved);

                    Assert.IsTrue(mapped, $"Failed to map world position {worldPos} for cell ({x},{y})");
                    Assert.AreEqual(original, resolved, $"Round-trip mismatch for cell ({x},{y})");
                }
            }
        }

        [Test]
        public void PointsFarOutsideBoard_ReturnFalseFromWorldToGrid()
        {
            Vector3 farOut = new Vector3(50f, 0f, 50f);
            bool mapped = m_BoardView.WorldToGrid(farOut, out GridPos resolved);

            Assert.IsFalse(mapped, "Points far outside the board must not resolve to any grid position");
        }

        [Test]
        public void CenterCell_IsLocatedAtBoardOrigin()
        {
            GridPos center = new GridPos(4, 4);
            Vector3 worldPos = m_BoardView.GridToWorld(center);

            Assert.AreEqual(0f, worldPos.x, 0.001f);
            Assert.AreEqual(0f, worldPos.z, 0.001f);
        }
    }
}

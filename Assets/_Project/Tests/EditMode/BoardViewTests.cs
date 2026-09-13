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
        public void RowDepth_AllCellCentersAndEdgesRemainPickable()
        {
            var theme = ScriptableObject.CreateInstance<Line98.Data.BoardThemeSO>();
            try
            {
                var serialized = new UnityEditor.SerializedObject(theme);
                float scale = 1f / Mathf.Sin(58f * Mathf.Deg2Rad);
                serialized.FindProperty("m_RowPitchScale").floatValue = scale;
                serialized.ApplyModifiedProperties();
                m_BoardView.Initialize(theme);
                m_BoardGo.transform.position = new Vector3(2f,0f,3f);
                for (int y=0;y<9;y++)
                for (int x=0;x<9;x++)
                {
                    var expected = new GridPos(x,y);
                    var world = m_BoardView.GridToWorld(expected);
                    Assert.IsTrue(m_BoardView.WorldToGrid(world + new Vector3(.49f,0,.49f*scale),out var actual));
                    Assert.AreEqual(expected,actual);
                }
                Assert.IsFalse(m_BoardView.WorldToGrid(m_BoardView.GridToWorld(new GridPos(4,8)) + new Vector3(0,0,.51f*scale),out _));
            }
            finally { Object.DestroyImmediate(theme); }
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

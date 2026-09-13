using NUnit.Framework;
using UnityEngine;
using Line98.Presentation;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class BoardFitSolverTests
    {
        private Camera m_Camera;
        private GameObject m_CameraGo;

        [SetUp]
        public void SetUp()
        {
            m_CameraGo = new GameObject("TestCamera");
            m_Camera = m_CameraGo.AddComponent<Camera>();
            m_Camera.fieldOfView = 28f;
            m_Camera.orthographic = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (m_CameraGo != null)
            {
                Object.DestroyImmediate(m_CameraGo);
            }
        }

        [Test]
        public void SolveCameraDistance_ReturnsFiniteDistanceWithinBounds()
        {
            float dist = BoardFitSolver.SolveCameraDistance(
                m_Camera,
                boardExtentX: 9.0f,
                boardExtentZ: 9.0f,
                center: Vector3.zero,
                pitchAngle: 58.0f,
                yawAngle: 0.0f,
                minViewport: BoardFitSolver.DefaultMinViewport,
                maxViewport: BoardFitSolver.DefaultMaxViewport,
                minDistance: 8.0f,
                maxDistance: 50.0f);

            Assert.GreaterOrEqual(dist, 8.0f);
            Assert.LessOrEqual(dist, 50.0f);
        }

        [Test]
        public void SolveCameraDistance_TighterViewportRequiresGreaterDistance()
        {
            Vector2 looseMin = new Vector2(0.01f, 0.01f);
            Vector2 looseMax = new Vector2(0.99f, 0.99f);

            Vector2 tightMin = new Vector2(0.20f, 0.30f);
            Vector2 tightMax = new Vector2(0.80f, 0.70f);

            float looseDist = BoardFitSolver.SolveCameraDistance(
                m_Camera, 9.0f, 9.0f, Vector3.zero, 58f, 0f, looseMin, looseMax);

            float tightDist = BoardFitSolver.SolveCameraDistance(
                m_Camera, 9.0f, 9.0f, Vector3.zero, 58f, 0f, tightMin, tightMax);

            Assert.Greater(tightDist, looseDist, "Tighter viewport padding must push camera farther away");
        }
    }
}

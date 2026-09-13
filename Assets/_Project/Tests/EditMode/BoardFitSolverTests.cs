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

        [Test]
        public void SolveOrthographicSize_ReturnsFiniteSizeWithinBounds()
        {
            float size = BoardFitSolver.SolveOrthographicSize(
                m_Camera,
                boardExtentX: 9.0f,
                boardExtentZ: 9.0f,
                center: Vector3.zero,
                pitchAngle: 58.0f,
                yawAngle: 0.0f,
                minViewport: BoardFitSolver.DefaultMinViewport,
                maxViewport: BoardFitSolver.DefaultMaxViewport,
                minOrthoSize: 2.0f,
                maxOrthoSize: 30.0f);

            Assert.GreaterOrEqual(size, 2.0f);
            Assert.LessOrEqual(size, 30.0f);
        }

        [TestCase(1920f)]
        [TestCase(2340f)]
        [TestCase(2640f)]
        public void HudViewport_ContainsSquareBoardAndCentersIt(float height)
        {
            var rigGo = new GameObject("TestHudRig");
            try
            {
                var rig = rigGo.AddComponent<CameraRig>();
                float depth = 9.4f / Mathf.Sin(58f * Mathf.Deg2Rad);
                rig.Initialize(null,Vector3.zero,9.4f,depth);
                rig.Camera.aspect = 1080f / height;
                var rect = HudLayoutSolver.Solve(1080f,height,60f,48f).BoardViewportRect;
                rig.SetTargetViewport(rect.min,rect.max);
                var center = rig.Camera.WorldToViewportPoint(Vector3.zero);
                Assert.AreEqual(rect.center.x,center.x,.001f);
                Assert.AreEqual(rect.center.y,center.y,.001f);
                var lower = rig.Camera.WorldToViewportPoint(new Vector3(-4.7f,0,-depth*.5f));
                var upper = rig.Camera.WorldToViewportPoint(new Vector3(4.7f,0,depth*.5f));
                Assert.GreaterOrEqual(lower.x,rect.xMin-.001f);
                Assert.GreaterOrEqual(lower.y,rect.yMin-.001f);
                Assert.LessOrEqual(upper.x,rect.xMax+.001f);
                Assert.LessOrEqual(upper.y,rect.yMax+.001f);
                Assert.AreEqual((upper.x-lower.x)*1080,(upper.y-lower.y)*height,1f);
            }
            finally { Object.DestroyImmediate(rigGo); }
        }

        [Test]
        public void SolveOrthographicSize_TighterViewportRequiresGreaterSize()
        {
            Vector2 looseMin = new Vector2(0.01f, 0.01f);
            Vector2 looseMax = new Vector2(0.99f, 0.99f);

            Vector2 tightMin = new Vector2(0.20f, 0.30f);
            Vector2 tightMax = new Vector2(0.80f, 0.70f);

            float looseSize = BoardFitSolver.SolveOrthographicSize(
                m_Camera, 9.0f, 9.0f, Vector3.zero, 58f, 0f, looseMin, looseMax);

            float tightSize = BoardFitSolver.SolveOrthographicSize(
                m_Camera, 9.0f, 9.0f, Vector3.zero, 58f, 0f, tightMin, tightMax);

            Assert.Greater(tightSize, looseSize, "Tighter viewport padding must require larger orthographic size (zooming out)");
        }

        [Test]
        public void SolveOrthographicSize_CornersFitWithinViewportBounds()
        {
            Vector2 minVp = BoardFitSolver.DefaultMinViewport;
            Vector2 maxVp = BoardFitSolver.DefaultMaxViewport;

            float solvedSize = BoardFitSolver.SolveOrthographicSize(
                m_Camera, 9.0f, 9.0f, Vector3.zero, 58f, 0f, minVp, maxVp);

            m_Camera.orthographic = true;
            m_Camera.orthographicSize = solvedSize;
            Quaternion rot = Quaternion.Euler(58f, 0f, 0f);
            m_Camera.transform.rotation = rot;
            m_Camera.transform.position = rot * new Vector3(0f, 0f, -20f);

            Vector3[] corners =
            {
                new Vector3(-4.5f, 0f, -4.5f),
                new Vector3( 4.5f, 0f, -4.5f),
                new Vector3(-4.5f, 0f,  4.5f),
                new Vector3( 4.5f, 0f,  4.5f)
            };

            foreach (var corner in corners)
            {
                Vector3 vp = m_Camera.WorldToViewportPoint(corner);
                Assert.GreaterOrEqual(vp.x, minVp.x - 0.01f, $"Corner {corner} vp.x must be >= minVp.x");
                Assert.LessOrEqual(vp.x, maxVp.x + 0.01f, $"Corner {corner} vp.x must be <= maxVp.x");
                Assert.GreaterOrEqual(vp.y, minVp.y - 0.01f, $"Corner {corner} vp.y must be >= minVp.y");
                Assert.LessOrEqual(vp.y, maxVp.y + 0.01f, $"Corner {corner} vp.y must be <= maxVp.y");
            }
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Line98.Core;
using Line98.Presentation;
using Line98.Presentation.Animation;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class BallGlowTests
    {
        private GameObject m_RootGo;
        private Mesh m_BallMesh;
        private Material m_GlowMaterial;
        private Material[] m_BallMaterials;

        [SetUp]
        public void SetUp()
        {
            m_RootGo = new GameObject("BallGlowTests_Root");
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_BallMesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(sphere);

            m_GlowMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Ball_GlowShell.mat");

            string[] matPaths = new string[]
            {
                "Assets/Art/Materials/M_Ball_Red.mat",
                "Assets/Art/Materials/M_Ball_Orange.mat",
                "Assets/Art/Materials/M_Ball_Yellow.mat",
                "Assets/Art/Materials/M_Ball_Green.mat",
                "Assets/Art/Materials/M_Ball_Cyan.mat",
                "Assets/Art/Materials/M_Ball_Purple.mat",
                "Assets/Art/Materials/M_Ball_Blue.mat"
            };

            m_BallMaterials = new Material[matPaths.Length];
            for (int i = 0; i < matPaths.Length; i++)
            {
                m_BallMaterials[i] = AssetDatabase.LoadAssetAtPath<Material>(matPaths[i]);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (m_RootGo != null)
            {
                Object.DestroyImmediate(m_RootGo);
            }
        }

        [Test]
        public void BallView_GlowShell_HasExtendedScale_AndTogglesOnSelection()
        {
            var ballGo = new GameObject("TestBall");
            ballGo.transform.SetParent(m_RootGo.transform);
            var ballView = ballGo.AddComponent<BallView>();
            ballView.InitializeHierarchy(m_BallMesh, null, m_GlowMaterial, null);

            var glowTransform = ballGo.transform.Find("GlowShell");
            Assert.IsNotNull(glowTransform, "GlowShell child object must exist");
            Assert.IsFalse(glowTransform.gameObject.activeSelf, "GlowShell must be inactive initially");
            Assert.AreEqual(1.10f, ballView.GlowShellScale, 0.001f);
            Assert.AreEqual(1.10f, glowTransform.localScale.x, 0.001f);

            ballView.SetSelected(true);
            Assert.IsTrue(glowTransform.gameObject.activeSelf, "GlowShell must be active when ball is selected");

            ballView.SetSelected(false);
            Assert.IsFalse(glowTransform.gameObject.activeSelf, "GlowShell must be inactive when ball is deselected");
        }

        [Test]
        public void BallView_SelectionHalo_PopsInBreathesAndCollapses()
        {
            var ballGo = new GameObject("AnimatedTestBall");
            ballGo.transform.SetParent(m_RootGo.transform);
            var ballView = ballGo.AddComponent<BallView>();
            var tweenRunner = new TweenRunner();
            ballView.InitializeHierarchy(m_BallMesh, null, m_GlowMaterial, null);
            ballView.Setup(new GridPos(0, 0), BallColor.Red, null, m_GlowMaterial, tweenRunner);

            var glowTransform = ballGo.transform.Find("GlowShell");
            float baseScale = ballView.GlowShellScale;
            float visualY = ballView.Visual.localPosition.y;

            ballView.SetSelected(true);
            Assert.IsTrue(glowTransform.gameObject.activeSelf, "GlowShell must activate immediately on selection");
            Assert.Less(glowTransform.localScale.x, baseScale, "GlowShell must begin from its collapsed scale");

            float minScale = glowTransform.localScale.x;
            float maxScale = minScale;
            for (int i = 0; i < 28; i++)
            {
                tweenRunner.Tick(0.016f);
                minScale = Mathf.Min(minScale, glowTransform.localScale.x);
                maxScale = Mathf.Max(maxScale, glowTransform.localScale.x);
            }

            Assert.Greater(maxScale - minScale, 0.005f, "GlowShell scale must animate while selected");
            Assert.GreaterOrEqual(minScale, baseScale * 0.75f);
            Assert.LessOrEqual(maxScale, baseScale * 1.15f);
            Assert.AreEqual(visualY, ballView.Visual.localPosition.y, 0.0001f,
                "Selection halo animation must not lift the visual");

            ballView.SetSelected(false);
            for (int i = 0; i < 7; i++)
            {
                tweenRunner.Tick(0.016f);
            }

            Assert.IsFalse(glowTransform.gameObject.activeSelf, "GlowShell must hide after its collapse animation");
            Assert.AreEqual(baseScale, glowTransform.localScale.x, 0.0001f);
            Assert.AreEqual(0, tweenRunner.ActiveCount);
        }

        [Test]
        public void BallView_HaloTween_DoesNotSurvivePoolRelease()
        {
            var ballGo = new GameObject("ReleaseTestBall");
            ballGo.transform.SetParent(m_RootGo.transform);
            var ballView = ballGo.AddComponent<BallView>();
            var tweenRunner = new TweenRunner();
            ballView.InitializeHierarchy(m_BallMesh, null, m_GlowMaterial, null);
            ballView.Setup(new GridPos(0, 0), BallColor.Red, null, m_GlowMaterial, tweenRunner);

            ballView.SetSelected(true);
            ballView.Release();

            Assert.AreEqual(0, tweenRunner.ActiveCount, "Pool release must cancel the active halo tween");
            Assert.IsFalse(ballGo.transform.Find("GlowShell").gameObject.activeSelf, "Released ball must not retain a visible halo");
        }

        [Test]
        public void BallViewManager_DerivesDistinctTintedGlowMaterials_PerColor()
        {
            var manager = new BallViewManager();
            manager.Initialize(
                m_BallMesh,
                null,
                m_BallMaterials,
                m_GlowMaterial,
                null,
                m_RootGo.transform,
                null,
                1.0f);

            var redGlow = manager.GetGlowMaterial(BallColor.Red);
            var blueGlow = manager.GetGlowMaterial(BallColor.Blue);

            Assert.IsNotNull(redGlow, "Red glow material must not be null");
            Assert.IsNotNull(blueGlow, "Blue glow material must not be null");
            Assert.AreNotSame(redGlow, blueGlow, "Red and Blue must have distinct glow material instances");

            Color redRim = redGlow.GetColor("_RimColor");
            Color blueRim = blueGlow.GetColor("_RimColor");

            Assert.Greater(redRim.r, redRim.b, "Red rim glow must have red dominant over blue");
            Assert.Greater(blueRim.b, blueRim.r, "Blue rim glow must have blue dominant over red");

            manager.Dispose();
        }

        [Test]
        public void BallView_Setup_AppliesColorSpecificGlowMaterial()
        {
            var manager = new BallViewManager();
            manager.Initialize(
                m_BallMesh,
                null,
                m_BallMaterials,
                m_GlowMaterial,
                null,
                m_RootGo.transform,
                null,
                1.0f);

            var ballView = manager.SpawnBall(new GridPos(0, 0), BallColor.Cyan, Vector3.zero);
            var glowRenderer = ballView.transform.Find("GlowShell").GetComponent<MeshRenderer>();
            var cyanGlow = manager.GetGlowMaterial(BallColor.Cyan);

            Assert.AreSame(cyanGlow, glowRenderer.sharedMaterial, "Spawned cyan ball must use cyan glow material");

            manager.Dispose();
        }

        [Test]
        public void BallView_ActionShake_DampedOscillationAndReset()
        {
            var ballGo = new GameObject("ShakeTestBall");
            ballGo.transform.SetParent(m_RootGo.transform);
            var ballView = ballGo.AddComponent<BallView>();
            var tweenRunner = new TweenRunner();
            ballView.InitializeHierarchy(m_BallMesh, null, m_GlowMaterial, null);
            ballView.Setup(new GridPos(0, 0), BallColor.Red, null, m_GlowMaterial, tweenRunner);

            ballView.PlayShake(Vector3.right, 0.08f);
            Assert.AreEqual(1, tweenRunner.ActiveCount, "PlayShake must register a tween with TweenRunner");

            float minX = 0f;
            float maxX = 0f;
            for (int i = 0; i < 20; i++)
            {
                tweenRunner.Tick(0.016f);
                minX = Mathf.Min(minX, ballView.ShakeOffset.x);
                maxX = Mathf.Max(maxX, ballView.ShakeOffset.x);
            }

            Assert.Less(minX, -0.005f, "Shake oscillation must swing into negative X");
            Assert.Greater(maxX, 0.005f, "Shake oscillation must swing into positive X");
            Assert.AreEqual(0f, ballView.ShakeOffset.y, 0.0001f, "Shake must not introduce Y offset");
            Assert.AreEqual(0f, ballView.ShakeOffset.z, 0.0001f, "Shake along right axis must have 0 Z offset");

            // Tick past the 240ms duration
            for (int i = 0; i < 10; i++)
            {
                tweenRunner.Tick(0.016f);
            }

            Assert.AreEqual(0, tweenRunner.ActiveCount, "Tween must complete after 240ms");
            Assert.AreEqual(Vector3.zero, ballView.ShakeOffset, "Shake offset must return to Vector3.zero upon completion");
        }

        [Test]
        public void BallView_ActionShake_FollowsShakeAxis()
        {
            var ballGo = new GameObject("ShakeAxisBall");
            ballGo.transform.SetParent(m_RootGo.transform);
            var ballView = ballGo.AddComponent<BallView>();
            ballView.InitializeHierarchy(m_BallMesh, null, m_GlowMaterial, null);

            ballView.ShakeAxis = Vector3.forward;
            ballView.OnTweenUpdate(BallView.ActionShake, 0.06f);

            Assert.AreEqual(new Vector3(0f, 0f, 0.06f), ballView.ShakeOffset, "ShakeOffset must follow configured ShakeAxis");
            Assert.AreEqual(0.06f, ballView.Visual.localPosition.z, 0.0001f, "Visual local Z must reflect ShakeOffset Z");

            ballView.OnTweenComplete(BallView.ActionShake);
            Assert.AreEqual(Vector3.zero, ballView.ShakeOffset, "OnTweenComplete must reset ShakeOffset");
        }

        [Test]
        public void BallView_ShakeTween_DoesNotSurvivePoolRelease()
        {
            var ballGo = new GameObject("ShakeReleaseBall");
            ballGo.transform.SetParent(m_RootGo.transform);
            var ballView = ballGo.AddComponent<BallView>();
            var tweenRunner = new TweenRunner();
            ballView.InitializeHierarchy(m_BallMesh, null, m_GlowMaterial, null);
            ballView.Setup(new GridPos(0, 0), BallColor.Red, null, m_GlowMaterial, tweenRunner);

            ballView.PlayShake(Vector3.right, 0.08f);
            Assert.AreEqual(1, tweenRunner.ActiveCount);

            ballView.Release();
            Assert.AreEqual(0, tweenRunner.ActiveCount, "Releasing ball must cancel active shake tween");
            Assert.AreEqual(Vector3.zero, ballView.ShakeOffset, "Released ball must reset shake offset");
        }
    }
}

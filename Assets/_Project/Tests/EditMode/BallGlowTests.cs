using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using Line98.Core;
using Line98.Presentation;

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
    }
}

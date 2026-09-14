using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Data;
using Line98.Presentation.Vfx;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class VfxServiceTests
    {
        private const string CatalogPath = "Assets/_Project/Content/Definitions/VfxCatalog_Default.asset";
        private GameObject m_TestRoot;
        private VfxCatalogSO m_Catalog;
        private VfxService m_VfxService;

        [SetUp]
        public void SetUp()
        {
            m_TestRoot = new GameObject("VfxTestRoot");
            m_Catalog = AssetDatabase.LoadAssetAtPath<VfxCatalogSO>(CatalogPath);
            Assert.IsNotNull(m_Catalog, $"Missing VfxCatalog at {CatalogPath}");

            m_VfxService = new VfxService(m_Catalog, m_TestRoot.transform);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_TestRoot != null)
            {
                Object.DestroyImmediate(m_TestRoot);
            }
        }

        [Test]
        public void BurstConcurrencyCap_EnforcedUnder10kPlays()
        {
            for (int i = 0; i < 200; i++)
            {
                bool played = m_VfxService.PlayBurst("ClearTier1", Vector3.zero);
                Assert.IsTrue(played, $"Failed to play burst at iteration {i}");
                Assert.LessOrEqual(m_VfxService.ActiveBurstCount, VfxService.MaxBurstConcurrency,
                    $"Burst concurrency exceeded cap of {VfxService.MaxBurstConcurrency} at iteration {i}");
            }
        }

        [Test]
        public void PopupConcurrencyCap_Enforced()
        {
            for (int i = 0; i < 20; i++)
            {
                bool played = m_VfxService.PlayScorePopup(100 * (i + 1), Vector3.zero);
                Assert.IsTrue(played);
                Assert.LessOrEqual(m_VfxService.ActivePopupCount, VfxService.MaxPopupConcurrency,
                    $"Popup concurrency exceeded cap of {VfxService.MaxPopupConcurrency}");
            }
        }

        [Test]
        public void PoolReuse_RecyclesExpiredInstances()
        {
            m_VfxService.PlayBurst("PlacementSettle", Vector3.zero);
            m_VfxService.PlayBurst("PlacementSettle", Vector3.one);
            Assert.AreEqual(2, m_VfxService.ActiveBurstCount);

            // Tick past lifetime (0.3s)
            m_VfxService.Tick(0.5f);
            Assert.AreEqual(0, m_VfxService.ActiveBurstCount, "All burst instances should be recycled after lifetime.");

            // Play again from recycled pool
            m_VfxService.PlayBurst("PlacementSettle", Vector3.zero);
            Assert.AreEqual(1, m_VfxService.ActiveBurstCount);
        }

        [Test]
        public void CancelByOwner_RecyclesOnlyMatchingOwner()
        {
            object ownerA = new object();
            object ownerB = new object();

            m_VfxService.PlayBurst("ClearTier1", Vector3.zero, ownerA);
            m_VfxService.PlayBurst("ClearTier2", Vector3.one, ownerB);
            Assert.AreEqual(2, m_VfxService.ActiveBurstCount);

            m_VfxService.CancelByOwner(ownerA);
            Assert.AreEqual(1, m_VfxService.ActiveBurstCount, "Only ownerA instances should be cancelled.");

            m_VfxService.CancelAll();
            Assert.AreEqual(0, m_VfxService.ActiveBurstCount, "CancelAll should recycle every active instance.");
        }

        [Test]
        public void AttachAndDetachTrail_ReparentsAndClears()
        {
            var ballGo = new GameObject("TestBall");
            ballGo.transform.SetParent(m_TestRoot.transform);

            var trailGo = m_VfxService.AttachTrail(ballGo.transform, Color.cyan);
            Assert.IsNotNull(trailGo, "AttachTrail must return an instance.");
            Assert.AreEqual(ballGo.transform, trailGo.transform.parent, "Trail must be parented to target ball during flight.");

            m_VfxService.DetachTrail(ballGo.transform);
            Assert.AreNotEqual(ballGo.transform, trailGo.transform.parent, "Trail must be unparented from ball after flight.");

            Object.DestroyImmediate(ballGo);
        }
    }
}

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Data;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class AnimAssetAuditTests
    {
        private const string CatalogPath = "Assets/_Project/Content/Definitions/VfxCatalog_Default.asset";
        private const string FeedbackProfilePath = "Assets/_Project/Content/Definitions/FeedbackProfile_Tiers.asset";
        private const string PrefabDir = "Assets/_Project/Content/Prefabs/Vfx";

        [Test]
        public void AllFeedbackProfileVfxKeys_ResolveInCatalog()
        {
            var feedbackProfile = AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(FeedbackProfilePath);
            Assert.IsNotNull(feedbackProfile, $"Missing feedback profile at {FeedbackProfilePath}");

            var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalogSO>(CatalogPath);
            Assert.IsNotNull(catalog, $"Missing VFX catalog at {CatalogPath}");

            var rules = feedbackProfile.ToRules();
            for (int i = 0; i < rules.Tiers.Length; i++)
            {
                string vfxKey = rules.Tiers[i].VfxKey;
                Assert.IsFalse(string.IsNullOrEmpty(vfxKey), $"Tier {i + 1} has empty VfxKey.");
                Assert.IsTrue(catalog.TryGetEntry(vfxKey, out var entry), $"Catalog does not contain entry for tier {i + 1} key '{vfxKey}'.");
                Assert.IsNotNull(entry.Prefab, $"Prefab for key '{vfxKey}' in catalog is null.");
            }
        }

        [Test]
        public void AllElevenCatalogKeys_ExistWithValidPrefabs()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalogSO>(CatalogPath);
            Assert.IsNotNull(catalog, $"Missing VFX catalog at {CatalogPath}");

            string[] expectedKeys =
            {
                "BallSelect",
                "BallTrail",
                "PlacementSettle",
                "InvalidShake",
                "ClearTier1",
                "ClearTier2",
                "ClearTier3",
                "ClearTier4",
                "ComboBurst",
                "ScorePopup",
                "GameOverFrost"
            };

            foreach (var key in expectedKeys)
            {
                Assert.IsTrue(catalog.TryGetEntry(key, out var entry), $"Catalog missing required key '{key}'.");
                Assert.IsNotNull(entry.Prefab, $"Catalog entry '{key}' has null prefab.");
                Assert.Greater(entry.PoolSize, 0, $"Catalog entry '{key}' must have positive pool size.");
                Assert.Greater(entry.Lifetime, 0f, $"Catalog entry '{key}' must have positive lifetime.");
            }
        }

        [Test]
        public void NoOrphanPrefabsInVfxFolder()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalogSO>(CatalogPath);
            Assert.IsNotNull(catalog, $"Missing VFX catalog at {CatalogPath}");

            var catalogPrefabs = catalog.Entries.Select(e => e.Prefab).Where(p => p != null).ToHashSet();

            var prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir });
            foreach (var guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsTrue(catalogPrefabs.Contains(prefab),
                    $"Found orphan VFX prefab at '{path}' not referenced by VfxCatalog.");
            }
        }

        [Test]
        public void NoBannedAnimatorsOrVfxGraphInVfxPrefabs()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VfxCatalogSO>(CatalogPath);
            Assert.IsNotNull(catalog, $"Missing VFX catalog at {CatalogPath}");

            foreach (var entry in catalog.Entries)
            {
                if (entry.Prefab == null) continue;

                // Animator component banned under VFX prefabs per Animation §7
                var animators = entry.Prefab.GetComponentsInChildren<Animator>(true);
                Assert.IsEmpty(animators,
                    $"Prefab '{entry.Prefab.name}' contains Animator component. Gameplay VFX must use Shuriken/TweenRunner per Animation §7.");

                // VFX Graph banned per Technical Stack §1 & GDD §20
                var components = entry.Prefab.GetComponentsInChildren<Component>(true);
                foreach (var c in components)
                {
                    if (c == null) continue;
                    Assert.AreNotEqual("VisualEffect", c.GetType().Name,
                        $"Prefab '{entry.Prefab.name}' contains banned VisualEffect (VFX Graph) component.");
                }
            }
        }

        [Test]
        public void GameScenePresentationRoot_HasAssignedProfiles()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Content/Scenes/Game.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            var root = Object.FindAnyObjectByType<Line98.Presentation.PresentationRoot>();
            Assert.IsNotNull(root, "PresentationRoot must exist in Game.unity");

            var so = new SerializedObject(root);
            var motionProp = so.FindProperty("m_MotionProfile");
            var feedbackProp = so.FindProperty("m_FeedbackProfile");

            Assert.IsNotNull(motionProp?.objectReferenceValue, "m_MotionProfile must not be null on PresentationRoot in Game.unity");
            Assert.IsNotNull(feedbackProp?.objectReferenceValue, "m_FeedbackProfile must not be null on PresentationRoot in Game.unity");
        }
    }
}

using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using Line98.Data;
using Line98.Services;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public sealed class CosmeticServiceTests
    {
        private const string CatalogPath = "Assets/_Project/Content/Definitions/ThemeCatalog_Default.asset";

        [Test]
        public void CosmeticSettings_DefaultsToClassic()
        {
            var settings = new CosmeticSettings();
            Assert.AreEqual(CosmeticSettings.CurrentVersion, settings.Version);
            Assert.AreEqual(ThemeIds.Classic, settings.ThemeId);
            Assert.IsNull(settings.BallOverrideId);
            Assert.IsNull(settings.BoardOverrideId);
            Assert.IsNull(settings.UiOverrideId);
            Assert.IsNull(settings.ClearEffectOverrideId);
        }

        [Test]
        public void CosmeticService_InitializesWithDefaultClassicTheme()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            Assert.IsNotNull(catalog, $"Missing default theme catalog at {CatalogPath}");

            var backend = new InMemorySaveBackend();
            var service = new CosmeticService(catalog, backend);

            Assert.AreEqual(ThemeIds.Classic, service.ActiveThemeId);
            Assert.IsNotNull(service.ActiveTheme);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveTheme.ThemeId);
        }

        [Test]
        public void CosmeticService_SetTheme_IdempotentFiresNoEvent()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();
            var service = new CosmeticService(catalog, backend);

            int eventCount = 0;
            service.OnThemeChanged += _ => eventCount++;

            service.SetTheme(ThemeIds.Classic);
            Assert.AreEqual(0, eventCount, "Idempotent SetTheme must not fire OnThemeChanged");

            service.SetTheme(ThemeIds.Crystal);
            Assert.AreEqual(1, eventCount, "Setting a different theme must fire OnThemeChanged once");
            Assert.AreEqual(ThemeIds.Crystal, service.ActiveThemeId);

            service.SetTheme(ThemeIds.Crystal);
            Assert.AreEqual(1, eventCount, "Setting the same theme again must not fire OnThemeChanged");
        }

        [Test]
        public void CosmeticService_UnknownOrEmptyId_FallsBackToDefaultClassic()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();
            var service = new CosmeticService(catalog, backend);

            service.SetTheme(ThemeIds.Crystal);
            Assert.AreEqual(ThemeIds.Crystal, service.ActiveThemeId);

            service.SetTheme("non_existent_theme_id_xyz");
            Assert.AreEqual(ThemeIds.Classic, service.ActiveThemeId, "Unknown id must fall back to the catalog default");

            service.SetTheme(null);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveThemeId, "Null id must fall back to the catalog default");

            service.SetTheme(string.Empty);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveThemeId, "Empty id must fall back to the catalog default");
        }

        [Test]
        public void CosmeticService_InheritanceFlattening_ResolvesAllParts()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();
            var service = new CosmeticService(catalog, backend);

            service.SetTheme("crystal");
            var theme = service.ActiveTheme;
            Assert.IsNotNull(theme);
            Assert.AreEqual("crystal", theme.ThemeId);

            // Crystal is a complete visual theme with a dedicated asset for each presentation surface.
            Assert.IsNotNull(theme.BallTheme, "BallTheme must be resolved");
            Assert.AreEqual("crystal", theme.BallTheme.ThemeId);

            Assert.IsNotNull(theme.BoardTheme, "BoardTheme must be resolved");
            Assert.AreEqual("crystal", theme.BoardTheme.ThemeId);

            Assert.IsNotNull(theme.UiTheme, "UiTheme must be resolved");
            Assert.AreEqual("crystal", theme.UiTheme.ThemeId);

            Assert.IsNotNull(theme.ClearEffect, "ClearEffect must be resolved");
            Assert.AreEqual("crystal", theme.ClearEffect.ThemeId);
        }

        [Test]
        public void CosmeticService_PersistenceRoundTrips_ViaSaveBackend()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();

            var service1 = new CosmeticService(catalog, backend);
            service1.SetTheme("crystal");
            Assert.AreEqual("crystal", service1.ActiveThemeId);

            // Create new service instance sharing the same save backend
            var service2 = new CosmeticService(catalog, backend);
            Assert.AreEqual("crystal", service2.ActiveThemeId, "Persisted theme selection must be restored");
            Assert.AreEqual("crystal", service2.ActiveTheme.ThemeId);
        }

        [Test]
        public void ThemeResolver_EmptyOrNullCatalog_FallsBackWithoutThrowing()
        {
            var resolved = ThemeResolver.Resolve(null, "classic");
            Assert.IsNull(resolved);

            var emptyCatalog = ScriptableObject.CreateInstance<ThemeCatalogSO>();
            var resolvedEmpty = ThemeResolver.Resolve(emptyCatalog, "any");
            Assert.IsNull(resolvedEmpty);

            Object.DestroyImmediate(emptyCatalog);
        }

        [Test]
        public void CosmeticService_ActiveBallTheme_ReturnsOverrideSO_WhenBallOverrideIdIsSet()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();
            var service = new CosmeticService(catalog, backend);

            service.SetTheme("classic");
            Assert.AreEqual("classic", service.ActiveBallThemeId);
            Assert.AreEqual("classic", service.ActiveBallTheme.ThemeId);

            service.SetBallTheme("crystal");
            Assert.AreEqual("crystal", service.BallOverrideId);
            Assert.AreEqual("crystal", service.ActiveBallThemeId);
            Assert.IsNotNull(service.ActiveBallTheme);
            Assert.AreEqual("crystal", service.ActiveBallTheme.ThemeId);
            Assert.AreEqual("classic", service.ActiveThemeId, "Active bundle theme must remain classic");
        }

        [Test]
        public void CosmeticService_ActiveBallTheme_FallsBackToBundle_WhenOverrideIsNull()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();
            var service = new CosmeticService(catalog, backend);

            service.SetTheme("crystal");
            Assert.IsNull(service.BallOverrideId);
            Assert.AreEqual("crystal", service.ActiveBallThemeId);
            Assert.IsNotNull(service.ActiveBallTheme);
            Assert.AreEqual("crystal", service.ActiveBallTheme.ThemeId);
        }

        [Test]
        public void CosmeticService_ActiveBallTheme_SurvivesServiceReload()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();

            var service1 = new CosmeticService(catalog, backend);
            service1.SetTheme("classic");
            service1.SetBallTheme("crystal");
            Assert.AreEqual("crystal", service1.ActiveBallThemeId);

            var service2 = new CosmeticService(catalog, backend);
            Assert.AreEqual("classic", service2.ActiveThemeId, "Persisted bundle theme must be restored");
            Assert.AreEqual("crystal", service2.BallOverrideId, "Persisted ball override must be restored");
            Assert.AreEqual("crystal", service2.ActiveBallThemeId, "Effective ball theme id must be restored");
            Assert.IsNotNull(service2.ActiveBallTheme);
            Assert.AreEqual("crystal", service2.ActiveBallTheme.ThemeId, "Effective ball theme SO must be restored");
        }

        [Test]
        public void CosmeticService_SetTheme_ClearsBallOverride_AndRestoresBundleBallTheme()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();
            var service = new CosmeticService(catalog, backend);

            service.SetTheme("classic");
            service.SetBallTheme("crystal");
            Assert.AreEqual("crystal", service.ActiveBallThemeId);

            service.SetTheme("classic");
            Assert.IsNull(service.BallOverrideId, "SetTheme must clear BallOverrideId");
            Assert.AreEqual("classic", service.ActiveBallThemeId, "ActiveBallThemeId must revert to bundle's ball theme");
            Assert.IsNotNull(service.ActiveBallTheme);
            Assert.AreEqual("classic", service.ActiveBallTheme.ThemeId);
        }

        [Test]
        public void CosmeticService_SetBallTheme_DedupesOverrideMatchingActiveBundle()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();
            var legacyNoOpOverride = new CosmeticSettings
            {
                Version = CosmeticSettings.CurrentVersion,
                ThemeId = ThemeIds.Crystal,
                BallOverrideId = ThemeIds.Crystal
            };
            backend.Save("line98_cosmetics", JsonUtility.ToJson(legacyNoOpOverride));

            var service = new CosmeticService(catalog, backend);
            Assert.AreEqual(ThemeIds.Crystal, service.BallOverrideId, "The seeded no-op override must exist before the write is normalized.");

            service.SetBallTheme(ThemeIds.Crystal);

            Assert.IsNull(service.BallOverrideId, "A category selection matching the active bundle must be stored as no override.");
            Assert.AreEqual(ThemeIds.Crystal, service.ActiveBallThemeId);
        }

        [Test]
        public void CosmeticService_LoadV2_DivergentOverrideWarnsOnceAndRemainsExplicit()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();
            var settings = new CosmeticSettings
            {
                Version = CosmeticSettings.CurrentVersion,
                ThemeId = ThemeIds.Crystal,
                BallOverrideId = ThemeIds.Classic
            };
            backend.Save("line98_cosmetics", JsonUtility.ToJson(settings));

            LogAssert.Expect(
                LogType.Warning,
                new Regex("Persisted Ball override 'classic' diverges from bundle 'crystal' part 'crystal'"));

            var service = new CosmeticService(catalog, backend);

            Assert.AreEqual(ThemeIds.Crystal, service.ActiveThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.BallOverrideId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBallThemeId);
        }

        [Test]
        public void CosmeticService_LoadV1_MigratesToV2AndDropsContradictoryOverride()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var backend = new InMemorySaveBackend();
            var legacySettings = new CosmeticSettings
            {
                Version = 1,
                ThemeId = ThemeIds.Crystal,
                BallOverrideId = ThemeIds.Classic
            };
            backend.Save("line98_cosmetics", JsonUtility.ToJson(legacySettings));

            LogAssert.Expect(
                LogType.Warning,
                new Regex("Migrated cosmetic settings v1 to v2; dropped divergent overrides: Ball='classic'"));

            var service = new CosmeticService(catalog, backend);

            Assert.AreEqual(ThemeIds.Crystal, service.ActiveThemeId);
            Assert.IsNull(service.BallOverrideId);
            Assert.AreEqual(ThemeIds.Crystal, service.ActiveBallThemeId);

            CosmeticSettings migrated = JsonUtility.FromJson<CosmeticSettings>(backend.Load("line98_cosmetics"));
            Assert.AreEqual(CosmeticSettings.CurrentVersion, migrated.Version);
            // JsonUtility round-trips a null string as empty; both mean "no override".
            Assert.IsTrue(string.IsNullOrEmpty(migrated.BallOverrideId));
        }

        [Test]
        public void CosmeticService_ResetCategoryOverride_ReturnsToBundlePart()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            var service = new CosmeticService(catalog, new InMemorySaveBackend());

            service.SetTheme(ThemeIds.Classic);
            service.SetBallTheme(ThemeIds.Crystal);
            Assert.AreEqual(ThemeIds.Crystal, service.BallOverrideId);

            service.ResetCategoryOverride(ThemeCategory.Ball);

            Assert.IsNull(service.BallOverrideId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBallThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBallTheme.ThemeId);
        }
    }
}

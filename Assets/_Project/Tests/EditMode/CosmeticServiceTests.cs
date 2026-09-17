using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
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
            Assert.AreEqual(1, settings.Version);
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
            service1.SetTheme("classic");
            Assert.AreEqual("classic", service1.ActiveThemeId);

            // Create new service instance sharing the same save backend
            var service2 = new CosmeticService(catalog, backend);
            Assert.AreEqual("classic", service2.ActiveThemeId, "Persisted theme selection must be restored");
            Assert.AreEqual("classic", service2.ActiveTheme.ThemeId);
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
    }
}

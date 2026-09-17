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
    /// <summary>
    /// Per-part cosmetic selection (ADR D35): Ball, Board and ClearEffect are independent axes;
    /// UI is derived from the selected board's pack and never persisted.
    /// </summary>
    [TestFixture]
    public sealed class CosmeticServiceTests
    {
        private const string CatalogPath = "Assets/_Project/Content/Definitions/ThemeCatalog_Default.asset";
        private const string SaveKey = "line98_cosmetics";

        private ThemeCatalogSO m_Catalog;

        [SetUp]
        public void SetUp()
        {
            m_Catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            Assert.IsNotNull(m_Catalog, $"Missing default theme catalog at {CatalogPath}");
            ThemeResolver.ResetWarnings();
        }

        [Test]
        public void CosmeticSettings_V3_DefaultsToClassicParts()
        {
            var settings = new CosmeticSettings();
            Assert.AreEqual(3, CosmeticSettings.CurrentVersion);
            Assert.AreEqual(CosmeticSettings.CurrentVersion, settings.Version);
            Assert.AreEqual(ThemeIds.Classic, settings.BallThemeId);
            Assert.AreEqual(ThemeIds.Classic, settings.BoardThemeId);
            Assert.AreEqual(ThemeIds.Classic, settings.ClearEffectThemeId);
        }

        [Test]
        public void CosmeticService_InitializesWithClassicPartsAndClassicPackUi()
        {
            var service = new CosmeticService(m_Catalog, new InMemorySaveBackend());

            Assert.AreEqual(ThemeIds.Classic, service.ActiveBallThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBoardThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveClearEffectThemeId);
            Assert.AreSame(m_Catalog.DefaultTheme.UiTheme, service.ActiveUiTheme);
            Assert.IsNotNull(service.ActiveBallTheme);
            Assert.IsNotNull(service.ActiveBoardTheme);
            Assert.IsNotNull(service.ActiveClearEffect);
        }

        [Test]
        public void SetBallTheme_ChangesOnlyBall()
        {
            var service = new CosmeticService(m_Catalog, new InMemorySaveBackend());
            var events = Record(service);

            service.SetBallTheme(ThemeIds.Crystal);

            Assert.AreEqual(ThemeIds.Crystal, service.ActiveBallThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBoardThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveClearEffectThemeId);
            Assert.AreSame(PackUi(ThemeIds.Classic), service.ActiveUiTheme, "Ball selection must not move the UI theme.");
            CollectionAssert.AreEqual(new[] { ThemeCategory.Ball }, Categories(events));
        }

        [Test]
        public void SetBoardTheme_EmitsBoardThenUi_AndUiFollowsBoardPack()
        {
            var service = new CosmeticService(m_Catalog, new InMemorySaveBackend());
            var events = Record(service);

            service.SetBoardTheme(ThemeIds.Crystal);

            Assert.AreEqual(ThemeIds.Crystal, service.ActiveBoardThemeId);
            Assert.AreSame(PackUi(ThemeIds.Crystal), service.ActiveUiTheme);
            Assert.AreEqual(service.ActiveUiTheme.ThemeId, service.ActiveUiThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBallThemeId, "Board selection must not move the ball theme.");
            Assert.AreEqual(ThemeIds.Classic, service.ActiveClearEffectThemeId, "Board selection must not move the clear effect.");
            CollectionAssert.AreEqual(new[] { ThemeCategory.Board, ThemeCategory.Ui }, Categories(events));
            Assert.AreEqual(service.ActiveUiThemeId, events[1].ThemeId);
        }

        [Test]
        public void SetClearEffectTheme_ChangesOnlyClearEffect()
        {
            var service = new CosmeticService(m_Catalog, new InMemorySaveBackend());
            var events = Record(service);

            service.SetClearEffectTheme(ThemeIds.Crystal);

            Assert.AreEqual(ThemeIds.Crystal, service.ActiveClearEffectThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBallThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBoardThemeId);
            Assert.AreSame(PackUi(ThemeIds.Classic), service.ActiveUiTheme);
            CollectionAssert.AreEqual(new[] { ThemeCategory.ClearEffect }, Categories(events));
        }

        [Test]
        public void Setters_AreIdempotent()
        {
            var service = new CosmeticService(m_Catalog, new InMemorySaveBackend());
            var events = Record(service);

            service.SetBallTheme(ThemeIds.Classic);
            service.SetBoardTheme(ThemeIds.Classic);
            service.SetClearEffectTheme(ThemeIds.Classic);
            Assert.AreEqual(0, events.Count);

            service.SetBoardTheme(ThemeIds.Crystal);
            service.SetBoardTheme(ThemeIds.Crystal);
            Assert.AreEqual(2, events.Count, "Repeating a board selection must not re-emit Board/Ui.");
        }

        [Test]
        public void CrystalBall_WithClassicBoard_ResolvesClassicUi()
        {
            var service = new CosmeticService(m_Catalog, new InMemorySaveBackend());
            service.SetBoardTheme(ThemeIds.Crystal);
            service.SetBallTheme(ThemeIds.Crystal);

            service.SetBoardTheme(ThemeIds.Classic);

            Assert.AreEqual(ThemeIds.Crystal, service.ActiveBallThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBoardThemeId);
            Assert.AreSame(PackUi(ThemeIds.Classic), service.ActiveUiTheme);
        }

        [Test]
        public void UnknownOrEmptyIds_FallBackToDefaultPart()
        {
            var service = new CosmeticService(m_Catalog, new InMemorySaveBackend());
            service.SetBallTheme(ThemeIds.Crystal);

            LogAssert.ignoreFailingMessages = true;
            service.SetBallTheme("non_existent_theme_id_xyz");
            LogAssert.ignoreFailingMessages = false;
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBallThemeId, "Unknown id must fall back to the catalog default");

            service.SetBoardTheme(ThemeIds.Crystal);
            service.SetBoardTheme(null);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBoardThemeId, "Null id must fall back to the catalog default");
            Assert.AreSame(PackUi(ThemeIds.Classic), service.ActiveUiTheme);
        }

        [Test]
        public void Persistence_RoundTripsThreeIndependentIds()
        {
            var backend = new InMemorySaveBackend();
            var service1 = new CosmeticService(m_Catalog, backend);
            service1.SetBallTheme(ThemeIds.Crystal);
            service1.SetBoardTheme(ThemeIds.Classic);
            service1.SetClearEffectTheme(ThemeIds.Crystal);

            var service2 = new CosmeticService(m_Catalog, backend);
            Assert.AreEqual(ThemeIds.Crystal, service2.ActiveBallThemeId);
            Assert.AreEqual(ThemeIds.Classic, service2.ActiveBoardThemeId);
            Assert.AreEqual(ThemeIds.Crystal, service2.ActiveClearEffectThemeId);
            Assert.AreSame(PackUi(ThemeIds.Classic), service2.ActiveUiTheme);

            string json = backend.Load(SaveKey);
            StringAssert.DoesNotContain("UiThemeId", json, "UI must never be persisted independently.");
            var saved = JsonUtility.FromJson<CosmeticSettings>(json);
            Assert.AreEqual(3, saved.Version);
        }

        [Test]
        public void LoadV2_KeepsBallOverrideAsNormalSelection()
        {
            var backend = new InMemorySaveBackend();
            backend.Save(SaveKey, "{\"Version\":2,\"ThemeId\":\"crystal\",\"BallOverrideId\":\"classic\",\"BoardOverrideId\":\"\",\"UiOverrideId\":\"\",\"ClearEffectOverrideId\":\"\"}");

            var service = new CosmeticService(m_Catalog, backend);

            Assert.AreEqual(ThemeIds.Classic, service.ActiveBallThemeId, "The v2 ball override becomes the ball selection.");
            Assert.AreEqual(ThemeIds.Crystal, service.ActiveBoardThemeId, "Board comes from the legacy bundle.");
            Assert.AreEqual(ThemeIds.Crystal, service.ActiveClearEffectThemeId, "Clear effect comes from the legacy bundle.");
            Assert.AreSame(PackUi(ThemeIds.Crystal), service.ActiveUiTheme);

            var migrated = JsonUtility.FromJson<CosmeticSettings>(backend.Load(SaveKey));
            Assert.AreEqual(3, migrated.Version);
            Assert.AreEqual(ThemeIds.Classic, migrated.BallThemeId);
            Assert.AreEqual(ThemeIds.Crystal, migrated.BoardThemeId);
        }

        [Test]
        public void LoadV1_FlattensBundleAndDropsUiOverrideWithWarning()
        {
            var backend = new InMemorySaveBackend();
            backend.Save(SaveKey, "{\"Version\":1,\"ThemeId\":\"classic\",\"BoardOverrideId\":\"crystal\",\"UiOverrideId\":\"default\"}");

            LogAssert.Expect(LogType.Warning, new Regex("Dropped legacy UI override 'default'"));
            var service = new CosmeticService(m_Catalog, backend);

            Assert.AreEqual(ThemeIds.Classic, service.ActiveBallThemeId);
            Assert.AreEqual(ThemeIds.Crystal, service.ActiveBoardThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveClearEffectThemeId);
            Assert.AreSame(PackUi(ThemeIds.Crystal), service.ActiveUiTheme, "The board's pack UI wins over the legacy UI override.");
            Assert.AreEqual(3, JsonUtility.FromJson<CosmeticSettings>(backend.Load(SaveKey)).Version);
        }

        [Test]
        public void ResetToDefault_RestoresAllDefaultParts()
        {
            var service = new CosmeticService(m_Catalog, new InMemorySaveBackend());
            service.SetBallTheme(ThemeIds.Crystal);
            service.SetBoardTheme(ThemeIds.Crystal);
            service.SetClearEffectTheme(ThemeIds.Crystal);

            service.ResetToDefault();

            Assert.AreEqual(ThemeIds.Classic, service.ActiveBallThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveBoardThemeId);
            Assert.AreEqual(ThemeIds.Classic, service.ActiveClearEffectThemeId);
            Assert.AreSame(PackUi(ThemeIds.Classic), service.ActiveUiTheme);
        }

        [Test]
        public void ThemeResolver_EmptyOrNullCatalog_FallsBackWithoutThrowing()
        {
            Assert.IsNull(ThemeResolver.Resolve(null, "classic"));

            var emptyCatalog = ScriptableObject.CreateInstance<ThemeCatalogSO>();
            LogAssert.ignoreFailingMessages = true;
            Assert.IsNull(ThemeResolver.Resolve(emptyCatalog, "any"));
            Assert.IsFalse(ThemeResolver.ResolveBoardWithUi(emptyCatalog, "any", out _, out _, out _));
            Assert.IsNull(ThemeResolver.ResolveClearEffect(emptyCatalog, "any"));
            LogAssert.ignoreFailingMessages = false;

            Object.DestroyImmediate(emptyCatalog);
        }

        [Test]
        public void ThemeResolver_ResolveBoardWithUi_ReturnsAtomicPair()
        {
            Assert.IsTrue(ThemeResolver.ResolveBoardWithUi(m_Catalog, ThemeIds.Crystal, out var board, out var ui, out var pack));
            Assert.AreEqual(ThemeIds.Crystal, pack.ThemeId);
            Assert.AreSame(pack.BoardTheme, board);
            Assert.AreSame(pack.UiTheme, ui);
        }

        private UiThemeSO PackUi(string packId)
        {
            Assert.IsTrue(m_Catalog.TryGetTheme(packId, out var pack));
            return pack.UiTheme;
        }

        private static List<ThemeChange> Record(CosmeticService service)
        {
            var events = new List<ThemeChange>();
            service.OnThemeChanged += events.Add;
            return events;
        }

        private static List<ThemeCategory> Categories(List<ThemeChange> events)
        {
            return events.ConvertAll(e => e.Category);
        }
    }
}

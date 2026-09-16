using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Data;
using Line98.Presentation;

namespace Line98.Tests.EditMode
{
    /// <summary>
    /// EditMode unit tests for ThemePartResolver and theme selection data models:
    /// - Returns 2 items for Ball, Board, ClearEffect
    /// - Dedupes by part id
    /// - Resolves 7 non-null preview sprites per theme
    /// - Resolves 3 non-null swatches (Red, Green, Blue) per theme
    /// </summary>
    [TestFixture]
    public sealed class ThemeSelectionTests
    {
        private const string CatalogPath = "Assets/_Project/Content/Definitions/ThemeCatalog_Default.asset";
        private ThemeCatalogSO m_Catalog;

        [SetUp]
        public void SetUp()
        {
            m_Catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            Assert.IsNotNull(m_Catalog, "ThemeCatalog_Default asset must exist");
        }

        [Test]
        public void ThemePartResolver_BallCategory_ReturnsTwoDedupedItems()
        {
            var items = ThemePartResolver.ResolveItems(m_Catalog, ThemeCategory.Ball);
            Assert.AreEqual(2, items.Count, "BALLS tab must display exactly 2 themes (classic and crystal).");

            Assert.AreEqual("classic", items[0].PartId);
            Assert.AreEqual("crystal", items[1].PartId);
        }

        [Test]
        public void ThemePartResolver_BoardAndClearEffectCategories_ReturnTwoItems()
        {
            var boardItems = ThemePartResolver.ResolveItems(m_Catalog, ThemeCategory.Board);
            Assert.AreEqual(2, boardItems.Count, "BOARD tab must resolve 2 items from catalog bundles.");

            var clearItems = ThemePartResolver.ResolveItems(m_Catalog, ThemeCategory.ClearEffect);
            Assert.AreEqual(2, clearItems.Count, "EFFECTS tab must resolve 2 items from catalog bundles.");
        }

        [Test]
        public void ThemePartResolver_PreviewSprites_ResolvesSevenNonNullSprites_ForClassicAndCrystal()
        {
            var classicSprites = ThemePartResolver.ResolvePreviewSprites(m_Catalog, ThemeCategory.Ball, "classic");
            Assert.AreEqual(7, classicSprites.Length);
            for (int i = 0; i < 7; i++)
            {
                Assert.IsNotNull(classicSprites[i], $"Classic preview sprite at index {i} must not be null.");
            }

            var crystalSprites = ThemePartResolver.ResolvePreviewSprites(m_Catalog, ThemeCategory.Ball, "crystal");
            Assert.AreEqual(7, crystalSprites.Length);
            for (int i = 0; i < 7; i++)
            {
                Assert.IsNotNull(crystalSprites[i], $"Crystal preview sprite at index {i} must not be null.");
            }
        }

        [Test]
        public void ThemePartResolver_Swatches_ResolvesThreeNonNullSwatches_ForClassicAndCrystal()
        {
            var classicBundle = m_Catalog.ThemeAt(0);
            var classicSwatches = ThemePartResolver.ResolveSwatches(m_Catalog, classicBundle, classicBundle.BallTheme);
            Assert.AreEqual(3, classicSwatches.Length);
            Assert.IsNotNull(classicSwatches[0], "Classic swatch 0 (Red) must not be null");
            Assert.IsNotNull(classicSwatches[1], "Classic swatch 1 (Green) must not be null");
            Assert.IsNotNull(classicSwatches[2], "Classic swatch 2 (Blue) must not be null");

            var crystalBundle = m_Catalog.ThemeAt(1);
            var crystalSwatches = ThemePartResolver.ResolveSwatches(m_Catalog, crystalBundle, crystalBundle.BallTheme);
            Assert.AreEqual(3, crystalSwatches.Length);
            Assert.IsNotNull(crystalSwatches[0], "Crystal swatch 0 (Red) must not be null");
            Assert.IsNotNull(crystalSwatches[1], "Crystal swatch 1 (Green) must not be null");
            Assert.IsNotNull(crystalSwatches[2], "Crystal swatch 2 (Blue) must not be null");
        }

        [Test]
        public void ThemePartResolver_DedupesDuplicateThemes()
        {
            var items = ThemePartResolver.ResolveItems(m_Catalog, ThemeCategory.Ball);
            var seen = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < items.Count; i++)
            {
                Assert.IsTrue(seen.Add(items[i].PartId), $"Duplicate part ID '{items[i].PartId}' detected.");
            }
        }
    }
}

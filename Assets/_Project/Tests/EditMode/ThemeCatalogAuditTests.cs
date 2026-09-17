using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Data;

namespace Line98.Tests.EditMode
{
    /// <summary>
    /// Audits the theme catalog registry and theme bundles for correctness, id uniqueness per namespace,
    /// complete part resolution, and inheritance depth constraints per GDD [P3.9] and ADR D34.
    /// </summary>
    [TestFixture]
    public sealed class ThemeCatalogAuditTests
    {
        private const string CatalogPath = "Assets/_Project/Content/Definitions/ThemeCatalog_Default.asset";

        private ThemeCatalogSO m_Catalog;

        [SetUp]
        public void SetUp()
        {
            m_Catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            Assert.IsNotNull(m_Catalog, $"Theme catalog asset must exist at '{CatalogPath}'");
        }

        [Test]
        public void CatalogAsset_ExistsAndHasValidDefaultTheme()
        {
            Assert.GreaterOrEqual(m_Catalog.Count, 2, "V1 requires at least 2 shipping themes (classic and crystal).");
            Assert.AreEqual(ThemeIds.Classic, m_Catalog.DefaultThemeId, "Default theme id must be 'classic'.");

            var defaultTheme = m_Catalog.DefaultTheme;
            Assert.IsNotNull(defaultTheme, "DefaultTheme must resolve to a valid ThemeDefinitionSO.");
            Assert.AreEqual(ThemeIds.Classic, defaultTheme.ThemeId, "Default theme's ThemeId must be 'classic'.");

            var firstTheme = m_Catalog.ThemeAt(0);
            Assert.IsNotNull(firstTheme, "ThemeAt(0) must exist.");
            Assert.AreEqual("classic", firstTheme.ThemeId, "Classic must be first in picker order.");
        }

        [Test]
        public void ThemeIds_UniqueNonEmptyPerNamespace()
        {
            var bundleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ballIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var boardIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var uiIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var clearIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < m_Catalog.Count; i++)
            {
                var theme = m_Catalog.ThemeAt(i);
                Assert.IsNotNull(theme, $"Theme at index {i} cannot be null");
                Assert.IsFalse(string.IsNullOrWhiteSpace(theme.ThemeId), $"Theme at index {i} has empty ThemeId");
                Assert.IsTrue(bundleIds.Add(theme.ThemeId), $"Duplicate bundle ThemeId '{theme.ThemeId}' found at index {i}");

                if (theme.BallTheme != null)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(theme.BallTheme.ThemeId));
                    ballIds.Add(theme.BallTheme.ThemeId);
                }

                if (theme.BoardTheme != null)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(theme.BoardTheme.ThemeId));
                    boardIds.Add(theme.BoardTheme.ThemeId);
                }

                if (theme.UiTheme != null)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(theme.UiTheme.ThemeId));
                    uiIds.Add(theme.UiTheme.ThemeId);
                }

                if (theme.ClearEffect != null)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(theme.ClearEffect.ThemeId));
                    clearIds.Add(theme.ClearEffect.ThemeId);
                }
            }

            Assert.GreaterOrEqual(bundleIds.Count, 2);
            Assert.GreaterOrEqual(ballIds.Count, 2, "Must have distinct ball themes for classic and crystal");
            Assert.GreaterOrEqual(boardIds.Count, 1);
            Assert.GreaterOrEqual(uiIds.Count, 1);
            Assert.GreaterOrEqual(clearIds.Count, 1);
        }

        [Test]
        public void AllThemes_ResolveNonNullPartsPostInheritance()
        {
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                var theme = m_Catalog.ThemeAt(i);
                Assert.IsNotNull(theme.BallTheme, $"Theme '{theme.ThemeId}' must resolve a non-null BallTheme");
                Assert.IsNotNull(theme.BoardTheme, $"Theme '{theme.ThemeId}' must resolve a non-null BoardTheme");
                Assert.IsNotNull(theme.UiTheme, $"Theme '{theme.ThemeId}' must resolve a non-null UiTheme");
                Assert.IsNotNull(theme.ClearEffect, $"Theme '{theme.ThemeId}' must resolve a non-null ClearEffect");
            }
        }

        [Test]
        public void AllThemes_HaveThumbnailAndValidDisplayMetadata()
        {
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                var theme = m_Catalog.ThemeAt(i);
                Assert.IsNotNull(theme.Thumbnail, $"Theme '{theme.ThemeId}' must have a non-null Thumbnail sprite");
                Assert.IsFalse(string.IsNullOrWhiteSpace(theme.DisplayName), $"Theme '{theme.ThemeId}' must have a valid DisplayName");
            }
        }

        /// <summary>
        /// ADR D35 invariant: a part id may appear in several packs only if it is the same asset,
        /// otherwise part→pack (and therefore board⇒UI pairing) is ambiguous.
        /// </summary>
        [Test]
        public void PartIds_MapDeterministicallyToOnePack()
        {
            var categories = new[] { ThemeCategory.Ball, ThemeCategory.Board, ThemeCategory.ClearEffect };
            foreach (var category in categories)
            {
                var assetsById = new Dictionary<string, UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < m_Catalog.Count; i++)
                {
                    var pack = m_Catalog.ThemeAt(i);
                    string partId = ThemeCatalogSO.PartId(pack, category);
                    UnityEngine.Object asset = category switch
                    {
                        ThemeCategory.Ball => pack.BallTheme,
                        ThemeCategory.Board => pack.BoardTheme,
                        _ => pack.ClearEffect
                    };

                    if (assetsById.TryGetValue(partId, out var existing))
                    {
                        Assert.AreSame(existing, asset, $"{category} id '{partId}' is declared by several packs with different assets.");
                    }
                    else
                    {
                        assetsById.Add(partId, asset);
                    }

                    Assert.IsTrue(m_Catalog.TryGetPackForPart(category, partId, out var owner));
                    Assert.AreSame(asset, category switch
                    {
                        ThemeCategory.Ball => owner.BallTheme,
                        ThemeCategory.Board => owner.BoardTheme,
                        _ => owner.ClearEffect
                    });
                }
            }
        }

        [Test]
        public void TryGetPartIds_AndDefaultPartIds_MatchPackParts()
        {
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                var pack = m_Catalog.ThemeAt(i);
                Assert.IsTrue(m_Catalog.TryGetPartIds(pack, out var ids));
                Assert.AreEqual(pack.BallTheme.ThemeId, ids.BallId);
                Assert.AreEqual(pack.BoardTheme.ThemeId, ids.BoardId);
                Assert.AreEqual(pack.UiTheme.ThemeId, ids.UiId);
                Assert.AreEqual(pack.ClearEffect.ThemeId, ids.ClearEffectId);
            }

            Assert.AreEqual(m_Catalog.DefaultTheme.UiTheme.ThemeId, m_Catalog.DefaultPartId(ThemeCategory.Ui));
            Assert.IsTrue(m_Catalog.TryGetUiTheme(m_Catalog.DefaultPartId(ThemeCategory.Ui), out _));
            Assert.IsTrue(m_Catalog.TryGetClearEffect(m_Catalog.DefaultPartId(ThemeCategory.ClearEffect), out _));
        }

        [Test]
        public void ThemeInheritance_DepthNeverExceedsOne()
        {
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                var theme = m_Catalog.ThemeAt(i);
                if (theme.InheritsFrom != null)
                {
                    Assert.IsNull(
                        theme.InheritsFrom.InheritsFrom,
                        $"Theme '{theme.ThemeId}' inherits from '{theme.InheritsFrom.ThemeId}', which also inherits from '{theme.InheritsFrom.InheritsFrom?.ThemeId}'. Maximum inheritance depth is 1.");
                    Assert.AreNotEqual(
                        theme,
                        theme.InheritsFrom,
                        $"Theme '{theme.ThemeId}' cannot inherit from itself.");
                }
            }
        }
    }
}

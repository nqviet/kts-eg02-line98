using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Data;
using Line98.Presentation;

namespace Line98.Tests.EditMode.UI
{
    [TestFixture]
    public sealed class ThemePreviewTests
    {
        private const string PopupCosmeticsPrefabPath = "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Cosmetics.prefab";
        private const string CatalogPath = "Assets/_Project/Content/Definitions/ThemeCatalog_Default.asset";

        private GameObject m_Prefab;
        private GameObject m_Instance;
        private UiThemePreviewPanel m_Panel;
        private ThemeCatalogSO m_Catalog;
        private BoardThemeSO m_CrystalBoard;
        private BallThemeSO m_CrystalBall;
        private UiThemeSO m_CrystalUi;

        [SetUp]
        public void SetUp()
        {
            m_Catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(CatalogPath);
            Assert.IsNotNull(m_Catalog, "Theme catalog asset must exist");

            Assert.IsTrue(m_Catalog.TryGetTheme(ThemeIds.Crystal, out var crystalPack), "Crystal theme pack must exist");
            m_CrystalBoard = crystalPack.BoardTheme;
            m_CrystalBall = crystalPack.BallTheme;
            m_CrystalUi = crystalPack.UiTheme;

            Assert.IsNotNull(m_CrystalBoard, "Crystal BoardTheme must not be null");
            Assert.IsNotNull(m_CrystalBall, "Crystal BallTheme must not be null");
            Assert.IsNotNull(m_CrystalUi, "Crystal UiTheme must not be null");

            m_Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopupCosmeticsPrefabPath);
            Assert.IsNotNull(m_Prefab, $"Popup_Cosmetics prefab must exist at '{PopupCosmeticsPrefabPath}'");

            m_Instance = Object.Instantiate(m_Prefab);
            m_Panel = m_Instance.GetComponentInChildren<UiThemePreviewPanel>(true);
            Assert.IsNotNull(m_Panel, "UiThemePreviewPanel component must exist on instantiated popup");
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Instance != null)
            {
                Object.DestroyImmediate(m_Instance);
            }
        }

        [Test]
        public void UiThemePreviewPanel_BoardEmphasis_AppliesBoardMaterialsToCellsAndFrame()
        {
            var request = new ThemePreviewRequest
            {
                DisplayName = "Crystal",
                BoardTheme = m_CrystalBoard,
                BallTheme = m_CrystalBall,
                UiTheme = m_CrystalUi,
                Emphasis = PreviewEmphasis.Board,
                IsApplied = false,
                Animate = false
            };

            m_Panel.Show(in request);

            Assert.IsNotNull(m_Panel.CellImages, "CellImages must not be null");
            Assert.AreEqual(25, m_Panel.CellImages.Length, "Must have exactly 25 preview cells (5x5)");

            for (int i = 0; i < m_Panel.CellImages.Length; i++)
            {
                var cell = m_Panel.CellImages[i];
                Assert.IsNotNull(cell, $"Cell {i} image must not be null");
                Assert.AreSame(m_CrystalBoard.BoardCellMaterial, cell.material, $"Cell {i} must use crystal BoardCellMaterial");
                Assert.AreEqual(Color.white, cell.color, $"Cell {i} tint must be white for material-driven SDF");
            }

            Assert.IsNotNull(m_Panel.BoardMockPlate, "BoardMockPlate must not be null");
            Assert.AreSame(m_CrystalBoard.BoardFrameMaterial, m_Panel.BoardMockPlate.material, "Plate must use crystal BoardFrameMaterial");
            Assert.AreEqual(Color.white, m_Panel.BoardMockPlate.color, "Plate tint must be white for material-driven SDF");

            Assert.IsNotNull(m_Panel.Slots, "Slots must not be null");
            Assert.AreEqual(7, m_Panel.Slots.Length, "Must have 7 preview slots");

            for (int i = 0; i < m_Panel.Slots.Length; i++)
            {
                var slot = m_Panel.Slots[i];
                Assert.IsNotNull(slot, $"Slot {i} must not be null");
                var rt = slot.GetComponent<RectTransform>();
                Assert.IsNotNull(rt, $"Slot {i} RectTransform must not be null");
                Assert.AreEqual(m_Panel.BoardPositions[i], rt.anchoredPosition, $"Slot {i} must be at board position {m_Panel.BoardPositions[i]}");
            }
        }

        [Test]
        public void UiThemePreviewPanel_GemsEmphasis_AppliesUiThemeCardMaterial()
        {
            var request = new ThemePreviewRequest
            {
                DisplayName = "Crystal",
                BoardTheme = m_CrystalBoard,
                BallTheme = m_CrystalBall,
                UiTheme = m_CrystalUi,
                Emphasis = PreviewEmphasis.Gems,
                IsApplied = false,
                Animate = false
            };

            m_Panel.Show(in request);

            Assert.IsNotNull(m_Panel.CellImages, "CellImages must not be null");
            Assert.AreEqual(25, m_Panel.CellImages.Length);

            for (int i = 0; i < m_Panel.CellImages.Length; i++)
            {
                var cell = m_Panel.CellImages[i];
                Assert.IsNotNull(cell, $"Cell {i} image must not be null");
                var expectedCellMat = m_CrystalUi.SurfaceMaterialCard != null ? m_CrystalUi.SurfaceMaterialCard : cell.defaultMaterial;
                Assert.AreSame(expectedCellMat, cell.material, $"Cell {i} must use UI card material under Gems emphasis");
                Assert.AreEqual(m_CrystalUi.PanelCell, cell.color, $"Cell {i} color must match UiTheme.PanelCell");
            }

            Assert.IsNotNull(m_Panel.BoardMockPlate);
            var expectedPlateMat = m_CrystalUi.SurfaceMaterialCard != null ? m_CrystalUi.SurfaceMaterialCard : m_Panel.BoardMockPlate.defaultMaterial;
            Assert.AreSame(expectedPlateMat, m_Panel.BoardMockPlate.material);
            Assert.AreEqual(m_CrystalUi.PanelTray, m_Panel.BoardMockPlate.color);

            for (int i = 0; i < m_Panel.Slots.Length; i++)
            {
                var slot = m_Panel.Slots[i];
                Assert.IsNotNull(slot);
                var rt = slot.GetComponent<RectTransform>();
                Assert.IsNotNull(rt);
                Assert.AreEqual(m_Panel.FlowerPositions[i], rt.anchoredPosition, $"Slot {i} must be at flower position {m_Panel.FlowerPositions[i]}");
            }
        }

        [Test]
        public void UiThemePreviewPanel_DisplayName_IsUpperCased()
        {
            var request = new ThemePreviewRequest
            {
                DisplayName = "crystal",
                BoardTheme = m_CrystalBoard,
                BallTheme = m_CrystalBall,
                UiTheme = m_CrystalUi,
                Emphasis = PreviewEmphasis.Board,
                IsApplied = false,
                Animate = false
            };

            m_Panel.Show(in request);

            Assert.AreEqual("CRYSTAL", m_Panel.ThemeName, "Theme preview header text must be uppercase");
            Assert.AreEqual("CRYSTAL", m_Panel.ThemeNameLabel.text);
        }

        [Test]
        public void UiThemePreviewPanel_NullBoardGraceful()
        {
            var request = new ThemePreviewRequest
            {
                DisplayName = "NullBoardTest",
                BoardTheme = null,
                BallTheme = m_CrystalBall,
                UiTheme = m_CrystalUi,
                Emphasis = PreviewEmphasis.Board,
                IsApplied = false,
                Animate = false
            };

            Assert.DoesNotThrow(() => m_Panel.Show(in request), "Show must not throw when BoardTheme is null");
            Assert.AreEqual("NULLBOARDTEST", m_Panel.ThemeName);
            Assert.IsNotNull(m_Panel.BoardMockPlate);
            var expectedPlateMat = m_CrystalUi.SurfaceMaterialCard != null ? m_CrystalUi.SurfaceMaterialCard : m_Panel.BoardMockPlate.defaultMaterial;
            Assert.AreSame(expectedPlateMat, m_Panel.BoardMockPlate.material, "Must fall back to UiTheme when board is null");
        }

        [Test]
        public void CosmeticsPopup_EffectsTab_ShowsPlaceholderAndHidesPreviewAndItems()
        {
            var popup = m_Instance.GetComponent<CosmeticsPopup>();
            Assert.IsNotNull(popup, "CosmeticsPopup component must exist");

            popup.Populate(m_Catalog, null, ThemeCategory.Ball);
            Assert.IsTrue(popup.PreviewPanel.gameObject.activeSelf, "Ball tab must show preview panel");
            Assert.IsTrue(popup.ItemsContainer.gameObject.activeSelf, "Ball tab must show items container");
            if (popup.PlaceholderRoot != null)
            {
                Assert.IsFalse(popup.PlaceholderRoot.gameObject.activeSelf, "Ball tab must hide placeholder");
            }

            popup.TabStrip.SetActive(ThemeCategory.ClearEffect, notify: true);

            Assert.IsFalse(popup.PreviewPanel.gameObject.activeSelf, "Effects tab must hide preview panel");
            Assert.IsFalse(popup.ItemsContainer.gameObject.activeSelf, "Effects tab must hide items container");
            Assert.IsNotNull(popup.PlaceholderRoot, "PlaceholderRoot must exist");
            Assert.IsTrue(popup.PlaceholderRoot.gameObject.activeSelf, "Effects tab must show placeholder root");
            Assert.IsNotNull(popup.PlaceholderText, "PlaceholderText must exist");
            Assert.IsFalse(string.IsNullOrEmpty(popup.PlaceholderText.text), "PlaceholderText must not be empty");
        }

        [Test]
        public void CosmeticsPopup_SwitchingBackFromEffects_RestoresPreviewAndItems()
        {
            var popup = m_Instance.GetComponent<CosmeticsPopup>();
            Assert.IsNotNull(popup);

            popup.Populate(m_Catalog, null, ThemeCategory.ClearEffect);
            Assert.IsFalse(popup.PreviewPanel.gameObject.activeSelf, "Effects tab must hide preview panel");
            Assert.IsFalse(popup.ItemsContainer.gameObject.activeSelf, "Effects tab must hide items container");
            Assert.IsTrue(popup.PlaceholderRoot.gameObject.activeSelf, "Effects tab must show placeholder root");

            popup.TabStrip.SetActive(ThemeCategory.Board, notify: true);
            Assert.IsTrue(popup.PreviewPanel.gameObject.activeSelf, "Board tab must show preview panel");
            Assert.IsTrue(popup.ItemsContainer.gameObject.activeSelf, "Board tab must show items container");
            Assert.IsFalse(popup.PlaceholderRoot.gameObject.activeSelf, "Board tab must hide placeholder root");
        }
    }
}

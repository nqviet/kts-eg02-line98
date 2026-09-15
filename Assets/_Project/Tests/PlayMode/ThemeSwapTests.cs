using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Line98.Core;
using Line98.Data;
using Line98.Presentation;

namespace Line98.Tests.PlayMode
{
    /// <summary>
    /// PlayMode integration test verifying runtime cosmetic theme swap:
    /// - classic -> crystal and back
    /// - active ball sharedMaterial instance IDs change
    /// - BallView.Color unchanged
    /// - BoardModel / GameSession byte-identical before and after
    /// - No MaterialPropertyBlock used (SRP batching intact per D33)
    /// </summary>
    [TestFixture]
    public sealed class ThemeSwapTests
    {
        [UnityTest]
        public IEnumerator ThemeSwap_ClassicToCrystalAndBack_PreservesGameplayAndSwapsMaterials()
        {
            if (!SceneManager.GetActiveScene().name.Equals("Game"))
            {
                yield return SceneManager.LoadSceneAsync("Game");
            }

            yield return null;

            var presRoot = Object.FindAnyObjectByType<PresentationRoot>();
            Assert.IsNotNull(presRoot, "PresentationRoot must exist in Game scene");
            Assert.IsTrue(presRoot.IsInitialized, "PresentationRoot must be initialized");

            Assert.IsNotNull(presRoot.ThemeSelector, "IThemeSelector must be wired on PresentationRoot");
            Assert.AreSame(presRoot.ThemeSelector, presRoot.UIRouter.ThemeSelector, "UiShell must receive PresentationRoot's selector");

            presRoot.ThemeSelector.RequestTheme("classic");
            yield return null;
            Assert.IsNotNull(presRoot.ActiveTheme);
            Assert.AreEqual("classic", presRoot.ActiveTheme.ThemeId, "The swap test must begin from the Classic theme.");

            var session = presRoot.Session;
            Assert.IsNotNull(session, "GameSession must be active");
            Assert.AreEqual(3, session.Board.OccupiedCount, "Initial game state must have exactly 3 occupied cells");

            // 1. Record initial state with Classic theme
            byte[] initialCells = session.Board.ExportCells();
            int initialScore = session.Score;
            int initialMoveCount = session.MoveCount;

            var initialBalls = new List<(GridPos pos, BallColor color, Material mat)>();
            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    var pos = new GridPos(x, y);
                    if (!session.Board.IsEmpty(pos))
                    {
                        var ballView = presRoot.BallManager.GetBallAt(pos);
                        Assert.IsNotNull(ballView, $"Active ball at {pos} must have a BallView instance");

                        var mr = ballView.Visual.GetComponent<MeshRenderer>();
                        Assert.IsNotNull(mr, $"Ball at {pos} must have a MeshRenderer");
                        Assert.IsNotNull(mr.sharedMaterial, $"Ball at {pos} must have a sharedMaterial");
                        Assert.IsFalse(mr.HasPropertyBlock(), "Ball must not use MaterialPropertyBlock (SRP batching intact)");

                        initialBalls.Add((pos, ballView.Color, mr.sharedMaterial));
                    }
                }
            }

            Assert.AreEqual(3, initialBalls.Count);

            // 2. Swap to Crystal
            presRoot.ThemeSelector.RequestTheme("crystal");
            yield return null;

            Assert.IsNotNull(presRoot.ActiveTheme);
            Assert.AreEqual("crystal", presRoot.ActiveTheme.ThemeId, "Active theme must be 'crystal'");

            // Assert balls have swapped materials, unchanged colors, no MPB
            for (int i = 0; i < initialBalls.Count; i++)
            {
                var (pos, initialColor, initialMat) = initialBalls[i];
                var ballView = presRoot.BallManager.GetBallAt(pos);
                Assert.IsNotNull(ballView);
                Assert.AreEqual(initialColor, ballView.Color, $"Ball color at {pos} must not change on theme swap");

                var mr = ballView.Visual.GetComponent<MeshRenderer>();
                Assert.IsNotNull(mr);
                Assert.IsNotNull(mr.sharedMaterial);
                Assert.AreNotSame(initialMat, mr.sharedMaterial, $"Ball material at {pos} must change on swap to crystal");
                Assert.IsTrue(mr.sharedMaterial.name.Contains("Crystal"), $"Ball material should be Crystal material, was {mr.sharedMaterial.name}");
                Assert.IsFalse(mr.HasPropertyBlock(), "Ball must not use MaterialPropertyBlock after swap");
            }

            // Assert board cells and gameplay state are byte-identical
            byte[] crystalCells = session.Board.ExportCells();
            CollectionAssert.AreEqual(initialCells, crystalCells, "Board cells must be byte-identical after theme swap");
            Assert.AreEqual(initialScore, session.Score);
            Assert.AreEqual(initialMoveCount, session.MoveCount);

            // 3. Swap back to Classic
            presRoot.ThemeSelector.RequestTheme("classic");
            yield return null;

            Assert.IsNotNull(presRoot.ActiveTheme);
            Assert.AreEqual("classic", presRoot.ActiveTheme.ThemeId, "Active theme must be 'classic'");

            // Assert balls have reverted to classic materials
            for (int i = 0; i < initialBalls.Count; i++)
            {
                var (pos, initialColor, initialMat) = initialBalls[i];
                var ballView = presRoot.BallManager.GetBallAt(pos);
                Assert.IsNotNull(ballView);
                Assert.AreEqual(initialColor, ballView.Color, $"Ball color at {pos} must not change");

                var mr = ballView.Visual.GetComponent<MeshRenderer>();
                Assert.IsNotNull(mr);
                Assert.AreSame(initialMat, mr.sharedMaterial, $"Ball material at {pos} must match initial Classic material");
                Assert.IsFalse(mr.HasPropertyBlock());
            }

            byte[] finalCells = session.Board.ExportCells();
            CollectionAssert.AreEqual(initialCells, finalCells, "Board cells must be byte-identical after swap back to classic");
            Assert.AreEqual(initialScore, session.Score);
            Assert.AreEqual(initialMoveCount, session.MoveCount);
        }

        [UnityTest]
        public IEnumerator CosmeticsPopup_ThemeTileChangesThemeWithoutGrowingTheGrid()
        {
            if (!SceneManager.GetActiveScene().name.Equals("Game"))
            {
                yield return SceneManager.LoadSceneAsync("Game");
            }

            yield return null;

            var presRoot = Object.FindAnyObjectByType<PresentationRoot>();
            Assert.IsNotNull(presRoot);
            Assert.IsNotNull(presRoot.ThemeSelector, "Direct scene entry must provide a theme selector.");

            presRoot.UIRouter.OpenCosmetics();
            yield return null;

            CosmeticsPopup popup = presRoot.UIRouter.CosmeticsPopup;
            Assert.IsNotNull(popup);
            Assert.IsNotNull(popup.Catalog);
            Assert.AreEqual(popup.Catalog.Count, popup.TilesContainer.childCount, "The picker must create one tile per theme.");

            Transform crystalTile = popup.TilesContainer.Find("Tile_crystal");
            Assert.IsNotNull(crystalTile, "The Crystal theme tile must be available.");
            var button = crystalTile.GetComponent<UnityEngine.UI.Button>();
            Assert.IsNotNull(button);

            button.onClick.Invoke();
            yield return null;

            Assert.IsNotNull(presRoot.ActiveTheme);
            Assert.AreEqual("crystal", presRoot.ActiveTheme.ThemeId);
            Assert.AreEqual(popup.Catalog.Count, popup.TilesContainer.childCount, "Changing a theme must update existing tiles instead of growing the grid.");
        }

        [UnityTest]
        public IEnumerator MainMenu_Settings_Themes_ChangesTheme()
        {
            yield return SceneManager.LoadSceneAsync("Boot");
            yield return WaitForMainMenu();

            UiShell shell = Object.FindAnyObjectByType<UiShell>();
            Assert.IsNotNull(shell, "MainMenu must have a UI shell.");
            Assert.IsNotNull(shell.ThemeSelector, "AppRoot must supply the MainMenu shell with a theme selector.");
            Assert.IsNotNull(shell.ThemeCatalog, "AppRoot must supply the MainMenu shell with a theme catalog.");
            Assert.IsNotNull(shell.SettingsPopup, "MainMenu must include the Settings popup.");
            Assert.IsNotNull(shell.CosmeticsPopup, "MainMenu must include the Themes popup.");

            shell.CloseAllPopups();
            shell.ThemeSelector.RequestTheme("classic");
            yield return null;

            shell.OpenSettings();
            Assert.IsTrue(shell.SettingsPopup.IsOpen, "Settings must open from MainMenu.");
            Assert.IsNotNull(shell.SettingsPopup.CosmeticsButton, "Settings must provide a Themes entry point.");

            shell.SettingsPopup.CosmeticsButton.onClick.Invoke();
            yield return null;

            CosmeticsPopup popup = shell.CosmeticsPopup;
            Assert.IsTrue(popup.IsOpen, "Themes must open from Settings.");
            Transform crystalTile = popup.TilesContainer.Find("Tile_crystal");
            Assert.IsNotNull(crystalTile, "The Crystal theme tile must be present in MainMenu.");

            crystalTile.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;

            Assert.AreEqual("crystal", popup.ActiveThemeId, "Selecting Crystal must update the active MainMenu theme.");
        }

        [UnityTest]
        public IEnumerator ThemesPopup_CloseThenReopen_WorksFromSettings()
        {
            yield return SceneManager.LoadSceneAsync("Boot");
            yield return WaitForMainMenu();

            UiShell shell = Object.FindAnyObjectByType<UiShell>();
            Assert.IsNotNull(shell);
            shell.CloseAllPopups();
            shell.OpenSettings();

            SettingsPopup settings = shell.SettingsPopup;
            settings.CosmeticsButton.onClick.Invoke();
            yield return null;

            CosmeticsPopup popup = shell.CosmeticsPopup;
            Assert.IsTrue(popup.IsOpen);
            Assert.IsNotNull(popup.CloseButton, "Themes must provide a close control.");

            popup.CloseButton.onClick.Invoke();
            yield return null;

            Assert.IsFalse(popup.IsOpen, "Closing Themes must remove it from the shell stack.");
            Assert.IsTrue(settings.IsOpen, "Closing Themes must return to Settings.");

            settings.CosmeticsButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(popup.IsOpen, "Themes must reopen after its close button is used.");
        }

        private static IEnumerator WaitForMainMenu()
        {
            const int maxFrames = 120;
            for (int i = 0; i < maxFrames && SceneManager.GetActiveScene().name != "MainMenu"; i++)
            {
                yield return null;
            }

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "Boot must load MainMenu within 120 frames.");
            yield return null;
        }
    }
}

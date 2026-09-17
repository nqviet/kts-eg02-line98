using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Line98.App;
using Line98.Core;
using Line98.Data;
using Line98.Presentation;
using Line98.Services;

namespace Line98.Tests.PlayMode
{
    /// <summary>
    /// PlayMode integration tests verifying both service-level category changes and the
    /// player-facing bundle-authoritative theme picker.
    /// </summary>
    [TestFixture]
    public sealed class ThemeCategorySwapTests
    {
        [UnityTest]
        public IEnumerator CategorySwap_BallThemeOnly_PreservesBoardAndUiAndClearEffect()
        {
            if (!SceneManager.GetActiveScene().name.Equals("Game"))
            {
                yield return SceneManager.LoadSceneAsync("Game");
            }

            yield return null;

            var presRoot = Object.FindAnyObjectByType<PresentationRoot>();
            Assert.IsNotNull(presRoot, "PresentationRoot must exist in Game scene");
            Assert.IsTrue(presRoot.IsInitialized, "PresentationRoot must be initialized");
            Assert.IsNotNull(presRoot.ThemeSelector, "ThemeSelector must be wired on PresentationRoot");

            // Ensure baseline is crystal
            presRoot.ThemeSelector.RequestTheme("crystal");
            yield return null;

            Assert.IsNotNull(presRoot.ActiveTheme);
            Assert.AreEqual("crystal", presRoot.ActiveTheme.ThemeId);

            var initialBoardTheme = presRoot.BoardView.Theme;
            var initialBoardThemeId = presRoot.ActiveTheme.BoardTheme.ThemeId;
            var initialUiThemeId = presRoot.ActiveTheme.UiTheme.ThemeId;
            var initialClearEffectId = presRoot.ActiveTheme.ClearEffect.ThemeId;

            // Record initial ball materials
            var initialBalls = new List<(GridPos pos, BallColor color, Material mat)>();
            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    var pos = new GridPos(x, y);
                    if (!presRoot.Session.Board.IsEmpty(pos))
                    {
                        var ballView = presRoot.BallManager.GetBallAt(pos);
                        Assert.IsNotNull(ballView);
                        var mr = ballView.Visual.GetComponent<MeshRenderer>();
                        Assert.IsNotNull(mr);
                        initialBalls.Add((pos, ballView.Color, mr.sharedMaterial));
                    }
                }
            }
            Assert.AreEqual(3, initialBalls.Count, "Initial game state must have 3 occupied cells");

            // 1. Request Ball theme change only to "classic"
            presRoot.ThemeSelector.RequestTheme(ThemeCategory.Ball, "classic");
            yield return null;

            Assert.AreEqual("classic", presRoot.ThemeSelector.ActiveBallThemeId, "ActiveBallThemeId must be 'classic'");

            // Assert balls have classic materials, unchanged colors, no MPB
            for (int i = 0; i < initialBalls.Count; i++)
            {
                var (pos, initialColor, initialMat) = initialBalls[i];
                var ballView = presRoot.BallManager.GetBallAt(pos);
                Assert.IsNotNull(ballView);
                Assert.AreEqual(initialColor, ballView.Color, $"Ball color at {pos} must not change");

                var mr = ballView.Visual.GetComponent<MeshRenderer>();
                Assert.IsNotNull(mr);
                Assert.IsNotNull(mr.sharedMaterial);
                Assert.AreNotSame(initialMat, mr.sharedMaterial, $"Ball material at {pos} must change to classic");
                Assert.IsFalse(mr.sharedMaterial.name.Contains("Crystal"), "Material must not be Crystal material");
                Assert.IsFalse(mr.HasPropertyBlock(), "Must not use MaterialPropertyBlock");
            }

            // Assert BoardTheme, UiTheme, ClearEffect are untouched
            Assert.AreSame(initialBoardTheme, presRoot.BoardView.Theme, "BoardView theme must remain untouched");
            Assert.AreEqual(initialBoardThemeId, presRoot.ActiveTheme.BoardTheme.ThemeId);
            Assert.AreEqual(initialUiThemeId, presRoot.ActiveTheme.UiTheme.ThemeId);
            Assert.AreEqual(initialClearEffectId, presRoot.ActiveTheme.ClearEffect.ThemeId);

            // Assert HUD preview tray sprite set and tints follow ball theme
            Assert.IsNotNull(presRoot.HudPresenter);
            Assert.IsNotNull(presRoot.HudPresenter.BallTheme);
            Assert.AreEqual("classic", presRoot.HudPresenter.BallTheme.ThemeId);
            var classicBallTheme = presRoot.HudPresenter.BallTheme;
            for (int i = 0; i < 3; i++)
            {
                var slotImg = presRoot.HudPresenter.PreviewView.SlotImages[i];
                if (slotImg != null && slotImg.enabled)
                {
                    var color = presRoot.Session.PreviewQueue[i];
                    var expectedSprite = classicBallTheme.PreviewSpriteSet.GetSprite(color);
                    var expectedTint = ResolveExpectedTint(classicBallTheme, color);
                    Assert.AreSame(expectedSprite, slotImg.sprite, $"Preview tray slot {i} sprite must match ball theme PreviewSpriteSet");
                    Assert.AreEqual(expectedTint, slotImg.color, $"Preview tray slot {i} tint must match ball theme PreviewTints");
                }
            }

            // 2. Idempotency check: re-requesting "classic" should be a clean no-op
            presRoot.ThemeSelector.RequestTheme(ThemeCategory.Ball, "classic");
            yield return null;

            Assert.AreEqual("classic", presRoot.ThemeSelector.ActiveBallThemeId);
            for (int i = 0; i < initialBalls.Count; i++)
            {
                var (pos, initialColor, _) = initialBalls[i];
                var ballView = presRoot.BallManager.GetBallAt(pos);
                Assert.AreEqual(initialColor, ballView.Color);
                var mr = ballView.Visual.GetComponent<MeshRenderer>();
                Assert.IsFalse(mr.sharedMaterial.name.Contains("Crystal"));
            }

            // 3. Swap Ball category back to "crystal"
            presRoot.ThemeSelector.RequestTheme(ThemeCategory.Ball, "crystal");
            yield return null;

            Assert.AreEqual("crystal", presRoot.ThemeSelector.ActiveBallThemeId, "ActiveBallThemeId must return to 'crystal'");

            for (int i = 0; i < initialBalls.Count; i++)
            {
                var (pos, initialColor, initialMat) = initialBalls[i];
                var ballView = presRoot.BallManager.GetBallAt(pos);
                Assert.IsNotNull(ballView);
                Assert.AreEqual(initialColor, ballView.Color);

                var mr = ballView.Visual.GetComponent<MeshRenderer>();
                Assert.IsNotNull(mr);
                Assert.AreSame(initialMat, mr.sharedMaterial, "Ball material must revert to initial crystal material");
            }

            // Board, UI, and ClearEffect still untouched
            Assert.AreSame(initialBoardTheme, presRoot.BoardView.Theme);
            Assert.AreEqual(initialBoardThemeId, presRoot.ActiveTheme.BoardTheme.ThemeId);
            Assert.AreEqual(initialUiThemeId, presRoot.ActiveTheme.UiTheme.ThemeId);
            Assert.AreEqual(initialClearEffectId, presRoot.ActiveTheme.ClearEffect.ThemeId);

            // Assert HUD preview tray sprite set reverted to crystal
            Assert.AreEqual("crystal", presRoot.HudPresenter.BallTheme.ThemeId);
            var crystalBallTheme = presRoot.HudPresenter.BallTheme;
            for (int i = 0; i < 3; i++)
            {
                var slotImg = presRoot.HudPresenter.PreviewView.SlotImages[i];
                if (slotImg != null && slotImg.enabled)
                {
                    var color = presRoot.Session.PreviewQueue[i];
                    var expectedSprite = crystalBallTheme.PreviewSpriteSet.GetSprite(color);
                    var expectedTint = ResolveExpectedTint(crystalBallTheme, color);
                    Assert.AreSame(expectedSprite, slotImg.sprite, $"Preview tray slot {i} sprite must revert to crystal PreviewSpriteSet");
                    Assert.AreEqual(expectedTint, slotImg.color, $"Preview tray slot {i} tint must revert to crystal PreviewTints");
                }
            }
        }

        private static Color ResolveExpectedTint(BallThemeSO ballTheme, BallColor color)
        {
            int colorIndex = (int)color - 1;
            if (ballTheme?.PreviewTints == null || colorIndex < 0 || colorIndex >= ballTheme.PreviewTints.Length)
            {
                return Color.white;
            }

            Color tint = ballTheme.PreviewTints[colorIndex];
            return tint.a > 0.001f ? tint : Color.white;
        }

        [UnityTest]
        public IEnumerator BundleSelection_AppliesToMainMenuGemsAndSurvivesSceneReload()
        {
            yield return SceneManager.LoadSceneAsync("Boot");
            const int maxFrames = 120;
            for (int i = 0; i < maxFrames && SceneManager.GetActiveScene().name != "MainMenu"; i++)
            {
                yield return null;
            }
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
            yield return null;

            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.IsNotNull(shell, "MainMenu UiShell must exist");
            var menuPresenter = Object.FindAnyObjectByType<MainMenuPresenter>();
            Assert.IsNotNull(menuPresenter, "MainMenuPresenter must exist");

            // 1. Establish baseline with classic bundle
            shell.ThemeSelector.RequestTheme("classic");
            yield return null;

            Assert.IsNotNull(menuPresenter.ShowcaseGems);
            Assert.IsTrue(menuPresenter.ShowcaseGems.Length > 0);
            var classicRedSprite = menuPresenter.ShowcaseGems[0].sprite;

            // 2. Select the Crystal bundle through the same popup flow as the player.
            shell.OpenSettings();
            yield return null;
            Assert.IsNotNull(shell.SettingsPopup);
            Assert.IsNotNull(shell.SettingsPopup.CosmeticsButton);

            shell.SettingsPopup.CosmeticsButton.onClick.Invoke();
            yield return null;

            var cosmeticsPopup = shell.CosmeticsPopup;
            Assert.IsNotNull(cosmeticsPopup);
            Assert.IsTrue(cosmeticsPopup.IsOpen);

            Transform crystalItem = cosmeticsPopup.ItemsContainer.Find("Item_crystal");
            Assert.IsNotNull(crystalItem, "Crystal must be available on the BALLS tab");
            var crystalListItem = crystalItem.GetComponent<UiThemeListItem>();
            Assert.IsNotNull(crystalListItem);
            Assert.IsNotNull(crystalListItem.StatusButton);

            crystalListItem.StatusButton.onClick.Invoke();
            yield return null;

            Assert.AreEqual("crystal", shell.ThemeSelector.ActiveThemeId, "The active bundle must be crystal");
            Assert.AreEqual("crystal", shell.ThemeSelector.ActiveBallThemeId, "The bundle's ball theme must be crystal");
            Assert.IsNotNull(menuPresenter.BallTheme);
            Assert.AreEqual("crystal", menuPresenter.BallTheme.ThemeId);

            // Assert MainMenu showcase gems updated to crystal
            var crystalRedSprite = menuPresenter.ShowcaseGems[0].sprite;
            Assert.AreNotSame(classicRedSprite, crystalRedSprite, "Showcase gem sprite must change when the Crystal bundle is selected");
            Assert.AreSame(menuPresenter.BallTheme.PreviewSpriteSet.GetSprite(BallColor.Red), crystalRedSprite);

            // Close Themes back to Settings and assert its gem slots updated too.
            cosmeticsPopup.Close();
            yield return null;
            Assert.IsTrue(shell.SettingsPopup.IsOpen);
            Assert.AreEqual("crystal", shell.SettingsPopup.BallTheme.ThemeId);
            if (shell.SettingsPopup.GemSlots.Length > 0 && shell.SettingsPopup.GemSlots[0] != null)
            {
                Assert.AreSame(crystalRedSprite, shell.SettingsPopup.GemSlots[0].sprite);
            }
            shell.SettingsPopup.Close();
            yield return null;

            // 3. Load Game scene and verify the bundle persists at bind-time
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;

            var presRoot = Object.FindAnyObjectByType<PresentationRoot>();
            Assert.IsNotNull(presRoot, "PresentationRoot must exist in Game scene");
            AssertGameUsesBundle(presRoot, ThemeIds.Crystal);

            // Board ball materials must be crystal
            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    var pos = new GridPos(x, y);
                    if (!presRoot.Session.Board.IsEmpty(pos))
                    {
                        var ballView = presRoot.BallManager.GetBallAt(pos);
                        var mr = ballView.Visual.GetComponent<MeshRenderer>();
                        Assert.IsTrue(mr.sharedMaterial.name.Contains("Crystal"), $"Ball at {pos} must have Crystal material after scene reload, was {mr.sharedMaterial.name}");
                    }
                }
            }

            // 4. Return to MainMenu and verify the bundle is still active
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            menuPresenter = Object.FindAnyObjectByType<MainMenuPresenter>();
            Assert.IsNotNull(menuPresenter);
            Assert.AreEqual("crystal", menuPresenter.BallTheme.ThemeId);
            Assert.AreSame(crystalRedSprite, menuPresenter.ShowcaseGems[0].sprite, "Showcase gem must still be crystal after scene reload");
        }

        [UnityTest]
        public IEnumerator ThemeCards_OnEveryTab_ApplyWholeBundleAcrossGameSurfaces()
        {
            yield return SceneManager.LoadSceneAsync("Boot");
            yield return WaitForScene("MainMenu");
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;

            var presRoot = Object.FindAnyObjectByType<PresentationRoot>();
            Assert.IsNotNull(presRoot);

            ThemeCategory[] tabs =
            {
                ThemeCategory.Ball,
                ThemeCategory.Board,
                ThemeCategory.ClearEffect
            };

            for (int i = 0; i < tabs.Length; i++)
            {
                presRoot.ThemeSelector.RequestTheme(ThemeIds.Classic);
                yield return null;
                AssertGameUsesBundle(presRoot, ThemeIds.Classic);

                presRoot.UIRouter.OpenCosmetics();
                yield return null;

                CosmeticsPopup popup = presRoot.UIRouter.CosmeticsPopup;
                Assert.IsNotNull(popup);
                popup.TabStrip.SetActive(tabs[i], notify: true);
                yield return null;

                Assert.IsTrue(popup.ItemsContainer.gameObject.activeSelf, $"{tabs[i]} must display bundle cards.");
                Transform crystalItem = popup.ItemsContainer.Find("Item_crystal");
                Assert.IsNotNull(crystalItem, $"Crystal must be selectable from the {tabs[i]} tab.");

                UiThemeListItem listItem = crystalItem.GetComponent<UiThemeListItem>();
                Assert.IsNotNull(listItem);
                listItem.StatusButton.onClick.Invoke();
                yield return null;

                Assert.AreEqual(ThemeIds.Crystal, popup.ActiveThemeId);
                AssertGameUsesBundle(presRoot, ThemeIds.Crystal);

                popup.Close();
                yield return null;
            }

            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;

            var menuPresenter = Object.FindAnyObjectByType<MainMenuPresenter>();
            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.IsNotNull(menuPresenter);
            Assert.IsNotNull(shell);
            Assert.AreEqual(ThemeIds.Crystal, menuPresenter.BallTheme.ThemeId);
            Assert.AreEqual(ThemeIds.Crystal, shell.UiTheme.ThemeId);
            Assert.AreSame(
                menuPresenter.BallTheme.PreviewSpriteSet.GetSprite(BallColor.Red),
                menuPresenter.ShowcaseGems[0].sprite);

            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;

            AssertGameUsesBundle(Object.FindAnyObjectByType<PresentationRoot>(), ThemeIds.Crystal);
        }

        [UnityTest]
        public IEnumerator LegacyCrystalBundleWithClassicBallOverride_BootsFullyCrystalAndMigratesSave()
        {
            AppRoot existingRoot = AppRoot.Instance;
            if (existingRoot != null)
            {
                Object.Destroy(existingRoot.gameObject);
                yield return null;
            }

            var backend = new InMemorySaveBackend();
            var legacySettings = new CosmeticSettings
            {
                Version = 1,
                ThemeId = ThemeIds.Crystal,
                BallOverrideId = ThemeIds.Classic
            };
            backend.Save("line98_cosmetics", JsonUtility.ToJson(legacySettings));

            var previousFactory = AppRoot.SaveBackendFactory;
            AppRoot.SaveBackendFactory = () => backend;
            LogAssert.Expect(
                LogType.Warning,
                new Regex("Migrated cosmetic settings v1 to v2; dropped divergent overrides: Ball='classic'"));

            try
            {
                yield return SceneManager.LoadSceneAsync("Boot");
                yield return WaitForScene("MainMenu");
                yield return SceneManager.LoadSceneAsync("Game");
                yield return null;

                AssertGameUsesBundle(Object.FindAnyObjectByType<PresentationRoot>(), ThemeIds.Crystal);

                CosmeticSettings migrated = JsonUtility.FromJson<CosmeticSettings>(backend.Load("line98_cosmetics"));
                Assert.AreEqual(CosmeticSettings.CurrentVersion, migrated.Version);
                // JsonUtility round-trips a null string as empty; both mean "no override".
                Assert.IsTrue(string.IsNullOrEmpty(migrated.BallOverrideId));
            }
            finally
            {
                AppRoot.SaveBackendFactory = previousFactory;
                if (AppRoot.Instance != null)
                {
                    Object.Destroy(AppRoot.Instance.gameObject);
                }
            }

            yield return null;
        }

        private static void AssertGameUsesBundle(PresentationRoot presRoot, string expectedThemeId)
        {
            Assert.IsNotNull(presRoot, "PresentationRoot must exist in Game scene.");
            Assert.IsNotNull(presRoot.ActiveTheme);
            Assert.AreEqual(expectedThemeId, presRoot.ThemeSelector.ActiveThemeId);
            Assert.AreEqual(expectedThemeId, presRoot.ActiveTheme.ThemeId);
            // Part ids are not guaranteed to equal the bundle id (classic's UI part is "default"),
            // so compare every surface against the bundle's own parts.
            ThemeDefinitionSO bundle = presRoot.ActiveTheme;
            Assert.AreSame(bundle.BoardTheme, presRoot.BoardView.Theme);
            Assert.AreSame(bundle.UiTheme, presRoot.UIRouter.UiTheme);
            Assert.AreSame(bundle.BallTheme, presRoot.BallManager.BallTheme);
            Assert.AreSame(bundle.BallTheme, presRoot.HudPresenter.BallTheme);

            BallThemeSO ballTheme = presRoot.BallManager.BallTheme;
            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    var pos = new GridPos(x, y);
                    if (presRoot.Session.Board.IsEmpty(pos)) continue;

                    BallView ballView = presRoot.BallManager.GetBallAt(pos);
                    Assert.IsNotNull(ballView);
                    var meshFilter = ballView.Visual.GetComponent<MeshFilter>();
                    var meshRenderer = ballView.Visual.GetComponent<MeshRenderer>();
                    int materialIndex = (int)ballView.Color - 1;
                    Assert.AreSame(ballTheme.BallMesh, meshFilter.sharedMesh, $"Ball at {pos} must use the {expectedThemeId} mesh.");
                    Assert.AreSame(ballTheme.BallMaterials[materialIndex], meshRenderer.sharedMaterial, $"Ball at {pos} must use the {expectedThemeId} material.");
                }
            }

            for (int i = 0; i < presRoot.HudPresenter.PreviewView.SlotImages.Length && i < presRoot.Session.PreviewQueue.Count; i++)
            {
                var slot = presRoot.HudPresenter.PreviewView.SlotImages[i];
                if (slot == null || !slot.enabled) continue;

                BallColor color = presRoot.Session.PreviewQueue[i];
                Assert.AreSame(ballTheme.PreviewSpriteSet.GetSprite(color), slot.sprite);
                Assert.AreEqual(ResolveExpectedTint(ballTheme, color), slot.color);
            }
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            const int maxFrames = 120;
            for (int i = 0; i < maxFrames && SceneManager.GetActiveScene().name != sceneName; i++)
            {
                yield return null;
            }

            Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name);
            yield return null;
        }
    }
}

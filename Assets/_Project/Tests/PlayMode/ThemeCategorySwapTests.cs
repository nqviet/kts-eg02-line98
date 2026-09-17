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
    /// PlayMode integration tests for per-part theme selection (ADR D35): Ball, Board and
    /// ClearEffect are independent; the UI theme always follows the selected board's pack.
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

            // Ensure baseline is all-crystal
            SelectPack(presRoot.ThemeSelector, ThemeIds.Crystal);
            yield return null;

            var initialBoardTheme = presRoot.BoardView.Theme;
            var initialBoardThemeId = presRoot.ThemeSelector.ActiveBoardThemeId;
            var initialUiTheme = presRoot.UIRouter.UiTheme;
            var initialClearEffectId = presRoot.ThemeSelector.ActiveClearEffectThemeId;

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
            Assert.AreEqual(initialBoardThemeId, presRoot.ThemeSelector.ActiveBoardThemeId);
            Assert.AreSame(initialUiTheme, presRoot.UIRouter.UiTheme);
            Assert.AreEqual(initialClearEffectId, presRoot.ThemeSelector.ActiveClearEffectThemeId);

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
            Assert.AreEqual(initialBoardThemeId, presRoot.ThemeSelector.ActiveBoardThemeId);
            Assert.AreSame(initialUiTheme, presRoot.UIRouter.UiTheme);
            Assert.AreEqual(initialClearEffectId, presRoot.ThemeSelector.ActiveClearEffectThemeId);

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
        public IEnumerator MixedSelection_AppliesToMainMenuAndSurvivesSceneReload()
        {
            yield return SceneManager.LoadSceneAsync("Boot");
            yield return WaitForScene("MainMenu");

            var shell = Object.FindAnyObjectByType<UiShell>();
            Assert.IsNotNull(shell, "MainMenu UiShell must exist");
            var menuPresenter = Object.FindAnyObjectByType<MainMenuPresenter>();
            Assert.IsNotNull(menuPresenter, "MainMenuPresenter must exist");

            // 1. Baseline: all Classic
            SelectPack(shell.ThemeSelector, ThemeIds.Classic);
            yield return null;
            Assert.IsNotNull(menuPresenter.ShowcaseGems);
            Assert.IsTrue(menuPresenter.ShowcaseGems.Length > 0);
            var classicRedSprite = menuPresenter.ShowcaseGems[0].sprite;

            // 2. Select Crystal on the BALLS tab through the player flow.
            shell.OpenSettings();
            yield return null;
            shell.SettingsPopup.CosmeticsButton.onClick.Invoke();
            yield return null;

            var cosmeticsPopup = shell.CosmeticsPopup;
            Assert.IsTrue(cosmeticsPopup.IsOpen);
            Assert.AreEqual(ThemeCategory.Ball, cosmeticsPopup.ActiveTab);
            PressSelect(cosmeticsPopup, ThemeIds.Crystal);
            yield return null;

            Assert.AreEqual(ThemeIds.Crystal, shell.ThemeSelector.ActiveBallThemeId);
            Assert.AreEqual(ThemeIds.Classic, shell.ThemeSelector.ActiveBoardThemeId, "Ball selection must not change the board.");
            Assert.AreEqual(ThemeIds.Classic, shell.ThemeSelector.ActiveClearEffectThemeId, "Ball selection must not change the clear effect.");
            Assert.AreSame(PackUi(ThemeIds.Classic), shell.UiTheme, "UI must stay on the Classic board's pack.");

            Assert.AreEqual(ThemeIds.Crystal, menuPresenter.BallTheme.ThemeId);
            var crystalRedSprite = menuPresenter.ShowcaseGems[0].sprite;
            Assert.AreNotSame(classicRedSprite, crystalRedSprite, "Showcase gem sprite must change with the ball theme");
            Assert.AreSame(menuPresenter.BallTheme.PreviewSpriteSet.GetSprite(BallColor.Red), crystalRedSprite);

            cosmeticsPopup.Close();
            yield return null;
            Assert.IsTrue(shell.SettingsPopup.IsOpen);
            Assert.AreEqual(ThemeIds.Crystal, shell.SettingsPopup.BallTheme.ThemeId);
            shell.SettingsPopup.Close();
            yield return null;

            // 3. Game scene binds the mixed selection
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;
            var presRoot = Object.FindAnyObjectByType<PresentationRoot>();
            AssertGameUsesParts(presRoot, ThemeIds.Crystal, ThemeIds.Classic, ThemeIds.Classic);

            // 4. Back to MainMenu: still mixed
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            menuPresenter = Object.FindAnyObjectByType<MainMenuPresenter>();
            shell = Object.FindAnyObjectByType<UiShell>();
            Assert.AreEqual(ThemeIds.Crystal, menuPresenter.BallTheme.ThemeId);
            Assert.AreSame(crystalRedSprite, menuPresenter.ShowcaseGems[0].sprite, "Showcase gem must still be crystal after scene reload");
            Assert.AreSame(PackUi(ThemeIds.Classic), shell.UiTheme);
        }

        [UnityTest]
        public IEnumerator ThemeCards_OnEachTab_ChangeOnlyThatPart()
        {
            yield return SceneManager.LoadSceneAsync("Boot");
            yield return WaitForScene("MainMenu");
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;

            var presRoot = Object.FindAnyObjectByType<PresentationRoot>();
            Assert.IsNotNull(presRoot);

            // BALLS
            SelectPack(presRoot.ThemeSelector, ThemeIds.Classic);
            yield return null;
            yield return SelectOnTab(presRoot, ThemeCategory.Ball, ThemeIds.Crystal);
            AssertGameUsesParts(presRoot, ThemeIds.Crystal, ThemeIds.Classic, ThemeIds.Classic);

            // BOARD (UI follows)
            SelectPack(presRoot.ThemeSelector, ThemeIds.Classic);
            yield return null;
            yield return SelectOnTab(presRoot, ThemeCategory.Board, ThemeIds.Crystal);
            AssertGameUsesParts(presRoot, ThemeIds.Classic, ThemeIds.Crystal, ThemeIds.Classic);

            // EFFECTS
            SelectPack(presRoot.ThemeSelector, ThemeIds.Classic);
            yield return null;
            yield return SelectOnTab(presRoot, ThemeCategory.ClearEffect, ThemeIds.Crystal);
            AssertGameUsesParts(presRoot, ThemeIds.Classic, ThemeIds.Classic, ThemeIds.Crystal);
        }

        [UnityTest]
        public IEnumerator UiThemeRequest_IsIgnored()
        {
            yield return SceneManager.LoadSceneAsync("Boot");
            yield return WaitForScene("MainMenu");
            yield return SceneManager.LoadSceneAsync("Game");
            yield return null;

            var presRoot = Object.FindAnyObjectByType<PresentationRoot>();
            SelectPack(presRoot.ThemeSelector, ThemeIds.Classic);
            yield return null;

            LogAssert.Expect(LogType.Warning, new Regex("Ignored UI theme request 'crystal'"));
            presRoot.ThemeSelector.RequestTheme(ThemeCategory.Ui, ThemeIds.Crystal);
            yield return null;

            AssertGameUsesParts(presRoot, ThemeIds.Classic, ThemeIds.Classic, ThemeIds.Classic);
        }

        [UnityTest]
        public IEnumerator LegacyV2CrystalBundleWithClassicBallOverride_BootsMixedAndMigratesSave()
        {
            AppRoot existingRoot = AppRoot.Instance;
            if (existingRoot != null)
            {
                Object.Destroy(existingRoot.gameObject);
                yield return null;
            }

            var backend = new InMemorySaveBackend();
            backend.Save("line98_cosmetics", "{\"Version\":2,\"ThemeId\":\"crystal\",\"BallOverrideId\":\"classic\"}");

            var previousFactory = AppRoot.SaveBackendFactory;
            AppRoot.SaveBackendFactory = () => backend;

            try
            {
                yield return SceneManager.LoadSceneAsync("Boot");
                yield return WaitForScene("MainMenu");
                yield return SceneManager.LoadSceneAsync("Game");
                yield return null;

                AssertGameUsesParts(Object.FindAnyObjectByType<PresentationRoot>(), ThemeIds.Classic, ThemeIds.Crystal, ThemeIds.Crystal);

                CosmeticSettings migrated = JsonUtility.FromJson<CosmeticSettings>(backend.Load("line98_cosmetics"));
                Assert.AreEqual(CosmeticSettings.CurrentVersion, migrated.Version);
                Assert.AreEqual(ThemeIds.Classic, migrated.BallThemeId);
                Assert.AreEqual(ThemeIds.Crystal, migrated.BoardThemeId);
                Assert.AreEqual(ThemeIds.Crystal, migrated.ClearEffectThemeId);
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

        private static IEnumerator SelectOnTab(PresentationRoot presRoot, ThemeCategory tab, string partId)
        {
            presRoot.UIRouter.OpenCosmetics();
            yield return null;

            CosmeticsPopup popup = presRoot.UIRouter.CosmeticsPopup;
            Assert.IsNotNull(popup);
            popup.TabStrip.SetActive(tab, notify: true);
            yield return null;

            Assert.IsTrue(popup.ItemsContainer.gameObject.activeSelf, $"{tab} must display part cards.");
            PressSelect(popup, partId);
            yield return null;

            Assert.AreEqual(partId, popup.AppliedPartId, $"{tab} card must mark its own part as applied.");
            popup.Close();
            yield return null;
        }

        private static void PressSelect(CosmeticsPopup popup, string partId)
        {
            Transform item = popup.ItemsContainer.Find($"Item_{partId}");
            Assert.IsNotNull(item, $"'{partId}' must be selectable from the {popup.ActiveTab} tab.");
            var listItem = item.GetComponent<UiThemeListItem>();
            Assert.IsNotNull(listItem);
            Assert.IsNotNull(listItem.StatusButton);
            listItem.StatusButton.onClick.Invoke();
        }

        private static void SelectPack(IThemeSelector selector, string packId)
        {
            selector.RequestTheme(ThemeCategory.Board, packId);
            selector.RequestTheme(ThemeCategory.Ball, packId);
            selector.RequestTheme(ThemeCategory.ClearEffect, packId);
        }

        private static ThemeDefinitionSO Pack(string packId)
        {
            var shell = Object.FindAnyObjectByType<UiShell>();
            ThemeCatalogSO catalog = shell != null ? shell.ThemeCatalog : null;
            Assert.IsNotNull(catalog, "A theme catalog must be reachable.");
            Assert.IsTrue(catalog.TryGetTheme(packId, out var pack), $"Pack '{packId}' must exist.");
            return pack;
        }

        private static UiThemeSO PackUi(string packId) => Pack(packId).UiTheme;

        private static void AssertGameUsesParts(PresentationRoot presRoot, string ballPackId, string boardPackId, string clearPackId)
        {
            Assert.IsNotNull(presRoot, "PresentationRoot must exist in Game scene.");
            ThemeDefinitionSO ballPack = Pack(ballPackId);
            ThemeDefinitionSO boardPack = Pack(boardPackId);
            ThemeDefinitionSO clearPack = Pack(clearPackId);

            Assert.AreEqual(ballPack.BallTheme.ThemeId, presRoot.ThemeSelector.ActiveBallThemeId);
            Assert.AreEqual(boardPack.BoardTheme.ThemeId, presRoot.ThemeSelector.ActiveBoardThemeId);
            Assert.AreEqual(clearPack.ClearEffect.ThemeId, presRoot.ThemeSelector.ActiveClearEffectThemeId);

            // Part ids are not guaranteed to equal pack ids (classic's UI part is "default"),
            // so compare every surface against asset references.
            Assert.AreSame(boardPack.BoardTheme, presRoot.BoardView.Theme);
            Assert.AreSame(boardPack.UiTheme, presRoot.UIRouter.UiTheme, "UI must follow the board's pack.");
            Assert.AreSame(boardPack.UiTheme, presRoot.ThemeSelector.ActiveUiTheme);
            Assert.AreSame(ballPack.BallTheme, presRoot.BallManager.BallTheme);
            Assert.AreSame(ballPack.BallTheme, presRoot.HudPresenter.BallTheme);
            Assert.AreSame(clearPack.ClearEffect, presRoot.ClearEffect);

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
                    Assert.AreSame(ballTheme.BallMesh, meshFilter.sharedMesh, $"Ball at {pos} must use the {ballPackId} mesh.");
                    Assert.AreSame(ballTheme.BallMaterials[materialIndex], meshRenderer.sharedMaterial, $"Ball at {pos} must use the {ballPackId} material.");
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

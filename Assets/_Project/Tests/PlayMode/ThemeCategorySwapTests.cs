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
    /// PlayMode integration tests verifying category-scoped theme changes:
    /// - SetBallTheme("classic") changes ball materials and leaves board/UI/clear untouched
    /// - Idempotency: re-requesting same ball theme is a no-op
    /// - SetBallTheme("crystal") swaps materials back to crystal and leaves board/UI/clear untouched
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
        }
    }
}

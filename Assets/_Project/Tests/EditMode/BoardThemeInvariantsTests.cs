using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Data;

namespace Line98.Tests.EditMode
{
    /// <summary>
    /// Enforces the permanent projection invariant C1:
    /// m_RowPitchScale = 1.1791784f is a baked projection constant across all board themes.
    /// </summary>
    [TestFixture]
    public sealed class BoardThemeInvariantsTests
    {
        public const float CanonicalRowPitchScale = 1.1791784f;

        [Test]
        public void AllBoardThemes_MaintainRowPitchScaleInvariant()
        {
            string[] guids = AssetDatabase.FindAssets("t:BoardThemeSO");
            Assert.IsNotEmpty(guids, "At least one BoardThemeSO must exist in the project.");

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var theme = AssetDatabase.LoadAssetAtPath<BoardThemeSO>(path);
                Assert.IsNotNull(theme, $"Failed to load BoardThemeSO at {path}");

                Assert.AreEqual(
                    CanonicalRowPitchScale,
                    theme.RowPitchScale,
                    0.00001f,
                    $"BoardTheme at '{path}' has RowPitchScale={theme.RowPitchScale}, but must be exactly {CanonicalRowPitchScale} to preserve 2.5D projection squareness (C1).");
            }
        }
    }
}

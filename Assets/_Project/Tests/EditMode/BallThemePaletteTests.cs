using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Data;

namespace Line98.Tests.EditMode
{
    /// <summary>
    /// Audits ball theme palettes across all BallThemeSO assets in the project:
    /// - Exactly 7 materials under Assets/Art/Materials/ with the theme's ball shader (Crystal: Line98/CrystalBall, else Line98/PolishedBall)
    /// - 7 distinct _BaseColor values per palette
    /// - D33 pattern rect parity: identical _PatternRect per color index across all ball themes
    /// - Mesh bounds and baked pivot contract preservation
    /// </summary>
    [TestFixture]
    public sealed class BallThemePaletteTests
    {
        private const string ClassicThemePath = "Assets/_Project/Content/Definitions/BallTheme_Classic.asset";

        private List<BallThemeSO> m_AllBallThemes;
        private BallThemeSO m_ClassicTheme;

        [SetUp]
        public void SetUp()
        {
            m_ClassicTheme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(ClassicThemePath);
            Assert.IsNotNull(m_ClassicTheme, $"Classic BallThemeSO must exist at '{ClassicThemePath}'");

            string[] guids = AssetDatabase.FindAssets("t:BallThemeSO");
            m_AllBallThemes = new List<BallThemeSO>();
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var theme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(path);
                if (theme != null)
                {
                    m_AllBallThemes.Add(theme);
                }
            }

            Assert.GreaterOrEqual(m_AllBallThemes.Count, 2, "Must find at least 2 BallThemeSO assets (classic and crystal).");
        }

        [Test]
        public void AllBallThemes_HaveExactlySevenMaterialsUnderMaterialsFolder()
        {
            foreach (var theme in m_AllBallThemes)
            {
                Assert.IsNotNull(theme.BallMaterials, $"BallTheme '{theme.ThemeId}' has null BallMaterials array.");
                Assert.AreEqual(7, theme.BallMaterials.Length, $"BallTheme '{theme.ThemeId}' must have exactly 7 materials.");

                for (int i = 0; i < 7; i++)
                {
                    var mat = theme.BallMaterials[i];
                    Assert.IsNotNull(mat, $"BallTheme '{theme.ThemeId}' has null material at index {i}.");

                    string assetPath = AssetDatabase.GetAssetPath(mat);
                    Assert.IsTrue(
                        assetPath.StartsWith("Assets/Art/Materials/"),
                        $"Material '{mat.name}' for theme '{theme.ThemeId}' is located at '{assetPath}', but must reside under 'Assets/Art/Materials/'");

                    Assert.IsNotNull(mat.shader, $"Material '{mat.name}' has null shader.");
                    string expectedShader = ExpectedBallShader(theme);
                    Assert.AreEqual(expectedShader, mat.shader.name, $"Material '{mat.name}' must use shader '{expectedShader}'.");
                }
            }
        }

        // Crystal renders as a faceted gem; every other theme uses the polished sphere.
        private static string ExpectedBallShader(BallThemeSO theme)
        {
            return theme.ThemeId == ThemeIds.Crystal ? "Line98/CrystalBall" : "Line98/PolishedBall";
        }

        [Test]
        public void AllBallThemes_HaveSevenDistinctBaseColors()
        {
            foreach (var theme in m_AllBallThemes)
            {
                var colors = new List<Color>();
                for (int i = 0; i < theme.BallMaterials.Length; i++)
                {
                    var mat = theme.BallMaterials[i];
                    Assert.IsTrue(mat.HasProperty("_BaseColor"), $"Material '{mat.name}' missing _BaseColor property.");
                    Color col = mat.GetColor("_BaseColor");

                    // Check that it's distinct from already recorded colors in this theme
                    for (int j = 0; j < colors.Count; j++)
                    {
                        Color prev = colors[j];
                        float diff = Mathf.Abs(col.r - prev.r) + Mathf.Abs(col.g - prev.g) + Mathf.Abs(col.b - prev.b);
                        Assert.Greater(diff, 0.05f, $"BallTheme '{theme.ThemeId}' has nearly identical colors between indices {j} and {i} ({prev} vs {col}).");
                    }
                    colors.Add(col);
                }
                Assert.AreEqual(7, colors.Count);
            }
        }

        [Test]
        public void AllBallThemes_MaintainIdenticalPatternRectParityWithCanonicalClassic()
        {
            var classicMaterials = m_ClassicTheme.BallMaterials;
            Assert.AreEqual(7, classicMaterials.Length);

            foreach (var theme in m_AllBallThemes)
            {
                for (int i = 0; i < 7; i++)
                {
                    var classicMat = classicMaterials[i];
                    var themeMat = theme.BallMaterials[i];

                    Vector4 classicRect = classicMat.GetVector("_PatternRect");
                    Vector4 themeRect = themeMat.GetVector("_PatternRect");

                    Assert.AreEqual(classicRect.x, themeRect.x, 0.001f, $"Theme '{theme.ThemeId}' index {i} _PatternRect.x mismatch with Classic");
                    Assert.AreEqual(classicRect.y, themeRect.y, 0.001f, $"Theme '{theme.ThemeId}' index {i} _PatternRect.y mismatch with Classic");
                    Assert.AreEqual(classicRect.z, themeRect.z, 0.001f, $"Theme '{theme.ThemeId}' index {i} _PatternRect.z mismatch with Classic");
                    Assert.AreEqual(classicRect.w, themeRect.w, 0.001f, $"Theme '{theme.ThemeId}' index {i} _PatternRect.w mismatch with Classic");
                }
            }
        }

        [Test]
        public void BallMesh_PreservesRadiusAndBakedPivot()
        {
            foreach (var theme in m_AllBallThemes)
            {
                if (theme.BallMesh != null)
                {
                    var bounds = theme.BallMesh.bounds;
                    // Extents in x/y should approximate ball radius (~0.44f for MESH_Ball_Gem_Centered)
                    Assert.LessOrEqual(bounds.extents.x, 0.50f, $"BallMesh on theme '{theme.ThemeId}' has unexpected extents.x: {bounds.extents.x}");
                    Assert.GreaterOrEqual(bounds.extents.x, 0.25f, $"BallMesh on theme '{theme.ThemeId}' has unexpected extents.x: {bounds.extents.x}");

                    // Baked pivot contract: center.z should be around -0.187f
                    Assert.AreEqual(-0.18746f, bounds.center.z, 0.01f, $"BallMesh on theme '{theme.ThemeId}' broke baked pivot contract (center.z={bounds.center.z})");
                }
            }
        }
    }
}

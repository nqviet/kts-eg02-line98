using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Data;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class AssetHygieneTests
    {
        private static readonly Regex s_BannedPathPattern = new Regex(@"(?i)(mockup|mock|copy|temp|placeholder)", RegexOptions.Compiled);

        [Test]
        public void NoProjectAssetPath_ContainsBannedTokens()
        {
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var violatingPaths = new List<string>();

            foreach (var path in allAssetPaths)
            {
                if (!path.StartsWith("Assets/Art/") && !path.StartsWith("Assets/_Project/"))
                {
                    continue;
                }

                if (s_BannedPathPattern.IsMatch(path))
                {
                    violatingPaths.Add(path);
                }
            }

            Assert.IsEmpty(violatingPaths, $"Found project assets with banned naming tokens:\n{string.Join("\n", violatingPaths)}");
        }

        [Test]
        public void BallThemeCrystal_HasSevenPolishedBallMaterialsWithDistinctHues()
        {
            var theme = AssetDatabase.LoadAssetAtPath<BallThemeSO>("Assets/_Project/Content/Definitions/BallTheme_Crystal.asset");
            Assert.IsNotNull(theme, "Missing BallTheme_Crystal asset.");

            var so = new SerializedObject(theme);
            var ballMatsProp = so.FindProperty("m_BallMaterials");
            Assert.AreEqual(7, ballMatsProp.arraySize, "BallTheme_Crystal must configure exactly 7 materials.");

            var distinctColors = new HashSet<Color>();

            for (int i = 0; i < ballMatsProp.arraySize; i++)
            {
                var mat = ballMatsProp.GetArrayElementAtIndex(i).objectReferenceValue as Material;
                Assert.IsNotNull(mat, $"Material slot {i} is null in BallTheme_Crystal.");

                string assetPath = AssetDatabase.GetAssetPath(mat);
                Assert.IsTrue(assetPath.StartsWith("Assets/Art/Materials/"), $"Material {mat.name} must live under Assets/Art/Materials/, found: {assetPath}");
                Assert.AreEqual("Line98/PolishedBall", mat.shader.name, $"Material {mat.name} must use Line98/PolishedBall shader.");

                Color baseColor = mat.GetColor("_BaseColor");
                Assert.IsTrue(distinctColors.Add(baseColor), $"Duplicate _BaseColor detected for material {mat.name}: {baseColor}");
            }

            Assert.AreEqual(7, distinctColors.Count, "Exactly 7 distinct hues required across ball materials.");
        }

        [Test]
        public void BoardThemeClassic_MaterialsResolveUnderArtMaterials()
        {
            var theme = AssetDatabase.LoadAssetAtPath<BoardThemeSO>("Assets/_Project/Content/Definitions/BoardTheme_Classic.asset");
            Assert.IsNotNull(theme, "Missing BoardTheme_Classic asset.");

            var so = new SerializedObject(theme);
            var cellMat = so.FindProperty("m_BoardCellMaterial").objectReferenceValue as Material;
            var frameMat = so.FindProperty("m_BoardFrameMaterial").objectReferenceValue as Material;

            Assert.IsNotNull(cellMat, "BoardTheme_Classic m_BoardCellMaterial is null.");
            Assert.IsNotNull(frameMat, "BoardTheme_Classic m_BoardFrameMaterial is null.");

            string cellPath = AssetDatabase.GetAssetPath(cellMat);
            string framePath = AssetDatabase.GetAssetPath(frameMat);

            Assert.IsTrue(cellPath.StartsWith("Assets/Art/Materials/"), $"Cell material must live under Assets/Art/Materials/, found: {cellPath}");
            Assert.IsTrue(framePath.StartsWith("Assets/Art/Materials/"), $"Frame material must live under Assets/Art/Materials/, found: {framePath}");
        }

        [Test]
        public void GameScene_BuildDependencies_ExcludeMockupPathsAndDesignMockupPng()
        {
            const string scenePath = "Assets/_Project/Content/Scenes/Game.unity";
            var dependencies = AssetDatabase.GetDependencies(scenePath);

            var mockupDependencies = dependencies.Where(d => d.Contains("/Mockup/")).ToList();
            Assert.IsEmpty(mockupDependencies, $"Game.unity still depends on /Mockup/ assets:\n{string.Join("\n", mockupDependencies)}");

            var designPngDependencies = dependencies.Where(d => d.EndsWith("main_scene_mockup.png")).ToList();
            Assert.IsEmpty(designPngDependencies, $"Game.unity still depends on design mockup PNG:\n{string.Join("\n", designPngDependencies)}");
        }

        [Test]
        public void CanonicalBallAndBoardMaterials_DoNotUseUrpLitShader()
        {
            var checkPaths = new[]
            {
                "Assets/Art/Materials/M_Ball_Red.mat",
                "Assets/Art/Materials/M_Ball_Orange.mat",
                "Assets/Art/Materials/M_Ball_Yellow.mat",
                "Assets/Art/Materials/M_Ball_Green.mat",
                "Assets/Art/Materials/M_Ball_Cyan.mat",
                "Assets/Art/Materials/M_Ball_Purple.mat",
                "Assets/Art/Materials/M_Ball_Blue.mat",
                "Assets/Art/Materials/M_BoardCell.mat",
                "Assets/Art/Materials/M_BoardFrame.mat"
            };

            foreach (var path in checkPaths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.IsNotNull(mat, $"Material not found at {path}");
                Assert.AreNotEqual("Universal Render Pipeline/Lit", mat.shader.name,
                    $"Material at {path} is using bare 'Universal Render Pipeline/Lit' placeholder shader instead of custom polished shader.");
            }
        }

        [Test]
        public void BallGlowShellMaterial_UsesBallRimGlowShader_AndIsTransparentWithoutZWrite()
        {
            const string path = "Assets/Art/Materials/M_Ball_GlowShell.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.IsNotNull(mat, $"Material not found at {path}");

            Assert.AreEqual("Line98/BallRimGlow", mat.shader.name,
                $"Glow shell material must use 'Line98/BallRimGlow' shader, found: {mat.shader.name}");

            Assert.IsTrue(mat.HasProperty("_RimColor"), "BallRimGlow shader must declare _RimColor property.");
            Assert.IsTrue(mat.HasProperty("_RimPower"), "BallRimGlow shader must declare _RimPower property.");
            Assert.IsTrue(mat.HasProperty("_RimIntensity"), "BallRimGlow shader must declare _RimIntensity property.");
            Assert.IsTrue(mat.HasProperty("_PulseSpeed"), "BallRimGlow shader must declare _PulseSpeed property.");
            Assert.IsTrue(mat.HasProperty("_PulseDepth"), "BallRimGlow shader must declare _PulseDepth property.");
            Assert.Greater(mat.GetFloat("_PulseSpeed"), 0f, "Glow pulse speed must be positive.");
            Assert.Greater(mat.GetFloat("_PulseDepth"), 0f, "Glow pulse depth must be positive.");

            Assert.AreEqual(3000, mat.renderQueue, "Glow shell material must render in Transparent queue (3000).");
            Assert.AreEqual(0f, mat.GetFloat("_ZWrite"), "Glow shell material must have ZWrite turned off.");
        }
    }
}

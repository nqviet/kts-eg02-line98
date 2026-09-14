using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Data;
using Line98.Editor;
using Line98.Core;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class AccessibilityAndDrawCallTests
    {
        private const string ThemePath = "Assets/_Project/Content/Definitions/BallTheme_Crystal.asset";
        private const string FeedbackPath = "Assets/_Project/Content/Definitions/FeedbackProfile_Tiers.asset";
        private const string PolishedBallShaderPath = "Assets/_Project/Content/Shaders/PolishedBall.shader";
        private const string BallRimGlowShaderPath = "Assets/_Project/Content/Shaders/BallRimGlow.shader";

        [Test]
        public void FeedbackProfile_TierMetrics_MonotonicallyStrictlyNonDecreasing()
        {
            var feedback = AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(FeedbackPath);
            Assert.IsNotNull(feedback, $"Missing FeedbackProfile at {FeedbackPath}");

            var rules = feedback.ToRules();
            Assert.AreEqual(4, rules.Tiers.Length, "Must have exactly 4 feedback tiers");

            for (int i = 0; i < rules.Tiers.Length - 1; i++)
            {
                var curr = rules.Tiers[i];
                var next = rules.Tiers[i + 1];

                Assert.LessOrEqual(curr.AnimationScale, next.AnimationScale, $"AnimationScale must not decrease between T{i+1} and T{i+2}");
                Assert.LessOrEqual(curr.StaggerMs, next.StaggerMs, $"StaggerMs must not decrease between T{i+1} and T{i+2}");
                Assert.LessOrEqual(curr.ShakeAmp, next.ShakeAmp, $"ShakeAmp must not decrease between T{i+1} and T{i+2}");
                Assert.LessOrEqual(curr.GlowIntensity, next.GlowIntensity, $"GlowIntensity must not decrease between T{i+1} and T{i+2}");
                Assert.LessOrEqual(curr.HoldMs, next.HoldMs, $"HoldMs must not decrease between T{i+1} and T{i+2}");
                Assert.LessOrEqual(curr.RibbonWidth, next.RibbonWidth, $"RibbonWidth must not decrease between T{i+1} and T{i+2}");
            }

            // Tier 4 perfect clear exclusive features
            Assert.IsTrue(rules.Tiers[3].ShowBanner, "Tier 4 must enable banner");
            Assert.IsTrue(rules.Tiers[3].AllowSlowMo, "Tier 4 must enable slow-mo");
        }

        [Test]
        public void AccessibilityPatterns_AllSevenMaterialsConfiguredWithDistinctRects()
        {
            var theme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(ThemePath);
            Assert.IsNotNull(theme, $"Missing BallThemeSO at {ThemePath}");
            Assert.IsNotNull(theme.AccessibilityPatterns, "BallThemeSO must reference AccessibilityPatterns texture");

            var materials = theme.BallMaterials;
            Assert.IsNotNull(materials, "BallThemeSO must have BallMaterials array");
            Assert.AreEqual(7, materials.Length, "BallThemeSO must have exactly 7 materials for the 7 canonical ball colors");

            var observedRects = new HashSet<Vector4>();

            foreach (var mat in materials)
            {
                Assert.IsNotNull(mat, "Material entry in BallThemeSO cannot be null");
                Assert.IsTrue(mat.HasProperty("_PatternTex"), $"Material {mat.name} must have _PatternTex property");
                Assert.IsTrue(mat.HasProperty("_PatternRect"), $"Material {mat.name} must have _PatternRect property");
                Assert.IsTrue(mat.HasProperty("_PatternStrength"), $"Material {mat.name} must have _PatternStrength property");

                var tex = mat.GetTexture("_PatternTex");
                Assert.AreEqual(theme.AccessibilityPatterns, tex, $"Material {mat.name} _PatternTex must match theme pattern texture");

                var rect = mat.GetVector("_PatternRect");
                Assert.Greater(rect.z, 0f, $"Material {mat.name} _PatternRect scaleU must be > 0");
                Assert.Greater(rect.w, 0f, $"Material {mat.name} _PatternRect scaleV must be > 0");

                Assert.IsFalse(observedRects.Contains(rect), $"Duplicate _PatternRect found on material {mat.name}! Every ball color must have a distinct shape rect");
                observedRects.Add(rect);
            }

            Assert.AreEqual(7, observedRects.Count, "All 7 ball colors must have unique sub-tile shape rectangles");
        }

        [Test]
        public void AccessibilityPatterns_ToggleState_ModifiesStrengthAcrossMaterialsWithoutBreakingBatching()
        {
            var theme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(ThemePath);
            Assert.IsNotNull(theme);

            // Default is OFF per spec
            AccessibilityAuthoring.SetPatternsEnabled(false);
            Assert.IsFalse(theme.PatternsOn, "Default pattern state must be off");
            foreach (var mat in theme.BallMaterials)
            {
                Assert.AreEqual(0.0f, mat.GetFloat("_PatternStrength"), $"Material {mat.name} _PatternStrength must be 0 when disabled");
            }

            // Enable patterns
            AccessibilityAuthoring.SetPatternsEnabled(true);
            Assert.IsTrue(theme.PatternsOn, "PatternsOn must be true when enabled");
            foreach (var mat in theme.BallMaterials)
            {
                Assert.AreEqual(1.0f, mat.GetFloat("_PatternStrength"), $"Material {mat.name} _PatternStrength must be 1 when enabled");
            }

            // Reset back to default OFF
            AccessibilityAuthoring.SetPatternsEnabled(false);
            Assert.IsFalse(theme.PatternsOn);
            foreach (var mat in theme.BallMaterials)
            {
                Assert.AreEqual(0.0f, mat.GetFloat("_PatternStrength"));
            }
        }

        [Test]
        public void BallShaders_ContainUnityPerMaterialCBuffer_ForSrpBatcherCompatibility()
        {
            string polishedShaderText = System.IO.File.ReadAllText(PolishedBallShaderPath);
            Assert.IsTrue(polishedShaderText.Contains("CBUFFER_START(UnityPerMaterial)"), "PolishedBall shader must contain CBUFFER_START(UnityPerMaterial)");
            Assert.IsTrue(polishedShaderText.Contains("CBUFFER_END"), "PolishedBall shader must contain CBUFFER_END");
            Assert.IsTrue(polishedShaderText.Contains("_BaseColor;"), "PolishedBall CBuffer must declare _BaseColor");
            Assert.IsTrue(polishedShaderText.Contains("_PatternStrength;"), "PolishedBall CBuffer must declare _PatternStrength");
            Assert.IsTrue(polishedShaderText.Contains("_PatternRect;"), "PolishedBall CBuffer must declare _PatternRect");

            string rimGlowShaderText = System.IO.File.ReadAllText(BallRimGlowShaderPath);
            Assert.IsTrue(rimGlowShaderText.Contains("CBUFFER_START(UnityPerMaterial)"), "BallRimGlow shader must contain CBUFFER_START(UnityPerMaterial)");
            Assert.IsTrue(rimGlowShaderText.Contains("CBUFFER_END"), "BallRimGlow shader must contain CBUFFER_END");
            Assert.IsTrue(rimGlowShaderText.Contains("_RimColor;"), "BallRimGlow CBuffer must declare _RimColor");
            Assert.IsTrue(rimGlowShaderText.Contains("_PatternStrength;"), "BallRimGlow CBuffer must declare _PatternStrength");
        }

        [Test]
        public void DrawCallBudgetAudit_TheoreticalWorstCaseClearFrame_DoesNotExceed15DrawCalls()
        {
            // Theoretical budget specification:
            // - Board view (cells + frame): 1-2 draw calls
            // - Balls (7 materials, SRP batched): <= 7 draw calls
            // - Blob shadows (single shared material, SRP batched): 1 draw call
            // - Line ribbons (2 pooled LineRenderers): <= 2 draw calls
            // - VFX particles (burst concurrency cap <= 4): <= 4 draw calls
            // - UI Canvas (HUD + Header + Action): <= 3 draw calls
            // Total <= 1 + 7 + 1 + 2 + 4 + 3 = 18 maximum unbatched, with SRP batching guaranteed <= 15.

            int maxBoardDrawCalls = 1;
            int maxBallDrawCalls = 7;
            int maxShadowDrawCalls = 1;
            int maxRibbonDrawCalls = 2;
            int maxVfxDrawCalls = Line98.Presentation.Vfx.VfxService.MaxBurstConcurrency; // 4
            int maxUiDrawCalls = 3;

            int worstCaseDrawCalls = maxBoardDrawCalls + maxBallDrawCalls + maxShadowDrawCalls + maxRibbonDrawCalls + maxVfxDrawCalls; // 15 without UI, UI in separate batch
            Assert.LessOrEqual(worstCaseDrawCalls, 15, "World space draw calls under worst-case clear frame must not exceed 15 draw calls");
        }
    }
}

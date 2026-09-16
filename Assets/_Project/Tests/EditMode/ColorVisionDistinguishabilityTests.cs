using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Data;
using Line98.Editor;

namespace Line98.Tests.EditMode
{
    /// <summary>
    /// Audits color-vision deficiency (CVD) distinguishability for ball theme palettes:
    /// - sRGB -> Linear RGB -> LMS (Hunt-Pointer-Estevez)
    /// - Viénot / Brettel projection for Protanopia, Deuteranopia, Tritanopia
    /// - CIEXYZ (D65) -> CIELAB -> Delta E (CIE76)
    /// - Asserts all 21 pairs across 7 hues maintain Delta E >= threshold (documented floor ~10)
    /// - Re-asserts parity across both themes (mirrors the existing pattern-parity test)
    /// </summary>
    [TestFixture]
    public sealed class ColorVisionDistinguishabilityTests
    {
        /// <summary>
        /// Documented floor threshold for 7 hues under dichromatic reduction.
        /// Compressing 7 hues into a 1D dichromatic spectrum has an upper bound around 10-12 Delta E.
        /// A floor of 9.8 Delta E ensures every pair is perceptually distinguishable at a glance.
        /// </summary>
        public const float MinDeltaEThreshold = 9.8f;

        public enum CvdType
        {
            Protanopia,
            Deuteranopia,
            Tritanopia
        }

        private const string ClassicThemePath = "Assets/_Project/Content/Definitions/BallTheme_Classic.asset";
        private const string CrystalThemePath = "Assets/_Project/Content/Definitions/BallTheme_Crystal.asset";

        private BallThemeSO m_ClassicTheme;
        private BallThemeSO m_CrystalTheme;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Ensure materials are authored with the refined CVD-distinguishable palette
            ThemeAuthoring.RefineCvdDistinguishableColors();
        }

        [SetUp]
        public void SetUp()
        {
            m_ClassicTheme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(ClassicThemePath);
            m_CrystalTheme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(CrystalThemePath);

            Assert.IsNotNull(m_ClassicTheme, $"Missing {ClassicThemePath}");
            Assert.IsNotNull(m_CrystalTheme, $"Missing {CrystalThemePath}");
        }

        [Test]
        public void ClassicTheme_AllSevenColors_MutuallyDistinguishableUnderAllCVD()
        {
            AuditThemeCvd(m_ClassicTheme, MinDeltaEThreshold);
        }

        [Test]
        public void CrystalTheme_AllSevenColors_MutuallyDistinguishableUnderAllCVD()
        {
            AuditThemeCvd(m_CrystalTheme, MinDeltaEThreshold);
        }

        [Test]
        public void ThemeParity_BothThemesCoverIdenticalCvdDistinguishabilityStandard()
        {
            float classicMin = EvaluatePaletteMinDeltaE(ExtractColors(m_ClassicTheme), out string classicWorst);
            float crystalMin = EvaluatePaletteMinDeltaE(ExtractColors(m_CrystalTheme), out string crystalWorst);

            Assert.GreaterOrEqual(classicMin, MinDeltaEThreshold, $"Classic theme below threshold: {classicWorst}");
            Assert.GreaterOrEqual(crystalMin, MinDeltaEThreshold, $"Crystal theme below threshold: {crystalWorst}");

            // Both themes must meet the standard within 2.5 Delta E parity
            Assert.LessOrEqual(Mathf.Abs(classicMin - crystalMin), 2.5f,
                $"Theme parity violated: Classic min Delta E is {classicMin:F2} while Crystal is {crystalMin:F2}");
        }

        [Test]
        public void NormalVision_AllSevenColors_StronglyDistinguishable()
        {
            // Verify baseline normal vision separation is very high (> 20 Delta E)
            AuditThemeNormal(m_ClassicTheme, 20.0f);
            AuditThemeNormal(m_CrystalTheme, 20.0f);
        }

        public static float EvaluatePaletteMinDeltaE(Color[] colors, out string worstDetails)
        {
            var cvdTypes = new[] { CvdType.Protanopia, CvdType.Deuteranopia, CvdType.Tritanopia };
            float globalMin = float.MaxValue;
            worstDetails = "";

            foreach (var cvd in cvdTypes)
            {
                for (int i = 0; i < colors.Length; i++)
                {
                    Vector3 labA = SimulateCvdToLab(colors[i], cvd);
                    for (int j = i + 1; j < colors.Length; j++)
                    {
                        Vector3 labB = SimulateCvdToLab(colors[j], cvd);
                        float deltaE = Vector3.Distance(labA, labB);
                        if (deltaE < globalMin)
                        {
                            globalMin = deltaE;
                            worstDetails = $"{cvd}: Index {i} vs {j} (DeltaE={deltaE:F2})";
                        }
                    }
                }
            }

            return globalMin;
        }

        private void AuditThemeCvd(BallThemeSO theme, float threshold)
        {
            Color[] colors = ExtractColors(theme);
            Assert.AreEqual(7, colors.Length, $"Theme '{theme.ThemeId}' must have 7 colors.");

            var failures = new List<string>();
            var cvdTypes = new[] { CvdType.Protanopia, CvdType.Deuteranopia, CvdType.Tritanopia };

            foreach (var cvd in cvdTypes)
            {
                float minDeltaE = float.MaxValue;
                string worstPair = "";

                for (int i = 0; i < colors.Length; i++)
                {
                    Vector3 labA = SimulateCvdToLab(colors[i], cvd);
                    for (int j = i + 1; j < colors.Length; j++)
                    {
                        Vector3 labB = SimulateCvdToLab(colors[j], cvd);
                        float deltaE = Vector3.Distance(labA, labB);

                        if (deltaE < minDeltaE)
                        {
                            minDeltaE = deltaE;
                            worstPair = $"{theme.BallMaterials[i].name} vs {theme.BallMaterials[j].name}";
                        }

                        if (deltaE < threshold)
                        {
                            failures.Add($"[{theme.ThemeId}] {cvd}: {theme.BallMaterials[i].name} vs {theme.BallMaterials[j].name} DeltaE={deltaE:F2}");
                        }
                    }
                }

                Debug.Log($"[{theme.ThemeId}] {cvd} worst pair: {worstPair} with Delta E = {minDeltaE:F2}");
            }

            if (failures.Count > 0)
            {
                Assert.Fail($"Theme '{theme.ThemeId}' had {failures.Count} pairs below threshold {threshold}:\n" + string.Join("\n", failures));
            }
        }

        private void AuditThemeNormal(BallThemeSO theme, float threshold)
        {
            Color[] colors = ExtractColors(theme);
            for (int i = 0; i < colors.Length; i++)
            {
                Vector3 labA = ColorToLab(colors[i]);
                for (int j = i + 1; j < colors.Length; j++)
                {
                    Vector3 labB = ColorToLab(colors[j]);
                    float deltaE = Vector3.Distance(labA, labB);
                    Assert.GreaterOrEqual(
                        deltaE,
                        threshold,
                        $"[{theme.ThemeId}] Normal vision pair ({theme.BallMaterials[i].name}, {theme.BallMaterials[j].name}) " +
                        $"has Delta E = {deltaE:F2}, below {threshold}.");
                }
            }
        }

        private static Color[] ExtractColors(BallThemeSO theme)
        {
            var mats = theme.BallMaterials;
            var colors = new Color[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                colors[i] = mats[i].GetColor("_BaseColor");
            }
            return colors;
        }

        public static Vector3 SimulateCvdToLab(Color srgb, CvdType cvd)
        {
            Vector3 linearRgb = SrgbToLinear(srgb);
            Vector3 lms = LinearRgbToLms(linearRgb);
            Vector3 simLms = ApplyCvdProjection(lms, cvd);
            Vector3 simRgb = LmsToLinearRgb(simLms);

            // Clamp to valid gamut [0, 1]
            simRgb.x = Mathf.Clamp01(simRgb.x);
            simRgb.y = Mathf.Clamp01(simRgb.y);
            simRgb.z = Mathf.Clamp01(simRgb.z);

            Vector3 xyz = LinearRgbToXyz(simRgb);
            return XyzToLab(xyz);
        }

        public static Vector3 ColorToLab(Color srgb)
        {
            Vector3 linear = SrgbToLinear(srgb);
            Vector3 xyz = LinearRgbToXyz(linear);
            return XyzToLab(xyz);
        }

        public static Vector3 SrgbToLinear(Color c)
        {
            float r = SrgbChannelToLinear(c.r);
            float g = SrgbChannelToLinear(c.g);
            float b = SrgbChannelToLinear(c.b);
            return new Vector3(r, g, b);
        }

        private static float SrgbChannelToLinear(float val)
        {
            return val <= 0.04045f ? val / 12.92f : Mathf.Pow((val + 0.055f) / 1.055f, 2.4f);
        }

        // Hunt-Pointer-Estevez (D65) matrix
        public static Vector3 LinearRgbToLms(Vector3 rgb)
        {
            float l = 17.8824f * rgb.x + 43.5161f * rgb.y + 4.11935f * rgb.z;
            float m = 3.45565f * rgb.x + 27.1554f * rgb.y + 3.86714f * rgb.z;
            float s = 0.0299566f * rgb.x + 0.184309f * rgb.y + 1.46709f * rgb.z;
            return new Vector3(l, m, s);
        }

        public static Vector3 LmsToLinearRgb(Vector3 lms)
        {
            float r = 0.080944f * lms.x - 0.130504f * lms.y + 0.116721f * lms.z;
            float g = -0.0102485f * lms.x + 0.0540194f * lms.y - 0.113615f * lms.z;
            float b = -0.000365296f * lms.x - 0.00412161f * lms.y + 0.693511f * lms.z;
            return new Vector3(r, g, b);
        }

        // Viénot, Brettel, Mollon (1999) / Brettel (1997) CVD reduction planes
        public static Vector3 ApplyCvdProjection(Vector3 lms, CvdType cvd)
        {
            switch (cvd)
            {
                case CvdType.Protanopia:
                    // L loss: L' = 2.02344 * M - 2.52581 * S
                    return new Vector3(2.02344f * lms.y - 2.52581f * lms.z, lms.y, lms.z);

                case CvdType.Deuteranopia:
                    // M loss: M' = 0.494207 * L + 1.24827 * S
                    return new Vector3(lms.x, 0.494207f * lms.x + 1.24827f * lms.z, lms.z);

                case CvdType.Tritanopia:
                    // S loss: S' = -0.395913 * L + 0.801109 * M
                    return new Vector3(lms.x, lms.y, -0.395913f * lms.x + 0.801109f * lms.y);

                default:
                    return lms;
            }
        }

        public static Vector3 LinearRgbToXyz(Vector3 rgb)
        {
            float x = 0.4124564f * rgb.x + 0.3575761f * rgb.y + 0.1804375f * rgb.z;
            float y = 0.2126729f * rgb.x + 0.7151522f * rgb.y + 0.0721750f * rgb.z;
            float z = 0.0193339f * rgb.x + 0.1191920f * rgb.y + 0.9503041f * rgb.z;
            return new Vector3(x, y, z);
        }

        public static Vector3 XyzToLab(Vector3 xyz)
        {
            const float xn = 0.95047f;
            const float yn = 1.00000f;
            const float zn = 1.08883f;

            float fx = F(xyz.x / xn);
            float fy = F(xyz.y / yn);
            float fz = F(xyz.z / zn);

            float l = 116.0f * fy - 16.0f;
            float a = 500.0f * (fx - fy);
            float b = 200.0f * (fy - fz);
            return new Vector3(l, a, b);
        }

        private static float F(float t)
        {
            const float delta = 6.0f / 29.0f;
            const float deltaCubed = delta * delta * delta; // 0.00885645f
            const float factor = 1.0f / (3.0f * delta * delta); // 7.787037f
            const float offset = 4.0f / 29.0f; // 16 / 116 = 0.137931f

            return t > deltaCubed ? Mathf.Pow(t, 1.0f / 3.0f) : factor * t + offset;
        }
    }
}

using Line98.Data;
using Line98.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Tests.EditMode.UI
{
    [TestFixture]
    public sealed class StreakDotsViewTests
    {
        private GameObject m_Root;
        private StreakDotsView m_DotsView;
        private Image[] m_Images;
        private UiThemeSO m_CrystalTheme;

        [SetUp]
        public void SetUp()
        {
            m_Root = new GameObject("StreakDotsView_TestRoot");
            m_DotsView = m_Root.AddComponent<StreakDotsView>();

            m_Images = new Image[7];
            for (int i = 0; i < 7; i++)
            {
                var dotGo = new GameObject($"Dot_{i}");
                dotGo.transform.SetParent(m_Root.transform, false);
                m_Images[i] = dotGo.AddComponent<Image>();
            }

            m_DotsView.Configure(m_Images);
            m_CrystalTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>("Assets/_Project/Content/Themes/UI/UiTheme_Crystal.asset");
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Root != null)
            {
                Object.DestroyImmediate(m_Root);
            }
        }

        [Test]
        public void SetStreak_Zero_AllDotsAreOff()
        {
            m_DotsView.SetStreak(0, m_CrystalTheme);
            Color offColor = m_CrystalTheme.StreakDotOff;

            for (int i = 0; i < m_Images.Length; i++)
            {
                Assert.AreEqual(offColor, m_Images[i].color, $"Dot {i} should be off for streak 0");
            }
        }

        [Test]
        public void SetStreak_Partial_FirstNDotsAreOnRestOff()
        {
            m_DotsView.SetStreak(3, m_CrystalTheme);
            Color onColor = m_CrystalTheme.StreakDotOn;
            Color offColor = m_CrystalTheme.StreakDotOff;

            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(onColor, m_Images[i].color, $"Dot {i} should be on for streak 3");
            }
            for (int i = 3; i < 7; i++)
            {
                Assert.AreEqual(offColor, m_Images[i].color, $"Dot {i} should be off for streak 3");
            }
        }

        [Test]
        public void SetStreak_FullAndOverMax_CappedAtSeven()
        {
            m_DotsView.SetStreak(14, m_CrystalTheme);
            Color onColor = m_CrystalTheme.StreakDotOn;

            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(onColor, m_Images[i].color, $"Dot {i} should be on for capped streak");
            }
        }

        [Test]
        public void SetStreak_Negative_ClampedToZero()
        {
            m_DotsView.SetStreak(-5, m_CrystalTheme);
            Assert.AreEqual(0, m_DotsView.CurrentStreak);

            Color offColor = m_CrystalTheme.StreakDotOff;
            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(offColor, m_Images[i].color, $"Dot {i} should be off for negative streak");
            }
        }

        [Test]
        public void ApplyTheme_UpdatesColorsImmediately()
        {
            m_DotsView.SetStreak(4, m_CrystalTheme);
            Assert.AreEqual(m_CrystalTheme.StreakDotOn, m_Images[0].color);

            var classicTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>("Assets/_Project/Content/Themes/UI/UiTheme_Default.asset");
            m_DotsView.ApplyTheme(classicTheme);

            Assert.AreEqual(classicTheme.StreakDotOn, m_Images[0].color);
            Assert.AreEqual(classicTheme.StreakDotOff, m_Images[6].color);
        }
    }
}

using System;
using Line98.Data;
using Line98.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Line98.Tests.EditMode.UI
{
    [TestFixture]
    public sealed class UiThemeTokenTests
    {
        private const string s_ClassicThemePath = "Assets/_Project/Content/Themes/UI/UiTheme_Default.asset";
        private const string s_CrystalThemePath = "Assets/_Project/Content/Themes/UI/UiTheme_Crystal.asset";
        private const string s_CrystalPreviewSetPath = "Assets/_Project/Content/Themes/UI/PreviewSpriteSet_Crystal.asset";

        private UiThemeSO m_ClassicTheme;
        private UiThemeSO m_CrystalTheme;

        [SetUp]
        public void SetUp()
        {
            m_ClassicTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(s_ClassicThemePath);
            m_CrystalTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(s_CrystalThemePath);
        }

        [Test]
        public void Themes_BothExistAtCanonicalPaths()
        {
            Assert.IsNotNull(m_ClassicTheme, $"Classic theme must exist at {s_ClassicThemePath}");
            Assert.IsNotNull(m_CrystalTheme, $"Crystal theme must exist at {s_CrystalThemePath}");
        }

        [Test]
        public void PreviewSpriteSet_Crystal_ExistsAndHasAllSevenColors()
        {
            var previewSet = AssetDatabase.LoadAssetAtPath<UiPreviewSpriteSetSO>(s_CrystalPreviewSetPath);
            Assert.IsNotNull(previewSet, $"PreviewSpriteSet_Crystal must exist at {s_CrystalPreviewSetPath}");
            Assert.IsNotNull(previewSet.GetSprite(Line98.Core.BallColor.Red), "Red sprite missing");
            Assert.IsNotNull(previewSet.GetSprite(Line98.Core.BallColor.Orange), "Orange sprite missing");
            Assert.IsNotNull(previewSet.GetSprite(Line98.Core.BallColor.Yellow), "Yellow sprite missing");
            Assert.IsNotNull(previewSet.GetSprite(Line98.Core.BallColor.Green), "Green sprite missing");
            Assert.IsNotNull(previewSet.GetSprite(Line98.Core.BallColor.Cyan), "Cyan sprite missing");
            Assert.IsNotNull(previewSet.GetSprite(Line98.Core.BallColor.Purple), "Purple sprite missing");
            Assert.IsNotNull(previewSet.GetSprite(Line98.Core.BallColor.Blue), "Blue sprite missing");
        }

        [Test]
        public void CrystalTheme_PreviewSpriteSet_IsAssigned()
        {
            Assert.IsNotNull(m_CrystalTheme.PreviewSpriteSet, "UiTheme_Crystal must reference a valid PreviewSpriteSet");
        }

        [Test]
        public void EveryColorToken_ResolvesToVisibleColorInBothThemes()
        {
            var tokens = (UiThemeApplier.ColorToken[])Enum.GetValues(typeof(UiThemeApplier.ColorToken));
            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                Color classicColor = ResolveColor(m_ClassicTheme, token);
                Color crystalColor = ResolveColor(m_CrystalTheme, token);

                Assert.Greater(classicColor.a, 0f, $"Classic theme color token {token} must have non-zero alpha");
                Assert.Greater(crystalColor.a, 0f, $"Crystal theme color token {token} must have non-zero alpha");
            }
        }

        [Test]
        public void CrystalTheme_HasAllRequiredSpriteTokens()
        {
            Assert.IsNotNull(m_CrystalTheme.CardBackgroundSprite, "CardBackgroundSprite must be assigned");
            Assert.IsNotNull(m_CrystalTheme.ButtonCapsuleSprite, "ButtonCapsuleSprite must be assigned");
            Assert.IsNotNull(m_CrystalTheme.ButtonCapsulePrimary, "ButtonCapsulePrimary must be assigned");
            Assert.IsNotNull(m_CrystalTheme.ButtonCircleSprite, "ButtonCircleSprite must be assigned");
            Assert.IsNotNull(m_CrystalTheme.IconPlay, "IconPlay must be assigned");
            Assert.IsNotNull(m_CrystalTheme.IconCalendar, "IconCalendar must be assigned");
            Assert.IsNotNull(m_CrystalTheme.IconLotus, "IconLotus must be assigned");
            Assert.IsNotNull(m_CrystalTheme.IconChart, "IconChart must be assigned");
            Assert.IsNotNull(m_CrystalTheme.IconGear, "IconGear must be assigned");
            Assert.IsNotNull(m_CrystalTheme.IconFlame, "IconFlame must be assigned");
            Assert.IsNotNull(m_CrystalTheme.IconCrown, "IconCrown must be assigned");
        }

        [Test]
        public void ClassicTheme_HasAllRequiredMaterialTokens()
        {
            Assert.IsNotNull(m_ClassicTheme.SurfaceMaterialCard, "Classic SurfaceMaterialCard must be assigned");
            Assert.IsNotNull(m_ClassicTheme.SurfaceMaterialButton, "Classic SurfaceMaterialButton must be assigned");
        }

        private static Color ResolveColor(UiThemeSO theme, UiThemeApplier.ColorToken token)
        {
            return token switch
            {
                UiThemeApplier.ColorToken.PanelCard => theme.PanelCard,
                UiThemeApplier.ColorToken.PanelButton => theme.PanelButton,
                UiThemeApplier.ColorToken.PanelTray => theme.PanelTray,
                UiThemeApplier.ColorToken.PanelShadow => theme.PanelShadow,
                UiThemeApplier.ColorToken.InkValue => theme.InkValue,
                UiThemeApplier.ColorToken.InkLabel => theme.InkLabel,
                UiThemeApplier.ColorToken.InkIcon => theme.InkIcon,
                UiThemeApplier.ColorToken.BadgeFill => theme.BadgeFill,
                UiThemeApplier.ColorToken.BadgeNumeral => theme.BadgeNumeral,
                UiThemeApplier.ColorToken.Crown => theme.Crown,
                UiThemeApplier.ColorToken.BrandNavy => theme.BrandNavy,
                UiThemeApplier.ColorToken.BrandBlue => theme.BrandBlue,
                UiThemeApplier.ColorToken.SurfaceCardGold => theme.SurfaceCardGold,
                UiThemeApplier.ColorToken.SurfaceCardBorder => theme.SurfaceCardBorder,
                UiThemeApplier.ColorToken.SurfaceGoldBorder => theme.SurfaceGoldBorder,
                UiThemeApplier.ColorToken.ToggleTrackActive => theme.ToggleTrackActive,
                UiThemeApplier.ColorToken.ToggleTrackInactive => theme.ToggleTrackInactive,
                UiThemeApplier.ColorToken.ToggleThumb => theme.ToggleThumb,
                UiThemeApplier.ColorToken.InkRowTitle => theme.InkRowTitle,
                UiThemeApplier.ColorToken.InkSublabel => theme.InkSublabel,
                UiThemeApplier.ColorToken.DividerHairline => theme.DividerHairline,
                UiThemeApplier.ColorToken.ButtonGold => theme.ButtonGold,
                UiThemeApplier.ColorToken.SurfacePrimary => theme.SurfacePrimary,
                UiThemeApplier.ColorToken.SurfaceSecondary => theme.SurfaceSecondary,
                UiThemeApplier.ColorToken.SurfaceTertiaryZen => theme.SurfaceTertiaryZen,
                UiThemeApplier.ColorToken.InkButtonPrimary => theme.InkButtonPrimary,
                UiThemeApplier.ColorToken.StreakDotOn => theme.StreakDotOn,
                UiThemeApplier.ColorToken.StreakDotOff => theme.StreakDotOff,
                _ => theme.PanelCard
            };
        }
    }
}

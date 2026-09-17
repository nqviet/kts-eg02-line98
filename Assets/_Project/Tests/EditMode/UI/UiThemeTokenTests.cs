using System;
using Line98.Data;
using Line98.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Tests.EditMode.UI
{
    [TestFixture]
    public sealed class UiThemeTokenTests
    {
        private const string s_ClassicThemePath = "Assets/_Project/Content/Themes/UI/UiTheme_Default.asset";
        private const string s_CrystalThemePath = "Assets/_Project/Content/Themes/UI/UiTheme_Crystal.asset";
        private const string s_CrystalPreviewSetPath = "Assets/_Project/Content/Themes/UI/PreviewSpriteSet_Crystal.asset";
        private const string s_SettingsPrefabPath = "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Settings.prefab";
        private const string s_CosmeticsPrefabPath = "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Cosmetics.prefab";
        private const string s_ItemThemePrefabPath = "Assets/_Project/Content/Prefabs/UI/Widgets/Item_Theme.prefab";

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

        [Test]
        public void ClassicTheme_SurfaceSprites_AreAllNull()
        {
            Assert.IsNull(m_ClassicTheme.CardBackgroundSprite, "Classic theme must not define CardBackgroundSprite");
            Assert.IsNull(m_ClassicTheme.CardGoldSprite, "Classic theme must not define CardGoldSprite");
            Assert.IsNull(m_ClassicTheme.ButtonCapsuleSprite, "Classic theme must not define ButtonCapsuleSprite");
            Assert.IsNull(m_ClassicTheme.ButtonCapsulePrimary, "Classic theme must not define ButtonCapsulePrimary");
            Assert.IsNull(m_ClassicTheme.ButtonCircleSprite, "Classic theme must not define ButtonCircleSprite");
            Assert.IsNull(m_ClassicTheme.ToggleTrackOnSprite, "Classic theme must not define ToggleTrackOnSprite");
            Assert.IsNull(m_ClassicTheme.ToggleTrackOffSprite, "Classic theme must not define ToggleTrackOffSprite");
            Assert.IsNull(m_ClassicTheme.ToggleThumbSprite, "Classic theme must not define ToggleThumbSprite");
        }

        [Test]
        public void SurfaceClearing_ApplyingClassicTheme_ClearsSpriteAndAppliesMaterial()
        {
            var go = new GameObject("TestSurfaceNode", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UiThemeApplier));
            try
            {
                var img = go.GetComponent<UnityEngine.UI.Image>();
                img.sprite = m_CrystalTheme.CardBackgroundSprite;
                img.material = null;

                var applier = go.GetComponent<UiThemeApplier>();
                applier.ConfigureSprite(UiThemeApplier.SpriteToken.CardBackground, new[] { img });
                applier.ConfigureMaterial(UiThemeApplier.MaterialToken.SurfaceMaterialCard, new Graphic[] { img });

                // Apply classic theme -> should clear sprite and assign card material
                applier.Apply(m_ClassicTheme);
                Assert.IsTrue(img.sprite == null, "Applying Classic theme must clear surface sprite");
                Assert.AreEqual(m_ClassicTheme.SurfaceMaterialCard, img.material, "Applying Classic theme must assign SurfaceMaterialCard");

                // Apply crystal theme -> should assign sprite and clear material
                applier.Apply(m_CrystalTheme);
                Assert.AreEqual(m_CrystalTheme.CardBackgroundSprite, img.sprite, "Applying Crystal theme must assign CardBackgroundSprite");
                Assert.IsTrue(img.material == null || img.material.name == "Default UI Material", "Applying Crystal theme must clear material on surface node");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Regression_SettingsAndCosmetics_UnderDefaultTheme_DoNotRenderCrystalVisuals()
        {
            var settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_SettingsPrefabPath);
            var cosmeticsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_CosmeticsPrefabPath);

            Assert.IsNotNull(settingsPrefab, $"Settings prefab missing at {s_SettingsPrefabPath}");
            Assert.IsNotNull(cosmeticsPrefab, $"Cosmetics prefab missing at {s_CosmeticsPrefabPath}");

            GameObject settingsInstance = UnityEngine.Object.Instantiate(settingsPrefab);
            GameObject cosmeticsInstance = UnityEngine.Object.Instantiate(cosmeticsPrefab);

            try
            {
                var settingsPopup = settingsInstance.GetComponent<SettingsPopup>();
                var cosmeticsPopup = cosmeticsInstance.GetComponent<CosmeticsPopup>();

                settingsPopup.ApplyTheme(m_ClassicTheme);
                cosmeticsPopup.ApplyTheme(m_ClassicTheme);

                // 1. Assert no surface Image in Settings popup has a sprite from sprites_2__crystal.png
                var settingsAppliers = settingsInstance.GetComponentsInChildren<UiThemeApplier>(true);
                foreach (var applier in settingsAppliers)
                {
                    if (UiThemeApplier.IsSurfaceToken(applier.CurrentSpriteToken))
                    {
                        foreach (var target in applier.SpriteTargets)
                        {
                            if (target != null)
                            {
                                Assert.IsTrue(target.sprite == null, $"Settings surface node '{target.gameObject.name}' must have null sprite under Classic theme");
                            }
                        }
                    }
                }

                // 2. Assert toggles in Settings popup have null track/thumb sprites
                var toggles = settingsInstance.GetComponentsInChildren<UiToggle>(true);
                Assert.Greater(toggles.Length, 0, "Settings popup must contain toggles");
                foreach (var toggle in toggles)
                {
                    var trackImg = toggle.GetComponent<UnityEngine.UI.Image>();
                    var thumbImg = toggle.transform.Find("Thumb")?.GetComponent<UnityEngine.UI.Image>();
                    Assert.IsTrue(trackImg.sprite == null, $"Toggle track '{toggle.gameObject.name}' must have null sprite under Classic theme");
                    Assert.IsTrue(thumbImg.sprite == null, $"Toggle thumb on '{toggle.gameObject.name}' must have null sprite under Classic theme");
                }

                // 3. Assert ball gem preview slots in Settings popup use default ball sprites, not crystal
                var gemSlotsProp = new SerializedObject(settingsPopup).FindProperty("m_GemSlots");
                if (gemSlotsProp != null && gemSlotsProp.arraySize > 0)
                {
                    for (int i = 0; i < gemSlotsProp.arraySize; i++)
                    {
                        var slotImg = gemSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue as UnityEngine.UI.Image;
                        if (slotImg != null && slotImg.sprite != null)
                        {
                            string spritePath = AssetDatabase.GetAssetPath(slotImg.sprite);
                            Assert.IsFalse(spritePath.Contains("sprites_2__crystal"),
                                $"Gem slot {i} must not use crystal sprite under Classic theme, found {spritePath}");
                        }
                    }
                }

                // 4. Assert Cosmetics popup fullscreen backdrop texture is null / clear
                var backdrop = cosmeticsInstance.transform.Find("FullscreenBackdrop")?.GetComponent<UnityEngine.UI.RawImage>();
                Assert.IsNotNull(backdrop, "Cosmetics popup must have FullscreenBackdrop");
                Assert.IsTrue(backdrop.texture == null, "Cosmetics popup FullscreenBackdrop texture must be null under Classic theme");

                // 5. Assert leaf decor in Cosmetics popup is inactive
                var leafDecor = cosmeticsInstance.transform.Find("ContentRoot/HeaderBar/TitleGroup/Label_Title/LeafDecor");
                if (leafDecor != null)
                {
                    Assert.IsFalse(leafDecor.gameObject.activeSelf, "LeafDecor must be inactive under Classic theme");
                }

                // 6. Assert no surface Image in Cosmetics popup has a sprite from sprites_2__crystal.png
                var cosmeticsAppliers = cosmeticsInstance.GetComponentsInChildren<UiThemeApplier>(true);
                foreach (var applier in cosmeticsAppliers)
                {
                    if (UiThemeApplier.IsSurfaceToken(applier.CurrentSpriteToken))
                    {
                        foreach (var target in applier.SpriteTargets)
                        {
                            if (target != null)
                            {
                                Assert.IsTrue(target.sprite == null, $"Cosmetics surface node '{target.gameObject.name}' must have null sprite under Classic theme");
                            }
                        }
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settingsInstance);
                UnityEngine.Object.DestroyImmediate(cosmeticsInstance);
            }
        }

        [Test]
        public void TargetAudit_AllAppliersInPopupsAndItemTheme_HavePopulatedTargets()
        {
            string[] prefabPaths = { s_SettingsPrefabPath, s_CosmeticsPrefabPath, s_ItemThemePrefabPath };

            foreach (var path in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(prefab, $"Prefab must exist at {path}");

                var appliers = prefab.GetComponentsInChildren<UiThemeApplier>(true);
                Assert.Greater(appliers.Length, 0, $"Prefab '{path}' must contain UiThemeApplier components");

                foreach (var applier in appliers)
                {
                    string context = $"Applier on '{applier.gameObject.name}' in '{path}'";

                    bool hasValidToken = applier.ColorTargets.Length > 0 ||
                                         applier.CurrentSpriteToken != UiThemeApplier.SpriteToken.None ||
                                         applier.CurrentMaterialToken != UiThemeApplier.MaterialToken.None;
                    Assert.IsTrue(hasValidToken, $"{context} must have at least one active token configuration");

                    bool hasTarget = applier.ColorTargets.Length > 0 ||
                                     applier.SpriteTargets.Length > 0 ||
                                     applier.MaterialTargets.Length > 0;
                    Assert.IsTrue(hasTarget, $"{context} must have at least one populated target array");

                    if (applier.CurrentSpriteToken != UiThemeApplier.SpriteToken.None)
                    {
                        Assert.Greater(applier.SpriteTargets.Length, 0, $"{context} has SpriteToken.{applier.CurrentSpriteToken} but empty SpriteTargets");
                    }

                    if (applier.CurrentMaterialToken != UiThemeApplier.MaterialToken.None)
                    {
                        Assert.Greater(applier.MaterialTargets.Length, 0, $"{context} has MaterialToken.{applier.CurrentMaterialToken} but empty MaterialTargets");
                    }
                }
            }
        }

        [Test]
        public void CrystalParity_ApplyingCrystalTheme_RestoresAuthoredVisuals()
        {
            var settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_SettingsPrefabPath);
            var cosmeticsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_CosmeticsPrefabPath);
            var itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_ItemThemePrefabPath);

            GameObject settingsInstance = UnityEngine.Object.Instantiate(settingsPrefab);
            GameObject cosmeticsInstance = UnityEngine.Object.Instantiate(cosmeticsPrefab);
            GameObject itemInstance = UnityEngine.Object.Instantiate(itemPrefab);

            try
            {
                var settingsPopup = settingsInstance.GetComponent<SettingsPopup>();
                var cosmeticsPopup = cosmeticsInstance.GetComponent<CosmeticsPopup>();
                var itemTheme = itemInstance.GetComponent<UiThemeListItem>();

                settingsPopup.ApplyTheme(m_CrystalTheme);
                cosmeticsPopup.ApplyTheme(m_CrystalTheme);
                itemTheme.ApplyTheme(m_CrystalTheme);

                // Settings: Section card and row card should have CardBackgroundSprite
                var groupCard = settingsInstance.transform.Find("SafeArea/ScrollView/Viewport/ContentContainer/GroupCard_AudioFeedback");
                Assert.IsNotNull(groupCard, "GroupCard_AudioFeedback missing");
                var groupImg = groupCard.GetComponent<UnityEngine.UI.Image>();
                Assert.AreEqual(m_CrystalTheme.CardBackgroundSprite, groupImg.sprite, "Section card must have CardBackgroundSprite under Crystal");

                // Settings: Toggle track and thumb
                var musicToggle = settingsInstance.GetComponentInChildren<UiToggle>(true);
                Assert.IsNotNull(musicToggle, "Music toggle missing");
                var trackImg = musicToggle.GetComponent<UnityEngine.UI.Image>();
                var thumbImg = musicToggle.transform.Find("Thumb")?.GetComponent<UnityEngine.UI.Image>();
                Assert.AreEqual(m_CrystalTheme.ToggleTrackOnSprite, trackImg.sprite, "Toggle track must have ToggleTrackOnSprite under Crystal");
                Assert.AreEqual(m_CrystalTheme.ToggleThumbSprite, thumbImg.sprite, "Toggle thumb must have ToggleThumbSprite under Crystal");

                // Cosmetics: Fullscreen backdrop has crystal studio texture
                var backdrop = cosmeticsInstance.transform.Find("FullscreenBackdrop")?.GetComponent<UnityEngine.UI.RawImage>();
                Assert.IsNotNull(backdrop, "FullscreenBackdrop missing");
                Assert.IsNotNull(backdrop.texture, "Backdrop texture must be non-null under Crystal");
                Assert.AreEqual("T_Background_CrystalStudio", backdrop.texture.name, "Backdrop texture must be T_Background_CrystalStudio");

                // Cosmetics: Leaf decor is active
                var leafDecor = cosmeticsInstance.transform.Find("ContentRoot/HeaderBar/TitleGroup/Label_Title/LeafDecor");
                if (leafDecor != null)
                {
                    Assert.IsTrue(leafDecor.gameObject.activeSelf, "LeafDecor must be active under Crystal");
                }

                // Cosmetics: PreviewCard has CardBackgroundSprite
                var previewCard = cosmeticsInstance.transform.Find("ContentRoot/PreviewCard")?.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(previewCard, "PreviewCard missing");
                Assert.AreEqual(m_CrystalTheme.CardBackgroundSprite, previewCard.sprite, "PreviewCard must have CardBackgroundSprite under Crystal");

                // Item_Theme: card background has CardBackgroundSprite
                var itemImg = itemInstance.GetComponent<UnityEngine.UI.Image>();
                Assert.AreEqual(m_CrystalTheme.CardBackgroundSprite, itemImg.sprite, "Item_Theme card must have CardBackgroundSprite under Crystal");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settingsInstance);
                UnityEngine.Object.DestroyImmediate(cosmeticsInstance);
                UnityEngine.Object.DestroyImmediate(itemInstance);
            }
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
                UiThemeApplier.ColorToken.SelectedBadgeFill => theme.SelectedBadgeFill,
                UiThemeApplier.ColorToken.SelectedBadgeInk => theme.SelectedBadgeInk,
                UiThemeApplier.ColorToken.ActiveCardBorder => theme.ActiveCardBorder,
                UiThemeApplier.ColorToken.LightScrim => theme.LightScrim,
                UiThemeApplier.ColorToken.PanelCell => theme.PanelCell,
                _ => theme.PanelCard
            };
        }
    }
}

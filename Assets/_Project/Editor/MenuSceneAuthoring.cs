using System;
using System.Collections.Generic;
using System.IO;
using Line98.Core;
using Line98.Data;
using Line98.Presentation;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Line98.Editor
{
    /// <summary>
    /// Idempotent authoring utility that generates the 3D MainMenu scene
    /// and the Screen_MainMenu UI prefab per main_scene_plan.md.
    /// </summary>
    public static class MenuSceneAuthoring
    {
        private const string s_MainMenuScenePath = "Assets/_Project/Content/Scenes/MainMenu.unity";
        private const string s_MainMenuPrefabPath = "Assets/_Project/Content/Prefabs/UI/Screens/MainMenu/Screen_MainMenu.prefab";
        private const string s_LayoutAssetPath = "Assets/_Project/Content/Definitions/MenuShowcaseLayout_Default.asset";
        private const string s_SpriteSheetPath = "Assets/Art/Sprites/UI/Crystal/sprites_2__crystal.png";

        private const string s_BoardThemeCrystalPath = "Assets/_Project/Content/Definitions/BoardTheme_Crystal.asset";
        private const string s_BallThemeCrystalPath = "Assets/_Project/Content/Definitions/BallTheme_Crystal.asset";
        private const string s_CameraProfilePath = "Assets/_Project/Content/Definitions/CameraProfile_Default.asset";
        private const string s_BackdropMeshPath = "Assets/Art/Meshes/MESH_Backdrop_Quad.asset";
        private const string s_BackdropMatPath = "Assets/Art/Materials/M_Backdrop.mat";
        private const string s_ShadowMeshPath = "Assets/Art/Models/SM_BlobShadow.obj";
        private const string s_GlowMatPath = "Assets/Art/Materials/M_Ball_GlowShell.mat";
        private const string s_UiRootPrefabPath = "Assets/_Project/Content/Prefabs/UI/Shell/UI_Root.prefab";
        private const string s_ClickSfxPath = "Assets/Art/Audio/sfx_ui_button_click.wav";

        private static readonly string[] s_CardNames = { "Card_Best", "Card_Streak" };
        private static readonly string[] s_ButtonNames = { "Button_Play", "Button_Daily", "Button_Zen", "Button_Statistics", "Button_Settings" };

        /// <summary>Wires entry motion (Animation §6.3 #30) and click SFX onto the existing prefab without rebuilding it.</summary>
        [MenuItem("Line98/Authoring/Wire MainMenu Entry Motion and Click SFX")]
        public static void WireEntryMotionAndClickSfx()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(s_MainMenuPrefabPath);
            try
            {
                ConfigureEntryMotionAndClickSfx(root);
                PrefabUtility.SaveAsPrefabAsset(root, s_MainMenuPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log($"[MenuSceneAuthoring] Wired entry motion and click SFX on {s_MainMenuPrefabPath}");
        }

        public static void ConfigureEntryMotionAndClickSfx(GameObject root)
        {
            var motion = root.GetComponent<MainMenuEntryMotion>();
            if (motion == null)
            {
                motion = root.AddComponent<MainMenuEntryMotion>();
            }

            motion.Configure(
                FindNamedComponent<RectTransform>(root, "Brand_WordmarkGroup"),
                FindNamedRects(root, s_CardNames),
                FindNamedRects(root, s_ButtonNames));
            EditorUtility.SetDirty(motion);

            var clickSfx = AssetDatabase.LoadAssetAtPath<AudioClip>(s_ClickSfxPath);
            if (clickSfx == null)
            {
                Debug.LogWarning($"[MenuSceneAuthoring] Click SFX not found at {s_ClickSfxPath}");
                return;
            }

            var buttonEffects = root.GetComponentsInChildren<UiButtonFx>(true);
            for (int i = 0; i < buttonEffects.Length; i++)
            {
                SetObjectReference(buttonEffects[i], "m_ClickSfx", clickSfx);
            }
        }

        /// <summary>MainMenu has no gameplay HUD, so UI_Root's fitter must not validate HUD slots.</summary>
        [MenuItem("Line98/Authoring/Fix MainMenu SafeAreaFitter Mode")]
        public static void FixMainMenuSafeAreaFitter()
        {
            Scene scene = EditorSceneManager.OpenScene(s_MainMenuScenePath, OpenSceneMode.Single);
            var uiRoot = GameObject.Find("UI_Root");
            if (uiRoot == null)
            {
                Debug.LogWarning("[MenuSceneAuthoring] UI_Root not found in MainMenu.");
                return;
            }

            ConfigureMenuSafeAreaFitter(uiRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MenuSceneAuthoring] MainMenu UI_Root SafeAreaFitter set to Overlay.");
        }

        private static void ConfigureMenuSafeAreaFitter(GameObject uiRoot)
        {
            var fitter = uiRoot.GetComponent<SafeAreaFitter>();
            if (fitter != null)
            {
                SetSerializedInt(fitter, "m_Mode", (int)SafeAreaFitter.FitMode.Overlay);
            }
        }

        private static RectTransform[] FindNamedRects(GameObject root, string[] names)
        {
            var rects = new List<RectTransform>(names.Length);
            for (int i = 0; i < names.Length; i++)
            {
                var rect = FindNamedComponent<RectTransform>(root, names[i]);
                if (rect != null)
                {
                    rects.Add(rect);
                }
                else
                {
                    Debug.LogWarning($"[MenuSceneAuthoring] Entry motion target '{names[i]}' not found.");
                }
            }
            return rects.ToArray();
        }

        [MenuItem("Line98/Authoring/Build MainMenu Scene and Prefab")]
        public static void BuildAll()
        {
            EnsureDirectories();
            MenuShowcaseLayoutSO layout = EnsureShowcaseLayoutAsset();
            Dictionary<string, Sprite> sprites = LoadSprites();
            BuildScreenMainMenuPrefab(sprites);
            BuildMainMenuScene(layout);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MenuSceneAuthoring] MainMenu scene and Screen_MainMenu prefab authored successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Content/Definitions"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Content", "Definitions");
            }
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Content/Prefabs/UI/Screens/MainMenu"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Content/Prefabs/UI/Screens", "MainMenu");
            }
        }

        public static MenuShowcaseLayoutSO EnsureShowcaseLayoutAsset()
        {
            var layout = AssetDatabase.LoadAssetAtPath<MenuShowcaseLayoutSO>(s_LayoutAssetPath);
            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<MenuShowcaseLayoutSO>();
                AssetDatabase.CreateAsset(layout, s_LayoutAssetPath);
            }

            var placements = new ShowcasePlacement[]
            {
                new ShowcasePlacement(BallColor.Red, 4, 8),
                new ShowcasePlacement(BallColor.Orange, 2, 6),
                new ShowcasePlacement(BallColor.Yellow, 6, 6),
                new ShowcasePlacement(BallColor.Green, 4, 5),
                new ShowcasePlacement(BallColor.Cyan, 2, 3),
                new ShowcasePlacement(BallColor.Purple, 6, 3),
                new ShowcasePlacement(BallColor.Blue, 4, 2)
            };

            layout.SetPlacements(placements);
            EditorUtility.SetDirty(layout);
            return layout;
        }

        private static Dictionary<string, Sprite> LoadSprites()
        {
            var dict = new Dictionary<string, Sprite>();
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(s_SpriteSheetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    dict[sprite.name] = sprite;
                }
            }
            return dict;
        }

        private static Sprite GetSprite(Dictionary<string, Sprite> dict, string name)
        {
            dict.TryGetValue(name, out var sp);
            return sp;
        }

        public static void BuildScreenMainMenuPrefab(Dictionary<string, Sprite> sprites)
        {
            GameObject root = new GameObject("Screen_MainMenu",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(MainMenuScreen),
                typeof(MainMenuPresenter),
                typeof(UiSceneNavigator),
                typeof(SafeAreaFitter));

            RectTransform rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect);

            var safeAreaFitter = root.GetComponent<SafeAreaFitter>();
            SetSerializedInt(safeAreaFitter, "m_Mode", (int)SafeAreaFitter.FitMode.Overlay);
            SetObjectReference(safeAreaFitter, "m_OverlayRoot", rootRect);

            var screen = root.GetComponent<MainMenuScreen>();
            var presenter = root.GetComponent<MainMenuPresenter>();
            screen.Configure(presenter);

            // Centred 9:16 layout column
            GameObject colGo = CreateRect("LayoutColumn", root.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1080f, 1920f));

            // 1. BRAND WORDMARK
            GameObject wordmarkGroup = CreateRect("Brand_WordmarkGroup", colGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -110f), new Vector2(440f, 100f));

            GameObject lineTextGo = CreateText("Brand_WordmarkNavy", wordmarkGroup.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(260f, 100f),
                "LINE", 96f, FontStyles.Bold, TextAlignmentOptions.Left);
            AddApplier(lineTextGo, colorToken: UiThemeApplier.ColorToken.BrandNavy);

            GameObject gold98TextGo = CreateText("Brand_WordmarkGold", wordmarkGroup.transform,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(160f, 100f),
                "98", 96f, FontStyles.Bold, TextAlignmentOptions.Right);
            AddApplier(gold98TextGo, colorToken: UiThemeApplier.ColorToken.Crown);

            // Tagline & flanking rules
            GameObject taglineGo = CreateText("Brand_Tagline", colGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -220f), new Vector2(360f, 36f),
                "COLOR LINES", 26f, FontStyles.Bold, TextAlignmentOptions.Center);
            taglineGo.GetComponent<TextMeshProUGUI>().characterSpacing = 8f;
            AddApplier(taglineGo, colorToken: UiThemeApplier.ColorToken.InkLabel);

            GameObject ruleLeft = CreateImage("Brand_RuleLeft", colGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, 0.5f),
                new Vector2(-190f, -220f), new Vector2(80f, 2f), null);
            AddApplier(ruleLeft, colorToken: UiThemeApplier.ColorToken.DividerHairline);

            GameObject ruleRight = CreateImage("Brand_RuleRight", colGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f),
                new Vector2(190f, -220f), new Vector2(80f, 2f), null);
            AddApplier(ruleRight, colorToken: UiThemeApplier.ColorToken.DividerHairline);

            // 2. CARD BEST
            Sprite cardSprite = GetSprite(sprites, "sp_ui_card_cream");
            GameObject cardBestGo = CreateImage("Card_Best", colGo.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(157f, -287f), new Vector2(336f, 160f), cardSprite, Image.Type.Sliced);
            AddApplier(cardBestGo,
                colorToken: UiThemeApplier.ColorToken.PanelCard,
                spriteToken: UiThemeApplier.SpriteToken.CardBackground,
                materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialCard);
            var bestCardComponent = cardBestGo.AddComponent<UiValueCard>();

            Sprite crownSprite = GetSprite(sprites, "sp_icon_crown");
            GameObject crownGo = CreateImage("Crown", cardBestGo.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(60f, 0f), new Vector2(56f, 48f), crownSprite);
            AddApplier(crownGo, colorToken: UiThemeApplier.ColorToken.Crown, spriteToken: UiThemeApplier.SpriteToken.IconCrown);

            GameObject bestLabelGo = CreateText("Label", cardBestGo.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(105f, 22f), new Vector2(190f, 24f),
                "BEST", 20f, FontStyles.Bold, TextAlignmentOptions.Left);
            AddApplier(bestLabelGo, colorToken: UiThemeApplier.ColorToken.InkLabel);

            GameObject bestValGo = CreateText("Value", cardBestGo.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(105f, -18f), new Vector2(210f, 48f),
                "0", 38f, FontStyles.Bold, TextAlignmentOptions.Left);
            var bestRolling = bestValGo.AddComponent<RollingNumber>();
            AddApplier(bestValGo, colorToken: UiThemeApplier.ColorToken.InkValue);

            // 3. CARD STREAK
            GameObject cardStreakGo = CreateImage("Card_Streak", colGo.transform,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(587f, -287f), new Vector2(336f, 160f), cardSprite, Image.Type.Sliced);
            AddApplier(cardStreakGo,
                colorToken: UiThemeApplier.ColorToken.PanelCard,
                spriteToken: UiThemeApplier.SpriteToken.CardBackground,
                materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialCard);

            Sprite flameSprite = GetSprite(sprites, "sp_icon_flame");
            GameObject flameGo = CreateImage("Flame", cardStreakGo.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(52f, 0f), new Vector2(48f, 50f), flameSprite);
            AddApplier(flameGo, colorToken: UiThemeApplier.ColorToken.StreakDotOn, spriteToken: UiThemeApplier.SpriteToken.IconFlame);

            GameObject streakLabelGo = CreateText("Label", cardStreakGo.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(95f, 22f), new Vector2(220f, 24f),
                "0 DAY STREAK", 20f, FontStyles.Bold, TextAlignmentOptions.Left);
            AddApplier(streakLabelGo, colorToken: UiThemeApplier.ColorToken.InkValue);

            GameObject dotsParentGo = CreateRect("Dots", cardStreakGo.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(95f, -18f), new Vector2(220f, 24f));
            var dotsLayout = dotsParentGo.AddComponent<HorizontalLayoutGroup>();
            dotsLayout.spacing = 6f;
            dotsLayout.childAlignment = TextAnchor.MiddleLeft;
            dotsLayout.childForceExpandWidth = false;
            dotsLayout.childForceExpandHeight = false;
            dotsLayout.childControlWidth = false;
            dotsLayout.childControlHeight = false;

            var streakDotsView = dotsParentGo.AddComponent<StreakDotsView>();
            Sprite knobSprite = GetSprite(sprites, "sp_toggle_knob");
            Image[] dotImages = new Image[7];
            for (int i = 0; i < 7; i++)
            {
                GameObject dot = CreateImage($"Dot_{i}", dotsParentGo.transform,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(20f, 20f), knobSprite);
                AddApplier(dot, colorToken: UiThemeApplier.ColorToken.StreakDotOff);
                dotImages[i] = dot.GetComponent<Image>();
            }
            streakDotsView.Configure(dotImages);

            // Wire Presenter
            SetObjectReference(presenter, "m_BestCard", bestCardComponent);
            SetObjectReference(presenter, "m_StreakText", streakLabelGo.GetComponent<TextMeshProUGUI>());
            SetObjectReference(presenter, "m_StreakDotsView", streakDotsView);
            SetObjectReference(bestCardComponent, "m_Label", bestLabelGo.GetComponent<TextMeshProUGUI>());
            SetObjectReference(bestCardComponent, "m_Value", bestRolling);
            SetObjectReference(bestCardComponent, "m_Adornment", crownGo.GetComponent<RectTransform>());

            // 4. BUTTON PLAY (y = -1139)
            Sprite capsuleGreen = GetSprite(sprites, "sp_btn_capsule_green");
            GameObject btnPlay = CreateButton("Button_Play", colGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -1139f), new Vector2(733f, 206f),
                capsuleGreen, UiNavigationButton.UiDestination.Game);
            AddApplier(btnPlay,
                colorToken: UiThemeApplier.ColorToken.SurfacePrimary,
                spriteToken: UiThemeApplier.SpriteToken.ButtonCapsulePrimary,
                materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialButton);

            Sprite iconPlaySprite = GetSprite(sprites, "sp_icon_play");
            GameObject playContent = CreateRect("Content", btnPlay.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400f, 80f));
            GameObject playIcon = CreateImage("Icon_Play", playContent.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-85f, 0f), new Vector2(62f, 62f), iconPlaySprite);
            AddApplier(playIcon, colorToken: UiThemeApplier.ColorToken.InkButtonPrimary, spriteToken: UiThemeApplier.SpriteToken.IconPlay);

            GameObject playLabel = CreateText("Label", playContent.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(55f, 0f), new Vector2(240f, 70f),
                "PLAY", 54f, FontStyles.Bold, TextAlignmentOptions.Left);
            AddApplier(playLabel, colorToken: UiThemeApplier.ColorToken.InkButtonPrimary);

            // 5. BUTTON DAILY (y = -1377)
            Sprite pillDailySprite = GetSprite(sprites, "sp_btn_capsule_cream");
            GameObject btnDaily = CreateButton("Button_Daily", colGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -1377f), new Vector2(723f, 113f),
                pillDailySprite, UiNavigationButton.UiDestination.Daily);
            AddApplier(btnDaily,
                colorToken: UiThemeApplier.ColorToken.SurfaceSecondary,
                spriteToken: UiThemeApplier.SpriteToken.ButtonCapsule,
                materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialButton);

            Sprite iconCalSprite = GetSprite(sprites, "sp_icon_calendar");
            GameObject dailyIcon = CreateImage("Icon_Calendar", btnDaily.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(70f, 0f), new Vector2(48f, 48f), iconCalSprite);
            AddApplier(dailyIcon, colorToken: UiThemeApplier.ColorToken.InkValue, spriteToken: UiThemeApplier.SpriteToken.IconCalendar);

            GameObject dailyLabel = CreateText("Label", btnDaily.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(24f, 0f), new Vector2(480f, 50f),
                "DAILY CHALLENGE", 28f, FontStyles.Bold, TextAlignmentOptions.Center);
            AddApplier(dailyLabel, colorToken: UiThemeApplier.ColorToken.InkValue);

            // 6. BUTTON ZEN (y = -1504)
            Sprite pillZenSprite = GetSprite(sprites, "sp_btn_capsule_zen") ?? pillDailySprite;
            GameObject btnZen = CreateButton("Button_Zen", colGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -1504f), new Vector2(701f, 120f),
                pillZenSprite, UiNavigationButton.UiDestination.Zen);
            AddApplier(btnZen,
                colorToken: UiThemeApplier.ColorToken.SurfaceTertiaryZen,
                spriteToken: UiThemeApplier.SpriteToken.ButtonCapsule,
                materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialButton);

            Sprite iconLotusSprite = GetSprite(sprites, "sp_icon_lotus");
            GameObject zenIcon = CreateImage("Icon_Lotus", btnZen.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(70f, 0f), new Vector2(48f, 48f), iconLotusSprite);
            AddApplier(zenIcon, colorToken: UiThemeApplier.ColorToken.InkValue, spriteToken: UiThemeApplier.SpriteToken.IconLotus);

            GameObject zenLabel = CreateText("Label", btnZen.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(24f, 0f), new Vector2(480f, 50f),
                "ZEN MODE", 28f, FontStyles.Bold, TextAlignmentOptions.Center);
            AddApplier(zenLabel, colorToken: UiThemeApplier.ColorToken.InkValue);

            // 7. FOOTER BUTTONS (STATISTICS & SETTINGS at y = -1710)
            Sprite circleSprite = GetSprite(sprites, "sp_btn_circle_cream");

            // Statistics
            GameObject btnStats = CreateButton("Button_Statistics", colGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(-140f, -1710f), new Vector2(110f, 110f),
                circleSprite, UiNavigationButton.UiDestination.Statistics, Image.Type.Simple);
            AddApplier(btnStats,
                colorToken: UiThemeApplier.ColorToken.PanelCard,
                spriteToken: UiThemeApplier.SpriteToken.ButtonCircle,
                materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialButton);

            Sprite iconChartSprite = GetSprite(sprites, "sp_icon_chart");
            GameObject chartIcon = CreateImage("Icon_Chart", btnStats.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(52f, 52f), iconChartSprite);
            AddApplier(chartIcon, colorToken: UiThemeApplier.ColorToken.InkValue, spriteToken: UiThemeApplier.SpriteToken.IconChart);

            GameObject statsLabel = CreateText("Label", btnStats.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(180f, 26f),
                "STATISTICS", 18f, FontStyles.Bold, TextAlignmentOptions.Center);
            AddApplier(statsLabel, colorToken: UiThemeApplier.ColorToken.InkLabel);

            // Settings
            GameObject btnSettings = CreateButton("Button_Settings", colGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(140f, -1710f), new Vector2(110f, 110f),
                circleSprite, UiNavigationButton.UiDestination.Settings, Image.Type.Simple);
            AddApplier(btnSettings,
                colorToken: UiThemeApplier.ColorToken.PanelCard,
                spriteToken: UiThemeApplier.SpriteToken.ButtonCircle,
                materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialButton);

            Sprite iconGearSprite = GetSprite(sprites, "sp_icon_gear");
            GameObject gearIcon = CreateImage("Icon_Gear", btnSettings.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(52f, 52f), iconGearSprite);
            AddApplier(gearIcon, colorToken: UiThemeApplier.ColorToken.InkValue, spriteToken: UiThemeApplier.SpriteToken.IconGear);

            GameObject settingsLabel = CreateText("Label", btnSettings.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(180f, 26f),
                "SETTINGS", 18f, FontStyles.Bold, TextAlignmentOptions.Center);
            AddApplier(settingsLabel, colorToken: UiThemeApplier.ColorToken.InkLabel);

            // Divider between buttons
            Sprite dividerSprite = GetSprite(sprites, "sp_divider_hairline");
            GameObject footerDivider = CreateImage("Footer_Divider", colGo.transform,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -1710f), new Vector2(2f, 80f), dividerSprite);
            AddApplier(footerDivider, colorToken: UiThemeApplier.ColorToken.DividerHairline);

            // Hydrate initial theme visuals on the authored prefab
            var crystalTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>("Assets/_Project/Content/Themes/UI/UiTheme_Crystal.asset");
            if (crystalTheme != null)
            {
                var appliers = root.GetComponentsInChildren<UiThemeApplier>(true);
                for (int i = 0; i < appliers.Length; i++)
                {
                    appliers[i].Apply(crystalTheme);
                }
            }

            ConfigureEntryMotionAndClickSfx(root);

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(root, s_MainMenuPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"[MenuSceneAuthoring] Saved {s_MainMenuPrefabPath}");
        }

        public static void BuildMainMenuScene(MenuShowcaseLayoutSO layout)
        {
            Scene scene = EditorSceneManager.OpenScene(s_MainMenuScenePath, OpenSceneMode.Single);

            // 1. DIRECTIONAL LIGHTS
            EnsureDirectionalLights();

            // 2. 3D SHOWCASE RIG
            EnsureShowcaseRig(layout);

            // 3. EVENT SYSTEM
            EnsureEventSystem();

            // 4. UI ROOT
            EnsureUiRoot();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MenuSceneAuthoring] Saved {s_MainMenuScenePath}");
        }

        private static void EnsureDirectionalLights()
        {
            var keyLight = GameObject.Find("Directional Light (Key)");
            if (keyLight == null)
            {
                keyLight = new GameObject("Directional Light (Key)");
            }
            var keyComp = keyLight.GetComponent<Light>();
            if (keyComp == null)
            {
                keyComp = keyLight.AddComponent<Light>();
            }
            keyComp.type = LightType.Directional;
            keyComp.color = Color.white;
            keyComp.intensity = 1.0f;
            keyLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var fillLight = GameObject.Find("Directional Light (Fill)");
            if (fillLight == null)
            {
                fillLight = new GameObject("Directional Light (Fill)");
            }
            var fillComp = fillLight.GetComponent<Light>();
            if (fillComp == null)
            {
                fillComp = fillLight.AddComponent<Light>();
            }
            fillComp.type = LightType.Directional;
            fillComp.color = new Color(0.85f, 0.91f, 1f);
            fillComp.intensity = 0.45f;
            fillLight.transform.rotation = Quaternion.Euler(30f, 150f, 0f);
        }

        private static void EnsureShowcaseRig(MenuShowcaseLayoutSO layout)
        {
            var rigGo = GameObject.Find("[MenuShowcaseRig]");
            if (rigGo == null)
            {
                rigGo = new GameObject("[MenuShowcaseRig]");
            }

            var rig = rigGo.GetComponent<MenuShowcaseRig>();
            if (rig == null)
            {
                rig = rigGo.AddComponent<MenuShowcaseRig>();
            }

            // Load dependencies
            var boardTheme = AssetDatabase.LoadAssetAtPath<BoardThemeSO>(s_BoardThemeCrystalPath);
            var ballTheme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(s_BallThemeCrystalPath);
            var cameraProfile = AssetDatabase.LoadAssetAtPath<CameraProfileSO>(s_CameraProfilePath);
            var shadowMesh = AssetDatabase.LoadAssetAtPath<Mesh>(s_ShadowMeshPath);
            var glowMat = AssetDatabase.LoadAssetAtPath<Material>(s_GlowMatPath);
            var backdropMesh = AssetDatabase.LoadAssetAtPath<Mesh>(s_BackdropMeshPath);
            var backdropMat = AssetDatabase.LoadAssetAtPath<Material>(s_BackdropMatPath);

            // Ensure backdrop quad
            var backdropGo = rigGo.transform.Find("Background_Backdrop");
            MeshRenderer backdropRenderer = null;
            if (backdropGo == null)
            {
                var bg = new GameObject("Background_Backdrop");
                bg.transform.SetParent(rigGo.transform, false);
                var mf = bg.AddComponent<MeshFilter>();
                mf.sharedMesh = backdropMesh;
                backdropRenderer = bg.AddComponent<MeshRenderer>();
                backdropRenderer.sharedMaterial = backdropMat;
            }
            else
            {
                backdropRenderer = backdropGo.GetComponent<MeshRenderer>();
            }

            // Ensure BoardView
            var boardGo = rigGo.transform.Find("BoardView");
            BoardView boardView = null;
            if (boardGo == null)
            {
                var bg = new GameObject("BoardView");
                bg.transform.SetParent(rigGo.transform, false);
                boardView = bg.AddComponent<BoardView>();
            }
            else
            {
                boardView = boardGo.GetComponent<BoardView>();
                if (boardView == null)
                {
                    boardView = boardGo.gameObject.AddComponent<BoardView>();
                }
            }

            // Ensure BallsRoot under BoardView
            var ballsRoot = boardView.transform.Find("Balls_Root");
            if (ballsRoot == null)
            {
                var bRoot = new GameObject("Balls_Root");
                bRoot.transform.SetParent(boardView.transform, false);
                ballsRoot = bRoot.transform;
            }

            // Ensure CameraRig
            var camRigGo = rigGo.transform.Find("CameraRig");
            CameraRig cameraRig = null;
            if (camRigGo == null)
            {
                var cg = new GameObject("CameraRig");
                cg.transform.SetParent(rigGo.transform, false);
                cameraRig = cg.AddComponent<CameraRig>();
            }
            else
            {
                cameraRig = camRigGo.GetComponent<CameraRig>();
                if (cameraRig == null)
                {
                    cameraRig = camRigGo.gameObject.AddComponent<CameraRig>();
                }
            }

            // Set serialized properties on rig
            SetObjectReference(rig, "m_CameraRig", cameraRig);
            SetObjectReference(rig, "m_BoardView", boardView);
            SetObjectReference(rig, "m_BallsRoot", ballsRoot);
            SetObjectReference(rig, "m_Backdrop", backdropRenderer);
            SetObjectReference(rig, "m_BoardTheme", boardTheme);
            SetObjectReference(rig, "m_BallTheme", ballTheme);
            SetObjectReference(rig, "m_CameraProfile", cameraProfile);
            SetObjectReference(rig, "m_Layout", layout);
            SetObjectReference(rig, "m_ShadowMesh", shadowMesh);
            SetObjectReference(rig, "m_GlowMaterial", glowMat);

            Rect viewport = new Rect(0.225f, 0.411f, 0.596f, 0.338f);
            rig.Initialize(boardTheme, ballTheme, cameraProfile, backdropRenderer, viewport, layout);
        }

        private static void EnsureEventSystem()
        {
            var es = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            }
            else if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static void EnsureUiRoot()
        {
            var uiRoot = GameObject.Find("UI_Root");
            if (uiRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(uiRoot);
            }

            // Instantiate UI_Root from prefab
            GameObject rootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_UiRootPrefabPath);
            if (rootPrefab != null)
            {
                uiRoot = (GameObject)PrefabUtility.InstantiatePrefab(rootPrefab);
                uiRoot.name = "UI_Root";
            }
            else
            {
                uiRoot = new GameObject("UI_Root", typeof(RectTransform), typeof(UiShell));
            }

            ConfigureMenuSafeAreaFitter(uiRoot);

            var shell = uiRoot.GetComponent<UiShell>();
            var crystalTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>("Assets/_Project/Content/Themes/UI/UiTheme_Crystal.asset");
            if (shell != null && crystalTheme != null)
            {
                SetObjectReference(shell, "m_UiTheme", crystalTheme);
            }

            Canvas staticCanvas = FindNamedComponent<Canvas>(uiRoot, "Canvas_StaticHUD");
            if (staticCanvas != null)
            {
                staticCanvas.gameObject.SetActive(false); // In MainMenu, gameplay HUD is hidden
            }

            Canvas dynamicCanvas = FindNamedComponent<Canvas>(uiRoot, "Canvas_DynamicHUD");
            if (dynamicCanvas != null)
            {
                dynamicCanvas.gameObject.SetActive(true);
                // Remove existing HUD or placeholder screens
                for (int i = dynamicCanvas.transform.childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(dynamicCanvas.transform.GetChild(i).gameObject);
                }

                // Instantiate Screen_MainMenu prefab under dynamicCanvas
                GameObject menuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_MainMenuPrefabPath);
                if (menuPrefab != null)
                {
                    GameObject screenInstance = (GameObject)PrefabUtility.InstantiatePrefab(menuPrefab, dynamicCanvas.transform);
                    screenInstance.name = "Screen_MainMenu";
                    RectTransform rect = screenInstance.GetComponent<RectTransform>();
                    Stretch(rect);
                }
            }
        }

        // --- Helper authoring methods ---

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static GameObject CreateRect(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return go;
        }

        private static GameObject CreateImage(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta, Sprite sprite,
            Image.Type imageType = Image.Type.Simple)
        {
            var go = CreateRect(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = imageType;
            img.color = Color.white;
            img.raycastTarget = false;
            return go;
        }

        private static GameObject CreateText(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta,
            string content, float fontSize, FontStyles style, TextAlignmentOptions align)
        {
            var go = CreateRect(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return go;
        }

        private static GameObject CreateButton(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPosition, Vector2 sizeDelta,
            Sprite sprite, UiNavigationButton.UiDestination destination,
            Image.Type imageType = Image.Type.Sliced)
        {
            var go = CreateImage(name, parent, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta, sprite, imageType);
            var img = go.GetComponent<Image>();
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            go.AddComponent<UiActionButton>();
            var nav = go.AddComponent<UiNavigationButton>();
            SetSerializedInt(nav, "m_Destination", (int)destination);
            go.AddComponent<UiButtonFx>();

            return go;
        }

        private static UiThemeApplier AddApplier(GameObject go,
            UiThemeApplier.ColorToken? colorToken = null,
            UiThemeApplier.FontSizeToken fontToken = UiThemeApplier.FontSizeToken.None,
            UiThemeApplier.SpriteToken spriteToken = UiThemeApplier.SpriteToken.None,
            UiThemeApplier.MaterialToken materialToken = UiThemeApplier.MaterialToken.None)
        {
            var applier = go.AddComponent<UiThemeApplier>();
            var graphic = go.GetComponent<Graphic>();
            var tmp = go.GetComponent<TMP_Text>();
            var img = go.GetComponent<Image>();

            if (colorToken.HasValue)
            {
                var colorTargets = graphic != null ? new Graphic[] { graphic } : Array.Empty<Graphic>();
                var fontTargets = tmp != null ? new TMP_Text[] { tmp } : Array.Empty<TMP_Text>();
                applier.Configure(colorToken.Value, colorTargets, fontToken, fontTargets);
            }
            else if (fontToken != UiThemeApplier.FontSizeToken.None && tmp != null)
            {
                applier.Configure(UiThemeApplier.ColorToken.PanelCard, Array.Empty<Graphic>(), fontToken, new TMP_Text[] { tmp });
            }

            if (spriteToken != UiThemeApplier.SpriteToken.None && img != null)
            {
                applier.ConfigureSprite(spriteToken, new Image[] { img });
            }

            if (materialToken != UiThemeApplier.MaterialToken.None && graphic != null)
            {
                applier.ConfigureMaterial(materialToken, new Graphic[] { graphic });
            }

            return applier;
        }

        private static T FindNamedComponent<T>(GameObject root, string name) where T : Component
        {
            var components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i].gameObject.name == name)
                {
                    return components[i];
                }
            }
            return null;
        }

        private static void SetObjectReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedInt(UnityEngine.Object target, string fieldName, int value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.intValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}

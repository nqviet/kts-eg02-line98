#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Line98.Data;
using Line98.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Editor
{
    public static class Phase3PopupAuthoring
    {
        private const string PopupFolder = "Assets/_Project/Content/Prefabs/UI/Popups";
        private const string DailyPath = PopupFolder + "/Popup_DailyChallenge.prefab";
        private const string ProgressPath = PopupFolder + "/Popup_Statistics.prefab";
        private const string UiRootPath = "Assets/_Project/Content/Prefabs/UI/Shell/UI_Root.prefab";
        private const string FontPath = "Assets/_Project/Content/Fonts/RobotoBold_Menu.asset";
        private const string CrystalThemePath = "Assets/_Project/Content/Themes/UI/UiTheme_Crystal.asset";
        private const string CrystalSpriteSheetPath = "Assets/Art/Sprites/UI/Crystal/sprites_2__crystal.png";
        private const string CrystalBackdropPath = "Assets/Art/Textures/T_Background_CrystalStudio.png";

        private static TMP_FontAsset s_Font;
        private static UiThemeSO s_Theme;

        [MenuItem("Line98/Phase 3/Author Daily & Progress UI")]
        public static void Author()
        {
            s_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            s_Theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(CrystalThemePath);
            if (s_Font == null) throw new InvalidOperationException($"Missing TMP font: {FontPath}");
            if (s_Theme == null) throw new InvalidOperationException($"Missing UI theme: {CrystalThemePath}");

            BuildDailyPrefab();
            BuildProgressPrefab();
            InstallIntoUiRoot();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Phase3PopupAuthoring] Authored Daily Challenge and Your Progress at 940x1670.");
        }

        [MenuItem("Line98/Phase 3/Author Your Progress UI")]
        public static void AuthorProgress()
        {
            s_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            s_Theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(CrystalThemePath);
            if (s_Font == null) throw new InvalidOperationException($"Missing TMP font: {FontPath}");
            if (s_Theme == null) throw new InvalidOperationException($"Missing UI theme: {CrystalThemePath}");

            BuildProgressPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("[Phase3PopupAuthoring] Authored Your Progress at 940x1670.");
        }

        public static void CaptureProgressReferences()
        {
            string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Phase3UiCaptures");
            Directory.CreateDirectory(outputFolder);
            CaptureProgress(Path.Combine(outputFolder, "Progress_Crystal.png"), CrystalThemePath, false);
            CaptureProgress(Path.Combine(outputFolder, "ProgressAchievements_Crystal.png"), CrystalThemePath, true);
            CaptureProgress(Path.Combine(outputFolder, "Progress_Classic.png"), "Assets/_Project/Content/Themes/UI/UiTheme_Default.asset", false);
        }

        public static void CaptureReferences()
        {
            string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Phase3UiCaptures");
            Directory.CreateDirectory(outputFolder);
            CaptureDaily(Path.Combine(outputFolder, "Daily_Crystal.png"), CrystalThemePath, "Assets/_Project/Content/Definitions/BallTheme_Crystal.asset");
            CaptureProgress(Path.Combine(outputFolder, "Progress_Crystal.png"), CrystalThemePath, false);
            CaptureProgress(Path.Combine(outputFolder, "ProgressAchievements_Crystal.png"), CrystalThemePath, true);
            CaptureDaily(Path.Combine(outputFolder, "Daily_Classic.png"), "Assets/_Project/Content/Themes/UI/UiTheme_Default.asset", "Assets/_Project/Content/Definitions/BallTheme_Classic.asset");
            CaptureProgress(Path.Combine(outputFolder, "Progress_Classic.png"), "Assets/_Project/Content/Themes/UI/UiTheme_Default.asset", false);
            CaptureProgress(Path.Combine(outputFolder, "ProgressAchievements_Classic.png"), "Assets/_Project/Content/Themes/UI/UiTheme_Default.asset", true);
            Debug.Log($"[Phase3PopupAuthoring] Captured Phase 3 UI to {outputFolder}");
        }

        private static void CaptureDaily(string outputPath, string themePath, string ballThemePath)
        {
            UiThemeSO theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(themePath);
            BallThemeSO ballTheme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(ballThemePath);
            GameObject instance = CreateCaptureStage(DailyPath, out Camera camera, out RenderTexture target);
            try
            {
                DailyChallengePopup popup = instance.GetComponent<DailyChallengePopup>();
                var board = new Line98.Core.BallColor[81];
                board[4] = Line98.Core.BallColor.Red;
                board[19] = Line98.Core.BallColor.Orange;
                board[25] = Line98.Core.BallColor.Yellow;
                board[40] = Line98.Core.BallColor.Green;
                board[47] = Line98.Core.BallColor.Cyan;
                board[51] = Line98.Core.BallColor.Purple;
                board[67] = Line98.Core.BallColor.Blue;
                DateTime today = new DateTime(2026, 9, 12);
                var week = new DailyDayVisual[7];
                for (int i = 0; i < 7; i++)
                {
                    week[i] = new DailyDayVisual(
                        today.AddDays(i - 5),
                        i < 5 ? DailyDayVisualState.Completed : i == 5 ? DailyDayVisualState.TodayCompleted : DailyDayVisualState.Future,
                        i == 5);
                }
                popup.Populate(new DailyPopupPayload
                {
                    Date = today,
                    Week = week,
                    CurrentStreak = 5,
                    BestScore = 7420,
                    PlayedToday = false,
                    CompletedToday = false,
                    CanPlayToday = true,
                    Board = board
                });
                popup.ApplyTheme(theme, ballTheme);
                popup.Show();
                SaveRender(camera, target, outputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camera.gameObject.transform.root.gameObject);
            }
        }

        private static void CaptureProgress(string outputPath, string themePath, bool showAchievements)
        {
            UiThemeSO theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(themePath);
            GameObject instance = CreateCaptureStage(ProgressPath, out Camera camera, out RenderTexture target);
            try
            {
                StatisticsPopup popup = instance.GetComponent<StatisticsPopup>();
                popup.Populate(new ProgressPopupPayload
                {
                    BestScore = 12480,
                    GamesPlayed = 42,
                    TotalScore = 86350,
                    TotalLinesCleared = 317,
                    LongestLine = 9,
                    HighestCombo = 4,
                    CurrentStreak = 5,
                    AchievementsUnlocked = 6,
                    AchievementsTotal = 10,
                    AchievementRatio = 0.6f,
                    Achievements = new[]
                    {
                        new AchievementPopupRow("perfect_9", "PERFECT — CLEAR 9", null, true, 9, 9, 1f),
                        new AchievementPopupRow("score_10000", "SCORE 10,000", null, true, 10000, 10000, 1f),
                        new AchievementPopupRow("streak_30", "30 DAY STREAK", null, false, 5, 30, 5f / 30f),
                        new AchievementPopupRow("score_50000", "SCORE 50,000", null, false, 12480, 50000, 12480f / 50000f),
                        new AchievementPopupRow("moves_100", "100 MOVES", null, false, 42, 100, 0.42f),
                        new AchievementPopupRow("streak_7", "7 DAY STREAK", null, false, 5, 7, 5f / 7f)
                    }
                });
                popup.ApplyTheme(theme);
                if (showAchievements) popup.ShowAchievements();
                else popup.ShowStatistics();
                popup.Show();
                SaveRender(camera, target, outputPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camera.gameObject.transform.root.gameObject);
            }
        }

        private static GameObject CreateCaptureStage(string prefabPath, out Camera camera, out RenderTexture target)
        {
            GameObject stage = new GameObject("Phase3CaptureStage");
            GameObject cameraObject = new GameObject("Camera", typeof(Camera));
            cameraObject.transform.SetParent(stage.transform, false);
            camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.88f, 0.94f, 0.91f, 1f);
            camera.orthographic = true;

            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(stage.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(940f, 1670f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasObject.transform);
            instance.SetActive(true);
            target = new RenderTexture(940, 1670, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            return instance;
        }

        private static void SaveRender(Camera camera, RenderTexture target, string outputPath)
        {
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            RenderTexture.active = previous;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }

        private static void BuildDailyPrefab()
        {
            GameObject root = CreatePopupRoot("Popup_DailyChallenge", out RectTransform content, out DailyChallengePopup popup);
            RawImage backdrop = CreateBackdrop(root.transform);
            RectTransform washRect = CreateRect("BackdropWash", root.transform, Vector2.zero, Vector2.zero);
            washRect.anchorMin = Vector2.zero;
            washRect.anchorMax = Vector2.one;
            washRect.offsetMin = Vector2.zero;
            washRect.offsetMax = Vector2.zero;
            washRect.SetSiblingIndex(1);
            Image backdropWash = washRect.gameObject.AddComponent<Image>();
            backdropWash.raycastTarget = false;
            var cardSurfaces = new List<Image>();

            Button close = CreateIconButton(content, "Button_Back", "<", new Vector2(-378f, 735f));
            ReplaceGlyphWithSprite(close, "sp_icon_back_arrow", new Vector2(30f, 45f));
            TMP_Text title = CreateText(content, "Title", "DAILY CHALLENGE", new Vector2(0f, 712f), new Vector2(760f, 86f), 58f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            title.characterSpacing = -4f;
            cardSurfaces.Add(close.GetComponent<Image>());
            CreateGlyph(content, "Leaf", UiGlyphGraphic.GlyphShape.Leaf, new Vector2(2f, 770f), new Vector2(26f, 44f),
                new Color(0.27f, 0.6f, 0.25f, 1f)).rectTransform.localRotation = Quaternion.Euler(0f, 0f, -40f);
            CreateRule(content, "TitleRule_Left", new Vector2(-104f, 762f), new Vector2(140f, 2f));
            CreateRule(content, "TitleRule_Right", new Vector2(104f, 762f), new Vector2(140f, 2f));
            Button info = CreateIconButton(content, "Button_Info", "i", new Vector2(378f, 735f));
            info.GetComponentInChildren<TMP_Text>().fontSize = 56f;
            cardSurfaces.Add(info.GetComponent<Image>());

            Image dateCard = CreatePanel(content, "DateCard", new Vector2(0f, 607f), new Vector2(482f, 106f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            CreateThemedIcon(dateCard.transform, "CalendarIcon", new Vector2(-160f, -2f), new Vector2(62f, 68f), UiThemeApplier.SpriteToken.IconCalendar);
            CreateText(dateCard.transform, "TodayLabel", "TODAY", new Vector2(62f, 24f), new Vector2(310f, 32f), 26f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            TMP_Text dateText = CreateText(dateCard.transform, "DateValue", "SEPTEMBER 12", new Vector2(62f, -16f), new Vector2(310f, 50f), 40f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            dateText.characterSpacing = -2f;
            cardSurfaces.Add(dateCard);

            Image boardPanel = CreatePanel(content, "BoardPreview", new Vector2(0f, 283f), new Vector2(560f, 510f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            boardPanel.pixelsPerUnitMultiplier = 0.8f;
            cardSurfaces.Add(boardPanel);
            RectTransform boardGrid = CreateRect("Grid", boardPanel.transform, Vector2.zero, new Vector2(510f, 470f));
            var grid = boardGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(52f, 47.5f);
            grid.spacing = new Vector2(5.25f, 5.25f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 9;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(0, 0, 0, 0);

            var boardBalls = new List<Image>(81);
            var boardCells = new List<Image>(81);
            for (int i = 0; i < 81; i++)
            {
                GameObject cell = new GameObject($"Cell_{i:00}", typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(boardGrid, false);
                Image cellImage = cell.GetComponent<Image>();
                cellImage.sprite = s_Theme.CardBackgroundSprite;
                cellImage.type = Image.Type.Sliced;
                cellImage.pixelsPerUnitMultiplier = 3.6f;
                boardCells.Add(cellImage);
                cellImage.color = s_Theme.PanelCell;
                cellImage.raycastTarget = false;
                AttachThemeSurface(cell, UiThemeApplier.ColorToken.PanelCell, cellImage, UiThemeApplier.SpriteToken.CardBackground);
                RectTransform ballRect = CreateRect("Ball", cell.transform, Vector2.zero, new Vector2(70f, 70f));
                Image ball = ballRect.gameObject.AddComponent<Image>();
                ball.raycastTarget = false;
                ball.preserveAspect = true;
                ball.gameObject.SetActive(false);
                boardBalls.Add(ball);
            }
            Color sparkleGold = new Color(0.96f, 0.76f, 0.24f, 1f);
            CreateGlyph(boardPanel.transform, "Sparkle_Large", UiGlyphGraphic.GlyphShape.Sparkle, new Vector2(42f, -42f), new Vector2(44f, 50f), sparkleGold);
            CreateGlyph(boardPanel.transform, "Sparkle_Top", UiGlyphGraphic.GlyphShape.Sparkle, new Vector2(70f, -12f), new Vector2(28f, 32f), sparkleGold);
            CreateGlyph(boardPanel.transform, "Sparkle_Small", UiGlyphGraphic.GlyphShape.Sparkle, new Vector2(66f, -72f), new Vector2(16f, 18f), sparkleGold);

            Image mission = CreatePanel(content, "MissionCard", new Vector2(0f, -52f), new Vector2(720f, 140f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            cardSurfaces.Add(mission);
            RectTransform link = CreateRect("BallLink", mission.transform, new Vector2(-238f, 3f), new Vector2(96f, 8f));
            Image linkImage = link.gameObject.AddComponent<Image>();
            linkImage.color = new Color(0.25f, 0.62f, 0.45f, 1f);
            linkImage.raycastTarget = false;
            Image[] missionBalls = CreateBallTrio(mission.transform, new Vector2(-238f, 3f), 46f, 49f);
            Image missionPill = CreateSpriteImage(mission.transform, "MissionPill", "sp_ui_card_gold", new Vector2(86f, 18f), new Vector2(460f, 66f));
            missionPill.type = Image.Type.Sliced;
            missionPill.pixelsPerUnitMultiplier = 0.95f;
            missionPill.preserveAspect = false;
            missionPill.color = Color.white;
            TMP_Text missionText = CreateText(missionPill.transform, "Mission", "MATCH 5 OR MORE", new Vector2(0f, 1f), new Vector2(430f, 52f), 40f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            DetachTheme(missionText.gameObject);
            missionText.color = new Color(0.48f, 0.33f, 0.07f, 1f);
            missionText.characterSpacing = -1f;
            CreateText(mission.transform, "Rule", "SAME BOARD. ONE DAILY RUN.", new Vector2(86f, -38f), new Vector2(480f, 32f), 23f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandBlue).characterSpacing = 6f;

            Image weekPanel = CreatePanel(content, "WeekPanel", new Vector2(0f, -235f), new Vector2(710f, 192f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            cardSurfaces.Add(weekPanel);
            TMP_Text[] weekdayTexts = new TMP_Text[7];
            Image[] dayBadges = new Image[7];
            UiCheckGraphic[] dayChecks = new UiCheckGraphic[7];
            UiGlyphGraphic[] dayGlows = new UiGlyphGraphic[7];
            for (int i = 0; i < 7; i++)
            {
                float x = -279f + i * 93f;
                weekdayTexts[i] = CreateText(weekPanel.transform, $"Weekday_{i}", "M", new Vector2(x, 58f), new Vector2(60f, 30f), 24f,
                    TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
                RectTransform glowRect = CreateRect($"DayGlow_{i}", weekPanel.transform, new Vector2(x, 2f), new Vector2(104f, 104f));
                dayGlows[i] = glowRect.gameObject.AddComponent<UiGlyphGraphic>();
                dayGlows[i].Shape = UiGlyphGraphic.GlyphShape.Halo;
                dayGlows[i].color = new Color(1f, 0.84f, 0.35f, 0.9f);
                dayGlows[i].raycastTarget = false;
                dayGlows[i].gameObject.SetActive(false);
                RectTransform badgeRect = CreateRect($"DayBadge_{i}", weekPanel.transform, new Vector2(x, 2f), new Vector2(62f, 62f));
                dayBadges[i] = badgeRect.gameObject.AddComponent<Image>();
                dayBadges[i].sprite = s_Theme.ButtonCircleSprite;
                dayBadges[i].color = s_Theme.StreakDotOff;
                dayBadges[i].raycastTarget = false;
                RectTransform checkRect = CreateRect("Check", badgeRect, Vector2.zero, new Vector2(32f, 32f));
                dayChecks[i] = checkRect.gameObject.AddComponent<UiCheckGraphic>();
                dayChecks[i].Thickness = 7f;
                dayChecks[i].raycastTarget = false;
            }
            TMP_Text streakText = CreateText(weekPanel.transform, "StreakValue", "5 DAY STREAK", new Vector2(0f, -66f), new Vector2(260f, 36f), 26f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            streakText.characterSpacing = 6f;
            CreateRule(weekPanel.transform, "StreakRule_Left", new Vector2(-220f, -66f), new Vector2(170f, 2f));
            CreateRule(weekPanel.transform, "StreakRule_Right", new Vector2(220f, -66f), new Vector2(170f, 2f));

            Image bestCard = CreatePanel(content, "BestCard", new Vector2(-183f, -410f), new Vector2(345f, 116f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            Image crown = CreateThemedIcon(bestCard.transform, "Crown", new Vector2(-100f, -2f), new Vector2(96f, 90f), UiThemeApplier.SpriteToken.IconCrown);
            DetachThemeColor(crown);
            cardSurfaces.Add(bestCard);
            CreateText(bestCard.transform, "BestLabel", "BEST", new Vector2(58f, 25f), new Vector2(180f, 30f), 24f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandBlue);
            TMP_Text bestText = CreateText(bestCard.transform, "BestValue", "7,420", new Vector2(58f, -16f), new Vector2(180f, 56f), 46f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            bestText.characterSpacing = -2f;

            Image todayCard = CreatePanel(content, "TodayCard", new Vector2(183f, -410f), new Vector2(345f, 116f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            cardSurfaces.Add(todayCard);
            CreateThemedIcon(todayCard.transform, "Calendar", new Vector2(-94f, -2f), new Vector2(62f, 68f), UiThemeApplier.SpriteToken.IconCalendar);
            CreateText(todayCard.transform, "TodayLabel", "TODAY", new Vector2(75f, 29f), new Vector2(180f, 30f), 24f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            TMP_Text todayValue = CreateText(todayCard.transform, "TodayValue", "—", new Vector2(75f, -2f), new Vector2(180f, 40f), 36f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            TMP_Text todayStatus = CreateText(todayCard.transform, "TodayStatus", "NOT PLAYED", new Vector2(75f, -34f), new Vector2(180f, 28f), 20f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.InkLabel);

            Button play = CreateButton(content, "Button_PlayDaily", "PLAY DAILY", new Vector2(0f, -576f), new Vector2(676f, 152f), true, 64f);
            TMP_Text playLabel = play.GetComponentInChildren<TMP_Text>();
            playLabel.rectTransform.anchoredPosition = new Vector2(58f, 2f);
            playLabel.characterSpacing = -3f;
            Image playIcon = CreateThemedIcon(play.transform, "PlayIcon", new Vector2(-196f, 0f), new Vector2(64f, 76f), UiThemeApplier.SpriteToken.IconPlay);
            playIcon.GetComponent<UiThemeApplier>().Configure(UiThemeApplier.ColorToken.InkButtonPrimary, new Graphic[] { playIcon },
                UiThemeApplier.FontSizeToken.None, Array.Empty<TMP_Text>());

            Button howTo = CreateLinkButton(content, "Button_HowToPlay", "HOW TO PLAY", new Vector2(0f, -712f), new Vector2(260f, 60f), 30f);
            howTo.GetComponentInChildren<TMP_Text>().characterSpacing = 0f;

            ConfigurePopupBase(root, content, close, play);
            SerializedObject so = new SerializedObject(popup);
            Set(so, "m_DateText", dateText);
            Set(so, "m_StreakText", streakText);
            Set(so, "m_BestText", bestText);
            Set(so, "m_TodayValueText", todayValue);
            Set(so, "m_TodayStatusText", todayStatus);
            SetArray(so, "m_WeekdayTexts", weekdayTexts);
            SetArray(so, "m_DayBadges", dayBadges);
            SetArray(so, "m_DayChecks", dayChecks);
            SetArray(so, "m_DayGlows", dayGlows);
            SetArray(so, "m_BoardBalls", boardBalls.ToArray());
            SetArray(so, "m_MissionBalls", missionBalls);
            Set(so, "m_PlayButton", play);
            Set(so, "m_HowToPlayButton", howTo);
            Set(so, "m_Backdrop", backdrop);
            Set(so, "m_BackdropWash", backdropWash);
            Set(so, "m_CrystalBackdropTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(CrystalBackdropPath));
            Set(so, "m_HeaderTitle", title);
            SetArray(so, "m_CardSurfaces", cardSurfaces.ToArray());
            SetArray(so, "m_BoardCells", boardCells.ToArray());
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplyThemeNow(root);
            SavePrefab(root, DailyPath);
        }

        private static RawImage CreateBackdrop(Transform parent)
        {
            RectTransform rect = CreateRect("FullscreenBackdrop", parent, Vector2.zero, Vector2.zero);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();
            RawImage backdrop = rect.gameObject.AddComponent<RawImage>();
            backdrop.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CrystalBackdropPath);
            // Also the input shield, so taps never reach the screen underneath.
            backdrop.raycastTarget = true;
            return backdrop;
        }

        private static Sprite LoadCrystalSprite(string spriteName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(CrystalSpriteSheetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName) return sprite;
            }
            throw new InvalidOperationException($"Missing sprite {spriteName} in {CrystalSpriteSheetPath}");
        }

        private static Image CreateSpriteImage(Transform parent, string name, string spriteName, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = LoadCrystalSprite(spriteName);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void ReplaceGlyphWithSprite(Button button, string spriteName, Vector2 size)
        {
            TMP_Text glyph = button.GetComponentInChildren<TMP_Text>();
            if (glyph != null) UnityEngine.Object.DestroyImmediate(glyph.gameObject);
            Image icon = CreateSpriteImage(button.transform, "Icon", spriteName, new Vector2(-3f, 0f), size);
            icon.color = s_Theme.BrandNavy;
            AttachTheme(icon.gameObject, UiThemeApplier.ColorToken.BrandNavy, icon);
        }

        private static UiGlyphGraphic CreateGlyph(Transform parent, string name, UiGlyphGraphic.GlyphShape shape, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            UiGlyphGraphic glyph = rect.gameObject.AddComponent<UiGlyphGraphic>();
            glyph.Shape = shape;
            glyph.color = color;
            glyph.raycastTarget = false;
            return glyph;
        }

        private static void CreateRule(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = s_Theme.InkLabel;
            image.raycastTarget = false;
            AttachTheme(rect.gameObject, UiThemeApplier.ColorToken.InkLabel, image);
        }

        private static Button CreateLinkButton(Transform parent, string name, string label, Vector2 position, Vector2 size, float fontSize)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            Image hitArea = rect.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            Button button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            TMP_Text text = CreateText(rect, "Label", label, Vector2.zero, size, fontSize,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            text.fontStyle = FontStyles.Bold | FontStyles.Underline;
            text.characterSpacing = 2f;
            return button;
        }

        private static void DetachTheme(GameObject go)
        {
            UiThemeApplier applier = go.GetComponent<UiThemeApplier>();
            if (applier != null) UnityEngine.Object.DestroyImmediate(applier);
        }

        private static void DetachThemeColor(Image image)
        {
            UiThemeApplier applier = image.GetComponent<UiThemeApplier>();
            if (applier != null) applier.Configure(UiThemeApplier.ColorToken.BrandNavy, Array.Empty<Graphic>(), UiThemeApplier.FontSizeToken.None, Array.Empty<TMP_Text>());
            image.color = Color.white;
        }

        private static void BuildProgressPrefab()
        {
            GameObject root = CreatePopupRoot("Popup_Statistics", out RectTransform content, out StatisticsPopup popup);
            RawImage backdrop = CreateBackdrop(root.transform);
            RectTransform washRect = CreateRect("BackdropWash", root.transform, Vector2.zero, Vector2.zero);
            washRect.anchorMin = Vector2.zero;
            washRect.anchorMax = Vector2.one;
            washRect.offsetMin = Vector2.zero;
            washRect.offsetMax = Vector2.zero;
            washRect.SetSiblingIndex(1);
            Image backdropWash = washRect.gameObject.AddComponent<Image>();
            backdropWash.raycastTarget = false;
            var cardSurfaces = new List<Image>();
            Color navy = s_Theme.BrandNavy;
            Color labelBlue = new Color(0.16f, 0.27f, 0.47f, 1f);

            Button close = CreateIconButton(content, "Button_Back", "<", new Vector2(-378f, 735f));
            ReplaceGlyphWithSprite(close, "sp_icon_back_arrow", new Vector2(30f, 45f));
            cardSurfaces.Add(close.GetComponent<Image>());
            TMP_Text title = CreateText(content, "Title", "YOUR PROGRESS", new Vector2(0f, 725f), new Vector2(700f, 90f), 64f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            title.characterSpacing = -4f;

            Button statsTab = CreateTab(content, "Tab_Statistics", "STATISTICS", new Vector2(-185f, 624f), out Image statsTabSurface,
                out TMP_Text statsTabLabel);
            UiGlyphGraphic statsIcon = CreateGlyph(statsTab.transform, "Icon", UiGlyphGraphic.GlyphShape.Bars, new Vector2(-104f, 0f), new Vector2(40f, 38f), Color.white);
            Button achievementsTab = CreateTab(content, "Tab_Achievements", "ACHIEVEMENTS", new Vector2(184f, 624f), out Image achievementsTabSurface,
                out TMP_Text achievementsTabLabel);
            achievementsTabLabel.rectTransform.anchoredPosition = new Vector2(38f, 0f);
            achievementsTabLabel.fontSize = 29f;
            UiGlyphGraphic achievementsIcon = CreateGlyph(achievementsTab.transform, "Icon", UiGlyphGraphic.GlyphShape.Trophy, new Vector2(-126f, 0f), new Vector2(42f, 42f), navy);
            statsTabLabel.rectTransform.anchoredPosition = new Vector2(36f, 0f);

            RectTransform statisticsSection = CreateRect("StatisticsSection", content, new Vector2(0f, 218f), new Vector2(744f, 681f));
            Image statisticsCard = CreatePanel(statisticsSection, "StatisticsCard", Vector2.zero, new Vector2(744f, 681f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            cardSurfaces.Add(statisticsCard);
            Image sunburst = CreateSpriteImage(statisticsCard.transform, "CrownGlow", "sp_fx_sunburst_gold", new Vector2(-195f, 222f), new Vector2(300f, 300f));
            sunburst.color = new Color(1f, 1f, 1f, 0.75f);
            Image crown = CreateThemedIcon(statisticsCard.transform, "BestCrown", new Vector2(-195f, 226f), new Vector2(210f, 196f), UiThemeApplier.SpriteToken.IconCrown);
            DetachThemeColor(crown);
            TMP_Text bestLabel = CreateText(statisticsCard.transform, "BestLabel", "BEST SCORE", new Vector2(122f, 293f), new Vector2(360f, 44f), 34f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            DetachTheme(bestLabel.gameObject);
            bestLabel.color = labelBlue;
            TMP_Text bestText = CreateText(statisticsCard.transform, "BestValue", "12,480", new Vector2(142f, 214f), new Vector2(400f, 124f), 124f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.Crown);
            bestText.characterSpacing = -8f;
            bestText.enableAutoSizing = true;
            bestText.fontSizeMin = 60f;
            bestText.fontSizeMax = 124f;
            Image[] heroBalls = CreateBallRow(statisticsCard.transform, new Vector2(118f, 130f), 48f, 50f);

            var statCardSurfaces = new List<Image>();
            TMP_Text games = CreateStatCard(statisticsCard.transform, "Games", "GAMES", new Vector2(-180f, 20f), labelBlue, statCardSurfaces, out RectTransform gamesIcon);
            CreateGamepadIcon(gamesIcon, navy);
            TMP_Text totalScore = CreateStatCard(statisticsCard.transform, "TotalScore", "TOTAL SCORE", new Vector2(182f, 20f), labelBlue, statCardSurfaces, out RectTransform starIcon);
            CreateGlyph(starIcon, "Star", UiGlyphGraphic.GlyphShape.Star, Vector2.zero, new Vector2(68f, 66f), navy);
            TMP_Text lines = CreateStatCard(statisticsCard.transform, "Lines", "LINES CLEARED", new Vector2(-180f, -119f), labelBlue, statCardSurfaces, out RectTransform dotsIcon);
            TintedSprite(dotsIcon, "DotsGrid", "sp_icon_dots_grid", new Vector2(62f, 60f));
            TMP_Text longest = CreateStatCard(statisticsCard.transform, "Longest", "LONGEST LINE", new Vector2(182f, -119f), labelBlue, statCardSurfaces, out RectTransform lineIcon);
            Image[] lineBalls = CreateBallTrio(lineIcon, new Vector2(-4f, 0f), 36f, 29f);
            TMP_Text combo = CreateStatCard(statisticsCard.transform, "Combo", "HIGHEST COMBO", new Vector2(-180f, -260f), labelBlue, statCardSurfaces, out RectTransform flameIcon);
            TintedSprite(flameIcon, "Flame", "sp_icon_flame", new Vector2(52f, 66f));
            TMP_Text streak = CreateStatCard(statisticsCard.transform, "Streak", "CURRENT STREAK", new Vector2(182f, -260f), labelBlue, statCardSurfaces, out RectTransform calendarIcon);
            Image calendar = TintedSprite(calendarIcon, "Calendar", "sp_icon_calendar", new Vector2(64f, 70f));
            calendar.color = Color.white;
            cardSurfaces.AddRange(statCardSurfaces);

            RectTransform achievementsSection = CreateRect("AchievementsSection", content, Vector2.zero, new Vector2(940f, 1670f));
            Image achievementsCard = CreatePanel(achievementsSection, "AchievementsCard", new Vector2(0f, -143f), new Vector2(744f, 540f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            achievementsCard.rectTransform.pivot = new Vector2(0.5f, 1f);
            cardSurfaces.Add(achievementsCard);
            TMP_Text achievementsLabel = CreateText(achievementsCard.transform, "AchievementsLabel", "ACHIEVEMENTS", new Vector2(-150f, -50f), new Vector2(360f, 56f), 44f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            achievementsLabel.characterSpacing = -3f;
            AnchorTop(achievementsLabel.rectTransform);
            TMP_Text achievementSummary = CreateText(achievementsCard.transform, "AchievementSummary", "6/10", new Vector2(125f, -50f), new Vector2(160f, 50f), 38f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            AnchorTop(achievementSummary.rectTransform);
            Image summaryFill = CreateProgressBar(achievementsCard.transform, "SummaryProgress", new Vector2(1f, -100f), new Vector2(672f, 30f));
            AnchorTop(summaryFill.transform.parent as RectTransform);

            const int rowCount = 8;
            var rows = new RectTransform[rowCount];
            var icons = new Image[rowCount];
            var trophies = new Graphic[rowCount];
            var locks = new GameObject[rowCount];
            var names = new TMP_Text[rowCount];
            var badges = new Image[rowCount];
            var checks = new UiCheckGraphic[rowCount];
            var fills = new Image[rowCount];
            var progressTexts = new TMP_Text[rowCount];
            Sprite greyCircle = LoadCrystalSprite("sp_btn_circle_grey");
            for (int i = 0; i < rowCount; i++)
            {
                Image rowPanel = CreatePanel(achievementsCard.transform, $"AchievementRow_{i}", new Vector2(1f, -189f - 121f * i), new Vector2(706f, 106f),
                    UiThemeApplier.ColorToken.SurfaceSecondary, UiThemeApplier.SpriteToken.CardBackground);
                AnchorTop(rowPanel.rectTransform);
                DetachTheme(rowPanel.gameObject);
                rowPanel.pixelsPerUnitMultiplier = 1.1f;
                rows[i] = rowPanel.rectTransform;

                RectTransform iconRect = CreateRect("Icon", rowPanel.transform, new Vector2(-279f, 0f), new Vector2(76f, 76f));
                icons[i] = iconRect.gameObject.AddComponent<Image>();
                icons[i].raycastTarget = false;
                UiGlyphGraphic trophy = CreateGlyph(rowPanel.transform, "Trophy", UiGlyphGraphic.GlyphShape.Trophy, new Vector2(-279f, 0f), new Vector2(80f, 76f), Color.white);
                trophy.SetGradient(UiGlyphGraphic.GradientMode.Vertical, new Color(1f, 0.84f, 0.3f, 1f), new Color(0.86f, 0.55f, 0.1f, 1f));
                CreateGlyph(trophy.transform, "Star", UiGlyphGraphic.GlyphShape.Star, new Vector2(0f, 20f), new Vector2(22f, 22f), new Color(1f, 0.95f, 0.75f, 1f));
                trophies[i] = trophy;
                UiGlyphGraphic lockGlyph = CreateGlyph(rowPanel.transform, "Lock", UiGlyphGraphic.GlyphShape.Lock, new Vector2(-279f, 0f), new Vector2(56f, 72f),
                    new Color(0.24f, 0.33f, 0.47f, 1f));
                lockGlyph.SetGradient(UiGlyphGraphic.GradientMode.Vertical, new Color(1.15f, 1.15f, 1.15f, 1f), new Color(0.8f, 0.8f, 0.8f, 1f));
                Color keyhole = new Color(0.93f, 0.93f, 0.9f, 1f);
                RectTransform holeRect = CreateRect("Keyhole", lockGlyph.transform, new Vector2(0f, -15f), new Vector2(13f, 13f));
                Image hole = holeRect.gameObject.AddComponent<Image>();
                hole.sprite = s_Theme.ButtonCircleSprite;
                hole.color = keyhole;
                hole.raycastTarget = false;
                RectTransform slotRect = CreateRect("KeyholeSlot", lockGlyph.transform, new Vector2(0f, -22f), new Vector2(5f, 12f));
                Image slot = slotRect.gameObject.AddComponent<Image>();
                slot.color = keyhole;
                slot.raycastTarget = false;
                locks[i] = lockGlyph.gameObject;

                names[i] = CreateText(rowPanel.transform, "Name", "ACHIEVEMENT", new Vector2(24f, 0f), new Vector2(440f, 40f), 30f,
                    TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
                names[i].characterSpacing = -1f;

                RectTransform badgeRect = CreateRect("StatusBadge", rowPanel.transform, new Vector2(296f, 0f), new Vector2(66f, 66f));
                badges[i] = badgeRect.gameObject.AddComponent<Image>();
                badges[i].sprite = s_Theme.ButtonCircleSprite;
                badges[i].color = s_Theme.StreakDotOn;
                badges[i].raycastTarget = false;
                RectTransform checkRect = CreateRect("Check", badgeRect, new Vector2(2f, 0f), new Vector2(36f, 36f));
                checks[i] = checkRect.gameObject.AddComponent<UiCheckGraphic>();
                checks[i].Thickness = 8f;
                checks[i].raycastTarget = false;

                fills[i] = CreateProgressBar(rowPanel.transform, "Progress", new Vector2(-37f, -25f), new Vector2(328f, 26f));
                progressTexts[i] = CreateText(rowPanel.transform, "ProgressText", "0/1", new Vector2(192f, -25f), new Vector2(126f, 34f), 26f,
                    TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
                progressTexts[i].enableAutoSizing = true;
                progressTexts[i].fontSizeMin = 14f;
                progressTexts[i].fontSizeMax = 26f;
            }

            Button viewAll = CreateButton(content, "Button_ViewAll", "VIEW ALL", new Vector2(0f, -697f), new Vector2(350f, 86f), false, 34f);
            TMP_Text viewAllLabel = viewAll.GetComponentInChildren<TMP_Text>();
            viewAllLabel.rectTransform.anchoredPosition = new Vector2(-18f, 0f);
            viewAllLabel.characterSpacing = -1f;
            Image chevron = CreateSpriteImage(viewAll.transform, "Chevron", "sp_icon_back_arrow", new Vector2(88f, 0f), new Vector2(18f, 26f));
            chevron.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            chevron.color = navy;
            AttachTheme(chevron.gameObject, UiThemeApplier.ColorToken.BrandNavy, chevron);
            cardSurfaces.Add(viewAll.GetComponent<Image>());

            ConfigurePopupBase(root, content, close, null);
            SerializedObject so = new SerializedObject(popup);
            Set(so, "m_StatisticsTabButton", statsTab);
            Set(so, "m_AchievementsTabButton", achievementsTab);
            Set(so, "m_StatisticsTabSurface", statsTabSurface);
            Set(so, "m_AchievementsTabSurface", achievementsTabSurface);
            Set(so, "m_StatisticsTabLabel", statsTabLabel);
            Set(so, "m_AchievementsTabLabel", achievementsTabLabel);
            Set(so, "m_StatisticsTabIcon", statsIcon);
            Set(so, "m_AchievementsTabIcon", achievementsIcon);
            Set(so, "m_StatisticsSection", statisticsSection);
            Set(so, "m_AchievementsSection", achievementsSection);
            Set(so, "m_GamesPlayedText", games);
            Set(so, "m_BestScoreText", bestText);
            Set(so, "m_TotalScoreText", totalScore);
            Set(so, "m_TotalLinesText", lines);
            Set(so, "m_LongestLineText", longest);
            Set(so, "m_HighestComboText", combo);
            Set(so, "m_CurrentStreakText", streak);
            SetArray(so, "m_HeroBalls", heroBalls);
            SetArray(so, "m_LineBalls", lineBalls);
            Set(so, "m_AchievementsCard", achievementsCard.rectTransform);
            Set(so, "m_ViewAllButton", viewAll);
            Set(so, "m_AchievementSummaryText", achievementSummary);
            Set(so, "m_AchievementSummaryFill", summaryFill);
            SetArray(so, "m_AchievementRows", rows);
            SetArray(so, "m_AchievementIcons", icons);
            SetArray(so, "m_AchievementTrophies", trophies);
            SetArray(so, "m_AchievementLocks", locks);
            SetArray(so, "m_AchievementNames", names);
            SetArray(so, "m_AchievementBadges", badges);
            SetArray(so, "m_AchievementChecks", checks);
            SetArray(so, "m_AchievementProgressFills", fills);
            SetArray(so, "m_AchievementProgressTexts", progressTexts);
            Set(so, "m_Backdrop", backdrop);
            Set(so, "m_BackdropWash", backdropWash);
            Set(so, "m_CrystalBackdropTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(CrystalBackdropPath));
            Set(so, "m_HeaderTitle", title);
            SetArray(so, "m_CardSurfaces", cardSurfaces.ToArray());
            Set(so, "m_RowFeaturedSprite", LoadCrystalSprite("sp_ui_card_gold"));
            Set(so, "m_RowUnlockedSprite", LoadCrystalSprite("sp_ui_card_green"));
            Set(so, "m_BadgeLockedSprite", greyCircle);
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplyThemeNow(root);
            SavePrefab(root, ProgressPath);
        }

        private static void AnchorTop(RectTransform rect)
        {
            Vector2 position = rect.anchoredPosition;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
        }

        private static Image TintedSprite(Transform parent, string name, string spriteName, Vector2 size)
        {
            return CreateSpriteImage(parent, name, spriteName, Vector2.zero, size);
        }

        private static void CreateGamepadIcon(Transform parent, Color color)
        {
            UiGlyphGraphic body = CreateGlyph(parent, "Gamepad", UiGlyphGraphic.GlyphShape.Gamepad, Vector2.zero, new Vector2(76f, 54f), color);
            Color detail = new Color(0.97f, 0.96f, 0.93f, 1f);
            CreateDetailRect(body.transform, "DpadH", new Vector2(-20f, 7f), new Vector2(16f, 5f), detail, 0f);
            CreateDetailRect(body.transform, "DpadV", new Vector2(-20f, 7f), new Vector2(5f, 16f), detail, 0f);
            CreateDetailRect(body.transform, "ButtonA", new Vector2(24f, 11f), new Vector2(6f, 6f), detail, 45f);
            CreateDetailRect(body.transform, "ButtonB", new Vector2(16f, 3f), new Vector2(6f, 6f), detail, 45f);
        }

        private static void CreateDetailRect(Transform parent, string name, Vector2 position, Vector2 size, Color color, float rotation)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void InstallIntoUiRoot()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UiRootPath);
            try
            {
                UiShell shell = root.GetComponent<UiShell>();
                if (shell == null) shell = root.GetComponentInChildren<UiShell>(true);
                if (shell == null) throw new InvalidOperationException("UI_Root has no UiShell.");

                Transform popupCanvas = FindRecursive(root.transform, "Canvas_Popups");
                if (popupCanvas == null) throw new InvalidOperationException("UI_Root has no Canvas_Popups.");

                DailyChallengePopup existingDaily = root.GetComponentInChildren<DailyChallengePopup>(true);
                if (existingDaily != null) UnityEngine.Object.DestroyImmediate(existingDaily.gameObject);

                GameObject dailyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DailyPath);
                GameObject dailyInstance = (GameObject)PrefabUtility.InstantiatePrefab(dailyPrefab, popupCanvas);
                dailyInstance.name = "Popup_DailyChallenge";
                dailyInstance.SetActive(false);

                StatisticsPopup progress = root.GetComponentInChildren<StatisticsPopup>(true);
                DailyChallengePopup daily = dailyInstance.GetComponent<DailyChallengePopup>();
                SerializedObject shellObject = new SerializedObject(shell);
                Set(shellObject, "m_StatisticsPopup", progress);
                Set(shellObject, "m_DailyChallengePopup", daily);
                shellObject.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, UiRootPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject CreatePopupRoot<T>(string name, out RectTransform content, out T popup) where T : PopupView
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(T), typeof(UiResponsiveModal));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            popup = root.GetComponent<T>();
            content = CreateRect("Reference_940x1670", root.transform, Vector2.zero, new Vector2(940f, 1670f));
            return root;
        }

        private static void ConfigurePopupBase(GameObject root, RectTransform content, Button close, Button primary)
        {
            PopupView popup = root.GetComponent<PopupView>();
            SerializedObject so = new SerializedObject(popup);
            Set(so, "m_ModalContainer", content);
            Set(so, "m_CanvasGroup", root.GetComponent<CanvasGroup>());
            Set(so, "m_PrimaryButton", primary);
            Set(so, "m_CloseButton", close);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateHeader(Transform parent, string title, Vector2 position, float size)
        {
            CreateText(parent, "Title", title, position, new Vector2(700f, 86f), size,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            RectTransform leafRect = CreateRect("Leaf", parent, position + new Vector2(0f, 52f), new Vector2(38f, 28f));
            UiGlyphGraphic leaf = leafRect.gameObject.AddComponent<UiGlyphGraphic>();
            leaf.Shape = UiGlyphGraphic.GlyphShape.Leaf;
            leaf.color = s_Theme.SurfacePrimary;
            leaf.raycastTarget = false;
            AttachTheme(leaf.gameObject, UiThemeApplier.ColorToken.SurfacePrimary, leaf);
        }

        private static Button CreateTab(Transform parent, string name, string label, Vector2 position, out Image surface, out TMP_Text labelText)
        {
            RectTransform rect = CreateRect(name, parent, position, new Vector2(370f, 86f));
            surface = rect.gameObject.AddComponent<Image>();
            surface.sprite = s_Theme.ButtonCapsuleSprite;
            surface.type = Image.Type.Sliced;
            surface.color = Color.white;
            Button button = rect.gameObject.AddComponent<Button>();
            labelText = CreateText(rect, "Label", label, Vector2.zero, new Vector2(230f, 60f), 30f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            labelText.characterSpacing = -1f;
            // The popup drives tab ink per selection state.
            DetachTheme(labelText.gameObject);
            return button;
        }

        private static TMP_Text CreateStatCard(Transform parent, string name, string label, Vector2 position, Color labelColor,
            List<Image> surfaces, out RectTransform iconSlot)
        {
            Image card = CreatePanel(parent, "Card_" + name, position, new Vector2(346f, 122f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            card.pixelsPerUnitMultiplier = 1.1f;
            surfaces.Add(card);
            iconSlot = CreateRect("IconSlot", card.transform, new Vector2(-108f, 0f), new Vector2(90f, 90f));
            TMP_Text labelText = CreateText(card.transform, "Label", label, new Vector2(56f, 25f), new Vector2(192f, 32f), 22f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            DetachTheme(labelText.gameObject);
            labelText.color = labelColor;
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 16f;
            labelText.fontSizeMax = 22f;
            TMP_Text value = CreateText(card.transform, "Value", "0", new Vector2(56f, -15f), new Vector2(192f, 58f), 48f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            value.characterSpacing = -2f;
            value.enableAutoSizing = true;
            value.fontSizeMin = 28f;
            value.fontSizeMax = 48f;
            return value;
        }

        private static Image CreateProgressBar(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform bgRect = CreateRect(name, parent, position, size);
            Image background = bgRect.gameObject.AddComponent<Image>();
            background.color = new Color(0.86f, 0.86f, 0.83f, 1f);
            background.sprite = s_Theme.ButtonCapsuleSprite;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 80f / size.y;
            background.raycastTarget = false;
            RectTransform fillRect = CreateRect("Fill", bgRect, Vector2.zero, size);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = s_Theme.ButtonCapsulePrimary;
            fill.type = Image.Type.Sliced;
            fill.pixelsPerUnitMultiplier = 88f / size.y;
            fill.color = Color.white;
            fill.raycastTarget = false;
            return fill;
        }

        private static Image[] CreateBallTrio(Transform parent, Vector2 center, float size, float spacing)
        {
            var images = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform rect = CreateRect($"MissionBall_{i}", parent, center + new Vector2((i - 1) * spacing, 0f), new Vector2(size, size));
                Image image = rect.gameObject.AddComponent<Image>();
                image.sprite = s_Theme.PreviewSpriteSet?.GetSpriteByIndex(i == 0 ? 4 : i == 1 ? 3 : 5);
                image.preserveAspect = true;
                image.raycastTarget = false;
                images[i] = image;
            }
            return images;
        }

        private static Image[] CreateBallRow(Transform parent, Vector2 center, float size, float spacing)
        {
            var images = new Image[7];
            for (int i = 0; i < 7; i++)
            {
                RectTransform rect = CreateRect($"HeroBall_{i}", parent, center + new Vector2((i - 3) * spacing, 0f), new Vector2(size, size));
                Image image = rect.gameObject.AddComponent<Image>();
                image.sprite = s_Theme.PreviewSpriteSet?.GetSpriteByIndex(i);
                image.preserveAspect = true;
                image.raycastTarget = false;
                images[i] = image;
            }
            return images;
        }

        private static Button CreateIconButton(Transform parent, string name, string glyph, Vector2 position)
        {
            RectTransform rect = CreateRect(name, parent, position, new Vector2(94f, 94f));
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = s_Theme.ButtonCircleSprite;
            image.type = Image.Type.Sliced;
            image.color = s_Theme.PanelButton;
            AttachThemeSurface(rect.gameObject, UiThemeApplier.ColorToken.PanelButton, image, UiThemeApplier.SpriteToken.ButtonCircle);
            Button button = rect.gameObject.AddComponent<Button>();
            CreateText(rect, "Glyph", glyph, Vector2.zero, new Vector2(70f, 76f), glyph == "i" ? 48f : 64f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            return button;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size, bool primary, float fontSize)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = primary ? s_Theme.ButtonCapsulePrimary : s_Theme.ButtonCapsuleSprite;
            image.type = Image.Type.Sliced;
            image.color = primary ? s_Theme.SurfacePrimary : s_Theme.PanelButton;
            AttachThemeSurface(rect.gameObject,
                primary ? UiThemeApplier.ColorToken.SurfacePrimary : UiThemeApplier.ColorToken.PanelButton,
                image,
                primary ? UiThemeApplier.SpriteToken.ButtonCapsulePrimary : UiThemeApplier.SpriteToken.ButtonCapsule);
            Button button = rect.gameObject.AddComponent<Button>();
            CreateText(rect, "Label", label, Vector2.zero, size - new Vector2(40f, 24f), fontSize,
                TextAlignmentOptions.Center,
                primary ? UiThemeApplier.ColorToken.InkButtonPrimary : UiThemeApplier.ColorToken.BrandNavy);
            return button;
        }

        private static Image CreateThemedIcon(Transform parent, string name, Vector2 position, Vector2 size, UiThemeApplier.SpriteToken token)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            UiThemeApplier applier = rect.gameObject.AddComponent<UiThemeApplier>();
            applier.Configure(UiThemeApplier.ColorToken.BrandNavy, new Graphic[] { image }, UiThemeApplier.FontSizeToken.None, Array.Empty<TMP_Text>());
            applier.ConfigureSprite(token, new[] { image });
            return image;
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 position, Vector2 size,
            UiThemeApplier.ColorToken colorToken, UiThemeApplier.SpriteToken spriteToken)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = ResolveSprite(spriteToken);
            image.type = Image.Type.Sliced;
            image.color = ResolveColor(colorToken);
            image.raycastTarget = false;
            AttachThemeSurface(rect.gameObject, colorToken, image, spriteToken);
            return image;
        }

        private static TMP_Text CreateText(Transform parent, string name, string value, Vector2 position, Vector2 size,
            float fontSize, TextAlignmentOptions alignment, UiThemeApplier.ColorToken colorToken)
        {
            RectTransform rect = CreateRect(name, parent, position, size);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = s_Font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = alignment;
            text.color = ResolveColor(colorToken);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            AttachTheme(rect.gameObject, colorToken, text);
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void AttachTheme(GameObject go, UiThemeApplier.ColorToken colorToken, Graphic graphic)
        {
            UiThemeApplier applier = go.GetComponent<UiThemeApplier>() ?? go.AddComponent<UiThemeApplier>();
            applier.Configure(colorToken, new[] { graphic }, UiThemeApplier.FontSizeToken.None, Array.Empty<TMP_Text>());
        }

        private static void AttachThemeSurface(GameObject go, UiThemeApplier.ColorToken colorToken, Image image, UiThemeApplier.SpriteToken spriteToken)
        {
            UiThemeApplier applier = go.GetComponent<UiThemeApplier>() ?? go.AddComponent<UiThemeApplier>();
            applier.Configure(colorToken, new Graphic[] { image }, UiThemeApplier.FontSizeToken.None, Array.Empty<TMP_Text>());
            applier.ConfigureSprite(spriteToken, new[] { image });
            applier.ConfigureMaterial(
                spriteToken == UiThemeApplier.SpriteToken.ButtonCapsulePrimary || spriteToken == UiThemeApplier.SpriteToken.ButtonCapsule || spriteToken == UiThemeApplier.SpriteToken.ButtonCircle
                    ? UiThemeApplier.MaterialToken.SurfaceMaterialButton
                    : UiThemeApplier.MaterialToken.SurfaceMaterialCard,
                new Graphic[] { image });
        }

        private static void ApplyThemeNow(GameObject root)
        {
            UiThemeApplier[] appliers = root.GetComponentsInChildren<UiThemeApplier>(true);
            for (int i = 0; i < appliers.Length; i++) appliers[i].Apply(s_Theme);
        }

        private static Sprite ResolveSprite(UiThemeApplier.SpriteToken token)
        {
            return token switch
            {
                UiThemeApplier.SpriteToken.CardBackground => s_Theme.CardBackgroundSprite,
                UiThemeApplier.SpriteToken.CardGold => s_Theme.CardGoldSprite,
                UiThemeApplier.SpriteToken.ButtonCapsule => s_Theme.ButtonCapsuleSprite,
                UiThemeApplier.SpriteToken.ButtonCapsulePrimary => s_Theme.ButtonCapsulePrimary,
                UiThemeApplier.SpriteToken.ButtonCircle => s_Theme.ButtonCircleSprite,
                _ => null
            };
        }

        private static Color ResolveColor(UiThemeApplier.ColorToken token)
        {
            return token switch
            {
                UiThemeApplier.ColorToken.PanelCard => s_Theme.PanelCard,
                UiThemeApplier.ColorToken.PanelButton => s_Theme.PanelButton,
                UiThemeApplier.ColorToken.PanelCell => s_Theme.PanelCell,
                UiThemeApplier.ColorToken.InkLabel => s_Theme.InkLabel,
                UiThemeApplier.ColorToken.Crown => s_Theme.Crown,
                UiThemeApplier.ColorToken.SurfaceCardGold => s_Theme.SurfaceCardGold,
                UiThemeApplier.ColorToken.SurfaceSecondary => s_Theme.SurfaceSecondary,
                UiThemeApplier.ColorToken.SurfacePrimary => s_Theme.SurfacePrimary,
                UiThemeApplier.ColorToken.InkButtonPrimary => s_Theme.InkButtonPrimary,
                _ => s_Theme.BrandNavy
            };
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindRecursive(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            root.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void Set(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"Missing serialized property {propertyName} on {serializedObject.targetObject}.");
            property.objectReferenceValue = value;
        }

        private static void SetArray<T>(SerializedObject serializedObject, string propertyName, T[] values) where T : UnityEngine.Object
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"Missing serialized array {propertyName} on {serializedObject.targetObject}.");
            property.arraySize = values?.Length ?? 0;
            for (int i = 0; i < property.arraySize; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
#endif

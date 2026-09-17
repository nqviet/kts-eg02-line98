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

            Button close = CreateIconButton(content, "Button_Back", "<", new Vector2(-390f, 728f));
            CreateHeader(content, "DAILY CHALLENGE", new Vector2(0f, 730f), 55f);
            CreateIconButton(content, "Button_Info", "i", new Vector2(390f, 728f));

            Image dateCard = CreatePanel(content, "DateCard", new Vector2(0f, 592f), new Vector2(520f, 112f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            CreateText(dateCard.transform, "TodayLabel", "TODAY", new Vector2(42f, 24f), new Vector2(360f, 36f), 27f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.InkLabel);
            TMP_Text dateText = CreateText(dateCard.transform, "DateValue", "SEPTEMBER 12", new Vector2(42f, -22f), new Vector2(380f, 50f), 39f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            CreateThemedIcon(dateCard.transform, "CalendarIcon", new Vector2(-190f, 0f), new Vector2(68f, 68f), UiThemeApplier.SpriteToken.IconCalendar);

            Image boardPanel = CreatePanel(content, "BoardPreview", new Vector2(0f, 260f), new Vector2(570f, 570f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            RectTransform boardGrid = CreateRect("Grid", boardPanel.transform, Vector2.zero, new Vector2(530f, 530f));
            var grid = boardGrid.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(54f, 54f);
            grid.spacing = new Vector2(5f, 5f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 9;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.padding = new RectOffset(0, 0, 0, 0);

            var boardBalls = new List<Image>(81);
            for (int i = 0; i < 81; i++)
            {
                GameObject cell = new GameObject($"Cell_{i:00}", typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(boardGrid, false);
                Image cellImage = cell.GetComponent<Image>();
                cellImage.color = s_Theme.PanelCell;
                cellImage.raycastTarget = false;
                AttachTheme(cell, UiThemeApplier.ColorToken.PanelCell, cellImage);
                RectTransform ballRect = CreateRect("Ball", cell.transform, Vector2.zero, new Vector2(48f, 48f));
                Image ball = ballRect.gameObject.AddComponent<Image>();
                ball.raycastTarget = false;
                ball.preserveAspect = true;
                ball.gameObject.SetActive(false);
                boardBalls.Add(ball);
            }

            Image mission = CreatePanel(content, "MissionCard", new Vector2(0f, -92f), new Vector2(770f, 128f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            Image[] missionBalls = CreateBallTrio(mission.transform, new Vector2(-285f, 12f), 48f);
            CreateText(mission.transform, "Mission", "MATCH 5 OR MORE", new Vector2(105f, 20f), new Vector2(450f, 48f), 36f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            CreateText(mission.transform, "Rule", "SAME BOARD. ONE DAILY RUN.", new Vector2(105f, -31f), new Vector2(480f, 34f), 24f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.InkLabel);

            Image weekPanel = CreatePanel(content, "WeekPanel", new Vector2(0f, -272f), new Vector2(770f, 190f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            TMP_Text[] weekdayTexts = new TMP_Text[7];
            Image[] dayBadges = new Image[7];
            UiCheckGraphic[] dayChecks = new UiCheckGraphic[7];
            for (int i = 0; i < 7; i++)
            {
                float x = -300f + i * 100f;
                weekdayTexts[i] = CreateText(weekPanel.transform, $"Weekday_{i}", "M", new Vector2(x, 54f), new Vector2(60f, 30f), 24f,
                    TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
                RectTransform badgeRect = CreateRect($"DayBadge_{i}", weekPanel.transform, new Vector2(x, 4f), new Vector2(58f, 58f));
                dayBadges[i] = badgeRect.gameObject.AddComponent<Image>();
                dayBadges[i].sprite = s_Theme.ButtonCircleSprite;
                dayBadges[i].color = s_Theme.StreakDotOff;
                dayBadges[i].raycastTarget = false;
                RectTransform checkRect = CreateRect("Check", badgeRect, Vector2.zero, new Vector2(34f, 34f));
                dayChecks[i] = checkRect.gameObject.AddComponent<UiCheckGraphic>();
                dayChecks[i].Thickness = 8f;
                dayChecks[i].raycastTarget = false;
            }
            TMP_Text streakText = CreateText(weekPanel.transform, "StreakValue", "5 DAY STREAK", new Vector2(0f, -63f), new Vector2(430f, 40f), 28f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);

            Image bestCard = CreatePanel(content, "BestCard", new Vector2(-195f, -477f), new Vector2(375f, 125f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            CreateThemedIcon(bestCard.transform, "Crown", new Vector2(-125f, 0f), new Vector2(75f, 75f), UiThemeApplier.SpriteToken.IconCrown);
            CreateText(bestCard.transform, "BestLabel", "BEST", new Vector2(58f, 27f), new Vector2(210f, 30f), 24f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.InkLabel);
            TMP_Text bestText = CreateText(bestCard.transform, "BestValue", "7,420", new Vector2(58f, -18f), new Vector2(220f, 55f), 43f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);

            Image todayCard = CreatePanel(content, "TodayCard", new Vector2(195f, -477f), new Vector2(375f, 125f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            CreateThemedIcon(todayCard.transform, "Calendar", new Vector2(-125f, 0f), new Vector2(68f, 68f), UiThemeApplier.SpriteToken.IconCalendar);
            CreateText(todayCard.transform, "TodayLabel", "TODAY", new Vector2(60f, 28f), new Vector2(210f, 30f), 24f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            TMP_Text todayValue = CreateText(todayCard.transform, "TodayValue", "—", new Vector2(60f, -10f), new Vector2(210f, 44f), 36f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            TMP_Text todayStatus = CreateText(todayCard.transform, "TodayStatus", "NOT PLAYED", new Vector2(60f, -44f), new Vector2(230f, 28f), 21f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.InkLabel);

            Button play = CreateButton(content, "Button_PlayDaily", "PLAY DAILY", new Vector2(0f, -635f), new Vector2(730f, 142f), true, 45f);
            Button howTo = CreateButton(content, "Button_HowToPlay", "HOW TO PLAY", new Vector2(0f, -752f), new Vector2(330f, 62f), false, 25f);

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
            SetArray(so, "m_BoardBalls", boardBalls.ToArray());
            SetArray(so, "m_MissionBalls", missionBalls);
            Set(so, "m_PlayButton", play);
            Set(so, "m_HowToPlayButton", howTo);
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplyThemeNow(root);
            SavePrefab(root, DailyPath);
        }

        private static void BuildProgressPrefab()
        {
            GameObject root = CreatePopupRoot("Popup_Statistics", out RectTransform content, out StatisticsPopup popup);
            Button close = CreateIconButton(content, "Button_Back", "<", new Vector2(-390f, 728f));
            CreateHeader(content, "YOUR PROGRESS", new Vector2(0f, 730f), 56f);

            Button statsTab = CreateTab(content, "Tab_Statistics", "STATISTICS", new Vector2(-190f, 610f), out Image statsTabSurface);
            Button achievementsTab = CreateTab(content, "Tab_Achievements", "ACHIEVEMENTS", new Vector2(190f, 610f), out Image achievementsTabSurface);

            RectTransform statisticsSection = CreateRect("StatisticsSection", content, new Vector2(0f, 150f), new Vector2(780f, 760f));
            Image statisticsCard = CreatePanel(statisticsSection, "StatisticsCard", Vector2.zero, new Vector2(780f, 760f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            CreateThemedIcon(statisticsCard.transform, "BestCrown", new Vector2(-235f, 245f), new Vector2(150f, 150f), UiThemeApplier.SpriteToken.IconCrown);
            CreateText(statisticsCard.transform, "BestLabel", "BEST SCORE", new Vector2(145f, 290f), new Vector2(390f, 50f), 36f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.InkLabel);
            TMP_Text bestText = CreateText(statisticsCard.transform, "BestValue", "12,480", new Vector2(145f, 220f), new Vector2(440f, 95f), 76f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.Crown);
            Image[] heroBalls = CreateBallRow(statisticsCard.transform, new Vector2(145f, 150f), 42f);

            TMP_Text games = CreateStatCard(statisticsCard.transform, "Games", "GAMES", new Vector2(-190f, 45f));
            TMP_Text totalScore = CreateStatCard(statisticsCard.transform, "TotalScore", "TOTAL SCORE", new Vector2(190f, 45f));
            TMP_Text lines = CreateStatCard(statisticsCard.transform, "Lines", "LINES CLEARED", new Vector2(-190f, -115f));
            TMP_Text longest = CreateStatCard(statisticsCard.transform, "Longest", "LONGEST LINE", new Vector2(190f, -115f));
            TMP_Text combo = CreateStatCard(statisticsCard.transform, "Combo", "HIGHEST COMBO", new Vector2(-190f, -275f));
            TMP_Text streak = CreateStatCard(statisticsCard.transform, "Streak", "CURRENT STREAK", new Vector2(190f, -275f));

            RectTransform achievementsSection = CreateRect("AchievementsSection", content, new Vector2(0f, 150f), new Vector2(780f, 760f));
            Image achievementsCard = CreatePanel(achievementsSection, "AchievementsCard", Vector2.zero, new Vector2(780f, 760f),
                UiThemeApplier.ColorToken.PanelCard, UiThemeApplier.SpriteToken.CardBackground);
            CreateText(achievementsCard.transform, "AchievementsLabel", "ACHIEVEMENTS", new Vector2(-170f, 330f), new Vector2(360f, 48f), 36f,
                TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
            TMP_Text achievementSummary = CreateText(achievementsCard.transform, "AchievementSummary", "6/10", new Vector2(205f, 330f), new Vector2(180f, 48f), 33f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            Image summaryFill = CreateProgressBar(achievementsCard.transform, "SummaryProgress", new Vector2(0f, 282f), new Vector2(700f, 24f));

            const int rowCount = 6;
            var rows = new RectTransform[rowCount];
            var icons = new Image[rowCount];
            var names = new TMP_Text[rowCount];
            var checks = new UiCheckGraphic[rowCount];
            var fills = new Image[rowCount];
            var progressTexts = new TMP_Text[rowCount];
            for (int i = 0; i < rowCount; i++)
            {
                float y = 220f - i * 96f;
                Image rowPanel = CreatePanel(achievementsCard.transform, $"AchievementRow_{i}", new Vector2(0f, y), new Vector2(700f, 82f),
                    i == 0 ? UiThemeApplier.ColorToken.SurfaceCardGold : UiThemeApplier.ColorToken.SurfaceSecondary,
                    i == 0 ? UiThemeApplier.SpriteToken.CardGold : UiThemeApplier.SpriteToken.CardBackground);
                rows[i] = rowPanel.rectTransform;
                RectTransform iconRect = CreateRect("Icon", rowPanel.transform, new Vector2(-290f, 0f), new Vector2(54f, 54f));
                icons[i] = iconRect.gameObject.AddComponent<Image>();
                icons[i].raycastTarget = false;
                names[i] = CreateText(rowPanel.transform, "Name", "ACHIEVEMENT", new Vector2(-25f, 13f), new Vector2(445f, 36f), 25f,
                    TextAlignmentOptions.Left, UiThemeApplier.ColorToken.BrandNavy);
                RectTransform checkBadge = CreateRect("UnlockedBadge", rowPanel.transform, new Vector2(292f, 0f), new Vector2(50f, 50f));
                Image badge = checkBadge.gameObject.AddComponent<Image>();
                badge.sprite = s_Theme.ButtonCircleSprite;
                badge.color = s_Theme.SurfacePrimary;
                AttachTheme(checkBadge.gameObject, UiThemeApplier.ColorToken.SurfacePrimary, badge);
                RectTransform checkRect = CreateRect("Check", checkBadge, Vector2.zero, new Vector2(30f, 30f));
                checks[i] = checkRect.gameObject.AddComponent<UiCheckGraphic>();
                checks[i].Thickness = 8f;
                checks[i].raycastTarget = false;
                fills[i] = CreateProgressBar(rowPanel.transform, "Progress", new Vector2(-35f, -20f), new Vector2(390f, 16f));
                progressTexts[i] = CreateText(rowPanel.transform, "ProgressText", "0/1", new Vector2(236f, -20f), new Vector2(120f, 28f), 20f,
                    TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            }

            ConfigurePopupBase(root, content, close, null);
            SerializedObject so = new SerializedObject(popup);
            Set(so, "m_StatisticsTabButton", statsTab);
            Set(so, "m_AchievementsTabButton", achievementsTab);
            Set(so, "m_StatisticsTabSurface", statsTabSurface);
            Set(so, "m_AchievementsTabSurface", achievementsTabSurface);
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
            Set(so, "m_AchievementSummaryText", achievementSummary);
            Set(so, "m_AchievementSummaryFill", summaryFill);
            SetArray(so, "m_AchievementRows", rows);
            SetArray(so, "m_AchievementIcons", icons);
            SetArray(so, "m_AchievementNames", names);
            SetArray(so, "m_AchievementChecks", checks);
            SetArray(so, "m_AchievementProgressFills", fills);
            SetArray(so, "m_AchievementProgressTexts", progressTexts);
            so.ApplyModifiedPropertiesWithoutUndo();
            ApplyThemeNow(root);
            SavePrefab(root, ProgressPath);
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

        private static Button CreateTab(Transform parent, string name, string label, Vector2 position, out Image surface)
        {
            RectTransform rect = CreateRect(name, parent, position, new Vector2(380f, 86f));
            surface = rect.gameObject.AddComponent<Image>();
            surface.sprite = s_Theme.ButtonCapsuleSprite;
            surface.type = Image.Type.Sliced;
            surface.color = s_Theme.SurfaceSecondary;
            Button button = rect.gameObject.AddComponent<Button>();
            CreateText(rect, "Label", label, Vector2.zero, new Vector2(340f, 60f), 28f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
            return button;
        }

        private static TMP_Text CreateStatCard(Transform parent, string name, string label, Vector2 position)
        {
            Image card = CreatePanel(parent, "Card_" + name, position, new Vector2(355f, 138f),
                UiThemeApplier.ColorToken.SurfaceSecondary, UiThemeApplier.SpriteToken.CardBackground);
            CreateText(card.transform, "Label", label, new Vector2(22f, 34f), new Vector2(280f, 34f), 24f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.InkLabel);
            return CreateText(card.transform, "Value", "0", new Vector2(22f, -15f), new Vector2(280f, 60f), 42f,
                TextAlignmentOptions.Center, UiThemeApplier.ColorToken.BrandNavy);
        }

        private static Image CreateProgressBar(Transform parent, string name, Vector2 position, Vector2 size)
        {
            RectTransform bgRect = CreateRect(name, parent, position, size);
            Image background = bgRect.gameObject.AddComponent<Image>();
            background.color = s_Theme.StreakDotOff;
            background.sprite = s_Theme.ButtonCapsuleSprite;
            background.type = Image.Type.Sliced;
            background.raycastTarget = false;
            RectTransform fillRect = CreateRect("Fill", bgRect, Vector2.zero, size);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillRect.gameObject.AddComponent<Image>();
            fill.sprite = s_Theme.ButtonCapsuleSprite;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0.5f;
            fill.color = s_Theme.SurfacePrimary;
            fill.raycastTarget = false;
            AttachTheme(fill.gameObject, UiThemeApplier.ColorToken.SurfacePrimary, fill);
            return fill;
        }

        private static Image[] CreateBallTrio(Transform parent, Vector2 center, float size)
        {
            var images = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform rect = CreateRect($"MissionBall_{i}", parent, center + new Vector2((i - 1) * size * 0.72f, 0f), new Vector2(size, size));
                Image image = rect.gameObject.AddComponent<Image>();
                image.sprite = s_Theme.PreviewSpriteSet?.GetSpriteByIndex(i == 0 ? 4 : i == 1 ? 3 : 6);
                image.preserveAspect = true;
                image.raycastTarget = false;
                images[i] = image;
            }
            return images;
        }

        private static Image[] CreateBallRow(Transform parent, Vector2 center, float size)
        {
            var images = new Image[7];
            for (int i = 0; i < 7; i++)
            {
                RectTransform rect = CreateRect($"HeroBall_{i}", parent, center + new Vector2((i - 3) * size * 0.82f, 0f), new Vector2(size, size));
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

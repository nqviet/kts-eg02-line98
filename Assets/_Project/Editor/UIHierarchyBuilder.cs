using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Line98.Data;
using Line98.Presentation;

namespace Line98.Editor
{
    /// <summary>
    /// Dev utility to scaffold the full LINE 98 UI hierarchy.
    /// NOTE: The authored Game.unity scene is the primary source of truth;
    /// this builder is preserved only as a developer reference utility.
    /// </summary>
    public static class UIHierarchyBuilder
    {
        [MenuItem("Line98/Dev/Build Game Scene UI")]
        public static void BuildUI()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.path.Contains("Game.unity"))
            {
                EditorSceneManager.OpenScene("Assets/_Project/Content/Scenes/Game.unity");
            }

            // Clean up existing UI_Root and EventSystem if any
            var existingRoot = GameObject.Find("UI_Root");
            if (existingRoot != null) Undo.DestroyObjectImmediate(existingRoot);

            var existingEvent = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
            if (existingEvent != null) Undo.DestroyObjectImmediate(existingEvent.gameObject);

            // 1. Create EventSystem with InputSystemUIInputModule
            var eventSystemGo = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystemGo, "Create EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();

            // 2. Load required assets
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>("Assets/_Project/Content/Definitions/UiTheme_Default.asset");
            var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_ui_button_click.wav");

            var spBallQuad = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_brand_ball_quad.png");
            var spBrandLogo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_brand_logo_text.png");
            var spBtnSquare = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_button_square_neumorphic.png");
            var spIconGear = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_icon_settings_gear.png");
            var spIconChart = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_icon_statistics_chart.png");
            var spCardHud = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_card_hud_container.png");
            var spTrayNext = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_tray_next_balls_recessed.png");
            var spCrown = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_icon_crown_gold.png");
            var spBtnUndo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_btn_action_undo.png");
            var spBtnNewGame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_btn_action_newgame.png");
            var spBtnDpad = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_btn_center_nav_dpad.png");
            var spBadgeUndo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_badge_undo_counter.png");
            var spScrim = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_overlay_modal_scrim.png");
            var spIconUndo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_icon_undo_arrow.png");
            var spIconNewGame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_icon_newgame_refresh.png");

            var spBallRed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Balls/sp_ball_red.png");
            var spBallOrange = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Balls/sp_ball_orange.png");
            var spBallYellow = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Balls/sp_ball_yellow.png");
            var spBallGreen = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Balls/sp_ball_green.png");
            var spBallCyan = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Balls/sp_ball_cyan.png");
            var spBallPurple = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Balls/sp_ball_purple.png");
            var spBallBlue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/Balls/sp_ball_blue.png");

            // 3. Create UI_Root
            var uiRoot = new GameObject("UI_Root");
            Undo.RegisterCreatedObjectUndo(uiRoot, "Create UI_Root");
            var safeAreaFitter = uiRoot.AddComponent<SafeAreaFitter>();
            var uiRouter = uiRoot.AddComponent<UIRouter>();
            var hudPresenter = uiRoot.AddComponent<HudPresenter>();

            // Helper to configure Canvas
            Canvas CreateCanvas(string name, int sortingOrder)
            {
                var go = new GameObject(name);
                go.transform.SetParent(uiRoot.transform, false);
                var c = go.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = sortingOrder;

                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(HudLayoutSolver.ReferenceWidth, HudLayoutSolver.ReferenceHeight);
                scaler.matchWidthOrHeight = 0f; // Match width
                scaler.referencePixelsPerUnit = 100f;

                go.AddComponent<GraphicRaycaster>();
                return c;
            }

            // ----------------------------------------------------
            // Canvas_StaticHUD (Order 0)
            // ----------------------------------------------------
            var staticCanvas = CreateCanvas("Canvas_StaticHUD", 0);

            // BrandBar (y 60..180)
            var brandBar = CreateRectChild(staticCanvas.gameObject, "BrandBar");
            brandBar.anchorMin = new Vector2(0f, 1f);
            brandBar.anchorMax = new Vector2(1f, 1f);
            brandBar.pivot = new Vector2(0.5f, 1f);
            brandBar.anchoredPosition = new Vector2(0f, -60f);
            brandBar.sizeDelta = new Vector2(0f, 120f);

            // BallQuad: center (121, 116)
            var ballQuad = CreateImageChild(brandBar.gameObject, "BallQuad", spBallQuad);
            ballQuad.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            ballQuad.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            ballQuad.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            ballQuad.rectTransform.anchoredPosition = new Vector2(121f, -4f);
            ballQuad.rectTransform.sizeDelta = new Vector2(118f, 118f);
            ballQuad.raycastTarget = false;

            // LogoText: x 222..608 (center 415)
            var logoText = CreateImageChild(brandBar.gameObject, "LogoText", spBrandLogo);
            logoText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            logoText.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            logoText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            logoText.rectTransform.anchoredPosition = new Vector2(415f, -4f);
            logoText.rectTransform.sizeDelta = new Vector2(386f, 110f);
            logoText.raycastTarget = false;

            // BtnSettings: right offset -234
            var (btnSettings, rectSettings) = CreateButtonChild(brandBar.gameObject, "BtnSettings", spBtnSquare, Image.Type.Sliced, clickClip);
            rectSettings.anchorMin = new Vector2(1f, 0.5f);
            rectSettings.anchorMax = new Vector2(1f, 0.5f);
            rectSettings.pivot = new Vector2(0.5f, 0.5f);
            rectSettings.anchoredPosition = new Vector2(-234f, -4f);
            rectSettings.sizeDelta = new Vector2(108f, 108f);

            var iconGear = CreateImageChild(btnSettings.gameObject, "Icon", spIconGear);
            iconGear.rectTransform.sizeDelta = new Vector2(51f, 51f);
            iconGear.raycastTarget = false;

            // BtnStats: right offset -96
            var (btnStats, rectStats) = CreateButtonChild(brandBar.gameObject, "BtnStats", spBtnSquare, Image.Type.Sliced, clickClip);
            rectStats.anchorMin = new Vector2(1f, 0.5f);
            rectStats.anchorMax = new Vector2(1f, 0.5f);
            rectStats.pivot = new Vector2(0.5f, 0.5f);
            rectStats.anchoredPosition = new Vector2(-96f, -4f);
            rectStats.sizeDelta = new Vector2(108f, 108f);

            var iconChart = CreateImageChild(btnStats.gameObject, "Icon", spIconChart);
            iconChart.rectTransform.sizeDelta = new Vector2(44f, 46f);
            iconChart.raycastTarget = false;

            // CardRow (y 224..400)
            var cardRow = CreateRectChild(staticCanvas.gameObject, "CardRow");
            cardRow.anchorMin = new Vector2(0f, 1f);
            cardRow.anchorMax = new Vector2(1f, 1f);
            cardRow.pivot = new Vector2(0.5f, 1f);
            cardRow.anchoredPosition = new Vector2(0f, -224f);
            cardRow.sizeDelta = new Vector2(0f, 176f);

            // Card_Score (x 45..363, center 204)
            var cardScore = CreateImageChild(cardRow.gameObject, "Card_Score", spCardHud, Image.Type.Sliced);
            cardScore.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            cardScore.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            cardScore.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            cardScore.rectTransform.anchoredPosition = new Vector2(-336f, 0f);
            cardScore.rectTransform.sizeDelta = new Vector2(318f, 176f);
            cardScore.color = new Color(0.918f, 0.949f, 1f, 0.40f);
            cardScore.raycastTarget = false;

            var labelScore = CreateTmpChild(cardScore.gameObject, "Label", "SCORE", fontAsset, 26f, FontStyles.Bold, new Color(0.278f, 0.388f, 0.569f));
            labelScore.characterSpacing = 6f;
            labelScore.alignment = TextAlignmentOptions.Center;
            labelScore.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            labelScore.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            labelScore.rectTransform.pivot = new Vector2(0.5f, 1f);
            labelScore.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            labelScore.rectTransform.sizeDelta = new Vector2(280f, 28f);

            // Card_Next (x 381..699, center 540)
            var cardNext = CreateImageChild(cardRow.gameObject, "Card_Next", spCardHud, Image.Type.Sliced);
            cardNext.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            cardNext.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            cardNext.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            cardNext.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            cardNext.rectTransform.sizeDelta = new Vector2(318f, 176f);
            cardNext.color = new Color(0.918f, 0.949f, 1f, 0.40f);
            cardNext.raycastTarget = false;

            var labelNext = CreateTmpChild(cardNext.gameObject, "Label", "NEXT", fontAsset, 26f, FontStyles.Bold, new Color(0.278f, 0.388f, 0.569f));
            labelNext.characterSpacing = 6f;
            labelNext.alignment = TextAlignmentOptions.Center;
            labelNext.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            labelNext.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            labelNext.rectTransform.pivot = new Vector2(0.5f, 1f);
            labelNext.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            labelNext.rectTransform.sizeDelta = new Vector2(280f, 28f);

            var trayNext = CreateImageChild(cardNext.gameObject, "Tray_Next", spTrayNext, Image.Type.Sliced);
            trayNext.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            trayNext.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            trayNext.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            trayNext.rectTransform.anchoredPosition = new Vector2(0f, -22f);
            trayNext.rectTransform.sizeDelta = new Vector2(260f, 77f);
            trayNext.raycastTarget = false;

            // Card_Best (x 717..1035, center 876)
            var cardBest = CreateImageChild(cardRow.gameObject, "Card_Best", spCardHud, Image.Type.Sliced);
            cardBest.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            cardBest.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            cardBest.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            cardBest.rectTransform.anchoredPosition = new Vector2(336f, 0f);
            cardBest.rectTransform.sizeDelta = new Vector2(318f, 176f);
            cardBest.color = new Color(0.918f, 0.949f, 1f, 0.40f);
            cardBest.raycastTarget = false;

            var crownBest = CreateImageChild(cardBest.gameObject, "Crown", spCrown);
            crownBest.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            crownBest.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            crownBest.rectTransform.pivot = new Vector2(0.5f, 1f);
            crownBest.rectTransform.anchoredPosition = new Vector2(0f, -14f);
            crownBest.rectTransform.sizeDelta = new Vector2(40f, 36f);
            crownBest.raycastTarget = false;

            var labelBest = CreateTmpChild(cardBest.gameObject, "Label", "BEST", fontAsset, 26f, FontStyles.Bold, new Color(0.278f, 0.388f, 0.569f));
            labelBest.characterSpacing = 6f;
            labelBest.alignment = TextAlignmentOptions.Center;
            labelBest.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            labelBest.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            labelBest.rectTransform.pivot = new Vector2(0.5f, 1f);
            labelBest.rectTransform.anchoredPosition = new Vector2(0f, -54f);
            labelBest.rectTransform.sizeDelta = new Vector2(280f, 28f);

            // ActionBar (y 1480..1680)
            var actionBar = CreateRectChild(staticCanvas.gameObject, "ActionBar");
            actionBar.anchorMin = new Vector2(0f, 0f);
            actionBar.anchorMax = new Vector2(1f, 0f);
            actionBar.pivot = new Vector2(0.5f, 0f);
            actionBar.anchoredPosition = new Vector2(0f, 240f);
            actionBar.sizeDelta = new Vector2(0f, 200f);

            // BtnUndo (296x162)
            var (btnUndo, rectUndo) = CreateButtonChild(actionBar.gameObject, "BtnUndo", spBtnUndo, Image.Type.Sliced, clickClip);
            rectUndo.anchorMin = new Vector2(0f, 0.5f);
            rectUndo.anchorMax = new Vector2(0f, 0.5f);
            rectUndo.pivot = new Vector2(0.5f, 0.5f);
            rectUndo.anchoredPosition = new Vector2(219f, -5f);
            rectUndo.sizeDelta = new Vector2(296f, 162f);

            var iconUndo = CreateImageChild(btnUndo.gameObject, "Icon", spIconUndo);
            iconUndo.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            iconUndo.rectTransform.sizeDelta = new Vector2(76f, 61f);
            iconUndo.color = new Color(0.04f, 0.06f, 0.20f);
            iconUndo.raycastTarget = false;

            var labelUndo = CreateTmpChild(btnUndo.gameObject, "Label", "Undo", fontAsset, 27f, FontStyles.Bold, new Color(0.04f, 0.06f, 0.20f));
            labelUndo.rectTransform.anchoredPosition = new Vector2(0f, -42f);
            labelUndo.rectTransform.sizeDelta = new Vector2(200f, 32f);
            labelUndo.alignment = TextAlignmentOptions.Center;

            var undoView = btnUndo.gameObject.AddComponent<UndoButtonView>();

            // BtnHint (200x200)
            var (btnHint, rectHint) = CreateButtonChild(actionBar.gameObject, "BtnHint", spBtnDpad, Image.Type.Simple, clickClip);
            rectHint.anchorMin = new Vector2(0.5f, 0.5f);
            rectHint.anchorMax = new Vector2(0.5f, 0.5f);
            rectHint.pivot = new Vector2(0.5f, 0.5f);
            rectHint.anchoredPosition = new Vector2(0f, 10f);
            rectHint.sizeDelta = new Vector2(200f, 200f);

            // BtnNewGame (296x162)
            var (btnNewGame, rectNewGame) = CreateButtonChild(actionBar.gameObject, "BtnNewGame", spBtnNewGame, Image.Type.Sliced, clickClip);
            rectNewGame.anchorMin = new Vector2(1f, 0.5f);
            rectNewGame.anchorMax = new Vector2(1f, 0.5f);
            rectNewGame.pivot = new Vector2(0.5f, 0.5f);
            rectNewGame.anchoredPosition = new Vector2(-221f, -5f);
            rectNewGame.sizeDelta = new Vector2(296f, 162f);

            var iconNewGame = CreateImageChild(btnNewGame.gameObject, "Icon", spIconNewGame);
            iconNewGame.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            iconNewGame.rectTransform.sizeDelta = new Vector2(64f, 61f);
            iconNewGame.color = new Color(0.04f, 0.06f, 0.20f);
            iconNewGame.raycastTarget = false;

            var labelNewGame = CreateTmpChild(btnNewGame.gameObject, "Label", "New Game", fontAsset, 27f, FontStyles.Bold, new Color(0.04f, 0.06f, 0.20f));
            labelNewGame.rectTransform.anchoredPosition = new Vector2(0f, -42f);
            labelNewGame.rectTransform.sizeDelta = new Vector2(200f, 32f);
            labelNewGame.alignment = TextAlignmentOptions.Center;

            // ----------------------------------------------------
            // Canvas_DynamicHUD (Order 10)
            // ----------------------------------------------------
            var dynamicCanvas = CreateCanvas("Canvas_DynamicHUD", 10);

            // ScoreValue (center (204, 328) -> (-336, -328) from top-center)
            var scoreValTmp = CreateTmpChild(dynamicCanvas.gameObject, "ScoreValue", "01234", fontAsset, 68f, FontStyles.Bold, new Color(0.039f, 0.063f, 0.200f));
            scoreValTmp.alignment = TextAlignmentOptions.Center;
            scoreValTmp.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            scoreValTmp.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            scoreValTmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            scoreValTmp.rectTransform.anchoredPosition = new Vector2(-336f, -328f);
            scoreValTmp.rectTransform.sizeDelta = new Vector2(300f, 80f);
            var scoreRolling = scoreValTmp.gameObject.AddComponent<RollingNumber>();

            // BestValue (center (876, 347) -> (336, -347) from top-center)
            var bestValTmp = CreateTmpChild(dynamicCanvas.gameObject, "BestValue", "02356", fontAsset, 57f, FontStyles.Bold, new Color(0.039f, 0.063f, 0.200f));
            bestValTmp.alignment = TextAlignmentOptions.Center;
            bestValTmp.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            bestValTmp.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            bestValTmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bestValTmp.rectTransform.anchoredPosition = new Vector2(336f, -347f);
            bestValTmp.rectTransform.sizeDelta = new Vector2(300f, 80f);
            var bestRolling = bestValTmp.gameObject.AddComponent<RollingNumber>();

            // PreviewQueue (centers x 448 / 540 / 632, y 334)
            var previewQueueGo = new GameObject("PreviewQueue");
            previewQueueGo.transform.SetParent(dynamicCanvas.transform, false);
            var pqRect = previewQueueGo.AddComponent<RectTransform>();
            pqRect.anchorMin = new Vector2(0.5f, 1f);
            pqRect.anchorMax = new Vector2(0.5f, 1f);
            pqRect.pivot = new Vector2(0.5f, 0.5f);
            pqRect.anchoredPosition = new Vector2(0f, -334f);
            pqRect.sizeDelta = new Vector2(260f, 80f);

            var previewView = previewQueueGo.AddComponent<PreviewQueueView>();
            previewView.SetSprites(spBallRed, spBallOrange, spBallYellow, spBallGreen, spBallCyan, spBallPurple, spBallBlue);

            Image slot0 = CreateImageChild(previewQueueGo, "Slot_0", spBallRed);
            slot0.rectTransform.anchoredPosition = new Vector2(-92f, 0f);
            slot0.rectTransform.sizeDelta = new Vector2(79.6f, 79.6f);
            slot0.preserveAspect = true;

            Image slot1 = CreateImageChild(previewQueueGo, "Slot_1", spBallBlue);
            slot1.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            slot1.rectTransform.sizeDelta = new Vector2(79.6f, 79.6f);
            slot1.preserveAspect = true;

            Image slot2 = CreateImageChild(previewQueueGo, "Slot_2", spBallYellow);
            slot2.rectTransform.anchoredPosition = new Vector2(92f, 0f);
            slot2.rectTransform.sizeDelta = new Vector2(79.6f, 79.6f);
            slot2.preserveAspect = true;

            previewView.SetSlots(slot0, slot1, slot2);

            // UndoBadge (at Undo card top-right corner)
            var undoBadgeImg = CreateImageChild(dynamicCanvas.gameObject, "UndoBadge", spBadgeUndo);
            undoBadgeImg.rectTransform.anchorMin = new Vector2(0f, 0f);
            undoBadgeImg.rectTransform.anchorMax = new Vector2(0f, 0f);
            undoBadgeImg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            undoBadgeImg.rectTransform.anchoredPosition = new Vector2(353f, 409f);
            undoBadgeImg.rectTransform.sizeDelta = new Vector2(48f, 48f);

            var badgeTextTmp = CreateTmpChild(undoBadgeImg.gameObject, "BadgeNum", "3", fontAsset, 26f, FontStyles.Bold, Color.white);
            badgeTextTmp.alignment = TextAlignmentOptions.Center;
            badgeTextTmp.rectTransform.sizeDelta = new Vector2(48f, 48f);

            undoView.BindComponents(btnUndo, btnUndo.GetComponent<CanvasGroup>(), undoBadgeImg, badgeTextTmp);

            // ----------------------------------------------------
            // Canvas_Popups (Order 20)
            // ----------------------------------------------------
            var popupCanvas = CreateCanvas("Canvas_Popups", 20);

            // Scrim
            var scrimImg = CreateImageChild(popupCanvas.gameObject, "ModalScrim", spScrim);
            scrimImg.rectTransform.anchorMin = Vector2.zero;
            scrimImg.rectTransform.anchorMax = Vector2.one;
            scrimImg.rectTransform.offsetMin = Vector2.zero;
            scrimImg.rectTransform.offsetMax = Vector2.zero;
            scrimImg.color = new Color(0.039f, 0.078f, 0.157f, 0.61f);
            scrimImg.raycastTarget = true;
            var scrimGroup = scrimImg.gameObject.AddComponent<CanvasGroup>();
            scrimGroup.alpha = 0f;
            scrimGroup.blocksRaycasts = false;

            // GameOverPopup
            var gameOverPopup = BuildGameOverModal(popupCanvas.gameObject, fontAsset, spCardHud, spBtnUndo, spBtnNewGame, clickClip);
            // ConfirmPopup
            var confirmPopup = BuildPopupModal<ConfirmPopup>(popupCanvas.gameObject, "ConfirmPopup", "CONFIRM", fontAsset, spCardHud, spBtnUndo, spBtnNewGame, clickClip);
            // SettingsPopup
            var settingsPopup = BuildPopupModal<SettingsPopup>(popupCanvas.gameObject, "SettingsPopup", "SETTINGS", fontAsset, spCardHud, spBtnUndo, spBtnNewGame, clickClip);
            // StatisticsPopup
            var statsPopup = BuildPopupModal<StatisticsPopup>(popupCanvas.gameObject, "StatisticsPopup", "STATISTICS", fontAsset, spCardHud, spBtnUndo, spBtnNewGame, clickClip);

            // Wire UIRouter
            SetSerializedField(uiRouter, "m_StaticCanvas", staticCanvas);
            SetSerializedField(uiRouter, "m_DynamicCanvas", dynamicCanvas);
            SetSerializedField(uiRouter, "m_PopupCanvas", popupCanvas);
            SetSerializedField(uiRouter, "m_ScrimImage", scrimImg);
            SetSerializedField(uiRouter, "m_ScrimGroup", scrimGroup);
            SetSerializedField(uiRouter, "m_GameOverPopup", gameOverPopup);
            SetSerializedField(uiRouter, "m_ConfirmPopup", confirmPopup);
            SetSerializedField(uiRouter, "m_SettingsPopup", settingsPopup);
            SetSerializedField(uiRouter, "m_StatisticsPopup", statsPopup);

            // Wire HudPresenter
            SetSerializedField(hudPresenter, "m_ScoreNumber", scoreRolling);
            SetSerializedField(hudPresenter, "m_BestNumber", bestRolling);
            SetSerializedField(hudPresenter, "m_BestCrown", crownBest.rectTransform);
            SetSerializedField(hudPresenter, "m_ScoreCard", cardScore.gameObject);
            SetSerializedField(hudPresenter, "m_BestCard", cardBest.gameObject);
            SetSerializedField(hudPresenter, "m_PreviewView", previewView);
            SetSerializedField(hudPresenter, "m_UndoButtonView", undoView);
            SetSerializedField(hudPresenter, "m_HintButton", btnHint);
            SetSerializedField(hudPresenter, "m_NewGameButton", btnNewGame);
            SetSerializedField(hudPresenter, "m_SettingsButton", btnSettings);
            SetSerializedField(hudPresenter, "m_StatsButton", btnStats);
            SetSerializedField(hudPresenter, "m_UIRouter", uiRouter);
            SetSerializedField(hudPresenter, "m_Theme", theme);

            // Wire PresentationRoot
            var presRoot = UnityEngine.Object.FindAnyObjectByType<PresentationRoot>();
            if (presRoot != null)
            {
                SetSerializedField(presRoot, "m_UIRouter", uiRouter);
                SetSerializedField(presRoot, "m_HudPresenter", hudPresenter);
                SetSerializedField(presRoot, "m_UiTheme", theme);
                EditorUtility.SetDirty(presRoot);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[UIHierarchyBuilder] Successfully built and saved full LINE 98 UI Hierarchy in Game.unity!");
        }

        private static RectTransform CreateRectChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go.AddComponent<RectTransform>();
        }

        private static Image CreateImageChild(GameObject parent, string name, Sprite sprite, Image.Type type = Image.Type.Simple)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            return img;
        }

        private static (Button btn, RectTransform rect) CreateButtonChild(GameObject parent, string name, Sprite sprite, Image.Type type, AudioClip clickClip)
        {
            var img = CreateImageChild(parent, name, sprite, type);
            var btn = img.gameObject.AddComponent<Button>();
            var fx = img.gameObject.AddComponent<UiButtonFx>();
            fx.Initialize(null, clickClip);
            return (btn, img.rectTransform);
        }

        private static TextMeshProUGUI CreateTmpChild(GameObject parent, string name, string text, TMP_FontAsset font, float fontSize, FontStyles style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static T BuildPopupModal<T>(GameObject parent, string name, string title, TMP_FontAsset font, Sprite cardSprite, Sprite btnPrimarySprite, Sprite btnSecondarySprite, AudioClip clickClip) where T : PopupView
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            // Modal card container
            var container = CreateImageChild(go, "Container", cardSprite, Image.Type.Sliced);
            container.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            container.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            container.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            container.rectTransform.sizeDelta = new Vector2(880f, 560f);

            var titleTmp = CreateTmpChild(container.gameObject, "Title", title, font, 36f, FontStyles.Bold, new Color(0.04f, 0.06f, 0.20f));
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            titleTmp.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            titleTmp.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleTmp.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            titleTmp.rectTransform.sizeDelta = new Vector2(700f, 50f);

            var bodyTmp = CreateTmpChild(container.gameObject, "Body", "", font, 26f, FontStyles.Normal, new Color(0.28f, 0.39f, 0.57f));
            bodyTmp.alignment = TextAlignmentOptions.Center;
            bodyTmp.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            bodyTmp.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            bodyTmp.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            bodyTmp.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            bodyTmp.rectTransform.sizeDelta = new Vector2(700f, 150f);

            var (btnPrimary, rectPrimary) = CreateButtonChild(container.gameObject, "BtnPrimary", btnPrimarySprite, Image.Type.Sliced, clickClip);
            rectPrimary.anchorMin = new Vector2(0.5f, 0f);
            rectPrimary.anchorMax = new Vector2(0.5f, 0f);
            rectPrimary.pivot = new Vector2(0.5f, 0f);
            rectPrimary.anchoredPosition = new Vector2(-180f, 40f);
            rectPrimary.sizeDelta = new Vector2(320f, 100f);

            var (btnSecondary, rectSecondary) = CreateButtonChild(container.gameObject, "BtnSecondary", btnSecondarySprite, Image.Type.Sliced, clickClip);
            rectSecondary.anchorMin = new Vector2(0.5f, 0f);
            rectSecondary.anchorMax = new Vector2(0.5f, 0f);
            rectSecondary.pivot = new Vector2(0.5f, 0f);
            rectSecondary.anchoredPosition = new Vector2(180f, 40f);
            rectSecondary.sizeDelta = new Vector2(320f, 100f);

            var popupComp = go.AddComponent<T>();
            SetSerializedField(popupComp, "m_ModalContainer", container.rectTransform);
            SetSerializedField(popupComp, "m_CanvasGroup", cg);
            SetSerializedField(popupComp, "m_TitleText", titleTmp);
            SetSerializedField(popupComp, "m_BodyText", bodyTmp);
            SetSerializedField(popupComp, "m_PrimaryButton", btnPrimary);
            SetSerializedField(popupComp, "m_SecondaryButton", btnSecondary);

            go.SetActive(false);
            return popupComp;
        }

        private static GameOverPopup BuildGameOverModal(GameObject parent, TMP_FontAsset font, Sprite cardSprite, Sprite btnPrimarySprite, Sprite btnSecondarySprite, AudioClip clickClip)
        {
            var go = new GameObject("GameOverPopup", typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.blocksRaycasts = false;

            // Modal card container
            var container = CreateImageChild(go, "Container", cardSprite, Image.Type.Sliced);
            container.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            container.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            container.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            container.rectTransform.sizeDelta = new Vector2(880f, 560f);

            var titleTmp = CreateTmpChild(container.gameObject, "Title", "GAME OVER", font, 36f, FontStyles.Bold, new Color(0.04f, 0.06f, 0.20f));
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            titleTmp.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            titleTmp.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleTmp.rectTransform.anchoredPosition = new Vector2(0f, -40f);
            titleTmp.rectTransform.sizeDelta = new Vector2(700f, 50f);

            var (btnPrimary, rectPrimary) = CreateButtonChild(container.gameObject, "BtnPrimary", btnPrimarySprite, Image.Type.Sliced, clickClip);
            rectPrimary.anchorMin = new Vector2(0.5f, 0f);
            rectPrimary.anchorMax = new Vector2(0.5f, 0f);
            rectPrimary.pivot = new Vector2(0.5f, 0f);
            rectPrimary.anchoredPosition = new Vector2(-180f, 40f);
            rectPrimary.sizeDelta = new Vector2(320f, 100f);
            var lblPri = CreateTmpChild(btnPrimary.gameObject, "Label", "CONTINUE", font, 26f, FontStyles.Bold, new Color(0.04f, 0.06f, 0.20f));
            lblPri.alignment = TextAlignmentOptions.Center;
            lblPri.rectTransform.anchorMin = Vector2.zero;
            lblPri.rectTransform.anchorMax = Vector2.one;
            lblPri.rectTransform.offsetMin = Vector2.zero;
            lblPri.rectTransform.offsetMax = Vector2.zero;

            var (btnSecondary, rectSecondary) = CreateButtonChild(container.gameObject, "BtnSecondary", btnSecondarySprite, Image.Type.Sliced, clickClip);
            rectSecondary.anchorMin = new Vector2(0.5f, 0f);
            rectSecondary.anchorMax = new Vector2(0.5f, 0f);
            rectSecondary.pivot = new Vector2(0.5f, 0f);
            rectSecondary.anchoredPosition = new Vector2(180f, 40f);
            rectSecondary.sizeDelta = new Vector2(320f, 100f);
            var lblSec = CreateTmpChild(btnSecondary.gameObject, "Label", "NEW GAME", font, 26f, FontStyles.Bold, new Color(0.04f, 0.06f, 0.20f));
            lblSec.alignment = TextAlignmentOptions.Center;
            lblSec.rectTransform.anchorMin = Vector2.zero;
            lblSec.rectTransform.anchorMax = Vector2.one;
            lblSec.rectTransform.offsetMin = Vector2.zero;
            lblSec.rectTransform.offsetMax = Vector2.zero;

            // StatsRows
            var rowsGo = new GameObject("StatsRows", typeof(RectTransform));
            rowsGo.transform.SetParent(container.transform, false);
            var rowsRect = rowsGo.GetComponent<RectTransform>();
            rowsRect.anchorMin = new Vector2(0.5f, 0.5f);
            rowsRect.anchorMax = new Vector2(0.5f, 0.5f);
            rowsRect.pivot = new Vector2(0.5f, 0.5f);
            rowsRect.anchoredPosition = new Vector2(0f, 10f);
            rowsRect.sizeDelta = new Vector2(680f, 260f);

            Color labelColor = new Color(0.28f, 0.39f, 0.57f);
            Color valueColor = new Color(0.04f, 0.06f, 0.20f);

            (TextMeshProUGUI label, TextMeshProUGUI val) SetupRow(string rowName, string labelText, string initialVal, float yPos)
            {
                var rowGo = new GameObject(rowName, typeof(RectTransform));
                rowGo.transform.SetParent(rowsGo.transform, false);
                var rRect = rowGo.GetComponent<RectTransform>();
                rRect.anchorMin = new Vector2(0.5f, 0.5f);
                rRect.anchorMax = new Vector2(0.5f, 0.5f);
                rRect.pivot = new Vector2(0.5f, 0.5f);
                rRect.anchoredPosition = new Vector2(0f, yPos);
                rRect.sizeDelta = new Vector2(640f, 44f);

                var lTmp = CreateTmpChild(rowGo, "Label", labelText, font, 24f, FontStyles.Bold, labelColor);
                lTmp.alignment = TextAlignmentOptions.Left;
                lTmp.rectTransform.anchorMin = new Vector2(0f, 0f);
                lTmp.rectTransform.anchorMax = new Vector2(0.6f, 1f);
                lTmp.rectTransform.offsetMin = Vector2.zero;
                lTmp.rectTransform.offsetMax = Vector2.zero;

                var vTmp = CreateTmpChild(rowGo, "Value", initialVal, font, 28f, FontStyles.Bold, valueColor);
                vTmp.alignment = TextAlignmentOptions.Right;
                vTmp.rectTransform.anchorMin = new Vector2(0.6f, 0f);
                vTmp.rectTransform.anchorMax = new Vector2(1f, 1f);
                vTmp.rectTransform.offsetMin = Vector2.zero;
                vTmp.rectTransform.offsetMax = Vector2.zero;

                return (lTmp, vTmp);
            }

            var (_, finalScoreVal) = SetupRow("Row_FinalScore", "FINAL SCORE", "00000", 96f);
            var (_, bestScoreVal) = SetupRow("Row_BestScore", "BEST SCORE", "00000", 48f);
            var (_, linesClearedVal) = SetupRow("Row_LinesCleared", "LINES CLEARED", "0", 0f);
            var (_, longestLineVal) = SetupRow("Row_LongestLine", "LONGEST LINE", "0", -48f);
            var (_, totalMovesVal) = SetupRow("Row_TotalMoves", "TOTAL MOVES", "0", -96f);

            var popupComp = go.AddComponent<GameOverPopup>();
            SetSerializedField(popupComp, "m_ModalContainer", container.rectTransform);
            SetSerializedField(popupComp, "m_CanvasGroup", cg);
            SetSerializedField(popupComp, "m_TitleText", titleTmp);
            SetSerializedField(popupComp, "m_PrimaryButton", btnPrimary);
            SetSerializedField(popupComp, "m_SecondaryButton", btnSecondary);
            SetSerializedField(popupComp, "m_FinalScoreText", finalScoreVal);
            SetSerializedField(popupComp, "m_BestScoreText", bestScoreVal);
            SetSerializedField(popupComp, "m_LinesClearedText", linesClearedVal);
            SetSerializedField(popupComp, "m_LongestLineText", longestLineVal);
            SetSerializedField(popupComp, "m_TotalMovesText", totalMovesVal);
            SetSerializedField(popupComp, "m_ContinueButton", btnPrimary);
            SetSerializedField(popupComp, "m_NewGameButton", btnSecondary);

            go.SetActive(false);
            return popupComp;
        }

        private static void SetSerializedField(UnityEngine.Object target, string fieldName, object value)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (value is UnityEngine.Object obj)
                {
                    prop.objectReferenceValue = obj;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}

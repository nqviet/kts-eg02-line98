using System.Collections.Generic;
using Line98.Data;
using Line98.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Editor
{
    /// <summary>Authors the Crystal UI theme and settings popup from the imported crystal sprite sheet.</summary>
    public static class CrystalThemeAuthoring
    {
        // Sheet 2 carries true alpha; sheet 1 has a baked checkerboard behind every sprite.
        private const string SpriteSheetPath = "Assets/Art/Sprites/UI/Crystal/sprites_2__crystal.png";
        private const string MenuFontPath = "Assets/_Project/Content/Fonts/RobotoBold_Menu.asset";
        private const string PopupPath = "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Settings.prefab";
        private const string UiThemePath = "Assets/_Project/Content/Themes/UI/UiTheme_Crystal.asset";
        private const string BoardThemePath = "Assets/_Project/Content/Definitions/BoardTheme_Crystal.asset";
        private const string ClearEffectPath = "Assets/_Project/Content/Definitions/ClearEffect_Crystal.asset";
        private const string CrystalThemePath = "Assets/_Project/Content/Definitions/Theme_Crystal.asset";

        private static readonly Color SurfaceCard = ParseHex("#F7F5EE");
        private static readonly Color SurfaceGold = ParseHex("#FFF8E7");
        private static readonly Color InkNavy = ParseHex("#0A1B3F");
        private static readonly Color InkRow = ParseHex("#0E1F44");
        private static readonly Color InkMuted = ParseHex("#6C7E95");
        private static readonly Color Divider = new Color(0.871f, 0.851f, 0.796f, 0.6f);

        // Settings popup tokens sampled from setting_menu__crystal.png.
        private static readonly Color InkTitle = ParseHex("#0F2557");
        private static readonly Color InkSubtitle = ParseHex("#5F7391");
        private static readonly Color RuleNavy = new Color(0.290f, 0.357f, 0.494f, 0.55f);
        private static readonly Color BackdropWash = new Color(0.953f, 0.961f, 0.933f, 0.90f);
        private static readonly Color SectionCardTint = new Color(1f, 1f, 1f, 0.82f);
        private static readonly Color ViewButtonRim = ParseHex("#E0AA35");
        private static readonly Color ViewButtonFill = ParseHex("#FFF6DC");
        private static readonly Color ViewButtonInk = ParseHex("#8A5A00");

        [MenuItem("Line98/Authoring/Build Crystal Theme and Settings")]
        public static void BuildCrystalThemeAndSettings()
        {
            Dictionary<string, Sprite> sprites = LoadSprites();
            ValidateRequiredSprites(sprites);

            UiThemeSO uiTheme = CreateUiTheme(sprites);
            BoardThemeSO boardTheme = CreateBoardTheme(sprites);
            ClearEffectSO clearEffect = CreateClearEffect(sprites);
            UpdateCrystalBundle(sprites, uiTheme, boardTheme, clearEffect);
            BuildSettingsPopup(sprites);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CrystalThemeAuthoring] Crystal theme and settings popup authored successfully.");
        }

        /// <summary>Rebuilds only the settings popup (and its toggle sprite tokens) without touching tuned theme colors.</summary>
        [MenuItem("Line98/Authoring/Build Crystal Settings Popup")]
        public static void BuildCrystalSettingsPopup()
        {
            Dictionary<string, Sprite> sprites = LoadSprites();
            ValidateRequiredSprites(sprites);

            AssignToggleSprites(sprites);
            BuildSettingsPopup(sprites);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CrystalThemeAuthoring] Crystal settings popup authored successfully.");
        }

        private static Dictionary<string, Sprite> LoadSprites()
        {
            var sprites = new Dictionary<string, Sprite>();
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    sprites[sprite.name] = sprite;
                }
            }

            return sprites;
        }

        private static void ValidateRequiredSprites(Dictionary<string, Sprite> sprites)
        {
            string[] required =
            {
                "sp_ui_card_cream", "sp_ui_card_gold", "sp_btn_capsule_cream", "sp_btn_circle_cream", "sp_btn_view_gold",
                "sp_toggle_track_on", "sp_toggle_track_off", "sp_toggle_knob", "sp_icon_back_arrow",
                "sp_icon_music", "sp_icon_sfx", "sp_icon_vibrate", "sp_icon_sparkle_fx", "sp_icon_globe",
                "sp_icon_no_ads", "sp_icon_restore", "sp_icon_shield", "sp_icon_mail", "sp_icon_external_link",
                "sp_gem_red", "sp_gem_orange", "sp_gem_yellow", "sp_gem_green", "sp_gem_cyan", "sp_gem_purple", "sp_gem_magenta"
            };

            for (int i = 0; i < required.Length; i++)
            {
                if (!sprites.ContainsKey(required[i]))
                {
                    throw new System.InvalidOperationException($"Crystal sprite '{required[i]}' is missing. Run the sheet slicing pass before authoring UI.");
                }
            }
        }

        private static UiThemeSO CreateUiTheme(Dictionary<string, Sprite> sprites)
        {
            UiThemeSO theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(UiThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UiThemeSO>();
                AssetDatabase.CreateAsset(theme, UiThemePath);
            }

            SerializedObject serializedTheme = new SerializedObject(theme);
            SetString(serializedTheme, "m_ThemeId", "crystal");
            SetString(serializedTheme, "m_DisplayName", "Crystal");
            SetBool(serializedTheme, "m_UnlockedByDefault", true);
            SetObject(serializedTheme, "m_Thumbnail", sprites["sp_gem_cyan"]);

            SetColor(serializedTheme, "m_PanelCard", SurfaceCard);
            SetColor(serializedTheme, "m_PanelButton", SurfaceCard);
            SetColor(serializedTheme, "m_PanelTray", ParseHex("#EAE6DC"));
            SetColor(serializedTheme, "m_PanelCell", ParseHex("#EAE6DC"));
            SetColor(serializedTheme, "m_PanelFrame", ParseHex("#F7F5EE"));
            SetColor(serializedTheme, "m_PanelShadow", new Color(0.039f, 0.106f, 0.247f, 0.16f));
            SetColor(serializedTheme, "m_Scrim", new Color(0.039f, 0.106f, 0.247f, 0.50f));
            SetColor(serializedTheme, "m_InkValue", InkNavy);
            SetColor(serializedTheme, "m_InkLabel", InkMuted);
            SetColor(serializedTheme, "m_InkIcon", InkNavy);
            SetColor(serializedTheme, "m_BrandNavy", InkNavy);
            SetColor(serializedTheme, "m_BrandBlue", ParseHex("#162850"));
            SetColor(serializedTheme, "m_SurfaceCardGold", SurfaceGold);
            SetColor(serializedTheme, "m_SurfaceCardBorder", ParseHex("#E8E2D2"));
            SetColor(serializedTheme, "m_SurfaceGoldBorder", ParseHex("#EAC352"));
            SetColor(serializedTheme, "m_ToggleTrackActive", ParseHex("#27B85F"));
            SetColor(serializedTheme, "m_ToggleTrackInactive", ParseHex("#D0CEC7"));
            SetColor(serializedTheme, "m_ToggleThumb", Color.white);
            SetColor(serializedTheme, "m_InkRowTitle", InkRow);
            SetColor(serializedTheme, "m_InkSublabel", InkMuted);
            SetColor(serializedTheme, "m_DividerHairline", Divider);
            SetColor(serializedTheme, "m_ButtonGold", ParseHex("#F5C74E"));

            SetFloat(serializedTheme, "m_LabelFontSize", 22f);
            SetFloat(serializedTheme, "m_ButtonFontSize", 24f);
            SetFloat(serializedTheme, "m_TitleFontSize", 40f);
            SetFloat(serializedTheme, "m_BodyFontSize", 26f);

            SetObject(serializedTheme, "m_CardBackgroundSprite", sprites["sp_ui_card_cream"]);
            SetObject(serializedTheme, "m_CardGoldSprite", sprites["sp_ui_card_gold"]);
            SetObject(serializedTheme, "m_ButtonCapsuleSprite", sprites["sp_btn_capsule_cream"]);
            SetObject(serializedTheme, "m_ButtonCircleSprite", sprites["sp_btn_circle_cream"]);
            SetObject(serializedTheme, "m_ToggleTrackOnSprite", sprites["sp_toggle_track_on"]);
            SetObject(serializedTheme, "m_ToggleTrackOffSprite", sprites["sp_toggle_track_off"]);
            SetObject(serializedTheme, "m_ToggleThumbSprite", sprites["sp_toggle_knob"]);
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
            return theme;
        }

        private static void AssignToggleSprites(Dictionary<string, Sprite> sprites)
        {
            UiThemeSO theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(UiThemePath);
            if (theme == null) return;

            SerializedObject serializedTheme = new SerializedObject(theme);
            SetObject(serializedTheme, "m_ToggleTrackOnSprite", sprites["sp_toggle_track_on"]);
            SetObject(serializedTheme, "m_ToggleTrackOffSprite", sprites["sp_toggle_track_off"]);
            SetObject(serializedTheme, "m_ToggleThumbSprite", sprites["sp_toggle_knob"]);
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        private static BoardThemeSO CreateBoardTheme(Dictionary<string, Sprite> sprites)
        {
            BoardThemeSO boardTheme = AssetDatabase.LoadAssetAtPath<BoardThemeSO>(BoardThemePath);
            if (boardTheme == null)
            {
                boardTheme = ScriptableObject.CreateInstance<BoardThemeSO>();
                AssetDatabase.CreateAsset(boardTheme, BoardThemePath);
            }

            BoardThemeSO classicTheme = AssetDatabase.LoadAssetAtPath<BoardThemeSO>("Assets/_Project/Content/Definitions/BoardTheme_Classic.asset");
            SerializedObject serializedTheme = new SerializedObject(boardTheme);
            SetString(serializedTheme, "m_ThemeId", "crystal");
            SetString(serializedTheme, "m_DisplayName", "Crystal");
            SetBool(serializedTheme, "m_UnlockedByDefault", true);
            SetObject(serializedTheme, "m_Thumbnail", sprites["sp_gem_cyan"]);

            if (classicTheme != null)
            {
                SetObject(serializedTheme, "m_BoardFramePrefab", classicTheme.BoardFramePrefab);
                SetObject(serializedTheme, "m_BoardCellPrefab", classicTheme.BoardCellPrefab);
                SetFloat(serializedTheme, "m_RowPitchScale", classicTheme.RowPitchScale);
                SetObject(serializedTheme, "m_BoardCellMaterial", CloneMaterial(classicTheme.BoardCellMaterial, "Assets/Art/Materials/Themes/Crystal/M_Board_Crystal_Cell.mat", ParseHex("#EAE6DC")));
                SetObject(serializedTheme, "m_BoardFrameMaterial", CloneMaterial(classicTheme.BoardFrameMaterial, "Assets/Art/Materials/Themes/Crystal/M_Board_Crystal_Frame.mat", ParseHex("#F7F5EE")));
            }

            serializedTheme.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(boardTheme);
            return boardTheme;
        }

        private static ClearEffectSO CreateClearEffect(Dictionary<string, Sprite> sprites)
        {
            ClearEffectSO effect = AssetDatabase.LoadAssetAtPath<ClearEffectSO>(ClearEffectPath);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<ClearEffectSO>();
                AssetDatabase.CreateAsset(effect, ClearEffectPath);
            }

            SerializedObject serializedEffect = new SerializedObject(effect);
            SetString(serializedEffect, "m_ThemeId", "crystal");
            SetString(serializedEffect, "m_DisplayName", "Crystal");
            SetBool(serializedEffect, "m_UnlockedByDefault", true);
            SetObject(serializedEffect, "m_Thumbnail", sprites["sp_gem_cyan"]);
            SetColor(serializedEffect, "m_RibbonTint", new Color(0.9f, 0.95f, 1f, 0.9f));
            SetColor(serializedEffect, "m_RibbonTintBanner", new Color(1f, 0.84f, 0.4f, 0.95f));
            SetColor(serializedEffect, "m_BurstTint", ParseHex("#58D8E8"));
            SetString(serializedEffect, "m_ClearBurstVfxKey", "sfx_crystal_clear_chime");
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);
            return effect;
        }

        private static void UpdateCrystalBundle(Dictionary<string, Sprite> sprites, UiThemeSO uiTheme, BoardThemeSO boardTheme, ClearEffectSO clearEffect)
        {
            ThemeDefinitionSO crystalTheme = AssetDatabase.LoadAssetAtPath<ThemeDefinitionSO>(CrystalThemePath);
            if (crystalTheme == null)
            {
                crystalTheme = ScriptableObject.CreateInstance<ThemeDefinitionSO>();
                AssetDatabase.CreateAsset(crystalTheme, CrystalThemePath);
            }

            BallThemeSO ballTheme = AssetDatabase.LoadAssetAtPath<BallThemeSO>("Assets/_Project/Content/Definitions/BallTheme_Crystal.asset");
            ThemeDefinitionSO classicTheme = AssetDatabase.LoadAssetAtPath<ThemeDefinitionSO>("Assets/_Project/Content/Definitions/Theme_Classic.asset");
            SerializedObject serializedTheme = new SerializedObject(crystalTheme);
            SetString(serializedTheme, "m_ThemeId", "crystal");
            SetString(serializedTheme, "m_DisplayName", "Crystal");
            SetBool(serializedTheme, "m_UnlockedByDefault", true);
            SetObject(serializedTheme, "m_Thumbnail", sprites["sp_gem_cyan"]);
            SetObject(serializedTheme, "m_InheritsFrom", classicTheme);
            SetObject(serializedTheme, "m_BallTheme", ballTheme);
            SetObject(serializedTheme, "m_BoardTheme", boardTheme);
            SetObject(serializedTheme, "m_UiTheme", uiTheme);
            SetObject(serializedTheme, "m_ClearEffect", clearEffect);
            serializedTheme.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(crystalTheme);
        }

        private static void BuildSettingsPopup(Dictionary<string, Sprite> sprites)
        {
            GameObject popup = PrefabUtility.LoadPrefabContents(PopupPath);
            try
            {
                while (popup.transform.childCount > 0)
                {
                    Object.DestroyImmediate(popup.transform.GetChild(0).gameObject);
                }

                RectTransform root = popup.GetComponent<RectTransform>();
                root.anchorMin = new Vector2(0.5f, 0.5f);
                root.anchorMax = new Vector2(0.5f, 0.5f);
                root.pivot = new Vector2(0.5f, 0.5f);
                root.sizeDelta = new Vector2(980f, 1680f);
                root.anchoredPosition = Vector2.zero;

                CanvasGroup canvasGroup = GetOrAdd<CanvasGroup>(popup);
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                SettingsPopup settingsPopup = GetOrAdd<SettingsPopup>(popup);
                UiResponsiveModal responsiveModal = GetOrAdd<UiResponsiveModal>(popup);

                // A soft full-screen wash lets the studio backdrop read through while hiding the menu underneath.
                GameObject background = CreateImage("BackgroundDecor", popup.transform, null, BackdropWash);
                Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(-1200f, -1200f), new Vector2(1200f, 1200f));
                background.GetComponent<Image>().raycastTarget = false;

                GameObject safeArea = CreateObject("SafeArea", popup.transform);
                Stretch(safeArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

                GameObject scrollView = CreateObject("ScrollView", safeArea.transform, typeof(ScrollRect));
                Stretch(scrollView.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.scrollSensitivity = 28f;

                GameObject viewport = CreateImage("Viewport", scrollView.transform, null, Color.white);
                Stretch(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                viewport.GetComponent<Image>().raycastTarget = true;

                // The mask graphic must stay opaque: the stencil material honours alpha clip, so a
                // near-zero alpha would discard the stencil write and cull every scrolled child.
                // showMaskGraphic = false is what keeps the mask itself invisible.
                GetOrAdd<Mask>(viewport).showMaskGraphic = false;

                GameObject content = CreateObject("ContentContainer", viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                RectTransform contentRect = content.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.sizeDelta = Vector2.zero;
                VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
                contentLayout.padding = new RectOffset(46, 46, 4, 24);
                contentLayout.spacing = 12f;
                contentLayout.childAlignment = TextAnchor.UpperCenter;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = true;
                contentLayout.childForceExpandWidth = true;
                contentLayout.childForceExpandHeight = false;
                ContentSizeFitter contentFitter = content.GetComponent<ContentSizeFitter>();
                contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scrollRect.viewport = viewport.GetComponent<RectTransform>();
                scrollRect.content = contentRect;

                Button backButton = CreateHeader(content.transform, sprites);
                UiToggle musicToggle = null;
                UiToggle sfxToggle = null;
                UiToggle vibrationToggle = null;
                UiToggle reduceEffectsToggle = null;
                CreateAudioGroup(content.transform, sprites, out musicToggle, out sfxToggle, out vibrationToggle, out reduceEffectsToggle);
                UiSettingRow languageRow = CreateLanguageGroup(content.transform, sprites);
                Button removeAdsButton = null;
                UiSettingRow restoreRow = null;
                CreatePurchaseGroup(content.transform, sprites, out removeAdsButton, out restoreRow);
                UiSettingRow privacyRow = null;
                UiSettingRow contactRow = null;
                CreateSupportGroup(content.transform, sprites, out privacyRow, out contactRow);
                TMP_Text versionText = CreateFooter(content.transform, sprites);

                GameObject legacyThemesBridge = CreateObject("LegacyThemesBridge", popup.transform, typeof(Image), typeof(Button));
                legacyThemesBridge.SetActive(false);
                Button cosmeticsButton = legacyThemesBridge.GetComponent<Button>();

                SerializedObject popupSerialized = new SerializedObject(settingsPopup);
                SetObject(popupSerialized, "m_BackButton", backButton);
                SetObject(popupSerialized, "m_MusicToggle", musicToggle);
                SetObject(popupSerialized, "m_SfxToggle", sfxToggle);
                SetObject(popupSerialized, "m_VibrationToggle", vibrationToggle);
                SetObject(popupSerialized, "m_ReduceEffectsToggle", reduceEffectsToggle);
                SetObject(popupSerialized, "m_LanguageRow", languageRow);
                SetObject(popupSerialized, "m_RemoveAdsButton", removeAdsButton);
                SetObject(popupSerialized, "m_RestorePurchasesRow", restoreRow);
                SetObject(popupSerialized, "m_PrivacyPolicyRow", privacyRow);
                SetObject(popupSerialized, "m_ContactSupportRow", contactRow);
                SetObject(popupSerialized, "m_VersionText", versionText);
                SetObject(popupSerialized, "m_CosmeticsButton", cosmeticsButton);
                SetObject(popupSerialized, "m_ModalContainer", root);
                SetObject(popupSerialized, "m_CanvasGroup", canvasGroup);
                SetObject(popupSerialized, "m_TitleText", null);
                SetObject(popupSerialized, "m_BodyText", null);
                SetObject(popupSerialized, "m_PrimaryButton", null);
                SetObject(popupSerialized, "m_SecondaryButton", null);
                SetObject(popupSerialized, "m_CloseButton", null);
                popupSerialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject modalSerialized = new SerializedObject(responsiveModal);
                SetObject(modalSerialized, "m_Popup", settingsPopup);
                modalSerialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(popup, PopupPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(popup);
            }
        }

        private static Button CreateHeader(Transform parent, Dictionary<string, Sprite> sprites)
        {
            GameObject header = CreateObject("HeaderBar", parent, typeof(LayoutElement));
            header.GetComponent<LayoutElement>().preferredHeight = 112f;

            Button back = CreateIconButton("BtnBack", header.transform, sprites["sp_btn_circle_cream"], sprites["sp_icon_back_arrow"], 104f, 0.40f);
            RectTransform backRect = back.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0f, 0.5f);
            backRect.anchorMax = new Vector2(0f, 0.5f);
            backRect.pivot = new Vector2(0f, 0.5f);
            backRect.anchoredPosition = new Vector2(-18f, 0f);
            backRect.sizeDelta = new Vector2(104f, 104f);
            RectTransform backIcon = back.transform.Find("Icon").GetComponent<RectTransform>();
            backIcon.anchoredPosition = new Vector2(-3f, 0f);

            GameObject titleGroup = CreateObject("TitleGroup", header.transform, typeof(HorizontalLayoutGroup));
            RectTransform titleGroupRect = titleGroup.GetComponent<RectTransform>();
            titleGroupRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleGroupRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleGroupRect.pivot = new Vector2(0.5f, 0.5f);
            titleGroupRect.anchoredPosition = new Vector2(24f, 0f);
            titleGroupRect.sizeDelta = new Vector2(640f, 110f);
            HorizontalLayoutGroup layout = titleGroup.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateLayoutSpacer("RuleLeft", titleGroup.transform, 72f, 4f, RuleNavy);
            TMP_Text title = CreateText("TitleText", titleGroup.transform, "SETTINGS", 80f, InkTitle, TextAlignmentOptions.Center, FontStyles.Bold);
            AddLayoutElement(title.gameObject, -1f, 100f);
            CreateLayoutSpacer("RuleRight", titleGroup.transform, 72f, 4f, RuleNavy);
            return back;
        }

        private static void CreateAudioGroup(Transform parent, Dictionary<string, Sprite> sprites, out UiToggle music, out UiToggle sfx, out UiToggle vibration, out UiToggle reduceEffects)
        {
            GameObject body = CreateGroup(parent, "GroupCard_AudioFeedback", "AUDIO & FEEDBACK", sprites["sp_ui_card_cream"]);
            music = CreateToggleRow(body.transform, "Row_Music", sprites["sp_icon_music"], "MUSIC", true, sprites);
            sfx = CreateToggleRow(body.transform, "Row_Sfx", sprites["sp_icon_sfx"], "SOUND EFFECTS", true, sprites);
            vibration = CreateToggleRow(body.transform, "Row_Vibration", sprites["sp_icon_vibrate"], "VIBRATION", true, sprites);
            reduceEffects = CreateToggleRow(body.transform, "Row_ReduceEffects", sprites["sp_icon_sparkle_fx"], "REDUCE EFFECTS", false, sprites);
        }

        private static UiSettingRow CreateLanguageGroup(Transform parent, Dictionary<string, Sprite> sprites)
        {
            GameObject body = CreateGroup(parent, "GroupCard_Language", "LANGUAGE", sprites["sp_ui_card_cream"]);
            return CreateActionRow(body.transform, "Row_Language", sprites["sp_icon_globe"], "ENGLISH", "TIẾNG VIỆT AVAILABLE", sprites["sp_icon_back_arrow"], sprites, true);
        }

        private static void CreatePurchaseGroup(Transform parent, Dictionary<string, Sprite> sprites, out Button removeAds, out UiSettingRow restore)
        {
            GameObject body = CreateGroup(parent, "GroupCard_Purchases", "PURCHASES", sprites["sp_ui_card_cream"]);
            GameObject featured = CreateImage("FeaturedCard_RemoveAds", body.transform, sprites["sp_ui_card_gold"], Color.white, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            Image featuredImage = featured.GetComponent<Image>();
            featuredImage.type = Image.Type.Sliced;
            featuredImage.raycastTarget = false;
            AddThemeSpriteTarget(featured, UiThemeApplier.SpriteToken.CardGold, featuredImage);
            LayoutElement featuredLayout = featured.GetComponent<LayoutElement>();
            featuredLayout.preferredHeight = 136f;
            HorizontalLayoutGroup layout = featured.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(34, 22, 14, 14);
            layout.spacing = 26f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateImageWithLayout("BadgeNoAds", featured.transform, sprites["sp_icon_no_ads"], Color.white, 96f, 96f);
            GameObject textBlock = CreateObject("TextBlock", featured.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
            LayoutElement textLayout = textBlock.GetComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            VerticalLayoutGroup textGroup = textBlock.GetComponent<VerticalLayoutGroup>();
            textGroup.childAlignment = TextAnchor.MiddleLeft;
            textGroup.spacing = 0f;
            textGroup.childControlWidth = true;
            textGroup.childControlHeight = true;
            textGroup.childForceExpandWidth = true;
            textGroup.childForceExpandHeight = false;
            CreateTextWithLayout("Title", textBlock.transform, "REMOVE ADS", 38f, InkTitle, FontStyles.Bold, 46f);
            CreateTextWithLayout("Subtitle", textBlock.transform, "PLAY WITHOUT INTERRUPTIONS", 21f, InkSubtitle, FontStyles.Bold, 28f);
            removeAds = CreateOutlinedButton("BtnView", featured.transform, sprites["sp_btn_view_gold"], "VIEW", 196f, 82f);

            restore = CreateActionRow(body.transform, "Row_Restore", sprites["sp_icon_restore"], "RESTORE PURCHASES", null, sprites["sp_icon_back_arrow"], sprites, true);
        }

        private static void CreateSupportGroup(Transform parent, Dictionary<string, Sprite> sprites, out UiSettingRow privacy, out UiSettingRow contact)
        {
            GameObject body = CreateGroup(parent, "GroupCard_Support", "SUPPORT", sprites["sp_ui_card_cream"]);
            privacy = CreateActionRow(body.transform, "Row_Privacy", sprites["sp_icon_shield"], "PRIVACY POLICY", null, sprites["sp_icon_external_link"], sprites, false);
            contact = CreateActionRow(body.transform, "Row_Contact", sprites["sp_icon_mail"], "CONTACT SUPPORT", null, sprites["sp_icon_external_link"], sprites, false);
        }

        private static TMP_Text CreateFooter(Transform parent, Dictionary<string, Sprite> sprites)
        {
            GameObject footer = CreateObject("FooterSection", parent, typeof(VerticalLayoutGroup));
            VerticalLayoutGroup layout = footer.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 10, 0);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            GameObject versionRow = CreateObject("VersionRow", footer.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            versionRow.GetComponent<LayoutElement>().preferredHeight = 36f;
            HorizontalLayoutGroup versionLayout = versionRow.GetComponent<HorizontalLayoutGroup>();
            versionLayout.spacing = 26f;
            versionLayout.childAlignment = TextAnchor.MiddleCenter;
            versionLayout.childControlWidth = true;
            versionLayout.childControlHeight = true;
            versionLayout.childForceExpandWidth = false;
            versionLayout.childForceExpandHeight = false;
            CreateLayoutSpacer("RuleLeft", versionRow.transform, 124f, 3f, RuleNavy);
            TMP_Text version = CreateText("VersionText", versionRow.transform, "VERSION 1.0.0", 26f, InkSubtitle, TextAlignmentOptions.Center, FontStyles.Bold);
            AddLayoutElement(version.gameObject, -1f, 34f);
            CreateLayoutSpacer("RuleRight", versionRow.transform, 124f, 3f, RuleNavy);

            GameObject gems = CreateObject("GemColorBar", footer.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            gems.GetComponent<LayoutElement>().preferredHeight = 60f;
            HorizontalLayoutGroup gemLayout = gems.GetComponent<HorizontalLayoutGroup>();
            gemLayout.spacing = 6f;
            gemLayout.childAlignment = TextAnchor.MiddleCenter;
            gemLayout.childControlWidth = true;
            gemLayout.childControlHeight = true;
            gemLayout.childForceExpandWidth = false;
            gemLayout.childForceExpandHeight = false;
            string[] gemNames = { "sp_gem_red", "sp_gem_orange", "sp_gem_yellow", "sp_gem_green", "sp_gem_cyan", "sp_gem_purple", "sp_gem_magenta" };
            for (int i = 0; i < gemNames.Length; i++)
            {
                CreateImageWithLayout(gemNames[i], gems.transform, sprites[gemNames[i]], Color.white, 60f, 60f);
            }

            return version;
        }

        /// <summary>Creates a frosted section card whose header (title + hairline) sits inside the card, returning the card as the row parent.</summary>
        private static GameObject CreateGroup(Transform parent, string name, string header, Sprite cardSprite)
        {
            GameObject group = CreateImage(name, parent, cardSprite, SectionCardTint, typeof(VerticalLayoutGroup), typeof(UiCardGroup));
            Image cardImage = group.GetComponent<Image>();
            cardImage.type = Image.Type.Sliced;
            cardImage.raycastTarget = false;
            AddThemeSpriteTarget(group, UiThemeApplier.SpriteToken.CardBackground, cardImage);
            VerticalLayoutGroup groupLayout = group.GetComponent<VerticalLayoutGroup>();
            groupLayout.padding = new RectOffset(26, 26, 10, 22);
            groupLayout.spacing = 10f;
            groupLayout.childAlignment = TextAnchor.UpperCenter;
            groupLayout.childControlWidth = true;
            groupLayout.childControlHeight = true;
            groupLayout.childForceExpandWidth = true;
            groupLayout.childForceExpandHeight = false;

            GameObject headerRow = CreateObject("Header", group.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            headerRow.GetComponent<LayoutElement>().preferredHeight = 58f;
            HorizontalLayoutGroup headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 26f;
            headerLayout.padding = new RectOffset(16, 10, 0, 0);
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;
            TMP_Text headerText = CreateText("HeaderText", headerRow.transform, header, 38f, InkTitle, TextAlignmentOptions.Left, FontStyles.Bold);
            AddLayoutElement(headerText.gameObject, -1f, 50f);
            GameObject rule = CreateLayoutSpacer("Rule", headerRow.transform, 0f, 3f, RuleNavy);
            rule.GetComponent<LayoutElement>().flexibleWidth = 1f;

            UiCardGroup cardGroup = group.GetComponent<UiCardGroup>();
            SerializedObject cardSerialized = new SerializedObject(cardGroup);
            SetObject(cardSerialized, "m_HeaderText", headerText);
            SetObject(cardSerialized, "m_CardImage", cardImage);
            cardSerialized.ApplyModifiedPropertiesWithoutUndo();
            return group;
        }

        private static UiToggle CreateToggleRow(Transform parent, string name, Sprite icon, string title, bool isOn, Dictionary<string, Sprite> sprites)
        {
            UiSettingRow row = CreateRow(parent, name, icon, title, null, sprites, out Transform actionSlot);
            UiToggle toggle = CreateToggle(actionSlot, sprites, isOn);
            SerializedObject rowSerialized = new SerializedObject(row);
            SetObject(rowSerialized, "m_Toggle", toggle);
            rowSerialized.ApplyModifiedPropertiesWithoutUndo();
            return toggle;
        }

        private static UiSettingRow CreateActionRow(Transform parent, string name, Sprite icon, string title, string subtitle, Sprite actionIcon, Dictionary<string, Sprite> sprites, bool rotateChevron)
        {
            UiSettingRow row = CreateRow(parent, name, icon, title, subtitle, sprites, out Transform actionSlot);
            Button button = CreateIconButton("ActionButton", actionSlot, null, actionIcon, 72f, rotateChevron ? 0.56f : 0.66f);
            // Transparent hit area: the chevron/link glyph is the only visible part of the action.
            button.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0f);
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 0.5f);
            buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = Vector2.zero;
            buttonRect.sizeDelta = new Vector2(72f, 72f);
            Image iconImage = button.transform.Find("Icon").GetComponent<Image>();
            if (rotateChevron) iconImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
            SerializedObject rowSerialized = new SerializedObject(row);
            SetObject(rowSerialized, "m_ActionButton", button);
            SetObject(rowSerialized, "m_ActionIcon", iconImage);
            rowSerialized.ApplyModifiedPropertiesWithoutUndo();
            return row;
        }

        private static UiSettingRow CreateRow(Transform parent, string name, Sprite icon, string title, string subtitle, Dictionary<string, Sprite> sprites, out Transform actionSlot)
        {
            bool hasSubtitle = !string.IsNullOrEmpty(subtitle);
            GameObject rowObject = CreateImage(name, parent, sprites["sp_ui_card_cream"], Color.white, typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(UiSettingRow));
            Image rowImage = rowObject.GetComponent<Image>();
            rowImage.type = Image.Type.Sliced;
            rowImage.raycastTarget = false;
            AddThemeSpriteTarget(rowObject, UiThemeApplier.SpriteToken.CardBackground, rowImage);
            LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = hasSubtitle ? 110f : 86f;
            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(26, 20, 8, 8);
            layout.spacing = 30f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            Image iconImage = CreateImageWithLayout("Icon", rowObject.transform, icon, Color.white, 66f, 66f);
            GameObject textBlock = CreateObject("TextBlock", rowObject.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
            LayoutElement textLayout = textBlock.GetComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            VerticalLayoutGroup textLayoutGroup = textBlock.GetComponent<VerticalLayoutGroup>();
            textLayoutGroup.spacing = 0f;
            textLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            textLayoutGroup.childControlWidth = true;
            textLayoutGroup.childControlHeight = true;
            textLayoutGroup.childForceExpandWidth = true;
            textLayoutGroup.childForceExpandHeight = false;
            TMP_Text titleText = CreateTextWithLayout("Title", textBlock.transform, title, hasSubtitle ? 38f : 32f, InkTitle, FontStyles.Bold, hasSubtitle ? 48f : 42f);
            TMP_Text subtitleText = CreateTextWithLayout("Subtitle", textBlock.transform, subtitle ?? string.Empty, 22f, InkSubtitle, FontStyles.Bold, 30f);
            subtitleText.gameObject.SetActive(hasSubtitle);

            GameObject action = CreateObject("ActionSlot", rowObject.transform, typeof(LayoutElement));
            LayoutElement actionLayout = action.GetComponent<LayoutElement>();
            actionLayout.preferredWidth = 128f;
            actionLayout.preferredHeight = 72f;
            actionSlot = action.transform;

            UiSettingRow row = rowObject.GetComponent<UiSettingRow>();
            SerializedObject rowSerialized = new SerializedObject(row);
            SetObject(rowSerialized, "m_IconImage", iconImage);
            SetObject(rowSerialized, "m_TitleText", titleText);
            SetObject(rowSerialized, "m_SubtitleText", subtitleText);
            rowSerialized.ApplyModifiedPropertiesWithoutUndo();
            return row;
        }

        private static UiToggle CreateToggle(Transform parent, Dictionary<string, Sprite> sprites, bool isOn)
        {
            GameObject toggleObject = CreateImage("Toggle", parent, isOn ? sprites["sp_toggle_track_on"] : sprites["sp_toggle_track_off"], Color.white, typeof(UiToggle));
            Image track = toggleObject.GetComponent<Image>();
            track.type = Image.Type.Simple;
            track.preserveAspect = true;
            track.raycastTarget = true;
            RectTransform trackRect = toggleObject.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(1f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.pivot = new Vector2(1f, 0.5f);
            trackRect.anchoredPosition = Vector2.zero;
            trackRect.sizeDelta = new Vector2(128f, 61f);

            // The Crystal track sprites already paint their knob, so the animated thumb stays as an
            // invisible transform to keep UiToggle's contract without drawing a second knob.
            GameObject thumbObject = CreateImage("Thumb", toggleObject.transform, sprites["sp_toggle_knob"], new Color(1f, 1f, 1f, 0f));
            Image thumb = thumbObject.GetComponent<Image>();
            thumb.raycastTarget = false;
            RectTransform thumbRect = thumbObject.GetComponent<RectTransform>();
            thumbRect.anchorMin = new Vector2(0.5f, 0.5f);
            thumbRect.anchorMax = new Vector2(0.5f, 0.5f);
            thumbRect.pivot = new Vector2(0.5f, 0.5f);
            thumbRect.sizeDelta = new Vector2(52f, 52f);
            thumbRect.anchoredPosition = isOn ? new Vector2(33f, 0f) : new Vector2(-33f, 0f);

            UiToggle toggle = toggleObject.GetComponent<UiToggle>();
            SerializedObject toggleSerialized = new SerializedObject(toggle);
            SetObject(toggleSerialized, "m_TrackImage", track);
            SetObject(toggleSerialized, "m_ThumbImage", thumb);
            SetObject(toggleSerialized, "m_ThumbTransform", thumbRect);
            SetObject(toggleSerialized, "m_TrackOnSprite", sprites["sp_toggle_track_on"]);
            SetObject(toggleSerialized, "m_TrackOffSprite", sprites["sp_toggle_track_off"]);
            SetObject(toggleSerialized, "m_ThumbSprite", sprites["sp_toggle_knob"]);
            SetFloat(toggleSerialized, "m_ThumbTravelDistance", 66f);
            SetFloat(toggleSerialized, "m_TransitionDuration", 0.22f);
            SetBool(toggleSerialized, "m_IsOn", isOn);
            toggleSerialized.ApplyModifiedPropertiesWithoutUndo();
            return toggle;
        }

        /// <summary>Gold-rimmed capsule: a tinted rim capsule with an inset cream capsule and label.</summary>
        private static Button CreateOutlinedButton(string name, Transform parent, Sprite capsule, string label, float width, float height)
        {
            GameObject buttonObject = CreateImage(name, parent, capsule, ViewButtonRim, typeof(Button), typeof(LayoutElement), typeof(UiButtonFx));
            Image rim = buttonObject.GetComponent<Image>();
            rim.type = Image.Type.Sliced;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = rim;
            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.flexibleWidth = 0f;

            GameObject fill = CreateImage("Fill", buttonObject.transform, capsule, ViewButtonFill);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.type = Image.Type.Sliced;
            fillImage.raycastTarget = false;
            Stretch(fillImage.rectTransform, Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));

            TMP_Text text = CreateText("Label", buttonObject.transform, label, 38f, ViewButtonInk, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static Button CreateIconButton(string name, Transform parent, Sprite backgroundSprite, Sprite iconSprite, float size, float iconScale = 0.56f)
        {
            GameObject buttonObject = CreateImage(name, parent, backgroundSprite, Color.white, typeof(Button), typeof(LayoutElement), typeof(UiButtonFx));
            Image background = buttonObject.GetComponent<Image>();
            if (backgroundSprite != null) background.preserveAspect = true;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            GameObject icon = CreateImage("Icon", buttonObject.transform, iconSprite, Color.white);
            Image iconImage = icon.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(size * iconScale, size * iconScale);
            return button;
        }

        private static Image CreateImageWithLayout(string name, Transform parent, Sprite sprite, Color color, float width, float height)
        {
            GameObject imageObject = CreateImage(name, parent, sprite, color, typeof(LayoutElement));
            LayoutElement layout = imageObject.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            Image image = imageObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static TMP_Text CreateTextWithLayout(string name, Transform parent, string value, float size, Color color, FontStyles style, float height)
        {
            TMP_Text text = CreateText(name, parent, value, size, color, TextAlignmentOptions.Left, style);
            AddLayoutElement(text.gameObject, -1f, height, 1f);
            return text;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment, FontStyles style)
        {
            GameObject textObject = CreateObject(name, parent, typeof(TextMeshProUGUI));
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            TMP_FontAsset menuFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MenuFontPath);
            text.font = menuFont != null ? menuFont : TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject CreateLayoutSpacer(string name, Transform parent, float width, float height, Color color)
        {
            GameObject spacer = CreateImage(name, parent, null, color, typeof(LayoutElement));
            spacer.GetComponent<Image>().raycastTarget = false;
            LayoutElement layout = spacer.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            return spacer;
        }

        private static void AddThemeSpriteTarget(GameObject target, UiThemeApplier.SpriteToken token, Image image)
        {
            UiThemeApplier applier = target.AddComponent<UiThemeApplier>();
            applier.ConfigureSprite(token, new[] { image });
        }

        private static GameObject CreateImage(string name, Transform parent, Sprite sprite, Color color, params System.Type[] extraTypes)
        {
            var types = new List<System.Type> { typeof(RectTransform), typeof(CanvasRenderer), typeof(Image) };
            if (extraTypes != null) types.AddRange(extraTypes);
            GameObject imageObject = new GameObject(name, types.ToArray());
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return imageObject;
        }

        private static GameObject CreateObject(string name, Transform parent, params System.Type[] extraTypes)
        {
            var types = new List<System.Type> { typeof(RectTransform) };
            if (extraTypes != null) types.AddRange(extraTypes);
            GameObject gameObject = new GameObject(name, types.ToArray());
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }

        private static void AddLayoutElement(GameObject target, float width, float height, float flexibleWidth = 0f)
        {
            LayoutElement layout = GetOrAdd<LayoutElement>(target);
            if (width >= 0f) layout.preferredWidth = width;
            if (height >= 0f) layout.preferredHeight = height;
            layout.flexibleWidth = flexibleWidth;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Material CloneMaterial(Material source, string path, Color tint)
        {
            if (source == null) return null;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Color ParseHex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' was not found.");
            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' was not found.");
            property.stringValue = value;
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' was not found.");
            property.boolValue = value;
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' was not found.");
            property.floatValue = value;
        }

        private static void SetColor(SerializedObject serializedObject, string propertyName, Color value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null) throw new System.InvalidOperationException($"Serialized property '{propertyName}' was not found.");
            property.colorValue = value;
        }
    }
}

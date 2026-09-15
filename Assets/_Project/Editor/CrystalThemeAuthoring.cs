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
        private const string SpriteSheetPath = "Assets/Art/Sprites/UI/Crystal/sprites_1__crystal.png";
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
                "sp_ui_card_cream", "sp_ui_card_gold", "sp_btn_capsule_cream", "sp_btn_circle_cream",
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

                GameObject background = CreateImage("BackgroundDecor", popup.transform, null, new Color(0.88f, 0.94f, 0.90f, 1f));
                Stretch(background.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                background.GetComponent<Image>().raycastTarget = false;
                CreateDecorLeaf("DecorLeafLeft", background.transform, sprites["sp_icon_leaf"], new Vector2(55f, 280f), new Vector2(290f, 290f), -28f);
                CreateDecorLeaf("DecorLeafRight", background.transform, sprites["sp_icon_leaf"], new Vector2(-55f, -370f), new Vector2(270f, 270f), 152f);

                GameObject safeArea = CreateObject("SafeArea", popup.transform);
                Stretch(safeArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(24f, 24f), new Vector2(-24f, -24f));

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
                contentLayout.padding = new RectOffset(20, 20, 24, 52);
                contentLayout.spacing = 20f;
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
            GameObject header = CreateObject("HeaderBar", parent, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            LayoutElement headerLayout = header.GetComponent<LayoutElement>();
            headerLayout.preferredHeight = 92f;
            HorizontalLayoutGroup layout = header.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            Button back = CreateIconButton("BtnBack", header.transform, sprites["sp_btn_circle_cream"], sprites["sp_icon_back_arrow"], 76f);
            CreateLayoutSpacer("RuleLeft", header.transform, 90f, 3f, Divider);
            TMP_Text title = CreateText("TitleText", header.transform, "SETTINGS", 40f, InkNavy, TextAlignmentOptions.Center, FontStyles.Bold);
            AddLayoutElement(title.gameObject, 380f, 68f, 1f);
            GameObject leaf = CreateImage("LeafAccent", title.transform, sprites["sp_icon_leaf"], Color.white);
            RectTransform leafRect = leaf.GetComponent<RectTransform>();
            leafRect.anchorMin = new Vector2(0.5f, 1f);
            leafRect.anchorMax = new Vector2(0.5f, 1f);
            leafRect.pivot = new Vector2(0.5f, 0.5f);
            leafRect.anchoredPosition = new Vector2(-52f, 6f);
            leafRect.sizeDelta = new Vector2(28f, 28f);
            leaf.GetComponent<Image>().raycastTarget = false;
            CreateLayoutSpacer("RuleRight", header.transform, 90f, 3f, Divider);
            CreateLayoutSpacer("HeaderSpacer", header.transform, 76f, 1f, Color.clear);
            return back;
        }

        private static void CreateAudioGroup(Transform parent, Dictionary<string, Sprite> sprites, out UiToggle music, out UiToggle sfx, out UiToggle vibration, out UiToggle reduceEffects)
        {
            GameObject body = CreateGroup(parent, "GroupCard_AudioFeedback", "AUDIO & FEEDBACK", sprites["sp_ui_card_cream"], 566f, out _);
            music = CreateToggleRow(body.transform, "Row_Music", sprites["sp_icon_music"], "MUSIC", true, sprites);
            sfx = CreateToggleRow(body.transform, "Row_Sfx", sprites["sp_icon_sfx"], "SOUND EFFECTS", true, sprites);
            vibration = CreateToggleRow(body.transform, "Row_Vibration", sprites["sp_icon_vibrate"], "VIBRATION", true, sprites);
            reduceEffects = CreateToggleRow(body.transform, "Row_ReduceEffects", sprites["sp_icon_sparkle_fx"], "REDUCE EFFECTS", false, sprites);
        }

        private static UiSettingRow CreateLanguageGroup(Transform parent, Dictionary<string, Sprite> sprites)
        {
            GameObject body = CreateGroup(parent, "GroupCard_Language", "LANGUAGE", sprites["sp_ui_card_cream"], 222f, out _);
            return CreateActionRow(body.transform, "Row_Language", sprites["sp_icon_globe"], "ENGLISH", "TIẾNG VIỆT AVAILABLE", sprites["sp_icon_back_arrow"], sprites, true);
        }

        private static void CreatePurchaseGroup(Transform parent, Dictionary<string, Sprite> sprites, out Button removeAds, out UiSettingRow restore)
        {
            GameObject body = CreateGroup(parent, "GroupCard_Purchases", "PURCHASES", sprites["sp_ui_card_cream"], 346f, out _);
            GameObject featured = CreateImage("FeaturedCard_RemoveAds", body.transform, sprites["sp_ui_card_gold"], Color.white, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            Image featuredImage = featured.GetComponent<Image>();
            featuredImage.type = Image.Type.Sliced;
            AddThemeSpriteTarget(featured, UiThemeApplier.SpriteToken.CardGold, featuredImage);
            LayoutElement featuredLayout = featured.GetComponent<LayoutElement>();
            featuredLayout.preferredHeight = 130f;
            HorizontalLayoutGroup layout = featured.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(22, 18, 12, 12);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            CreateImageWithLayout("BadgeNoAds", featured.transform, sprites["sp_icon_no_ads"], Color.white, 86f, 86f);
            GameObject textBlock = CreateObject("TextBlock", featured.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
            LayoutElement textLayout = textBlock.GetComponent<LayoutElement>();
            textLayout.preferredWidth = 348f;
            textLayout.flexibleWidth = 1f;
            VerticalLayoutGroup textGroup = textBlock.GetComponent<VerticalLayoutGroup>();
            textGroup.childAlignment = TextAnchor.MiddleLeft;
            textGroup.spacing = 2f;
            textGroup.childControlWidth = true;
            textGroup.childControlHeight = true;
            textGroup.childForceExpandHeight = false;
            CreateTextWithLayout("Title", textBlock.transform, "REMOVE ADS", 28f, InkNavy, FontStyles.Bold, 34f);
            CreateTextWithLayout("Subtitle", textBlock.transform, "PLAY WITHOUT INTERRUPTIONS", 16f, InkMuted, FontStyles.Bold, 24f);
            removeAds = CreateTextButton("BtnView", featured.transform, sprites["sp_btn_view_gold"], "VIEW", 128f, 64f, ParseHex("#8A5700"));

            restore = CreateActionRow(body.transform, "Row_Restore", sprites["sp_icon_restore"], "RESTORE PURCHASES", null, sprites["sp_icon_back_arrow"], sprites, true);
        }

        private static void CreateSupportGroup(Transform parent, Dictionary<string, Sprite> sprites, out UiSettingRow privacy, out UiSettingRow contact)
        {
            GameObject body = CreateGroup(parent, "GroupCard_Support", "SUPPORT", sprites["sp_ui_card_cream"], 326f, out _);
            privacy = CreateActionRow(body.transform, "Row_Privacy", sprites["sp_icon_shield"], "PRIVACY POLICY", null, sprites["sp_icon_external_link"], sprites, false);
            contact = CreateActionRow(body.transform, "Row_Contact", sprites["sp_icon_mail"], "CONTACT SUPPORT", null, sprites["sp_icon_external_link"], sprites, false);
        }

        private static TMP_Text CreateFooter(Transform parent, Dictionary<string, Sprite> sprites)
        {
            GameObject footer = CreateObject("FooterSection", parent, typeof(VerticalLayoutGroup), typeof(LayoutElement));
            LayoutElement footerLayout = footer.GetComponent<LayoutElement>();
            footerLayout.preferredHeight = 132f;
            VerticalLayoutGroup layout = footer.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            GameObject versionRow = CreateObject("VersionRow", footer.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            versionRow.GetComponent<LayoutElement>().preferredHeight = 34f;
            HorizontalLayoutGroup versionLayout = versionRow.GetComponent<HorizontalLayoutGroup>();
            versionLayout.spacing = 14f;
            versionLayout.childAlignment = TextAnchor.MiddleCenter;
            versionLayout.childControlWidth = true;
            versionLayout.childControlHeight = true;
            versionLayout.childForceExpandWidth = false;
            CreateLayoutSpacer("RuleLeft", versionRow.transform, 120f, 2f, InkMuted);
            TMP_Text version = CreateText("VersionText", versionRow.transform, "VERSION 1.0.0", 18f, InkMuted, TextAlignmentOptions.Center, FontStyles.Bold);
            AddLayoutElement(version.gameObject, 190f, 30f);
            CreateLayoutSpacer("RuleRight", versionRow.transform, 120f, 2f, InkMuted);

            GameObject gems = CreateObject("GemColorBar", footer.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            gems.GetComponent<LayoutElement>().preferredHeight = 70f;
            HorizontalLayoutGroup gemLayout = gems.GetComponent<HorizontalLayoutGroup>();
            gemLayout.spacing = 10f;
            gemLayout.childAlignment = TextAnchor.MiddleCenter;
            gemLayout.childControlWidth = true;
            gemLayout.childControlHeight = true;
            gemLayout.childForceExpandWidth = false;
            string[] gemNames = { "sp_gem_red", "sp_gem_orange", "sp_gem_yellow", "sp_gem_green", "sp_gem_cyan", "sp_gem_purple", "sp_gem_magenta" };
            for (int i = 0; i < gemNames.Length; i++)
            {
                CreateImageWithLayout(gemNames[i], gems.transform, sprites[gemNames[i]], Color.white, 56f, 56f);
            }

            return version;
        }

        private static GameObject CreateGroup(Transform parent, string name, string header, Sprite cardSprite, float height, out UiCardGroup cardGroup)
        {
            GameObject group = CreateObject(name, parent, typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(UiCardGroup));
            group.GetComponent<LayoutElement>().preferredHeight = height;
            VerticalLayoutGroup groupLayout = group.GetComponent<VerticalLayoutGroup>();
            groupLayout.spacing = 8f;
            groupLayout.childAlignment = TextAnchor.UpperCenter;
            groupLayout.childControlWidth = true;
            groupLayout.childControlHeight = true;
            groupLayout.childForceExpandHeight = false;

            GameObject headerRow = CreateObject("Header", group.transform, typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            headerRow.GetComponent<LayoutElement>().preferredHeight = 42f;
            HorizontalLayoutGroup headerLayout = headerRow.GetComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 14f;
            headerLayout.padding = new RectOffset(18, 18, 0, 0);
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;
            TMP_Text headerText = CreateText("HeaderText", headerRow.transform, header, 22f, InkNavy, TextAlignmentOptions.Left, FontStyles.Bold);
            AddLayoutElement(headerText.gameObject, 285f, 40f);
            CreateLayoutSpacer("Rule", headerRow.transform, 330f, 2f, InkMuted);

            GameObject body = CreateImage("CardBody", group.transform, cardSprite, Color.white, typeof(VerticalLayoutGroup), typeof(LayoutElement));
            Image bodyImage = body.GetComponent<Image>();
            bodyImage.type = Image.Type.Sliced;
            AddThemeSpriteTarget(body, UiThemeApplier.SpriteToken.CardBackground, bodyImage);
            LayoutElement bodyLayout = body.GetComponent<LayoutElement>();
            bodyLayout.flexibleHeight = 1f;
            VerticalLayoutGroup bodyGroup = body.GetComponent<VerticalLayoutGroup>();
            bodyGroup.padding = new RectOffset(16, 16, 16, 16);
            bodyGroup.spacing = 10f;
            bodyGroup.childAlignment = TextAnchor.UpperCenter;
            bodyGroup.childControlWidth = true;
            bodyGroup.childControlHeight = true;
            bodyGroup.childForceExpandWidth = true;
            bodyGroup.childForceExpandHeight = false;

            cardGroup = group.GetComponent<UiCardGroup>();
            SerializedObject cardSerialized = new SerializedObject(cardGroup);
            SetObject(cardSerialized, "m_HeaderText", headerText);
            SetObject(cardSerialized, "m_CardImage", bodyImage);
            cardSerialized.ApplyModifiedPropertiesWithoutUndo();
            return body;
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
            Button button = CreateIconButton("ActionButton", actionSlot, null, actionIcon, 62f);
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
            GameObject rowObject = CreateImage(name, parent, sprites["sp_ui_card_cream"], Color.white, typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(UiSettingRow));
            Image rowImage = rowObject.GetComponent<Image>();
            rowImage.type = Image.Type.Sliced;
            AddThemeSpriteTarget(rowObject, UiThemeApplier.SpriteToken.CardBackground, rowImage);
            LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = subtitle == null ? 94f : 114f;
            HorizontalLayoutGroup layout = rowObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 12, 12);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            Image iconImage = CreateImageWithLayout("Icon", rowObject.transform, icon, Color.white, 62f, 62f);
            GameObject textBlock = CreateObject("TextBlock", rowObject.transform, typeof(VerticalLayoutGroup), typeof(LayoutElement));
            LayoutElement textLayout = textBlock.GetComponent<LayoutElement>();
            textLayout.flexibleWidth = 1f;
            VerticalLayoutGroup textLayoutGroup = textBlock.GetComponent<VerticalLayoutGroup>();
            textLayoutGroup.spacing = 2f;
            textLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            textLayoutGroup.childControlWidth = true;
            textLayoutGroup.childControlHeight = true;
            textLayoutGroup.childForceExpandHeight = false;
            TMP_Text titleText = CreateTextWithLayout("Title", textBlock.transform, title, 25f, InkRow, FontStyles.Bold, 36f);
            TMP_Text subtitleText = CreateTextWithLayout("Subtitle", textBlock.transform, subtitle ?? string.Empty, 16f, InkMuted, FontStyles.Bold, 24f);
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));

            GameObject action = CreateObject("ActionSlot", rowObject.transform, typeof(LayoutElement));
            LayoutElement actionLayout = action.GetComponent<LayoutElement>();
            actionLayout.preferredWidth = 124f;
            actionLayout.preferredHeight = 70f;
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
            GameObject toggleObject = CreateImage("Toggle", parent, sprites["sp_toggle_track_on"], Color.white, typeof(LayoutElement), typeof(UiToggle));
            Image track = toggleObject.GetComponent<Image>();
            track.type = Image.Type.Sliced;
            track.raycastTarget = true;
            LayoutElement layout = toggleObject.GetComponent<LayoutElement>();
            layout.preferredWidth = 108f;
            layout.preferredHeight = 62f;

            GameObject thumbObject = CreateImage("Thumb", toggleObject.transform, sprites["sp_toggle_knob"], Color.white);
            Image thumb = thumbObject.GetComponent<Image>();
            thumb.raycastTarget = false;
            RectTransform thumbRect = thumbObject.GetComponent<RectTransform>();
            thumbRect.anchorMin = new Vector2(0.5f, 0.5f);
            thumbRect.anchorMax = new Vector2(0.5f, 0.5f);
            thumbRect.pivot = new Vector2(0.5f, 0.5f);
            thumbRect.sizeDelta = new Vector2(52f, 52f);
            thumbRect.anchoredPosition = isOn ? new Vector2(22f, 0f) : new Vector2(-22f, 0f);

            UiToggle toggle = toggleObject.GetComponent<UiToggle>();
            SerializedObject toggleSerialized = new SerializedObject(toggle);
            SetObject(toggleSerialized, "m_TrackImage", track);
            SetObject(toggleSerialized, "m_ThumbImage", thumb);
            SetObject(toggleSerialized, "m_ThumbTransform", thumbRect);
            SetObject(toggleSerialized, "m_TrackOnSprite", sprites["sp_toggle_track_on"]);
            SetObject(toggleSerialized, "m_TrackOffSprite", sprites["sp_toggle_track_off"]);
            SetObject(toggleSerialized, "m_ThumbSprite", sprites["sp_toggle_knob"]);
            SetFloat(toggleSerialized, "m_ThumbTravelDistance", 44f);
            SetFloat(toggleSerialized, "m_TransitionDuration", 0.22f);
            SetBool(toggleSerialized, "m_IsOn", isOn);
            toggleSerialized.ApplyModifiedPropertiesWithoutUndo();
            return toggle;
        }

        private static Button CreateTextButton(string name, Transform parent, Sprite sprite, string label, float width, float height, Color labelColor)
        {
            GameObject buttonObject = CreateImage(name, parent, sprite, Color.white, typeof(Button), typeof(LayoutElement), typeof(UiButtonFx));
            Image image = buttonObject.GetComponent<Image>();
            image.type = Image.Type.Sliced;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            TMP_Text text = CreateText("Label", buttonObject.transform, label, 24f, labelColor, TextAlignmentOptions.Center, FontStyles.Bold);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            text.raycastTarget = false;
            return button;
        }

        private static Button CreateIconButton(string name, Transform parent, Sprite backgroundSprite, Sprite iconSprite, float size)
        {
            GameObject buttonObject = CreateImage(name, parent, backgroundSprite, Color.white, typeof(Button), typeof(LayoutElement), typeof(UiButtonFx));
            Image background = buttonObject.GetComponent<Image>();
            if (backgroundSprite != null) background.type = Image.Type.Sliced;
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = size;
            layout.preferredHeight = size;
            GameObject icon = CreateImage("Icon", buttonObject.transform, iconSprite, Color.white);
            Image iconImage = icon.GetComponent<Image>();
            iconImage.raycastTarget = false;
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(size * 0.56f, size * 0.56f);
            return button;
        }

        private static Image CreateImageWithLayout(string name, Transform parent, Sprite sprite, Color color, float width, float height)
        {
            GameObject imageObject = CreateImage(name, parent, sprite, color, typeof(LayoutElement));
            LayoutElement layout = imageObject.GetComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            return imageObject.GetComponent<Image>();
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
            text.font = TMP_Settings.defaultFontAsset;
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

        private static void CreateDecorLeaf(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size, float rotation)
        {
            GameObject leaf = CreateImage(name, parent, sprite, new Color(0.20f, 0.48f, 0.31f, 0.16f));
            Image image = leaf.GetComponent<Image>();
            image.raycastTarget = false;
            RectTransform rect = leaf.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(position.x >= 0f ? 0f : 1f, position.y >= 0f ? 1f : 0f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
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

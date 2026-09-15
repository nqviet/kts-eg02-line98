using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Line98.Data;
using Line98.Presentation;

namespace Line98.Editor
{
    public static class ThemeAuthoring
    {
        private const string CrystalMaterialsFolder = "Assets/Art/Materials/Themes/Crystal";
        private const string DefinitionsFolder = "Assets/_Project/Content/Definitions";
        private const string BallMeshPath = "Assets/Art/Meshes/MESH_Ball_Gem_Centered.asset";
        private const string PatternTexturePath = "Assets/Art/Textures/T_Ball_Accessibility_Patterns.png";
        private const string BlobShadowPath = "Assets/Art/Materials/M_Ball_BlobShadow.mat";
        private const string BrandQuadSpritePath = "Assets/Art/Sprites/UI/Icons/ui_brand_ball_quad.png";
        private const string CyanBallSpritePath = "Assets/Art/Sprites/Balls/sp_ball_cyan.png";
        private const string PreviewSpriteSetPath = "Assets/_Project/Content/Themes/UI/PreviewSpriteSet_Default.asset";

        // Crystal Palette colors per GDD / theme_plan.md §11.3
        public static readonly Color CrystalRed    = ParseHex("#F04E6A");
        public static readonly Color CrystalOrange = ParseHex("#EB8B3C");
        public static readonly Color CrystalYellow = ParseHex("#EFD35E");
        public static readonly Color CrystalGreen  = ParseHex("#2FB574");
        public static readonly Color CrystalCyan   = ParseHex("#58D8E8");
        public static readonly Color CrystalPurple = ParseHex("#A85FE0");
        public static readonly Color CrystalBlue   = ParseHex("#3A6BE8");

        private static Color ParseHex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        [MenuItem("Line98/Authoring/Setup Themes and Catalog")]
        public static void SetupThemesAndCatalog()
        {
            // 1. Ensure Crystal materials directory
            if (!AssetDatabase.IsValidFolder("Assets/Art/Materials/Themes"))
            {
                AssetDatabase.CreateFolder("Assets/Art/Materials", "Themes");
            }
            if (!AssetDatabase.IsValidFolder(CrystalMaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/Art/Materials/Themes", "Crystal");
            }

            var patternTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PatternTexturePath);
            var ballMesh = AssetDatabase.LoadAssetAtPath<Mesh>(BallMeshPath);
            var blobShadowMat = AssetDatabase.LoadAssetAtPath<Material>(BlobShadowPath);
            var brandQuadSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BrandQuadSpritePath);
            var cyanBallSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CyanBallSpritePath);
            var previewSpriteSet = AssetDatabase.LoadAssetAtPath<UiPreviewSpriteSetSO>(PreviewSpriteSetPath);

            var shader = Shader.Find("Line98/PolishedBall");

            // 2. Author 7 Crystal materials with exact pattern rect parity
            var crystalColors = new[]
            {
                ("Red", CrystalRed, AccessibilityAuthoring.RectCircleRed),
                ("Orange", CrystalOrange, AccessibilityAuthoring.RectCrossOrange),
                ("Yellow", CrystalYellow, AccessibilityAuthoring.RectStarYellow),
                ("Green", CrystalGreen, AccessibilityAuthoring.RectDiamondGreen),
                ("Cyan", CrystalCyan, AccessibilityAuthoring.RectHexCyan),
                ("Purple", CrystalPurple, AccessibilityAuthoring.RectRingPurple),
                ("Blue", CrystalBlue, AccessibilityAuthoring.RectTriBlue)
            };

            var crystalMats = new Material[7];

            for (int i = 0; i < crystalColors.Length; i++)
            {
                var (colorName, colorVal, patternRect) = crystalColors[i];
                string matPath = $"{CrystalMaterialsFolder}/M_Ball_Crystal_{colorName}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(shader);
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                else
                {
                    mat.shader = shader;
                }

                mat.SetColor("_BaseColor", colorVal);
                mat.SetFloat("_Smoothness", 0.95f);
                if (patternTex != null) mat.SetTexture("_PatternTex", patternTex);
                mat.SetVector("_PatternRect", patternRect);
                mat.SetColor("_PatternColor", Color.white);
                mat.SetFloat("_PatternStrength", 0.0f);
                EditorUtility.SetDirty(mat);
                crystalMats[i] = mat;
            }

            // 3. Update BallTheme_Classic.asset
            string ballClassicPath = $"{DefinitionsFolder}/BallTheme_Classic.asset";
            var ballClassic = AssetDatabase.LoadAssetAtPath<BallThemeSO>(ballClassicPath);
            if (ballClassic != null)
            {
                var so = new SerializedObject(ballClassic);
                so.FindProperty("m_ThemeId").stringValue = "classic";
                so.FindProperty("m_DisplayName").stringValue = "Classic";
                so.FindProperty("m_UnlockedByDefault").boolValue = true;
                so.FindProperty("m_BallMesh").objectReferenceValue = ballMesh;
                so.FindProperty("m_Thumbnail").objectReferenceValue = brandQuadSprite;

                var previewTintsProp = so.FindProperty("m_PreviewTints");
                previewTintsProp.arraySize = 7;
                for (int i = 0; i < 7; i++)
                {
                    previewTintsProp.GetArrayElementAtIndex(i).colorValue = Color.white;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(ballClassic);
            }

            // 4. Author BallTheme_Crystal.asset
            string ballCrystalPath = $"{DefinitionsFolder}/BallTheme_Crystal.asset";
            var ballCrystal = AssetDatabase.LoadAssetAtPath<BallThemeSO>(ballCrystalPath);
            if (ballCrystal == null)
            {
                ballCrystal = ScriptableObject.CreateInstance<BallThemeSO>();
                AssetDatabase.CreateAsset(ballCrystal, ballCrystalPath);
            }

            {
                var so = new SerializedObject(ballCrystal);
                so.FindProperty("m_ThemeId").stringValue = "crystal";
                so.FindProperty("m_DisplayName").stringValue = "Crystal";
                so.FindProperty("m_UnlockedByDefault").boolValue = true;
                so.FindProperty("m_BallMesh").objectReferenceValue = ballMesh;
                so.FindProperty("m_Thumbnail").objectReferenceValue = cyanBallSprite;
                so.FindProperty("m_AccessibilityPatterns").objectReferenceValue = patternTex;
                so.FindProperty("m_BlobShadowMaterial").objectReferenceValue = blobShadowMat;
                so.FindProperty("m_PatternsOn").boolValue = false;

                var matsProp = so.FindProperty("m_BallMaterials");
                matsProp.arraySize = 7;
                for (int i = 0; i < 7; i++)
                {
                    matsProp.GetArrayElementAtIndex(i).objectReferenceValue = crystalMats[i];
                }

                var tintsProp = so.FindProperty("m_PreviewTints");
                tintsProp.arraySize = 7;
                for (int i = 0; i < 7; i++)
                {
                    tintsProp.GetArrayElementAtIndex(i).colorValue = crystalColors[i].Item2;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(ballCrystal);
            }

            // 5. Update BoardTheme_Classic.asset
            string boardClassicPath = $"{DefinitionsFolder}/BoardTheme_Classic.asset";
            var boardClassic = AssetDatabase.LoadAssetAtPath<BoardThemeSO>(boardClassicPath);
            if (boardClassic != null)
            {
                var so = new SerializedObject(boardClassic);
                so.FindProperty("m_ThemeId").stringValue = "classic";
                so.FindProperty("m_DisplayName").stringValue = "Classic";
                so.FindProperty("m_UnlockedByDefault").boolValue = true;
                so.FindProperty("m_Thumbnail").objectReferenceValue = brandQuadSprite;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(boardClassic);
            }

            // 6. Update UiTheme_Default.asset
            string uiThemePath = "Assets/_Project/Content/Themes/UI/UiTheme_Default.asset";
            var uiTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(uiThemePath);
            if (uiTheme != null)
            {
                var so = new SerializedObject(uiTheme);
                so.FindProperty("m_ThemeId").stringValue = "default";
                so.FindProperty("m_DisplayName").stringValue = "Default";
                so.FindProperty("m_UnlockedByDefault").boolValue = true;
                so.FindProperty("m_Thumbnail").objectReferenceValue = brandQuadSprite;
                so.FindProperty("m_PreviewSpriteSet").objectReferenceValue = previewSpriteSet;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(uiTheme);
            }

            // 7. Author ClearEffect_Classic.asset
            string clearClassicPath = $"{DefinitionsFolder}/ClearEffect_Classic.asset";
            var clearClassic = AssetDatabase.LoadAssetAtPath<ClearEffectSO>(clearClassicPath);
            if (clearClassic == null)
            {
                clearClassic = ScriptableObject.CreateInstance<ClearEffectSO>();
                AssetDatabase.CreateAsset(clearClassic, clearClassicPath);
            }

            {
                var so = new SerializedObject(clearClassic);
                so.FindProperty("m_ThemeId").stringValue = "classic";
                so.FindProperty("m_DisplayName").stringValue = "Classic";
                so.FindProperty("m_UnlockedByDefault").boolValue = true;
                so.FindProperty("m_Thumbnail").objectReferenceValue = brandQuadSprite;
                so.FindProperty("m_RibbonTint").colorValue = new Color(1f, 1f, 1f, 0.85f);
                so.FindProperty("m_RibbonTintBanner").colorValue = new Color(1f, 0.84f, 0f, 0.95f);
                so.FindProperty("m_BurstTint").colorValue = Color.white;
                so.FindProperty("m_ClearBurstVfxKey").stringValue = "Vfx_ClearBurst";
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(clearClassic);
            }

            // 8. Author Theme_Classic.asset
            string themeClassicPath = $"{DefinitionsFolder}/Theme_Classic.asset";
            var themeClassic = AssetDatabase.LoadAssetAtPath<ThemeDefinitionSO>(themeClassicPath);
            if (themeClassic == null)
            {
                themeClassic = ScriptableObject.CreateInstance<ThemeDefinitionSO>();
                AssetDatabase.CreateAsset(themeClassic, themeClassicPath);
            }

            {
                var so = new SerializedObject(themeClassic);
                so.FindProperty("m_ThemeId").stringValue = "classic";
                so.FindProperty("m_DisplayName").stringValue = "Classic";
                so.FindProperty("m_UnlockedByDefault").boolValue = true;
                so.FindProperty("m_Thumbnail").objectReferenceValue = brandQuadSprite;
                so.FindProperty("m_InheritsFrom").objectReferenceValue = null;
                so.FindProperty("m_BallTheme").objectReferenceValue = ballClassic;
                so.FindProperty("m_BoardTheme").objectReferenceValue = boardClassic;
                so.FindProperty("m_UiTheme").objectReferenceValue = uiTheme;
                so.FindProperty("m_ClearEffect").objectReferenceValue = clearClassic;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(themeClassic);
            }

            // 9. Author Theme_Crystal.asset
            string themeCrystalPath = $"{DefinitionsFolder}/Theme_Crystal.asset";
            var themeCrystal = AssetDatabase.LoadAssetAtPath<ThemeDefinitionSO>(themeCrystalPath);
            if (themeCrystal == null)
            {
                themeCrystal = ScriptableObject.CreateInstance<ThemeDefinitionSO>();
                AssetDatabase.CreateAsset(themeCrystal, themeCrystalPath);
            }

            {
                var so = new SerializedObject(themeCrystal);
                so.FindProperty("m_ThemeId").stringValue = "crystal";
                so.FindProperty("m_DisplayName").stringValue = "Crystal";
                so.FindProperty("m_UnlockedByDefault").boolValue = true;
                so.FindProperty("m_Thumbnail").objectReferenceValue = cyanBallSprite;
                so.FindProperty("m_InheritsFrom").objectReferenceValue = themeClassic;
                so.FindProperty("m_BallTheme").objectReferenceValue = ballCrystal;
                so.FindProperty("m_BoardTheme").objectReferenceValue = null;
                so.FindProperty("m_UiTheme").objectReferenceValue = null;
                so.FindProperty("m_ClearEffect").objectReferenceValue = null;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(themeCrystal);
            }

            // 10. Author ThemeCatalog_Default.asset
            string catalogPath = $"{DefinitionsFolder}/ThemeCatalog_Default.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(catalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ThemeCatalogSO>();
                AssetDatabase.CreateAsset(catalog, catalogPath);
            }

            {
                var so = new SerializedObject(catalog);
                so.FindProperty("m_DefaultThemeId").stringValue = "classic";
                var themesProp = so.FindProperty("m_Themes");
                themesProp.arraySize = 2;
                themesProp.GetArrayElementAtIndex(0).objectReferenceValue = themeClassic;
                themesProp.GetArrayElementAtIndex(1).objectReferenceValue = themeCrystal;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ThemeAuthoring] Setup completed successfully!");
        }

        [MenuItem("Line98/Authoring/Setup Cosmetics UI and Prefabs")]
        public static void SetupCosmeticsUIAndPrefabs()
        {
            const string cosmeticsPrefabPath = "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Cosmetics.prefab";
            const string settingsPrefabPath = "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Settings.prefab";
            const string uiRootPrefabPath = "Assets/_Project/Content/Prefabs/UI/Shell/UI_Root.prefab";
            const string catalogPath = "Assets/_Project/Content/Definitions/ThemeCatalog_Default.asset";

            // 1. Create or update Popup_Cosmetics.prefab
            GameObject cosmeticsGo;
            bool isNew = false;
            if (File.Exists(cosmeticsPrefabPath))
            {
                cosmeticsGo = PrefabUtility.LoadPrefabContents(cosmeticsPrefabPath);
            }
            else
            {
                isNew = true;
                cosmeticsGo = new GameObject("Popup_Cosmetics", typeof(RectTransform), typeof(CanvasGroup), typeof(CosmeticsPopup), typeof(UiResponsiveModal));
            }

            try
            {
                var rootRt = cosmeticsGo.GetComponent<RectTransform>();
                rootRt.anchorMin = Vector2.zero;
                rootRt.anchorMax = Vector2.one;
                rootRt.offsetMin = Vector2.zero;
                rootRt.offsetMax = Vector2.zero;
                rootRt.pivot = new Vector2(0.5f, 0.5f);

                var canvasGroup = cosmeticsGo.GetComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                var cosmeticsPopup = cosmeticsGo.GetComponent<CosmeticsPopup>();
                var responsiveModal = cosmeticsGo.GetComponent<UiResponsiveModal>();

                // Container
                Transform containerT = cosmeticsGo.transform.Find("Container");
                GameObject containerGo;
                if (containerT == null)
                {
                    containerGo = new GameObject("Container", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                    containerGo.transform.SetParent(cosmeticsGo.transform, false);
                }
                else
                {
                    containerGo = containerT.gameObject;
                }

                var containerRt = containerGo.GetComponent<RectTransform>();
                containerRt.anchorMin = new Vector2(0.5f, 0.5f);
                containerRt.anchorMax = new Vector2(0.5f, 0.5f);
                containerRt.pivot = new Vector2(0.5f, 0.5f);
                containerRt.sizeDelta = new Vector2(880, 560);
                containerRt.anchoredPosition = Vector2.zero;

                var bgImg = containerGo.GetComponent<UnityEngine.UI.Image>();
                bgImg.color = new Color(0.96f, 0.97f, 0.98f, 1f);

                // Title
                Transform titleT = containerGo.transform.Find("Title");
                GameObject titleGo;
                if (titleT == null)
                {
                    titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
                    titleGo.transform.SetParent(containerGo.transform, false);
                }
                else
                {
                    titleGo = titleT.gameObject;
                }
                var titleRt = titleGo.GetComponent<RectTransform>();
                titleRt.anchorMin = new Vector2(0.5f, 1.0f);
                titleRt.anchorMax = new Vector2(0.5f, 1.0f);
                titleRt.pivot = new Vector2(0.5f, 1.0f);
                titleRt.sizeDelta = new Vector2(700, 50);
                titleRt.anchoredPosition = new Vector2(0, -40);
                var titleText = titleGo.GetComponent<TextMeshProUGUI>();
                titleText.text = "THEMES";
                titleText.fontSize = 36;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = new Color(0.04f, 0.06f, 0.2f, 1f);

                // TilesContainer
                Transform tilesT = containerGo.transform.Find("TilesContainer");
                GameObject tilesGo;
                if (tilesT == null)
                {
                    tilesGo = new GameObject("TilesContainer", typeof(RectTransform), typeof(GridLayoutGroup));
                    tilesGo.transform.SetParent(containerGo.transform, false);
                }
                else
                {
                    tilesGo = tilesT.gameObject;
                }
                var tilesRt = tilesGo.GetComponent<RectTransform>();
                tilesRt.anchorMin = new Vector2(0.5f, 0.5f);
                tilesRt.anchorMax = new Vector2(0.5f, 0.5f);
                tilesRt.pivot = new Vector2(0.5f, 0.5f);
                tilesRt.sizeDelta = new Vector2(720, 300);
                tilesRt.anchoredPosition = new Vector2(0, 15);
                var grid = tilesGo.GetComponent<GridLayoutGroup>() ?? tilesGo.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(200, 240);
                grid.spacing = new Vector2(40, 20);
                grid.childAlignment = TextAnchor.MiddleCenter;

                // BtnClose
                Transform btnCloseT = containerGo.transform.Find("BtnClose");
                GameObject btnCloseGo;
                if (btnCloseT == null)
                {
                    btnCloseGo = new GameObject("BtnClose", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(UiButtonFx), typeof(UiActionButton));
                    btnCloseGo.transform.SetParent(containerGo.transform, false);
                }
                else
                {
                    btnCloseGo = btnCloseT.gameObject;
                }
                var btnCloseRt = btnCloseGo.GetComponent<RectTransform>();
                btnCloseRt.anchorMin = new Vector2(0.5f, 0f);
                btnCloseRt.anchorMax = new Vector2(0.5f, 0f);
                btnCloseRt.pivot = new Vector2(0.5f, 0f);
                btnCloseRt.sizeDelta = new Vector2(280, 80);
                btnCloseRt.anchoredPosition = new Vector2(0, 30);
                var btnImg = btnCloseGo.GetComponent<UnityEngine.UI.Image>();
                btnImg.color = new Color(0.12f, 0.44f, 0.94f, 1f);

                Transform labelT = btnCloseGo.transform.Find("Label");
                GameObject labelGo;
                if (labelT == null)
                {
                    labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                    labelGo.transform.SetParent(btnCloseGo.transform, false);
                }
                else
                {
                    labelGo = labelT.gameObject;
                }
                var labelRt = labelGo.GetComponent<RectTransform>();
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                var labelText = labelGo.GetComponent<TextMeshProUGUI>();
                labelText.text = "CLOSE";
                labelText.fontSize = 26;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = Color.white;

                // Wire CosmeticsPopup
                var popupSo = new SerializedObject(cosmeticsPopup);
                popupSo.FindProperty("m_ModalContainer").objectReferenceValue = containerRt;
                popupSo.FindProperty("m_CanvasGroup").objectReferenceValue = canvasGroup;
                popupSo.FindProperty("m_TitleText").objectReferenceValue = titleText;
                popupSo.FindProperty("m_CloseButton").objectReferenceValue = btnCloseGo.GetComponent<UnityEngine.UI.Button>();
                popupSo.FindProperty("m_TilesContainer").objectReferenceValue = tilesRt;
                popupSo.ApplyModifiedProperties();

                // Wire UiResponsiveModal
                var modalSo = new SerializedObject(responsiveModal);
                modalSo.FindProperty("m_Popup").objectReferenceValue = cosmeticsPopup;
                modalSo.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(cosmeticsGo, cosmeticsPrefabPath);
            }
            finally
            {
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(cosmeticsGo);
                }
                else
                {
                    PrefabUtility.UnloadPrefabContents(cosmeticsGo);
                }
            }

            // 2. Update Popup_Settings.prefab
            var settingsContents = PrefabUtility.LoadPrefabContents(settingsPrefabPath);
            try
            {
                var settingsPopup = settingsContents.GetComponent<SettingsPopup>();
                var btnSec = settingsContents.transform.Find("Container/BtnSecondary");
                if (settingsPopup != null && btnSec != null)
                {
                    var btnComp = btnSec.GetComponent<UnityEngine.UI.Button>();
                    var settingsSo = new SerializedObject(settingsPopup);
                    settingsSo.FindProperty("m_CosmeticsButton").objectReferenceValue = btnComp;
                    settingsSo.ApplyModifiedProperties();

                    // Check or add label "THEMES"
                    var label = btnSec.Find("Label");
                    if (label == null)
                    {
                        var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                        lblGo.transform.SetParent(btnSec, false);
                        var lRt = lblGo.GetComponent<RectTransform>();
                        lRt.anchorMin = Vector2.zero;
                        lRt.anchorMax = Vector2.one;
                        lRt.offsetMin = Vector2.zero;
                        lRt.offsetMax = Vector2.zero;
                        var tmp = lblGo.GetComponent<TextMeshProUGUI>();
                        tmp.text = "THEMES";
                        tmp.fontSize = 24;
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.color = new Color(0.04f, 0.06f, 0.2f, 1f);
                    }
                    else
                    {
                        var tmp = label.GetComponent<TMP_Text>();
                        if (tmp != null) tmp.text = "THEMES";
                    }

                    PrefabUtility.SaveAsPrefabAsset(settingsContents, settingsPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(settingsContents);
            }

            // 3. Update UI_Root.prefab
            var uiRootContents = PrefabUtility.LoadPrefabContents(uiRootPrefabPath);
            try
            {
                var uiShell = uiRootContents.GetComponent<UiShell>();
                var popupsT = uiRootContents.transform.Find("Canvas_Popups");
                var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(catalogPath);

                if (uiShell != null && popupsT != null)
                {
                    Transform existingCosmetics = popupsT.Find("CosmeticsPopup");
                    GameObject cosmeticsInstance;
                    if (existingCosmetics == null)
                    {
                        var cosmeticsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cosmeticsPrefabPath);
                        cosmeticsInstance = (GameObject)PrefabUtility.InstantiatePrefab(cosmeticsPrefab, popupsT);
                        cosmeticsInstance.name = "CosmeticsPopup";
                    }
                    else
                    {
                        cosmeticsInstance = existingCosmetics.gameObject;
                    }

                    var cosmeticsComp = cosmeticsInstance.GetComponent<CosmeticsPopup>();
                    var shellSo = new SerializedObject(uiShell);
                    shellSo.FindProperty("m_CosmeticsPopup").objectReferenceValue = cosmeticsComp;
                    shellSo.FindProperty("m_ThemeCatalog").objectReferenceValue = catalog;
                    shellSo.ApplyModifiedProperties();

                    PrefabUtility.SaveAsPrefabAsset(uiRootContents, uiRootPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(uiRootContents);
            }

            // 4. Update Scene UI_Root if in open scene
            var sceneShell = UnityEngine.Object.FindFirstObjectByType<UiShell>();
            if (sceneShell != null)
            {
                var popupsT = sceneShell.transform.Find("Canvas_Popups");
                var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(catalogPath);
                if (popupsT != null)
                {
                    var existingCosmetics = popupsT.Find("CosmeticsPopup");
                    if (existingCosmetics == null)
                    {
                        var cosmeticsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cosmeticsPrefabPath);
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(cosmeticsPrefab, popupsT);
                        inst.name = "CosmeticsPopup";
                    }
                    var shellSo = new SerializedObject(sceneShell);
                    shellSo.FindProperty("m_CosmeticsPopup").objectReferenceValue = popupsT.Find("CosmeticsPopup").GetComponent<CosmeticsPopup>();
                    shellSo.FindProperty("m_ThemeCatalog").objectReferenceValue = catalog;
                    shellSo.ApplyModifiedProperties();
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sceneShell.gameObject.scene);
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sceneShell.gameObject.scene);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ThemeAuthoring] SetupCosmeticsUIAndPrefabs completed successfully!");
        }
    }
}

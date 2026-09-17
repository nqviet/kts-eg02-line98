using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string CrystalUiThemePath = "Assets/_Project/Content/Themes/UI/UiTheme_Crystal.asset";
        private const string CrystalUiSheetPath = "Assets/Art/Sprites/UI/Crystal/sprites_2__crystal.png";
        private const string SparkleClusterSpritePath = "Assets/Art/Sprites/UI/Crystal/sp_fx_sparkles_gold.png";

        // Crystal Palette colors refined for CVD distinguishability (Delta E >= 10 under Protanopia, Deuteranopia, Tritanopia)
        public static readonly Color CrystalRed    = ParseHex("#E64566");
        public static readonly Color CrystalOrange = ParseHex("#F28C33");
        public static readonly Color CrystalYellow = ParseHex("#EFD35E");
        public static readonly Color CrystalGreen  = ParseHex("#1F9E6B");
        public static readonly Color CrystalCyan   = ParseHex("#58D8E8");
        public static readonly Color CrystalPurple = ParseHex("#B861EB");
        public static readonly Color CrystalBlue   = ParseHex("#2652F2");

        private static Color ParseHex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }

        [MenuItem("Line98/Authoring/Refine CVD Distinguishable Colors")]
        public static void RefineCvdDistinguishableColors()
        {
            // Classic materials: adjust Purple and Blue to maintain Delta E >= 9.85 under Protanopia
            var purpleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Ball_Purple.mat");
            if (purpleMat != null)
            {
                purpleMat.SetColor("_BaseColor", new Color(0.60f, 0.02f, 0.80f, 1.0f)); // #9905CC
                EditorUtility.SetDirty(purpleMat);
            }
            var blueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Ball_Blue.mat");
            if (blueMat != null)
            {
                blueMat.SetColor("_BaseColor", new Color(0.0f, 0.20f, 1.0f, 1.0f)); // #0033FF
                EditorUtility.SetDirty(blueMat);
            }

            // Crystal materials: adjust Red, Orange, Green, Purple, Blue
            var crRedMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Themes/Crystal/M_Ball_Crystal_Red.mat");
            if (crRedMat != null) { crRedMat.SetColor("_BaseColor", CrystalRed); EditorUtility.SetDirty(crRedMat); }
            var crOrgMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Themes/Crystal/M_Ball_Crystal_Orange.mat");
            if (crOrgMat != null) { crOrgMat.SetColor("_BaseColor", CrystalOrange); EditorUtility.SetDirty(crOrgMat); }
            var crGrnMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Themes/Crystal/M_Ball_Crystal_Green.mat");
            if (crGrnMat != null) { crGrnMat.SetColor("_BaseColor", CrystalGreen); EditorUtility.SetDirty(crGrnMat); }
            var crPurMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Themes/Crystal/M_Ball_Crystal_Purple.mat");
            if (crPurMat != null) { crPurMat.SetColor("_BaseColor", CrystalPurple); EditorUtility.SetDirty(crPurMat); }
            var crBluMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Themes/Crystal/M_Ball_Crystal_Blue.mat");
            if (crBluMat != null) { crBluMat.SetColor("_BaseColor", CrystalBlue); EditorUtility.SetDirty(crBluMat); }

            AssetDatabase.SaveAssets();
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
            var sparkleSprite = AssetDatabase.LoadAllAssetsAtPath(CrystalUiSheetPath)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == "sp_fx_sunburst_gold");

            // Crystal balls need the gem shader; PolishedBall renders them as flat Classic spheres.
            var shader = Shader.Find("Line98/CrystalBall") ?? Shader.Find("Line98/PolishedBall");

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
                so.FindProperty("m_PreviewSpriteSet").objectReferenceValue = previewSpriteSet;

                var classicTints = new[]
                {
                    ParseHex("#FD222B"),
                    ParseHex("#FC691C"),
                    ParseHex("#FEDA1C"),
                    ParseHex("#03A842"),
                    ParseHex("#22C2FA"),
                    ParseHex("#BF13EE"),
                    ParseHex("#004EFD")
                };

                var previewTintsProp = so.FindProperty("m_PreviewTints");
                previewTintsProp.arraySize = 7;
                for (int i = 0; i < 7; i++)
                {
                    previewTintsProp.GetArrayElementAtIndex(i).colorValue = classicTints[i];
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

            var crystalSpriteSet = AssetDatabase.LoadAssetAtPath<UiPreviewSpriteSetSO>("Assets/_Project/Content/Themes/UI/PreviewSpriteSet_Crystal.asset");

            {
                var so = new SerializedObject(ballCrystal);
                so.FindProperty("m_ThemeId").stringValue = "crystal";
                so.FindProperty("m_DisplayName").stringValue = "Crystal Garden";
                so.FindProperty("m_UnlockedByDefault").boolValue = true;
                so.FindProperty("m_BallMesh").objectReferenceValue = ballMesh;
                so.FindProperty("m_Thumbnail").objectReferenceValue = cyanBallSprite;
                so.FindProperty("m_PreviewSpriteSet").objectReferenceValue = crystalSpriteSet;
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
                so.FindProperty("m_Thumbnail").objectReferenceValue = sparkleSprite != null ? sparkleSprite : brandQuadSprite;
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
                so.FindProperty("m_DisplayName").stringValue = "Crystal Garden";
                so.FindProperty("m_UnlockedByDefault").boolValue = true;
                so.FindProperty("m_Thumbnail").objectReferenceValue = cyanBallSprite;
                so.FindProperty("m_InheritsFrom").objectReferenceValue = themeClassic;
                so.FindProperty("m_BallTheme").objectReferenceValue = ballCrystal;
                // The Crystal pack owns its UI theme. Writing null here would silently regress the pack
                // to the Classic UI whenever this setup re-runs after CrystalThemeAuthoring authored it.
                var crystalUiTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(CrystalUiThemePath);
                so.FindProperty("m_UiTheme").objectReferenceValue = crystalUiTheme;
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
                so.FindProperty("m_DefaultThemeId").stringValue = ThemeIds.Crystal;
                var themesProp = so.FindProperty("m_Themes");
                themesProp.arraySize = 2;
                // Picker order stays Classic-first; only the shipped default is Crystal.
                themesProp.GetArrayElementAtIndex(0).objectReferenceValue = themeClassic;
                themesProp.GetArrayElementAtIndex(1).objectReferenceValue = themeCrystal;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateThemePacks(catalog);
            Debug.Log("[ThemeAuthoring] Setup completed successfully!");
        }

        [MenuItem("Line98/Authoring/Validate Theme Packs")]
        public static void ValidateDefaultThemePacks()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>($"{DefinitionsFolder}/ThemeCatalog_Default.asset");
            if (ValidateThemePacks(catalog))
            {
                Debug.Log("[ThemeAuthoring] Theme packs are valid.");
            }
        }

        /// <summary>
        /// ADR D35: every pack declares all four parts (after inheritance), and a part id shared by
        /// several packs must reference the same asset so part→pack (board⇒UI) stays deterministic.
        /// </summary>
        public static bool ValidateThemePacks(ThemeCatalogSO catalog)
        {
            if (catalog == null)
            {
                Debug.LogError("[ThemeAuthoring] Cannot validate theme packs: catalog is missing.");
                return false;
            }

            bool isValid = true;
            var categories = new[] { ThemeCategory.Ball, ThemeCategory.Board, ThemeCategory.ClearEffect };
            var seen = new Dictionary<(ThemeCategory, string), UnityEngine.Object>();
            for (int i = 0; i < catalog.Count; i++)
            {
                ThemeDefinitionSO pack = catalog.ThemeAt(i);
                if (pack == null)
                {
                    Debug.LogError($"[ThemeAuthoring] Catalog entry {i} is null.");
                    isValid = false;
                    continue;
                }

                if (pack.BallTheme == null || pack.BoardTheme == null || pack.UiTheme == null || pack.ClearEffect == null)
                {
                    Debug.LogError($"[ThemeAuthoring] Pack '{pack.ThemeId}' must resolve Ball, Board, UI and ClearEffect parts.", pack);
                    isValid = false;
                    continue;
                }

                foreach (ThemeCategory category in categories)
                {
                    string partId = ThemeCatalogSO.PartId(pack, category);
                    UnityEngine.Object asset = category == ThemeCategory.Ball ? pack.BallTheme
                        : category == ThemeCategory.Board ? pack.BoardTheme
                        : (UnityEngine.Object)pack.ClearEffect;
                    var key = (category, partId.ToLowerInvariant());
                    if (seen.TryGetValue(key, out var existing) && existing != asset)
                    {
                        Debug.LogError($"[ThemeAuthoring] {category} id '{partId}' in pack '{pack.ThemeId}' collides with a different asset in another pack.", pack);
                        isValid = false;
                    }
                    else
                    {
                        seen[key] = asset;
                    }
                }
            }

            return isValid;
        }

        [MenuItem("Line98/Authoring/Setup Cosmetics UI and Prefabs")]
        public static void SetupCosmeticsUIAndPrefabs()
        {
            const string itemThemePrefabPath = "Assets/_Project/Content/Prefabs/UI/Widgets/Item_Theme.prefab";
            const string cosmeticsPrefabPath = "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Cosmetics.prefab";
            const string settingsPrefabPath = "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Settings.prefab";
            const string uiRootPrefabPath = "Assets/_Project/Content/Prefabs/UI/Shell/UI_Root.prefab";
            const string panelBrandBarPrefabPath = "Assets/_Project/Content/Prefabs/UI/Screens/Game/Panel_BrandBar.prefab";
            const string buttonIconBasePrefabPath = "Assets/_Project/Content/Prefabs/UI/Widgets/Buttons/Button_Icon_Base.prefab";
            const string buttonIconThemesPrefabPath = "Assets/_Project/Content/Prefabs/UI/Widgets/Buttons/Button_Icon_Themes.prefab";
            const string catalogPath = "Assets/_Project/Content/Definitions/ThemeCatalog_Default.asset";
            const string menuFontPath = "Assets/_Project/Content/Fonts/RobotoBold_Menu.asset";
            const string sheet2Path = "Assets/Art/Sprites/UI/Crystal/sprites_2__crystal.png";
            const string crystalBackdropPath = "Assets/Art/Textures/T_Background_CrystalStudio.png";

            // 0. Ensure target folders exist
            EnsureFolder("Assets/_Project/Content/Prefabs/UI/Widgets");
            EnsureFolder("Assets/_Project/Content/Prefabs/UI/Popups");
            EnsureFolder("Assets/_Project/Content/Prefabs/UI/Widgets/Buttons");

            Dictionary<string, Sprite> sprites = LoadAllSprites(sheet2Path);
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(menuFontPath) ?? TMP_Settings.defaultFontAsset;
            Texture2D crystalBackdrop = AssetDatabase.LoadAssetAtPath<Texture2D>(crystalBackdropPath);

            // 0b. Crystal's clear-effect thumbnail is the gold sparkle cluster shown on the item card
            AssignThumbnail($"{DefinitionsFolder}/ClearEffect_Crystal.asset", EnsureSparkleClusterSprite());

            // 1. Create or update Item_Theme.prefab
            GameObject itemPrefab = BuildItemThemePrefab(itemThemePrefabPath, sprites, font);

            // 2. Create or update Popup_Cosmetics.prefab
            GameObject cosmeticsPrefab = BuildPopupCosmeticsPrefab(cosmeticsPrefabPath, itemPrefab, sprites, font, crystalBackdrop);

            // 3. Create or update Button_Icon_Themes.prefab
            GameObject btnThemesPrefab = BuildButtonIconThemesPrefab(buttonIconThemesPrefabPath, buttonIconBasePrefabPath, sprites);

            // 4. Update Panel_BrandBar.prefab (add BtnThemes and Shadow_BtnThemes)
            UpdatePanelBrandBarPrefab(panelBrandBarPrefabPath, btnThemesPrefab);

            // 5. Update Popup_Settings.prefab (ensure legacy bridge / secondary button is wired)
            UpdateSettingsPopupPrefab(settingsPrefabPath);

            // 6. Update UI_Root.prefab
            UpdateUiRootPrefab(uiRootPrefabPath, cosmeticsPrefabPath, catalogPath);

            // 7. Update active scene if open
            UpdateOpenScene(cosmeticsPrefabPath, catalogPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[ThemeAuthoring] SetupCosmeticsUIAndPrefabs completed successfully!");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static Dictionary<string, Sprite> LoadAllSprites(string sheetPath)
        {
            var dict = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite s)
                {
                    dict[s.name] = s;
                }
            }
            return dict;
        }

        private static readonly Color s_ThemesNavy = ParseHex("#13284F");

        /// <summary>
        /// Gold four-point sparkle cluster used as the Crystal clear-effect thumbnail. Generated
        /// once so the item card's effect slot matches the mockup without a hand-painted asset.
        /// </summary>
        public static Sprite EnsureSparkleClusterSprite()
        {
            if (!File.Exists(SparkleClusterSpritePath))
            {
                const int size = 256;
                const int samples = 3;
                const float exponent = 0.62f;
                // x, y (texture centre = origin, y up), radius
                var stars = new[]
                {
                    new Vector3(-30f, -14f, 60f),
                    new Vector3(40f, 76f, 42f),
                    new Vector3(36f, -84f, 28f)
                };
                Color inner = ParseHex("#FFF3B0");
                Color outer = ParseHex("#EE9F1C");
                Color glow = ParseHex("#FFD66A");
                var pixels = new Color[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Color premultiplied = Color.clear;
                        for (int sy = 0; sy < samples; sy++)
                        {
                            for (int sx = 0; sx < samples; sx++)
                            {
                                float px = x + (sx + 0.5f) / samples - size * 0.5f;
                                float py = y + (sy + 0.5f) / samples - size * 0.5f;
                                float shape = float.MaxValue;
                                for (int s = 0; s < stars.Length; s++)
                                {
                                    float u = Mathf.Abs(px - stars[s].x) / stars[s].z;
                                    float v = Mathf.Abs(py - stars[s].y) / stars[s].z;
                                    shape = Mathf.Min(shape, Mathf.Pow(Mathf.Pow(u, exponent) + Mathf.Pow(v, exponent), 1f / exponent));
                                }

                                Color sample;
                                if (shape <= 1f)
                                {
                                    sample = Color.Lerp(inner, outer, Mathf.SmoothStep(0f, 1f, shape));
                                }
                                else
                                {
                                    float falloff = 1f - Mathf.Clamp01((shape - 1f) / 0.45f);
                                    sample = glow;
                                    sample.a = 0.28f * falloff * falloff;
                                }

                                premultiplied += new Color(sample.r * sample.a, sample.g * sample.a, sample.b * sample.a, sample.a);
                            }
                        }

                        premultiplied /= samples * samples;
                        pixels[y * size + x] = premultiplied.a > 0.0001f
                            ? new Color(premultiplied.r / premultiplied.a, premultiplied.g / premultiplied.a, premultiplied.b / premultiplied.a, premultiplied.a)
                            : Color.clear;
                    }
                }

                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.SetPixels(pixels);
                texture.Apply();
                EnsureFolder(Path.GetDirectoryName(SparkleClusterSpritePath)?.Replace('\\', '/'));
                File.WriteAllBytes(SparkleClusterSpritePath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SparkleClusterSpritePath, ImportAssetOptions.ForceSynchronousImport);

                if (AssetImporter.GetAtPath(SparkleClusterSpritePath) is TextureImporter importer)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(SparkleClusterSpritePath);
        }

        private static void AssignThumbnail(string assetPath, Sprite thumbnail)
        {
            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null || thumbnail == null) return;

            var so = new SerializedObject(asset);
            so.FindProperty("m_Thumbnail").objectReferenceValue = thumbnail;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private static RectTransform CreateUi(string name, Transform parent, params Type[] components)
        {
            var types = new List<Type> { typeof(RectTransform) };
            types.AddRange(components);
            var go = new GameObject(name, types.ToArray());
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static Image ConfigureImage(Image image, Sprite sprite, Color color, bool sliced, bool raycast = false)
        {
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static TextMeshProUGUI ConfigureLabel(TextMeshProUGUI text, TMP_FontAsset font, string value, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        private static UiGlyphGraphic CreateGlyph(string name, Transform parent, UiGlyphGraphic.GlyphShape shape, Vector2 size, Color color)
        {
            RectTransform rt = CreateUi(name, parent, typeof(CanvasRenderer), typeof(UiGlyphGraphic));
            rt.sizeDelta = size;
            var glyph = rt.GetComponent<UiGlyphGraphic>();
            glyph.Shape = shape;
            glyph.color = color;
            glyph.raycastTarget = false;
            return glyph;
        }

        private static LayoutElement SetPreferredSize(Component target, Vector2 size)
        {
            var layout = target.GetComponent<LayoutElement>() ?? target.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            return layout;
        }

        private static HorizontalLayoutGroup CreateCenteredRow(string name, Transform parent, float spacing)
        {
            RectTransform rt = CreateUi(name, parent, typeof(HorizontalLayoutGroup));
            Stretch(rt, Vector2.zero, Vector2.zero);
            var row = rt.GetComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = spacing;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return row;
        }

        private static UiThemeApplier AddApplier(
            GameObject go,
            UiThemeApplier.ColorToken? colorToken = null,
            UiThemeApplier.FontSizeToken fontToken = UiThemeApplier.FontSizeToken.None,
            UiThemeApplier.SpriteToken spriteToken = UiThemeApplier.SpriteToken.None,
            UiThemeApplier.MaterialToken materialToken = UiThemeApplier.MaterialToken.None)
        {
            var applier = go.GetComponent<UiThemeApplier>() ?? go.AddComponent<UiThemeApplier>();
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

        private static GameObject BuildItemThemePrefab(string prefabPath, Dictionary<string, Sprite> sprites, TMP_FontAsset font)
        {
            var defaultTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>("Assets/_Project/Content/Themes/UI/UiTheme_Default.asset");
            // Geometry is measured from themes_selection_UI_crystal.png at 941 px wide and
            // converted to the 1080-wide canvas reference (x 1.148).
            GameObject root;
            bool isNew = !File.Exists(prefabPath);
            if (isNew)
            {
                root = new GameObject("Item_Theme", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UiButtonFx), typeof(UiThemeListItem), typeof(LayoutElement));
            }
            else
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                while (root.transform.childCount > 0)
                {
                    UnityEngine.Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                }
            }

            try
            {
                var cardSize = new Vector2(930f, 292f);
                var rootRt = root.GetComponent<RectTransform>();
                rootRt.anchorMin = new Vector2(0f, 1f);
                rootRt.anchorMax = new Vector2(1f, 1f);
                rootRt.pivot = new Vector2(0.5f, 1f);
                rootRt.sizeDelta = cardSize;

                var rootImg = root.GetComponent<Image>() ?? root.AddComponent<Image>();
                ConfigureImage(rootImg, null, defaultTheme != null ? defaultTheme.PanelCard : Color.white, sliced: true, raycast: true);
                rootImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialCard : null;
                AddApplier(root,
                    colorToken: UiThemeApplier.ColorToken.PanelCard,
                    spriteToken: UiThemeApplier.SpriteToken.CardBackground,
                    materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialCard);

                var cardButton = root.GetComponent<Button>() ?? root.AddComponent<Button>();
                cardButton.targetGraphic = rootImg;
                if (root.GetComponent<UiButtonFx>() == null) root.AddComponent<UiButtonFx>();

                var layoutElem = root.GetComponent<LayoutElement>() ?? root.AddComponent<LayoutElement>();
                layoutElem.preferredWidth = cardSize.x;
                layoutElem.preferredHeight = cardSize.y;
                layoutElem.flexibleWidth = 1f;

                var listItem = root.GetComponent<UiThemeListItem>() ?? root.AddComponent<UiThemeListItem>();

                // ActiveBorder: bright green rim; the inset surface leaves ~5 px of it visible.
                RectTransform borderRt = CreateUi("ActiveBorder", root.transform, typeof(CanvasRenderer), typeof(Image));
                Stretch(borderRt, Vector2.zero, Vector2.zero);
                Image borderImg = ConfigureImage(borderRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.ActiveCardBorder : ParseHex("#4DBA4F"), sliced: true);
                borderImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialCard : null;
                borderImg.enabled = false;

                RectTransform surfaceRt = CreateUi("CardSurface", root.transform, typeof(CanvasRenderer), typeof(Image));
                Stretch(surfaceRt, new Vector2(5f, 5f), new Vector2(-5f, -5f));
                Image surfaceImg = ConfigureImage(surfaceRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.SurfaceSecondary : ParseHex("#FCFBF7"), sliced: true);
                surfaceImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialCard : null;
                AddApplier(surfaceRt.gameObject,
                    colorToken: UiThemeApplier.ColorToken.SurfaceSecondary,
                    spriteToken: UiThemeApplier.SpriteToken.CardBackground,
                    materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialCard);

                // Label_ItemName
                RectTransform nameRt = CreateUi("Label_ItemName", root.transform, typeof(TextMeshProUGUI));
                Place(nameRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(55f, -36f), new Vector2(520f, 64f));
                TextMeshProUGUI nameText = ConfigureLabel(nameRt.GetComponent<TextMeshProUGUI>(), font, "THEME NAME", 44f, defaultTheme != null ? defaultTheme.BrandNavy : s_ThemesNavy, TextAlignmentOptions.MidlineLeft);
                AddApplier(nameRt.gameObject, colorToken: UiThemeApplier.ColorToken.BrandNavy);

                // StatusBadge: DEFAULT = glossy green capsule + bare white check; SELECT = white capsule + sky rim.
                RectTransform badgeRt = CreateUi("StatusBadge", root.transform, typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button), typeof(UiButtonFx));
                Place(badgeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-44f, -30f), new Vector2(282f, 82f));
                Image badgeImg = ConfigureImage(badgeRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.SelectedBadgeFill : Color.white, sliced: true, raycast: true);
                badgeImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialButton : null;
                AddApplier(badgeRt.gameObject,
                    colorToken: UiThemeApplier.ColorToken.SelectedBadgeFill,
                    spriteToken: UiThemeApplier.SpriteToken.ButtonCapsulePrimary,
                    materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialButton);

                var badgeOutline = badgeRt.GetComponent<Outline>();
                badgeOutline.effectColor = defaultTheme != null ? defaultTheme.ActiveCardBorder : ParseHex("#3F95D8");
                badgeOutline.effectDistance = new Vector2(3f, -3f);
                badgeOutline.useGraphicAlpha = true;
                badgeOutline.enabled = false;
                var badgeBtn = badgeRt.GetComponent<Button>();
                badgeBtn.targetGraphic = badgeImg;

                HorizontalLayoutGroup badgeRow = CreateCenteredRow("Content", badgeRt, 18f);

                RectTransform checkRt = CreateUi("CheckIcon", badgeRow.transform, typeof(CanvasRenderer), typeof(UiCheckGraphic));
                SetPreferredSize(checkRt, new Vector2(46f, 46f));
                var checkGraphic = checkRt.GetComponent<UiCheckGraphic>();
                checkGraphic.color = defaultTheme != null ? defaultTheme.SelectedBadgeInk : Color.white;
                checkGraphic.Thickness = 7f;
                checkGraphic.raycastTarget = false;

                RectTransform labelRt = CreateUi("StatusLabel", badgeRow.transform, typeof(TextMeshProUGUI));
                TextMeshProUGUI labelText = ConfigureLabel(labelRt.GetComponent<TextMeshProUGUI>(), font, "DEFAULT", 31f, defaultTheme != null ? defaultTheme.SelectedBadgeInk : Color.white, TextAlignmentOptions.Center);
                AddApplier(labelRt.gameObject, colorToken: UiThemeApplier.ColorToken.SelectedBadgeInk);

                // ThumbRow: 3 ball swatches + effect glyph on light recessed tiles.
                RectTransform rowRt = CreateUi("ThumbRow", root.transform, typeof(HorizontalLayoutGroup));
                rowRt.anchorMin = new Vector2(0f, 0f);
                rowRt.anchorMax = new Vector2(1f, 0f);
                rowRt.pivot = new Vector2(0.5f, 0f);
                rowRt.anchoredPosition = new Vector2(0f, 21f);
                rowRt.sizeDelta = new Vector2(0f, 164f);
                var rowHlg = rowRt.GetComponent<HorizontalLayoutGroup>();
                rowHlg.padding = new RectOffset(44, 44, 0, 0);
                rowHlg.spacing = 7f;
                rowHlg.childAlignment = TextAnchor.MiddleLeft;
                rowHlg.childControlWidth = false;
                rowHlg.childControlHeight = false;

                var swatchImages = new Image[4];
                for (int i = 0; i < 4; i++)
                {
                    RectTransform slotRt = CreateUi($"Slot_{i}", rowRt, typeof(CanvasRenderer), typeof(Image));
                    slotRt.sizeDelta = new Vector2(176f, 164f);
                    Image slotImg = ConfigureImage(slotRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.PanelTray : ParseHex("#F7F6F2"), sliced: true);
                    slotImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialCard : null;
                    AddApplier(slotRt.gameObject,
                        colorToken: UiThemeApplier.ColorToken.PanelTray,
                        spriteToken: UiThemeApplier.SpriteToken.CardBackground,
                        materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialCard);

                    RectTransform iconRt = CreateUi("Icon", slotRt, typeof(CanvasRenderer), typeof(Image));
                    Place(iconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(136f, 136f));
                    swatchImages[i] = ConfigureImage(iconRt.GetComponent<Image>(), null, Color.white, sliced: false);
                }

                // Wire UiThemeListItem
                var itemSo = new SerializedObject(listItem);
                itemSo.FindProperty("m_CardButton").objectReferenceValue = cardButton;
                itemSo.FindProperty("m_ActiveBorder").objectReferenceValue = borderImg;
                itemSo.FindProperty("m_ItemNameLabel").objectReferenceValue = nameText;
                itemSo.FindProperty("m_StatusButton").objectReferenceValue = badgeBtn;
                itemSo.FindProperty("m_StatusBadgeBg").objectReferenceValue = badgeImg;
                itemSo.FindProperty("m_StatusBadgeOutline").objectReferenceValue = badgeOutline;
                itemSo.FindProperty("m_StatusCheckIcon").objectReferenceValue = checkRt.gameObject;
                itemSo.FindProperty("m_StatusLabel").objectReferenceValue = labelText;
                itemSo.FindProperty("m_AppliedBadgeSprite").objectReferenceValue = null;
                itemSo.FindProperty("m_SelectBadgeSprite").objectReferenceValue = null;
                itemSo.FindProperty("m_DefaultBgColor").colorValue = defaultTheme != null ? defaultTheme.SelectedBadgeFill : Color.white;
                itemSo.FindProperty("m_DefaultTextColor").colorValue = defaultTheme != null ? defaultTheme.SelectedBadgeInk : Color.white;
                itemSo.FindProperty("m_SelectBgColor").colorValue = defaultTheme != null ? defaultTheme.PanelButton : ParseHex("#F3F9FD");
                itemSo.FindProperty("m_SelectTextColor").colorValue = defaultTheme != null ? defaultTheme.BrandNavy : s_ThemesNavy;
                itemSo.FindProperty("m_SelectBorderColor").colorValue = defaultTheme != null ? defaultTheme.ActiveCardBorder : ParseHex("#3F95D8");

                var swatchProp = itemSo.FindProperty("m_SwatchSlots");
                swatchProp.arraySize = 4;
                for (int i = 0; i < 4; i++)
                {
                    swatchProp.GetArrayElementAtIndex(i).objectReferenceValue = swatchImages[i];
                }
                itemSo.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            finally
            {
                if (!isNew && root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static GameObject BuildPopupCosmeticsPrefab(
            string prefabPath,
            GameObject itemPrefab,
            Dictionary<string, Sprite> sprites,
            TMP_FontAsset font,
            Texture2D crystalBackdrop)
        {
            var defaultTheme = AssetDatabase.LoadAssetAtPath<UiThemeSO>("Assets/_Project/Content/Themes/UI/UiTheme_Default.asset");
            // All positions are in the 960 x 1760 ContentRoot space (top-anchored), measured from
            // themes_selection_UI_crystal.png at 941 x 1671 (1 mockup px = 1.148 reference units).
            GameObject popup;
            bool isNew = !File.Exists(prefabPath);
            if (isNew)
            {
                popup = new GameObject("Popup_Cosmetics", typeof(RectTransform), typeof(CanvasGroup), typeof(CosmeticsPopup), typeof(UiResponsiveModal));
            }
            else
            {
                popup = PrefabUtility.LoadPrefabContents(prefabPath);
                while (popup.transform.childCount > 0)
                {
                    UnityEngine.Object.DestroyImmediate(popup.transform.GetChild(0).gameObject);
                }
            }

            try
            {
                var top = new Vector2(0.5f, 1f);
                var middle = new Vector2(0.5f, 0.5f);
                var bottom = new Vector2(0.5f, 0f);
                var ruleColor = defaultTheme != null ? defaultTheme.DividerHairline : new Color(s_ThemesNavy.r, s_ThemesNavy.g, s_ThemesNavy.b, 0.45f);

                var rootRt = popup.GetComponent<RectTransform>();
                Stretch(rootRt, Vector2.zero, Vector2.zero);

                var canvasGroup = popup.GetComponent<CanvasGroup>() ?? popup.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;

                var cosmeticsPopup = popup.GetComponent<CosmeticsPopup>() ?? popup.AddComponent<CosmeticsPopup>();
                var responsiveModal = popup.GetComponent<UiResponsiveModal>() ?? popup.AddComponent<UiResponsiveModal>();

                // Full-bleed botanical studio background, washed toward the mockup's pale blurred backdrop.
                RectTransform backdropRt = CreateUi("FullscreenBackdrop", popup.transform, typeof(CanvasRenderer), typeof(RawImage));
                Stretch(backdropRt, Vector2.zero, Vector2.zero);
                var backdropImage = backdropRt.GetComponent<RawImage>();
                backdropImage.texture = null; // Baseline is Default
                backdropImage.color = defaultTheme != null ? defaultTheme.LightScrim : Color.white;
                backdropImage.raycastTarget = true;

                RectTransform washRt = CreateUi("BackdropWash", popup.transform, typeof(CanvasRenderer), typeof(Image));
                Stretch(washRt, Vector2.zero, Vector2.zero);
                ConfigureImage(washRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.LightScrim : new Color(0.98f, 0.975f, 0.95f, 0.42f), sliced: false);
                AddApplier(washRt.gameObject, colorToken: UiThemeApplier.ColorToken.LightScrim);

                RectTransform contentRt = CreateUi("ContentRoot", popup.transform, typeof(CanvasRenderer), typeof(Image), typeof(UiLayoutScaleExempt));
                Place(contentRt, top, top, new Vector2(0f, -24f), new Vector2(960f, 1760f));
                ConfigureImage(contentRt.GetComponent<Image>(), null, Color.clear, sliced: false);

                // ---------- Header (centre y = 91) ----------
                RectTransform headerRt = CreateUi("HeaderBar", contentRt);
                headerRt.anchorMin = new Vector2(0f, 1f);
                headerRt.anchorMax = new Vector2(1f, 1f);
                headerRt.pivot = top;
                headerRt.anchoredPosition = new Vector2(0f, -37f);
                headerRt.sizeDelta = new Vector2(0f, 108f);

                RectTransform btnBackRt = CreateUi("BtnBack", headerRt, typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UiButtonFx));
                Place(btnBackRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-14f, 0f), new Vector2(118f, 118f));
                Image btnBackImg = ConfigureImage(btnBackRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.PanelButton : Color.white, sliced: false, raycast: true);
                btnBackImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialButton : null;
                AddApplier(btnBackRt.gameObject,
                    colorToken: UiThemeApplier.ColorToken.PanelButton,
                    spriteToken: UiThemeApplier.SpriteToken.ButtonCircle,
                    materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialButton);
                var backBtn = btnBackRt.GetComponent<Button>();
                backBtn.targetGraphic = btnBackImg;

                RectTransform backIconRt = CreateUi("Icon", btnBackRt, typeof(CanvasRenderer), typeof(Image));
                Place(backIconRt, middle, middle, new Vector2(-3f, 0f), new Vector2(44f, 58f));
                Image backIconImg = ConfigureImage(backIconRt.GetComponent<Image>(), sprites.GetValueOrDefault("sp_icon_back_arrow"), defaultTheme != null ? defaultTheme.BrandNavy : s_ThemesNavy, sliced: false);
                AddApplier(backIconRt.gameObject, colorToken: UiThemeApplier.ColorToken.BrandNavy);

                RectTransform titleGroupRt = CreateUi("TitleGroup", headerRt, typeof(HorizontalLayoutGroup));
                Place(titleGroupRt, middle, middle, Vector2.zero, new Vector2(660f, 108f));
                var titleHlg = titleGroupRt.GetComponent<HorizontalLayoutGroup>();
                titleHlg.childAlignment = TextAnchor.MiddleCenter;
                titleHlg.spacing = 18f;
                titleHlg.childControlWidth = false;
                titleHlg.childControlHeight = false;
                titleHlg.childForceExpandWidth = false;
                titleHlg.childForceExpandHeight = false;

                RectTransform ruleLeftRt = CreateUi("RuleLeft", titleGroupRt, typeof(CanvasRenderer), typeof(Image));
                ruleLeftRt.sizeDelta = new Vector2(98f, 3f);
                ConfigureImage(ruleLeftRt.GetComponent<Image>(), null, ruleColor, sliced: false);
                AddApplier(ruleLeftRt.gameObject, colorToken: UiThemeApplier.ColorToken.DividerHairline);

                RectTransform titleTextRt = CreateUi("Label_Title", titleGroupRt, typeof(TextMeshProUGUI));
                titleTextRt.sizeDelta = new Vector2(380f, 100f);
                TextMeshProUGUI titleText = ConfigureLabel(titleTextRt.GetComponent<TextMeshProUGUI>(), font, "THEMES", 88f, defaultTheme != null ? defaultTheme.BrandNavy : Color.white, TextAlignmentOptions.Center);
                titleText.characterSpacing = -2f;
                titleText.enableVertexGradient = false;
                AddApplier(titleTextRt.gameObject, colorToken: UiThemeApplier.ColorToken.BrandNavy);

                UiGlyphGraphic leaf = CreateGlyph("LeafDecor", titleTextRt, UiGlyphGraphic.GlyphShape.Leaf, new Vector2(30f, 54f), Color.white);
                Place((RectTransform)leaf.transform, middle, new Vector2(0.5f, 0f), new Vector2(22f, 30f), new Vector2(30f, 54f));
                leaf.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
                leaf.SetGradient(UiGlyphGraphic.GradientMode.Vertical, ParseHex("#6FD058"), ParseHex("#23933A"));
                leaf.gameObject.SetActive(false); // Baseline is Default

                RectTransform ruleRightRt = CreateUi("RuleRight", titleGroupRt, typeof(CanvasRenderer), typeof(Image));
                ruleRightRt.sizeDelta = new Vector2(98f, 3f);
                ConfigureImage(ruleRightRt.GetComponent<Image>(), null, ruleColor, sliced: false);
                AddApplier(ruleRightRt.gameObject, colorToken: UiThemeApplier.ColorToken.DividerHairline);

                // ---------- TabStrip (y 169, 889 x 92) ----------
                RectTransform tabStripRt = CreateUi("TabStrip", contentRt, typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup), typeof(UiTabStrip));
                Place(tabStripRt, top, top, new Vector2(0f, -163f), new Vector2(902f, 104f));
                Image tabStripImg = ConfigureImage(tabStripRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.PanelButton : Color.white, sliced: true);
                tabStripImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialButton : null;
                AddApplier(tabStripRt.gameObject,
                    colorToken: UiThemeApplier.ColorToken.PanelButton,
                    spriteToken: UiThemeApplier.SpriteToken.ButtonCapsule,
                    materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialButton);

                var tabHlg = tabStripRt.GetComponent<HorizontalLayoutGroup>();
                tabHlg.padding = new RectOffset(0, 0, 0, 0);
                tabHlg.spacing = 0f;
                tabHlg.childAlignment = TextAnchor.MiddleCenter;
                tabHlg.childControlWidth = true;
                tabHlg.childControlHeight = true;
                tabHlg.childForceExpandWidth = true;
                tabHlg.childForceExpandHeight = true;

                // Hairline between BOARD and EFFECTS; drawn first so an active pill covers it.
                RectTransform dividerRt = CreateUi("Divider", tabStripRt, typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                dividerRt.GetComponent<LayoutElement>().ignoreLayout = true;
                Place(dividerRt, new Vector2(2f / 3f, 0.5f), middle, Vector2.zero, new Vector2(2f, 60f));
                ConfigureImage(dividerRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.DividerHairline : new Color(s_ThemesNavy.r, s_ThemesNavy.g, s_ThemesNavy.b, 0.1f), sliced: false);
                AddApplier(dividerRt.gameObject, colorToken: UiThemeApplier.ColorToken.DividerHairline);

                var tabStripComp = tabStripRt.GetComponent<UiTabStrip>();
                var tabSegments = new UiTabStrip.TabSegment[3];
                var tabConfigs = new[]
                {
                    ("Tab_Balls", ThemeCategory.Ball, "BALLS", new Vector2(86f, 28f)),
                    ("Tab_Board", ThemeCategory.Board, "BOARD", new Vector2(42f, 42f)),
                    ("Tab_Effects", ThemeCategory.ClearEffect, "EFFECTS", new Vector2(52f, 54f))
                };

                for (int i = 0; i < 3; i++)
                {
                    var (tName, tCat, tLabel, tIconSize) = tabConfigs[i];
                    bool tActive = i == 0;
                    Color inkColor = tActive ? (defaultTheme != null ? defaultTheme.SelectedBadgeInk : Color.white) : (defaultTheme != null ? defaultTheme.BrandNavy : s_ThemesNavy);

                    RectTransform tRt = CreateUi(tName, tabStripRt, typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UiButtonFx));
                    Image tPillImg = ConfigureImage(tRt.GetComponent<Image>(), null, tActive ? (defaultTheme != null ? defaultTheme.SelectedBadgeFill : Color.white) : Color.clear, sliced: true, raycast: true);
                    tPillImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialButton : null;
                    var tBtn = tRt.GetComponent<Button>();
                    tBtn.targetGraphic = tPillImg;

                    HorizontalLayoutGroup tRow = CreateCenteredRow("Content", tRt, 22f);

                    Graphic tIconGraphic;
                    if (tCat == ThemeCategory.ClearEffect)
                    {
                        RectTransform iconRt = CreateUi("Icon", tRow.transform, typeof(CanvasRenderer), typeof(Image));
                        tIconGraphic = ConfigureImage(iconRt.GetComponent<Image>(), sprites.GetValueOrDefault("sp_icon_sparkle_fx"), inkColor, sliced: false);
                    }
                    else
                    {
                        var shape = tCat == ThemeCategory.Ball ? UiGlyphGraphic.GlyphShape.Dots : UiGlyphGraphic.GlyphShape.Grid;
                        tIconGraphic = CreateGlyph("Icon", tRow.transform, shape, tIconSize, inkColor);
                    }
                    SetPreferredSize(tIconGraphic, tIconSize);

                    RectTransform tLblRt = CreateUi("Label", tRow.transform, typeof(TextMeshProUGUI));
                    TextMeshProUGUI tLblText = ConfigureLabel(tLblRt.GetComponent<TextMeshProUGUI>(), font, tLabel, 31f, inkColor, TextAlignmentOptions.Center);

                    tabSegments[i] = new UiTabStrip.TabSegment
                    {
                        Category = tCat,
                        Button = tBtn,
                        BackgroundPill = tPillImg,
                        Label = tLblText,
                        Icon = tIconGraphic
                    };
                }

                var tabSo = new SerializedObject(tabStripComp);
                var segsProp = tabSo.FindProperty("m_Segments");
                segsProp.arraySize = 3;
                for (int i = 0; i < 3; i++)
                {
                    var elem = segsProp.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("Category").enumValueIndex = (int)tabSegments[i].Category;
                    elem.FindPropertyRelative("Button").objectReferenceValue = tabSegments[i].Button;
                    elem.FindPropertyRelative("BackgroundPill").objectReferenceValue = tabSegments[i].BackgroundPill;
                    elem.FindPropertyRelative("Label").objectReferenceValue = tabSegments[i].Label;
                    elem.FindPropertyRelative("Icon").objectReferenceValue = tabSegments[i].Icon;
                }
                tabSo.FindProperty("m_ActivePillColor").colorValue = defaultTheme != null ? defaultTheme.SelectedBadgeFill : Color.white;
                tabSo.FindProperty("m_InactiveTextColor").colorValue = defaultTheme != null ? defaultTheme.BrandNavy : s_ThemesNavy;
                tabSo.FindProperty("m_InactiveIconColor").colorValue = defaultTheme != null ? defaultTheme.BrandNavy : s_ThemesNavy;
                tabSo.ApplyModifiedProperties();

                // ---------- PreviewCard (y 288, 720 x 770) ----------
                RectTransform previewRt = CreateUi("PreviewCard", contentRt, typeof(CanvasRenderer), typeof(Image), typeof(UiThemePreviewPanel));
                Place(previewRt, top, top, new Vector2(0f, -288f), new Vector2(720f, 770f));
                Image previewImg = ConfigureImage(previewRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.PanelCard : ParseHex("#FCFBF7"), sliced: true);
                previewImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialCard : null;
                AddApplier(previewRt.gameObject,
                    colorToken: UiThemeApplier.ColorToken.PanelCard,
                    spriteToken: UiThemeApplier.SpriteToken.CardBackground,
                    materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialCard);
                var previewPanel = previewRt.GetComponent<UiThemePreviewPanel>();

                RectTransform pNameRt = CreateUi("Label_ThemeName", previewRt, typeof(TextMeshProUGUI));
                Place(pNameRt, top, middle, new Vector2(0f, -53f), new Vector2(660f, 60f));
                TextMeshProUGUI pNameText = ConfigureLabel(pNameRt.GetComponent<TextMeshProUGUI>(), font, "CRYSTAL GARDEN", 46f, defaultTheme != null ? defaultTheme.BrandNavy : s_ThemesNavy, TextAlignmentOptions.Center);
                AddApplier(pNameRt.gameObject, colorToken: UiThemeApplier.ColorToken.BrandNavy);

                // BoardMockRoot: 5 x 6 recessed cells
                RectTransform boardMockRt = CreateUi("BoardMockRoot", previewRt, typeof(CanvasRenderer), typeof(Image));
                Place(boardMockRt, middle, middle, new Vector2(0f, 19f), new Vector2(526f, 552f));
                Image boardMockImg = ConfigureImage(boardMockRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.PanelTray : ParseHex("#FBFAF6"), sliced: true);
                boardMockImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialCard : null;
                AddApplier(boardMockRt.gameObject,
                    colorToken: UiThemeApplier.ColorToken.PanelTray,
                    spriteToken: UiThemeApplier.SpriteToken.CardBackground,
                    materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialCard);

                RectTransform gridRt = CreateUi("Grid", boardMockRt, typeof(GridLayoutGroup));
                Stretch(gridRt, Vector2.zero, Vector2.zero);
                var gridLayout = gridRt.GetComponent<GridLayoutGroup>();
                gridLayout.cellSize = new Vector2(90f, 78f);
                gridLayout.spacing = new Vector2(13f, 12f);
                gridLayout.padding = new RectOffset(12, 12, 12, 12);
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 5;
                gridLayout.childAlignment = TextAnchor.MiddleCenter;

                for (int i = 0; i < 30; i++)
                {
                    RectTransform cellRt = CreateUi($"Cell_{i}", gridRt, typeof(CanvasRenderer), typeof(Image));
                    Image cellImg = ConfigureImage(cellRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.PanelCell : ParseHex("#F3F2EE"), sliced: true);
                    cellImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialCard : null;
                    AddApplier(cellRt.gameObject,
                        colorToken: UiThemeApplier.ColorToken.PanelCell,
                        spriteToken: UiThemeApplier.SpriteToken.CardBackground,
                        materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialCard);
                }

                RectTransform slotsRootRt = CreateUi("SlotsRoot", boardMockRt);
                Stretch(slotsRootRt, Vector2.zero, Vector2.zero);

                var centerBall = new Vector2(0f, 30f);
                UiGlyphGraphic glow = CreateGlyph("PreviewGlow", slotsRootRt, UiGlyphGraphic.GlyphShape.Halo, new Vector2(168f, 168f), new Color(1f, 0.82f, 0.32f, 0.95f));
                Place((RectTransform)glow.transform, middle, middle, centerBall, new Vector2(168f, 168f));

                // Diamond flower measured from the mockup (board centre = origin).
                var flowerPositions = new[]
                {
                    new Vector2(0f, 219f),     // 0: Red
                    new Vector2(-117f, 122f),  // 1: Orange
                    new Vector2(117f, 122f),   // 2: Yellow
                    centerBall,                // 3: Green (centre)
                    new Vector2(-127f, -50f),  // 4: Cyan
                    new Vector2(127f, -50f),   // 5: Purple
                    new Vector2(0f, -139f)     // 6: Blue
                };

                var slots = new UiPreviewSlot[7];
                for (int i = 0; i < 7; i++)
                {
                    RectTransform slotRt = CreateUi($"Slot_{i}", slotsRootRt, typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(UiPreviewSlot));
                    Place(slotRt, middle, middle, flowerPositions[i], new Vector2(110f, 110f));
                    ConfigureImage(slotRt.GetComponent<Image>(), null, Color.white, sliced: false);
                    slots[i] = slotRt.GetComponent<UiPreviewSlot>();
                }

                var sparkles = new[]
                {
                    (centerBall + new Vector2(38f, 76f), 52f),
                    (centerBall + new Vector2(70f, 32f), 32f),
                    (centerBall + new Vector2(-76f, -16f), 24f)
                };
                for (int i = 0; i < sparkles.Length; i++)
                {
                    var (position, size) = sparkles[i];
                    UiGlyphGraphic sparkle = CreateGlyph($"Sparkle_{i}", slotsRootRt, UiGlyphGraphic.GlyphShape.Sparkle, new Vector2(size, size), Color.white);
                    Place((RectTransform)sparkle.transform, middle, middle, position, new Vector2(size, size));
                    sparkle.SetGradient(UiGlyphGraphic.GradientMode.Radial, ParseHex("#FFF1A8"), ParseHex("#F0A21E"));
                }

                // SelectedBadge (bottom 20, 460 x 86)
                RectTransform selRt = CreateUi("SelectedBadge", previewRt, typeof(CanvasRenderer), typeof(Image));
                Place(selRt, bottom, bottom, new Vector2(0f, 20f), new Vector2(460f, 86f));
                Image selImg = ConfigureImage(selRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.SelectedBadgeFill : Color.white, sliced: true);
                selImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialButton : null;
                AddApplier(selRt.gameObject,
                    colorToken: UiThemeApplier.ColorToken.SelectedBadgeFill,
                    spriteToken: UiThemeApplier.SpriteToken.ButtonCapsulePrimary,
                    materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialButton);
                selRt.GetComponent<Image>().pixelsPerUnitMultiplier = 0.75f;

                HorizontalLayoutGroup selRow = CreateCenteredRow("Content", selRt, 26f);
                RectTransform selCheckRt = CreateUi("CheckIcon", selRow.transform, typeof(CanvasRenderer), typeof(Image));
                SetPreferredSize(selCheckRt, new Vector2(70f, 70f));
                Image selCheckImg = ConfigureImage(selCheckRt.GetComponent<Image>(), null, defaultTheme != null ? defaultTheme.SelectedBadgeInk : ParseHex("#2BA84A"), sliced: false);
                selCheckImg.material = defaultTheme != null ? defaultTheme.SurfaceMaterialButton : null;
                AddApplier(selCheckRt.gameObject,
                    colorToken: UiThemeApplier.ColorToken.SelectedBadgeInk,
                    spriteToken: UiThemeApplier.SpriteToken.ButtonCircle,
                    materialToken: UiThemeApplier.MaterialToken.SurfaceMaterialButton);

                RectTransform selCheckMarkRt = CreateUi("Mark", selCheckRt, typeof(CanvasRenderer), typeof(UiCheckGraphic));
                Place(selCheckMarkRt, middle, middle, Vector2.zero, new Vector2(40f, 40f));
                var selCheckGraphic = selCheckMarkRt.GetComponent<UiCheckGraphic>();
                selCheckGraphic.color = Color.white;
                selCheckGraphic.Thickness = 7f;
                selCheckGraphic.raycastTarget = false;

                RectTransform selTextRt = CreateUi("Label", selRow.transform, typeof(TextMeshProUGUI));
                ConfigureLabel(selTextRt.GetComponent<TextMeshProUGUI>(), font, "SELECTED", 40f, defaultTheme != null ? defaultTheme.SelectedBadgeInk : ParseHex("#17662D"), TextAlignmentOptions.Center);
                AddApplier(selTextRt.gameObject, colorToken: UiThemeApplier.ColorToken.SelectedBadgeInk);

                // EmptyStateRoot (BOARD / EFFECTS tabs)
                RectTransform emptyRt = CreateUi("EmptyStateRoot", previewRt);
                Stretch(emptyRt, Vector2.zero, Vector2.zero);
                emptyRt.gameObject.SetActive(false);

                UiGlyphGraphic emptyIcon = CreateGlyph("Icon", emptyRt, UiGlyphGraphic.GlyphShape.Grid, new Vector2(80f, 80f), ParseHex("#8C9BB0"));
                Place((RectTransform)emptyIcon.transform, middle, middle, new Vector2(0f, 30f), new Vector2(80f, 80f));

                RectTransform emptyTextRt = CreateUi("Label_Empty", emptyRt, typeof(TextMeshProUGUI));
                Place(emptyTextRt, middle, middle, new Vector2(0f, -40f), new Vector2(500f, 44f));
                TextMeshProUGUI emptyText = ConfigureLabel(emptyTextRt.GetComponent<TextMeshProUGUI>(), font, "COMING SOON", 30f, s_ThemesNavy, TextAlignmentOptions.Center);

                var panelSo = new SerializedObject(previewPanel);
                panelSo.FindProperty("m_ThemeNameLabel").objectReferenceValue = pNameText;
                panelSo.FindProperty("m_BoardMockRoot").objectReferenceValue = boardMockRt.gameObject;
                panelSo.FindProperty("m_PreviewGlow").objectReferenceValue = glow;
                panelSo.FindProperty("m_SelectedBadge").objectReferenceValue = selRt.gameObject;
                panelSo.FindProperty("m_EmptyStateRoot").objectReferenceValue = emptyRt.gameObject;
                panelSo.FindProperty("m_EmptyStateLabel").objectReferenceValue = emptyText;
                var slotsProp = panelSo.FindProperty("m_Slots");
                slotsProp.arraySize = 7;
                for (int i = 0; i < 7; i++)
                {
                    slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
                }
                panelSo.ApplyModifiedProperties();

                // ---------- ItemsContainer (y 1081, 918 wide, 25 gap) ----------
                RectTransform itemsRt = CreateUi("ItemsContainer", contentRt, typeof(VerticalLayoutGroup));
                Place(itemsRt, top, top, new Vector2(0f, -1075f), new Vector2(930f, 610f));
                var itemsVlg = itemsRt.GetComponent<VerticalLayoutGroup>();
                itemsVlg.spacing = 13f;
                itemsVlg.childAlignment = TextAnchor.UpperCenter;
                itemsVlg.childControlWidth = true;
                itemsVlg.childControlHeight = false;
                itemsVlg.childForceExpandWidth = true;
                itemsVlg.childForceExpandHeight = false;

                // ---------- Footer (centre 24.5 above the content bottom) ----------
                RectTransform footerGroupRt = CreateUi("FooterGroup", contentRt, typeof(HorizontalLayoutGroup));
                Place(footerGroupRt, bottom, middle, new Vector2(0f, 24.5f), new Vector2(760f, 40f));
                var footerHlg = footerGroupRt.GetComponent<HorizontalLayoutGroup>();
                footerHlg.childAlignment = TextAnchor.MiddleCenter;
                footerHlg.spacing = 23f;
                footerHlg.childControlWidth = true;
                footerHlg.childControlHeight = true;
                footerHlg.childForceExpandWidth = false;
                footerHlg.childForceExpandHeight = false;

                RectTransform footerRuleLeftRt = CreateUi("RuleLeft", footerGroupRt, typeof(CanvasRenderer), typeof(Image));
                SetPreferredSize(footerRuleLeftRt, new Vector2(67f, 2.5f));
                ConfigureImage(footerRuleLeftRt.GetComponent<Image>(), null, ruleColor, sliced: false);
                AddApplier(footerRuleLeftRt.gameObject, colorToken: UiThemeApplier.ColorToken.DividerHairline);

                RectTransform footerRt = CreateUi("FooterNote", footerGroupRt, typeof(TextMeshProUGUI));
                TextMeshProUGUI footerText = ConfigureLabel(footerRt.GetComponent<TextMeshProUGUI>(), font, "THEMES CHANGE VISUALS ONLY", 25f, defaultTheme != null ? defaultTheme.BrandNavy : s_ThemesNavy, TextAlignmentOptions.Center);
                footerText.fontStyle = FontStyles.Normal;
                footerText.characterSpacing = 5f;
                AddApplier(footerRt.gameObject, colorToken: UiThemeApplier.ColorToken.BrandNavy);

                RectTransform footerRuleRightRt = CreateUi("RuleRight", footerGroupRt, typeof(CanvasRenderer), typeof(Image));
                SetPreferredSize(footerRuleRightRt, new Vector2(67f, 2.5f));
                ConfigureImage(footerRuleRightRt.GetComponent<Image>(), null, ruleColor, sliced: false);
                AddApplier(footerRuleRightRt.gameObject, colorToken: UiThemeApplier.ColorToken.DividerHairline);

                // Wire CosmeticsPopup & PopupView
                var popupSo = new SerializedObject(cosmeticsPopup);
                popupSo.FindProperty("m_ModalContainer").objectReferenceValue = contentRt;
                popupSo.FindProperty("m_ContentRoot").objectReferenceValue = contentRt;
                popupSo.FindProperty("m_FullscreenBackdrop").objectReferenceValue = backdropImage;
                popupSo.FindProperty("m_CrystalBackdropTexture").objectReferenceValue = crystalBackdrop;
                popupSo.FindProperty("m_CanvasGroup").objectReferenceValue = canvasGroup;
                popupSo.FindProperty("m_BackButton").objectReferenceValue = backBtn;
                popupSo.FindProperty("m_CloseButton").objectReferenceValue = backBtn;
                popupSo.FindProperty("m_TitleLabel").objectReferenceValue = titleText;
                popupSo.FindProperty("m_TitleText").objectReferenceValue = titleText;
                popupSo.FindProperty("m_TabStrip").objectReferenceValue = tabStripComp;
                popupSo.FindProperty("m_PreviewPanel").objectReferenceValue = previewPanel;
                popupSo.FindProperty("m_ItemsContainer").objectReferenceValue = itemsRt;
                popupSo.FindProperty("m_ItemPrefab").objectReferenceValue = itemPrefab;
                popupSo.FindProperty("m_FooterNote").objectReferenceValue = footerText;
                popupSo.FindProperty("m_BrandNavy").colorValue = defaultTheme != null ? defaultTheme.BrandNavy : s_ThemesNavy;
                popupSo.ApplyModifiedProperties();

                var modalSo = new SerializedObject(responsiveModal);
                modalSo.FindProperty("m_Popup").objectReferenceValue = cosmeticsPopup;
                modalSo.ApplyModifiedProperties();

                PrefabUtility.SaveAsPrefabAsset(popup, prefabPath);
                if (isNew)
                {
                    UnityEngine.Object.DestroyImmediate(popup);
                }

                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
            finally
            {
                if (!isNew && popup != null)
                {
                    PrefabUtility.UnloadPrefabContents(popup);
                }
            }
        }

        private static GameObject BuildButtonIconThemesPrefab(string prefabPath, string basePrefabPath, Dictionary<string, Sprite> sprites)
        {
            if (File.Exists(prefabPath))
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            GameObject inst;
            if (basePrefab != null)
            {
                inst = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            }
            else
            {
                inst = new GameObject("Button_Icon_Themes", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UiButtonFx), typeof(UiActionButton));
            }

            inst.name = "Button_Icon_Themes";
            var iconGraphic = inst.GetComponentInChildren<HudIconGraphic>();
            if (iconGraphic != null)
            {
                iconGraphic.Kind = HudIconGraphic.IconKind.Palette;
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
            UnityEngine.Object.DestroyImmediate(inst);
            return saved;
        }

        private static void UpdatePanelBrandBarPrefab(string prefabPath, GameObject btnThemesPrefab)
        {
            if (!File.Exists(prefabPath)) return;
            var contents = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                Transform shadowSettings = contents.transform.Find("Shadow_BtnSettings");
                Transform existingShadowThemes = contents.transform.Find("Shadow_BtnThemes");
                if (existingShadowThemes == null && shadowSettings != null)
                {
                    var shadowObj = UnityEngine.Object.Instantiate(shadowSettings.gameObject, contents.transform);
                    shadowObj.name = "Shadow_BtnThemes";
                    var rt = shadowObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 0.5f);
                    rt.anchorMax = new Vector2(1f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-372f, -16f);
                    rt.sizeDelta = new Vector2(140f, 140f);
                    shadowObj.transform.SetSiblingIndex(shadowSettings.GetSiblingIndex());
                }

                Transform existingBtnThemes = contents.transform.Find("BtnThemes");
                if (existingBtnThemes == null)
                {
                    GameObject btnThemesObj;
                    if (btnThemesPrefab != null)
                    {
                        btnThemesObj = (GameObject)PrefabUtility.InstantiatePrefab(btnThemesPrefab, contents.transform);
                    }
                    else
                    {
                        var btnSettings = contents.transform.Find("BtnSettings");
                        btnThemesObj = UnityEngine.Object.Instantiate(btnSettings.gameObject, contents.transform);
                    }

                    btnThemesObj.name = "BtnThemes";
                    var rt = btnThemesObj.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1f, 0.5f);
                    rt.anchorMax = new Vector2(1f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(-372f, -4f);
                    rt.sizeDelta = new Vector2(108f, 108f);

                    var icon = btnThemesObj.GetComponentInChildren<HudIconGraphic>();
                    if (icon != null)
                    {
                        icon.Kind = HudIconGraphic.IconKind.Palette;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void UpdateSettingsPopupPrefab(string settingsPrefabPath)
        {
            if (!File.Exists(settingsPrefabPath)) return;
            var settingsContents = PrefabUtility.LoadPrefabContents(settingsPrefabPath);
            try
            {
                var settingsPopup = settingsContents.GetComponent<SettingsPopup>();
                var btnSec = settingsContents.transform.Find("Container/BtnSecondary");
                if (settingsPopup != null && btnSec != null)
                {
                    var btnComp = btnSec.GetComponent<Button>();
                    var settingsSo = new SerializedObject(settingsPopup);
                    settingsSo.FindProperty("m_CosmeticsButton").objectReferenceValue = btnComp;
                    settingsSo.ApplyModifiedProperties();

                    var label = btnSec.Find("Label");
                    if (label != null)
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
        }

        private static void UpdateUiRootPrefab(string uiRootPath, string cosmeticsPrefabPath, string catalogPath)
        {
            if (!File.Exists(uiRootPath)) return;
            var rootContents = PrefabUtility.LoadPrefabContents(uiRootPath);
            try
            {
                var uiShell = rootContents.GetComponent<UiShell>();
                var hudPresenter = rootContents.GetComponent<HudPresenter>();
                var popupsT = rootContents.transform.Find("Canvas_Popups");
                var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(catalogPath);
                var cosmeticsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cosmeticsPrefabPath);

                if (popupsT != null && cosmeticsPrefab != null)
                {
                    var existingCosmetics = popupsT.Find("CosmeticsPopup");
                    if (existingCosmetics != null)
                    {
                        UnityEngine.Object.DestroyImmediate(existingCosmetics.gameObject);
                    }

                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(cosmeticsPrefab, popupsT);
                    inst.name = "CosmeticsPopup";
                    var comp = inst.GetComponent<CosmeticsPopup>();

                    if (uiShell != null)
                    {
                        var shellSo = new SerializedObject(uiShell);
                        shellSo.FindProperty("m_CosmeticsPopup").objectReferenceValue = comp;
                        shellSo.FindProperty("m_ThemeCatalog").objectReferenceValue = catalog;
                        shellSo.ApplyModifiedProperties();
                    }
                }

                if (hudPresenter != null)
                {
                    Button btnThemes = rootContents.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.gameObject.name == "BtnThemes");
                    UiActionButton actionBtn = btnThemes != null ? btnThemes.GetComponent<UiActionButton>() : null;

                    var hudSo = new SerializedObject(hudPresenter);
                    if (btnThemes != null) hudSo.FindProperty("m_ThemesButton").objectReferenceValue = btnThemes;
                    if (actionBtn != null) hudSo.FindProperty("m_ThemesActionButton").objectReferenceValue = actionBtn;
                    hudSo.ApplyModifiedProperties();
                }

                PrefabUtility.SaveAsPrefabAsset(rootContents, uiRootPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(rootContents);
            }
        }

        private static void UpdateOpenScene(string cosmeticsPrefabPath, string catalogPath)
        {
            var sceneShell = UnityEngine.Object.FindAnyObjectByType<UiShell>();
            var sceneHud = UnityEngine.Object.FindAnyObjectByType<HudPresenter>();
            var catalog = AssetDatabase.LoadAssetAtPath<ThemeCatalogSO>(catalogPath);
            var cosmeticsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cosmeticsPrefabPath);

            if (sceneShell != null)
            {
                var popupsT = sceneShell.transform.Find("Canvas_Popups");
                if (popupsT != null && cosmeticsPrefab != null)
                {
                    bool isUiRootInstance = PrefabUtility.IsPartOfPrefabInstance(sceneShell.gameObject);
                    if (isUiRootInstance)
                    {
                        // UI_Root.prefab already carries the popup: restore it if the scene removed it.
                        foreach (var removed in PrefabUtility.GetRemovedGameObjects(sceneShell.gameObject))
                        {
                            if (removed.assetGameObject != null && removed.assetGameObject.GetComponent<CosmeticsPopup>() != null)
                            {
                                removed.Revert(InteractionMode.AutomatedAction);
                            }
                        }
                    }

                    // Drop every scene-local copy; earlier runs only removed the first match and
                    // left one extra popup per run.
                    CosmeticsPopup prefabProvided = null;
                    var popupsInScene = popupsT.GetComponentsInChildren<CosmeticsPopup>(true);
                    for (int i = 0; i < popupsInScene.Length; i++)
                    {
                        GameObject popupGo = popupsInScene[i].gameObject;
                        if (popupGo.transform.parent != popupsT) continue;

                        bool fromUiRoot = isUiRootInstance && !PrefabUtility.IsAddedGameObjectOverride(popupGo);
                        if (fromUiRoot && prefabProvided == null)
                        {
                            prefabProvided = popupsInScene[i];
                        }
                        else if (!fromUiRoot)
                        {
                            UnityEngine.Object.DestroyImmediate(popupGo);
                        }
                    }

                    CosmeticsPopup sceneCosmetics = prefabProvided;
                    if (sceneCosmetics == null)
                    {
                        var inst = (GameObject)PrefabUtility.InstantiatePrefab(cosmeticsPrefab, popupsT);
                        inst.name = "CosmeticsPopup";
                        sceneCosmetics = inst.GetComponent<CosmeticsPopup>();
                    }

                    var shellSo = new SerializedObject(sceneShell);
                    shellSo.FindProperty("m_CosmeticsPopup").objectReferenceValue = sceneCosmetics;
                    shellSo.FindProperty("m_ThemeCatalog").objectReferenceValue = catalog;
                    shellSo.ApplyModifiedProperties();
                }

                if (sceneHud != null)
                {
                    Button btnThemes = sceneShell.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.gameObject.name == "BtnThemes");
                    UiActionButton actionBtn = btnThemes != null ? btnThemes.GetComponent<UiActionButton>() : null;

                    var hudSo = new SerializedObject(sceneHud);
                    if (btnThemes != null) hudSo.FindProperty("m_ThemesButton").objectReferenceValue = btnThemes;
                    if (actionBtn != null) hudSo.FindProperty("m_ThemesActionButton").objectReferenceValue = actionBtn;
                    hudSo.ApplyModifiedProperties();
                }

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sceneShell.gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sceneShell.gameObject.scene);
            }
        }
    }
}

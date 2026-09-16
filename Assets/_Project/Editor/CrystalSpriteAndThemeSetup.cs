using System.Collections.Generic;
using System.IO;
using Line98.Data;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Line98.Editor
{
    /// <summary>
    /// Sets up crystal sprites, recolors gem 7 to blue (D9 parity),
    /// wires new theme tokens for both Classic and Crystal themes,
    /// creates PreviewSpriteSet_Crystal, and applies data fixes.
    /// </summary>
    public static class CrystalSpriteAndThemeSetup
    {
        private const string Sheet2Path = "Assets/Art/Sprites/UI/Crystal/sprites_2__crystal.png";
        private const string Sheet1Path = "Assets/Art/Sprites/UI/Crystal/sprites_1__crystal.png";
        private const string PreviewSetCrystalPath = "Assets/_Project/Content/Themes/UI/PreviewSpriteSet_Crystal.asset";
        private const string UiThemeCrystalPath = "Assets/_Project/Content/Themes/UI/UiTheme_Crystal.asset";
        private const string UiThemeDefaultPath = "Assets/_Project/Content/Themes/UI/UiTheme_Default.asset";
        private const string BallThemeCrystalPath = "Assets/_Project/Content/Definitions/BallTheme_Crystal.asset";
        private const string ClearEffectCrystalPath = "Assets/_Project/Content/Definitions/ClearEffect_Crystal.asset";
        private const string BoardThemeClassicPath = "Assets/_Project/Content/Definitions/BoardTheme_Classic.asset";
        private const string BoardThemeCrystalPath = "Assets/_Project/Content/Definitions/BoardTheme_Crystal.asset";

        [MenuItem("Line98/Authoring/Setup Crystal Sprites and Theme Data")]
        public static void RunSetup()
        {
            RecolorGem7ToBlue(Sheet2Path);
            RecolorGem7ToBlue(Sheet1Path);

            SliceSheet(Sheet2Path);
            SliceSheet(Sheet1Path);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Dictionary<string, Sprite> sprites2 = LoadSprites(Sheet2Path);
            CreatePreviewSpriteSetCrystal(sprites2);
            ConfigureUiThemeCrystal(sprites2);
            ConfigureUiThemeDefault();
            FixBallThemeCrystal();
            FixClearEffectCrystal();
            FixBoardThemes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CrystalSpriteAndThemeSetup] Setup completed successfully!");
        }

        private static void RecolorGem7ToBlue(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool wasReadable = importer.isReadable;
            importer.isReadable = true;
            importer.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return;

            var colors = tex.GetPixels32();
            int w = tex.width;
            int h = tex.height;

            // Recolor gem 7 region: x: 930..1070, y: 1090..1235
            int minX = Mathf.Clamp(930, 0, w - 1);
            int maxX = Mathf.Clamp(1075, 0, w - 1);
            int minY = Mathf.Clamp(1090, 0, h - 1);
            int maxY = Mathf.Clamp(1235, 0, h - 1);

            bool modified = false;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int idx = y * w + x;
                    Color32 c = colors[idx];
                    if (c.a < 15) continue;

                    Color.RGBToHSV(c, out float hue, out float sat, out float val);
                    // Magenta hue is around 0.80 - 0.98 or near 0.04
                    if (hue >= 0.80f || hue <= 0.04f)
                    {
                        // Shift to Deep Sapphire Blue matching M_Ball_Crystal_Blue (hue 0.620f)
                        Color newCol = Color.HSVToRGB(0.620f, sat, val);
                        newCol.a = c.a / 255f;
                        colors[idx] = newCol;
                        modified = true;
                    }
                }
            }

            if (modified)
            {
                var newTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                newTex.SetPixels32(colors);
                newTex.Apply(false);
                byte[] png = newTex.EncodeToPNG();
                Object.DestroyImmediate(newTex);
                File.WriteAllBytes(path, png);
            }

            importer.isReadable = wasReadable;
            importer.SaveAndReimport();
        }

        private static void SliceSheet(string path)
        {
            var texImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (texImporter == null) return;

            texImporter.spriteImportMode = SpriteImportMode.Multiple;
            texImporter.spritePixelsToUnits = 100f;
            texImporter.SaveAndReimport();

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(texImporter);
            dataProvider.InitSpriteEditorDataProvider();

            var spriteRects = new List<SpriteRect>
            {
                // Gems
                CreateSpriteRect("sp_gem_red", new Rect(38, 1098, 128, 128)),
                CreateSpriteRect("sp_gem_orange", new Rect(188, 1098, 128, 128)),
                CreateSpriteRect("sp_gem_yellow", new Rect(338, 1098, 128, 128)),
                CreateSpriteRect("sp_gem_green", new Rect(488, 1098, 128, 128)),
                CreateSpriteRect("sp_gem_cyan", new Rect(636, 1098, 128, 128)),
                CreateSpriteRect("sp_gem_purple", new Rect(784, 1098, 128, 128)),
                CreateSpriteRect("sp_gem_blue", new Rect(936, 1098, 128, 128)),
                CreateSpriteRect("sp_gem_magenta", new Rect(936, 1098, 128, 128)), // alias pointing to 7th ball
                CreateSpriteRect("sp_gem_shadow", new Rect(1095, 1101, 124, 44)),

                // Navigation & Menu Icons
                CreateSpriteRect("sp_icon_back", new Rect(30, 332, 44, 66)),
                CreateSpriteRect("sp_icon_back_arrow", new Rect(30, 332, 44, 66)),
                CreateSpriteRect("sp_icon_close", new Rect(114, 333, 62, 64)),
                CreateSpriteRect("sp_icon_play", new Rect(214, 331, 60, 68)),
                CreateSpriteRect("sp_icon_calendar", new Rect(308, 328, 70, 76)),
                CreateSpriteRect("sp_icon_lotus", new Rect(404, 332, 88, 64)),
                CreateSpriteRect("sp_icon_chart", new Rect(518, 330, 78, 70)),
                CreateSpriteRect("sp_icon_trophy", new Rect(624, 330, 72, 72)),
                CreateSpriteRect("sp_icon_gear", new Rect(728, 326, 78, 76)),
                CreateSpriteRect("sp_icon_undo", new Rect(836, 328, 70, 70)),
                CreateSpriteRect("sp_icon_hint", new Rect(950, 326, 48, 66)),
                CreateSpriteRect("sp_icon_music", new Rect(1044, 328, 68, 76)),
                CreateSpriteRect("sp_icon_sfx", new Rect(1146, 330, 76, 66)),
                CreateSpriteRect("sp_icon_sound", new Rect(1146, 330, 76, 66)),
                CreateSpriteRect("sp_icon_share", new Rect(576, 243, 50, 62)),
                CreateSpriteRect("sp_icon_flame", new Rect(1158, 238, 64, 80)),
                CreateSpriteRect("sp_icon_crown", new Rect(1110, 118, 120, 112)),
                CreateSpriteRect("sp_icon_leaf", new Rect(407, 328, 78, 76)),
                CreateSpriteRect("sp_icon_globe", new Rect(176, 234, 74, 76)),
                CreateSpriteRect("sp_icon_no_ads", new Rect(253, 234, 82, 76)),
                CreateSpriteRect("sp_icon_restore", new Rect(337, 234, 76, 76)),
                CreateSpriteRect("sp_icon_shield", new Rect(416, 234, 76, 76)),
                CreateSpriteRect("sp_icon_mail", new Rect(495, 234, 78, 76)),
                CreateSpriteRect("sp_icon_vibrate", new Rect(24, 234, 64, 76)),
                CreateSpriteRect("sp_icon_sparkle_fx", new Rect(99, 234, 72, 76)),
                CreateSpriteRect("sp_icon_external_link", new Rect(963, 234, 82, 76)),

                // Buttons & Capsules (with 9-slice borders)
                CreateSpriteRect("sp_btn_capsule_green", new Rect(20, 674, 230, 94), new Vector4(44, 44, 44, 44)),
                CreateSpriteRect("sp_btn_capsule_darkgreen", new Rect(252, 675, 214, 92), new Vector4(44, 44, 44, 44)),
                CreateSpriteRect("sp_btn_capsule_zen", new Rect(465, 677, 210, 89), new Vector4(42, 42, 42, 42)),
                CreateSpriteRect("sp_btn_capsule_cream", new Rect(677, 679, 170, 84), new Vector4(40, 40, 40, 40)),
                CreateSpriteRect("sp_btn_capsule_beige", new Rect(848, 678, 172, 85), new Vector4(40, 40, 40, 40)),
                CreateSpriteRect("sp_btn_circle_cream", new Rect(1026, 670, 102, 102)),
                CreateSpriteRect("sp_btn_circle_grey", new Rect(1135, 669, 102, 103)),
                CreateSpriteRect("sp_btn_view_gold", new Rect(677, 679, 170, 84), new Vector4(30, 30, 30, 30)),

                // Cards & Surfaces
                CreateSpriteRect("sp_ui_card_cream", new Rect(22, 516, 242, 142), new Vector4(32, 32, 32, 32)),
                CreateSpriteRect("sp_ui_card_gold", new Rect(425, 530, 165, 110), new Vector4(32, 32, 32, 32)),
                CreateSpriteRect("sp_ui_card_small_cream", new Rect(267, 530, 156, 110), new Vector4(32, 32, 32, 32)),
                CreateSpriteRect("sp_ui_card_green", new Rect(591, 530, 168, 110), new Vector4(32, 32, 32, 32)),
                CreateSpriteRect("sp_ui_card_grey", new Rect(759, 530, 167, 110), new Vector4(32, 32, 32, 32)),
                CreateSpriteRect("sp_pill_long_cream", new Rect(934, 587, 302, 68), new Vector4(32, 32, 32, 32)),
                CreateSpriteRect("sp_pill_small_green", new Rect(933, 517, 149, 65), new Vector4(30, 30, 30, 30)),
                CreateSpriteRect("sp_pill_small_cream", new Rect(1087, 517, 148, 65), new Vector4(30, 30, 30, 30)),

                // Toggles & Miscellaneous
                CreateSpriteRect("sp_toggle_track_on", new Rect(24, 432, 126, 60), new Vector4(28, 28, 28, 28)),
                CreateSpriteRect("sp_toggle_track_off", new Rect(154, 432, 126, 60), new Vector4(28, 28, 28, 28)),
                CreateSpriteRect("sp_toggle_knob", new Rect(1028, 674, 92, 92)),
                CreateSpriteRect("sp_divider_hairline", new Rect(904, 457, 122, 13)),

                // Game-over popup glyphs. Both sit in the sheet's unsliced gaps, so they are
                // listed here to survive a re-slice pass.
                CreateSpriteRect("sp_icon_dots_grid", new Rect(1057, 240, 69, 65)),
                CreateSpriteRect("sp_fx_sunburst_gold", new Rect(470, 130, 92, 92))
            };

            dataProvider.SetSpriteRects(spriteRects.ToArray());
            dataProvider.Apply();
            texImporter.SaveAndReimport();
        }

        private static SpriteRect CreateSpriteRect(string name, Rect rect, Vector4 border = default)
        {
            return new SpriteRect
            {
                name = name,
                rect = rect,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = border,
                spriteID = GUID.Generate()
            };
        }

        private static Dictionary<string, Sprite> LoadSprites(string sheetPath)
        {
            AssetDatabase.ImportAsset(sheetPath, ImportAssetOptions.ForceUpdate);
            var dict = new Dictionary<string, Sprite>();
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sheetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite s)
                {
                    dict[s.name] = s;
                }
            }
            return dict;
        }

        private static void CreatePreviewSpriteSetCrystal(Dictionary<string, Sprite> sprites)
        {
            var set = AssetDatabase.LoadAssetAtPath<UiPreviewSpriteSetSO>(PreviewSetCrystalPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<UiPreviewSpriteSetSO>();
                AssetDatabase.CreateAsset(set, PreviewSetCrystalPath);
            }

            Sprite red = sprites.GetValueOrDefault("sp_gem_red");
            Sprite orange = sprites.GetValueOrDefault("sp_gem_orange");
            Sprite yellow = sprites.GetValueOrDefault("sp_gem_yellow");
            Sprite green = sprites.GetValueOrDefault("sp_gem_green");
            Sprite cyan = sprites.GetValueOrDefault("sp_gem_cyan");
            Sprite purple = sprites.GetValueOrDefault("sp_gem_purple");
            Sprite blue = sprites.GetValueOrDefault("sp_gem_blue") ?? sprites.GetValueOrDefault("sp_gem_magenta");

            set.SetSprites(red, orange, yellow, green, cyan, purple, blue);
            EditorUtility.SetDirty(set);
        }

        private static void ConfigureUiThemeCrystal(Dictionary<string, Sprite> sprites)
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(UiThemeCrystalPath);
            if (theme == null) return;

            var so = new SerializedObject(theme);
            var previewSet = AssetDatabase.LoadAssetAtPath<UiPreviewSpriteSetSO>(PreviewSetCrystalPath);
            so.FindProperty("m_PreviewSpriteSet").objectReferenceValue = previewSet;

            // Surface colors
            // The sprite carries the gradient; this gentle tint brings its saturation in line
            // with the softer green from the Crystal menu reference.
            so.FindProperty("m_SurfacePrimary").colorValue = new Color(1f, 0.90f, 1f, 1f);
            so.FindProperty("m_SurfaceSecondary").colorValue = new Color(0.969f, 0.961f, 0.933f, 1f);     // #F7F5EE Daily cream
            so.FindProperty("m_SurfaceTertiaryZen").colorValue = new Color(0.839f, 0.910f, 0.969f, 1f);   // #D6E8F7 Zen pale blue
            so.FindProperty("m_InkButtonPrimary").colorValue = Color.white;
            so.FindProperty("m_StreakDotOn").colorValue = new Color(0.145f, 0.722f, 0.365f, 1f);          // #25B85D
            so.FindProperty("m_StreakDotOff").colorValue = new Color(0.812f, 0.839f, 0.863f, 1f);         // #CFD6DC

            // Sprites
            so.FindProperty("m_ButtonCapsulePrimary").objectReferenceValue = sprites.GetValueOrDefault("sp_btn_capsule_green");
            so.FindProperty("m_ButtonCapsuleSprite").objectReferenceValue = sprites.GetValueOrDefault("sp_btn_capsule_cream");
            so.FindProperty("m_ButtonCircleSprite").objectReferenceValue = sprites.GetValueOrDefault("sp_btn_circle_cream");
            so.FindProperty("m_CardBackgroundSprite").objectReferenceValue = sprites.GetValueOrDefault("sp_ui_card_cream");
            so.FindProperty("m_CardGoldSprite").objectReferenceValue = sprites.GetValueOrDefault("sp_ui_card_gold");
            so.FindProperty("m_IconPlay").objectReferenceValue = sprites.GetValueOrDefault("sp_icon_play");
            so.FindProperty("m_IconCalendar").objectReferenceValue = sprites.GetValueOrDefault("sp_icon_calendar");
            so.FindProperty("m_IconLotus").objectReferenceValue = sprites.GetValueOrDefault("sp_icon_lotus");
            so.FindProperty("m_IconChart").objectReferenceValue = sprites.GetValueOrDefault("sp_icon_chart");
            so.FindProperty("m_IconGear").objectReferenceValue = sprites.GetValueOrDefault("sp_icon_gear");
            so.FindProperty("m_IconFlame").objectReferenceValue = sprites.GetValueOrDefault("sp_icon_flame");
            so.FindProperty("m_IconCrown").objectReferenceValue = sprites.GetValueOrDefault("sp_icon_crown");

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        private static void ConfigureUiThemeDefault()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UiThemeSO>(UiThemeDefaultPath);
            if (theme == null) return;

            var so = new SerializedObject(theme);
            so.FindProperty("m_SurfacePrimary").colorValue = new Color(0.18f, 0.52f, 0.87f, 1f);
            so.FindProperty("m_SurfaceSecondary").colorValue = new Color(0.91f, 0.93f, 0.95f, 1f);
            so.FindProperty("m_SurfaceTertiaryZen").colorValue = new Color(0.76f, 0.82f, 0.89f, 1f);
            so.FindProperty("m_InkButtonPrimary").colorValue = Color.white;
            so.FindProperty("m_StreakDotOn").colorValue = new Color(0.18f, 0.83f, 0.45f, 1f);
            so.FindProperty("m_StreakDotOff").colorValue = new Color(0.45f, 0.49f, 0.55f, 1f);

            var cardMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/UI/M_Ui_Card.mat");
            var buttonMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/UI/M_Ui_Action.mat");
            so.FindProperty("m_SurfaceMaterialCard").objectReferenceValue = cardMat;
            so.FindProperty("m_SurfaceMaterialButton").objectReferenceValue = buttonMat;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        private static void FixBallThemeCrystal()
        {
            var theme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(BallThemeCrystalPath);
            if (theme == null) return;

            var so = new SerializedObject(theme);
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/T_Ball_InnerRefraction_Mask.png");
            var shadowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_BlobShadow.mat");
            var blueMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Themes/Crystal/M_Ball_Crystal_Blue.mat");

            so.FindProperty("m_InnerRefractionMask").objectReferenceValue = mask;
            so.FindProperty("m_BlobShadowMaterial").objectReferenceValue = shadowMat;

            var ballMatsProp = so.FindProperty("m_BallMaterials");
            if (ballMatsProp != null && ballMatsProp.arraySize >= 7)
            {
                ballMatsProp.GetArrayElementAtIndex(6).objectReferenceValue = blueMat;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(theme);
        }

        private static void FixClearEffectCrystal()
        {
            var effect = AssetDatabase.LoadAssetAtPath<ClearEffectSO>(ClearEffectCrystalPath);
            if (effect == null) return;

            var so = new SerializedObject(effect);
            so.FindProperty("m_ClearBurstVfxKey").stringValue = "Vfx_ClearBurst";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);
        }

        private static void FixBoardThemes()
        {
            var cellMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Meshes/MESH_BoardCell.asset");
            var frameMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Meshes/MESH_BoardFrame.asset");

            void UpdateBoardTheme(string path)
            {
                var theme = AssetDatabase.LoadAssetAtPath<BoardThemeSO>(path);
                if (theme == null) return;
                var so = new SerializedObject(theme);
                so.FindProperty("m_BoardCellMesh").objectReferenceValue = cellMesh;
                so.FindProperty("m_BoardFrameMesh").objectReferenceValue = frameMesh;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(theme);
            }

            UpdateBoardTheme(BoardThemeClassicPath);
            UpdateBoardTheme(BoardThemeCrystalPath);
        }
    }
}

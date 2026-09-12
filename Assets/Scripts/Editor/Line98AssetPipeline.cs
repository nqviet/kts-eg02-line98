#if UNITY_EDITOR
using System.IO;
using Line98.Design;
using UnityEditor;
using UnityEngine;

namespace Line98.Editor
{
    /// <summary>
    /// Automation pipeline to configure texture importers, create URP materials,
    /// generate VFX prefabs, wire ScriptableObject definitions, and bind high-fidelity visual assets.
    /// </summary>
    public static class Line98AssetPipeline
    {
        [MenuItem("Line98/Run Asset Pipeline")]
        public static void RunFullPipeline()
        {
            Debug.Log("[Line98AssetPipeline] Starting Game Design Asset Setup...");
            ConfigureTextures();
            CreateMaterials();
            CreateVfxPrefabs();
            CreateScriptableObjectDefinitions();
            UpdateThemeData();
            Debug.Log("[Line98AssetPipeline] Game Design Asset Setup Complete!");
        }

        public static void ConfigureTextures()
        {
            // 1. UI Sprites
            ConfigureSprite("Assets/Art/Sprites/UI/ui_card_hud_container.png", new Vector4(40, 40, 40, 40));
            ConfigureSprite("Assets/Art/Sprites/UI/ui_tray_next_balls_recessed.png", new Vector4(30, 30, 30, 30));
            ConfigureSprite("Assets/Art/Sprites/UI/ui_button_square_neumorphic.png", new Vector4(32, 32, 32, 32));
            ConfigureSprite("Assets/Art/Sprites/UI/ui_btn_action_undo.png", new Vector4(40, 40, 40, 40));
            ConfigureSprite("Assets/Art/Sprites/UI/ui_btn_action_newgame.png", new Vector4(40, 40, 40, 40));
            ConfigureSprite("Assets/Art/Sprites/UI/ui_cell_recessed.png", new Vector4(28, 28, 28, 28));
            ConfigureSprite("Assets/Art/Sprites/UI/ui_board_frame.png", new Vector4(48, 48, 48, 48));

            ConfigureSprite("Assets/Art/Sprites/UI/ui_icon_settings_gear.png");
            ConfigureSprite("Assets/Art/Sprites/UI/ui_icon_statistics_chart.png");
            ConfigureSprite("Assets/Art/Sprites/UI/ui_icon_crown_gold.png");
            ConfigureSprite("Assets/Art/Sprites/UI/ui_btn_center_nav_dpad.png");
            ConfigureSprite("Assets/Art/Sprites/UI/ui_badge_undo_counter.png");
            ConfigureSprite("Assets/Art/Sprites/UI/ui_overlay_modal_scrim.png");
            ConfigureSprite("Assets/Art/Sprites/UI/ui_brand_ball_quad.png");
            ConfigureSprite("Assets/Art/Sprites/UI/ui_brand_logo_text.png");

            // 2. Ball Sprites
            string[] ballFiles = { "sp_ball_red.png", "sp_ball_orange.png", "sp_ball_yellow.png", "sp_ball_green.png", "sp_ball_cyan.png", "sp_ball_purple.png", "sp_ball_blue.png" };
            foreach (string file in ballFiles)
            {
                ConfigureSprite("Assets/Art/Sprites/Balls/" + file);
            }

            // 3. VFX Sprites
            ConfigureSprite("Assets/Art/Sprites/VFX/T_VFX_Sparkle_Star.png");
            ConfigureSprite("Assets/Art/Sprites/VFX/T_VFX_Glow_Halo_Ring.png");
            ConfigureSprite("Assets/Art/Sprites/VFX/T_VFX_Shockwave_Ring.png");
            ConfigureSprite("Assets/Art/Sprites/VFX/T_VFX_Gem_Shard.png");
            ConfigureSprite("Assets/Art/Sprites/VFX/T_VFX_Smoke_Puff.png");

            // 4. Background Landscape Texture
            ConfigureLandscapeTexture("Assets/Art/Textures/T_Background_AlpineLake.png");
        }

        private static void ConfigureSprite(string path, Vector4 border = default)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool modified = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                modified = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                modified = true;
            }

            if (importer.spritePivot != new Vector2(0.5f, 0.5f))
            {
                importer.spritePivot = new Vector2(0.5f, 0.5f);
                modified = true;
            }

            if (importer.spriteBorder != border)
            {
                importer.spriteBorder = border;
                modified = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                modified = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureLandscapeTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool modified = false;
            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                modified = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        private static void CreateMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            (string name, Color color, float smoothness)[] ballMats =
            {
                ("M_Ball_Red", new Color(0.91f, 0.12f, 0.12f), 0.95f),
                ("M_Ball_Orange", new Color(0.96f, 0.48f, 0.0f), 0.95f),
                ("M_Ball_Yellow", new Color(0.99f, 0.82f, 0.10f), 0.96f),
                ("M_Ball_Green", new Color(0.12f, 0.62f, 0.22f), 0.95f),
                ("M_Ball_Cyan", new Color(0.0f, 0.72f, 0.98f), 0.96f),
                ("M_Ball_Purple", new Color(0.58f, 0.15f, 0.72f), 0.95f),
                ("M_Ball_Blue", new Color(0.06f, 0.35f, 0.85f), 0.95f),
            };

            foreach (var item in ballMats)
            {
                CreateOrUpdateMaterial("Assets/Art/Materials/" + item.name + ".mat", litShader, item.color, item.smoothness);
            }

            CreateOrUpdateMaterial("Assets/Art/Materials/M_BoardCell.mat", litShader, new Color(0.91f, 0.93f, 0.95f), 0.4f);
            CreateOrUpdateMaterial("Assets/Art/Materials/M_BoardFrame.mat", litShader, new Color(0.97f, 0.98f, 0.99f), 0.8f);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
            string shadowMatPath = "Assets/Art/Materials/M_BlobShadow.mat";
            Material shadowMat = AssetDatabase.LoadAssetAtPath<Material>(shadowMatPath);
            if (shadowMat == null)
            {
                shadowMat = new Material(unlitShader);
                AssetDatabase.CreateAsset(shadowMat, shadowMatPath);
            }
            shadowMat.color = new Color(0.05f, 0.11f, 0.16f, 0.55f);
            Texture2D shadowTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/T_Blob_Shadow_Soft.png");
            if (shadowTex != null)
            {
                shadowMat.mainTexture = shadowTex;
            }
            EditorUtility.SetDirty(shadowMat);
        }

        private static void CreateOrUpdateMaterial(string path, Shader shader, Color color, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            mat.color = color;
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", smoothness);
            }
            EditorUtility.SetDirty(mat);
        }

        private static void CreateVfxPrefabs()
        {
            CreateVfxPrefab("VFX_Ball_Select_Pulse", "Assets/Art/Sprites/VFX/T_VFX_Glow_Halo_Ring.png", new Color(0.2f, 0.9f, 1f, 0.8f), 0.5f, 1f, true);
            CreateVfxPrefab("VFX_Placement_Settle", "Assets/Art/Sprites/VFX/T_VFX_Shockwave_Ring.png", Color.white, 0.35f, 0.6f, false);
            CreateVfxPrefab("VFX_Clear_Tier1_5Balls", "Assets/Art/Sprites/VFX/T_VFX_Sparkle_Star.png", new Color(1f, 0.9f, 0.4f, 1f), 0.6f, 1.2f, false);
            CreateVfxPrefab("VFX_Clear_Tier2_6_7Balls", "Assets/Art/Sprites/VFX/T_VFX_Gem_Shard.png", new Color(0.3f, 0.8f, 1f, 1f), 0.8f, 1.5f, false);
            CreateVfxPrefab("VFX_Clear_Tier3_8Balls", "Assets/Art/Sprites/VFX/T_VFX_Sparkle_Star.png", new Color(1f, 0.4f, 0.8f, 1f), 1.1f, 2.0f, false);
            CreateVfxPrefab("VFX_Clear_Tier4_Perfect", "Assets/Art/Sprites/VFX/T_VFX_Sparkle_Star.png", new Color(1f, 0.85f, 0.2f, 1f), 1.6f, 2.8f, false);
        }

        private static void CreateVfxPrefab(string name, string spritePath, Color color, float duration, float startSize, bool looping)
        {
            string prefabPath = "Assets/Art/Prefabs/VFX/" + name + ".prefab";
            GameObject go = new GameObject(name, typeof(ParticleSystem));
            ParticleSystem ps = go.GetComponent<ParticleSystem>();

            var main = ps.main;
            main.duration = duration;
            main.loop = looping;
            main.startLifetime = duration;
            main.startSize = startSize;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            if (looping)
            {
                emission.rateOverTime = 2f;
            }
            else
            {
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });
            }

            ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Unlit/Transparent");
            Material vfxMat = new Material(unlitShader);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
            if (tex != null)
            {
                vfxMat.mainTexture = tex;
            }
            vfxMat.color = color;
            rend.material = vfxMat;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"Created VFX prefab at {prefabPath}");
        }

        private static void CreateScriptableObjectDefinitions()
        {
            string defsDir = "Assets/Data/Definitions";
            if (!AssetDatabase.IsValidFolder(defsDir))
            {
                Directory.CreateDirectory(defsDir);
                AssetDatabase.Refresh();
            }

            // 1. BallTheme_Crystal.asset
            string ballThemePath = defsDir + "/BallTheme_Crystal.asset";
            BallThemeSO ballTheme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(ballThemePath);
            if (ballTheme == null)
            {
                ballTheme = ScriptableObject.CreateInstance<BallThemeSO>();
                AssetDatabase.CreateAsset(ballTheme, ballThemePath);
            }
            SerializedObject ballSo = new SerializedObject(ballTheme);
            string[] matNames = { "M_Ball_Red", "M_Ball_Orange", "M_Ball_Yellow", "M_Ball_Green", "M_Ball_Cyan", "M_Ball_Purple", "M_Ball_Blue" };
            SerializedProperty matProp = ballSo.FindProperty("m_BallMaterials");
            matProp.arraySize = matNames.Length;
            for (int i = 0; i < matNames.Length; i++)
            {
                matProp.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/" + matNames[i] + ".mat");
            }
            ballSo.FindProperty("m_AccessibilityPatterns").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/T_Ball_Accessibility_Patterns.png");
            ballSo.FindProperty("m_InnerRefractionMask").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/T_Ball_InnerRefraction_Mask.png");
            ballSo.FindProperty("m_BlobShadowMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_BlobShadow.mat");
            ballSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(ballTheme);

            // 2. BoardTheme_Classic.asset
            string boardThemePath = defsDir + "/BoardTheme_Classic.asset";
            BoardThemeSO boardTheme = AssetDatabase.LoadAssetAtPath<BoardThemeSO>(boardThemePath);
            if (boardTheme == null)
            {
                boardTheme = ScriptableObject.CreateInstance<BoardThemeSO>();
                AssetDatabase.CreateAsset(boardTheme, boardThemePath);
            }
            SerializedObject boardSo = new SerializedObject(boardTheme);
            boardSo.FindProperty("m_BoardCellMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_BoardCell.mat");
            boardSo.FindProperty("m_BoardFrameMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_BoardFrame.mat");
            boardSo.FindProperty("m_BoardFramePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/SM_BoardFrame.obj");
            boardSo.FindProperty("m_BoardCellPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/SM_BoardCell.obj");
            boardSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(boardTheme);

            // 3. CameraProfile_Default.asset
            string camPath = defsDir + "/CameraProfile_Default.asset";
            CameraProfileSO camProfile = AssetDatabase.LoadAssetAtPath<CameraProfileSO>(camPath);
            if (camProfile == null)
            {
                camProfile = ScriptableObject.CreateInstance<CameraProfileSO>();
                AssetDatabase.CreateAsset(camProfile, camPath);
            }
            SerializedObject camSo = new SerializedObject(camProfile);
            camSo.FindProperty("m_FieldOfView").floatValue = 28.0f;
            camSo.FindProperty("m_PitchAngle").floatValue = 58.0f;
            camSo.FindProperty("m_YawAngle").floatValue = 0.0f;
            camSo.FindProperty("m_Distance").floatValue = 18.5f;
            camSo.FindProperty("m_ParallaxFactor").floatValue = 0.05f;
            camSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(camProfile);

            // 4. FeedbackProfile_Tiers.asset
            string fbPath = defsDir + "/FeedbackProfile_Tiers.asset";
            FeedbackProfileSO fbProfile = AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(fbPath);
            if (fbProfile == null)
            {
                fbProfile = ScriptableObject.CreateInstance<FeedbackProfileSO>();
                AssetDatabase.CreateAsset(fbProfile, fbPath);
            }
            SerializedObject fbSo = new SerializedObject(fbProfile);
            SerializedProperty tiersProp = fbSo.FindProperty("m_Tiers");
            tiersProp.arraySize = 4;
            // Tier 1
            var t0 = tiersProp.GetArrayElementAtIndex(0);
            t0.FindPropertyRelative("MinimumBalls").intValue = 5;
            t0.FindPropertyRelative("MaximumBalls").intValue = 5;
            t0.FindPropertyRelative("VfxPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/VFX/VFX_Clear_Tier1_5Balls.prefab");
            t0.FindPropertyRelative("SfxClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_clear_tier1.wav");
            t0.FindPropertyRelative("CameraShakeIntensity").floatValue = 0.04f;
            t0.FindPropertyRelative("TierLabel").stringValue = "Nice!";
            // Tier 2
            var t1 = tiersProp.GetArrayElementAtIndex(1);
            t1.FindPropertyRelative("MinimumBalls").intValue = 6;
            t1.FindPropertyRelative("MaximumBalls").intValue = 7;
            t1.FindPropertyRelative("VfxPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/VFX/VFX_Clear_Tier2_6_7Balls.prefab");
            t1.FindPropertyRelative("SfxClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_clear_tier2.wav");
            t1.FindPropertyRelative("CameraShakeIntensity").floatValue = 0.08f;
            t1.FindPropertyRelative("TierLabel").stringValue = "Great!";
            // Tier 3
            var t2 = tiersProp.GetArrayElementAtIndex(2);
            t2.FindPropertyRelative("MinimumBalls").intValue = 8;
            t2.FindPropertyRelative("MaximumBalls").intValue = 8;
            t2.FindPropertyRelative("VfxPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/VFX/VFX_Clear_Tier3_8Balls.prefab");
            t2.FindPropertyRelative("SfxClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_clear_tier3.wav");
            t2.FindPropertyRelative("CameraShakeIntensity").floatValue = 0.14f;
            t2.FindPropertyRelative("TierLabel").stringValue = "Awesome!";
            // Tier 4
            var t3 = tiersProp.GetArrayElementAtIndex(3);
            t3.FindPropertyRelative("MinimumBalls").intValue = 9;
            t3.FindPropertyRelative("MaximumBalls").intValue = 99;
            t3.FindPropertyRelative("VfxPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/VFX/VFX_Clear_Tier4_Perfect.prefab");
            t3.FindPropertyRelative("SfxClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_clear_tier4_perfect.wav");
            t3.FindPropertyRelative("CameraShakeIntensity").floatValue = 0.22f;
            t3.FindPropertyRelative("TierLabel").stringValue = "PERFECT LINE!";
            fbSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(fbProfile);

            // 5. VfxCatalog_Default.asset
            string vfxCatPath = defsDir + "/VfxCatalog_Default.asset";
            VfxCatalogSO vfxCatalog = AssetDatabase.LoadAssetAtPath<VfxCatalogSO>(vfxCatPath);
            if (vfxCatalog == null)
            {
                vfxCatalog = ScriptableObject.CreateInstance<VfxCatalogSO>();
                AssetDatabase.CreateAsset(vfxCatalog, vfxCatPath);
            }
            SerializedObject vfxSo = new SerializedObject(vfxCatalog);
            vfxSo.FindProperty("m_SelectPulsePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/VFX/VFX_Ball_Select_Pulse.prefab");
            vfxSo.FindProperty("m_PlacementSettlePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/VFX/VFX_Placement_Settle.prefab");
            vfxSo.FindProperty("m_ClearTier1Prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/VFX/VFX_Clear_Tier1_5Balls.prefab");
            vfxSo.FindProperty("m_ClearTier2Prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/VFX/VFX_Clear_Tier2_6_7Balls.prefab");
            vfxSo.FindProperty("m_ClearTier3Prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/VFX/VFX_Clear_Tier3_8Balls.prefab");
            vfxSo.FindProperty("m_ClearTier4Prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/VFX/VFX_Clear_Tier4_Perfect.prefab");
            vfxSo.FindProperty("m_PrewarmPoolCapacity").intValue = 4;
            vfxSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(vfxCatalog);

            // 6. AudioCatalog_Default.asset
            string audioCatPath = defsDir + "/AudioCatalog_Default.asset";
            AudioCatalogSO audioCatalog = AssetDatabase.LoadAssetAtPath<AudioCatalogSO>(audioCatPath);
            if (audioCatalog == null)
            {
                audioCatalog = ScriptableObject.CreateInstance<AudioCatalogSO>();
                AssetDatabase.CreateAsset(audioCatalog, audioCatPath);
            }
            SerializedObject aSo = new SerializedObject(audioCatalog);
            aSo.FindProperty("m_BallSelectClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_ball_select.wav");
            aSo.FindProperty("m_BallDeselectClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_ball_deselect.wav");
            aSo.FindProperty("m_BallMoveFlightClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_ball_move_flight.wav");
            aSo.FindProperty("m_BallPlaceSettleClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_ball_place_settle.wav");
            aSo.FindProperty("m_BallInvalidClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_ball_invalid.wav");
            aSo.FindProperty("m_SpawnPopClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_spawn_pop.wav");
            aSo.FindProperty("m_ClearTier1Clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_clear_tier1.wav");
            aSo.FindProperty("m_ClearTier2Clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_clear_tier2.wav");
            aSo.FindProperty("m_ClearTier3Clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_clear_tier3.wav");
            aSo.FindProperty("m_ClearTier4Clip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_clear_tier4_perfect.wav");
            aSo.FindProperty("m_ComboUpClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_combo_up.wav");
            aSo.FindProperty("m_ButtonClickClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_ui_button_click.wav");
            aSo.FindProperty("m_RewardEarnedClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_reward_earned.wav");
            aSo.FindProperty("m_GameOverClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/sfx_game_over.wav");
            aSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(audioCatalog);

            Debug.Log("[Line98AssetPipeline] All 6 ScriptableObject Definitions created and bound!");
        }

        private static void UpdateThemeData()
        {
            const string themePath = "Assets/Settings/DefaultLine98Theme.asset";
            Line98ThemeData theme = AssetDatabase.LoadAssetAtPath<Line98ThemeData>(themePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<Line98ThemeData>();
                AssetDatabase.CreateAsset(theme, themePath);
            }

            SerializedObject so = new SerializedObject(theme);

            // Assign Landscape Texture
            Texture2D landscape = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/T_Background_AlpineLake.png");
            if (landscape != null)
            {
                so.FindProperty("m_CustomLandscapeTexture").objectReferenceValue = landscape;
            }

            // Assign 7 Ball Sprites:
            // 0: Red, 1: Blue, 2: Yellow, 3: Green, 4: Purple, 5: Cyan, 6: Orange
            string[] ballPaths =
            {
                "Assets/Art/Sprites/Balls/sp_ball_red.png",
                "Assets/Art/Sprites/Balls/sp_ball_blue.png",
                "Assets/Art/Sprites/Balls/sp_ball_yellow.png",
                "Assets/Art/Sprites/Balls/sp_ball_green.png",
                "Assets/Art/Sprites/Balls/sp_ball_purple.png",
                "Assets/Art/Sprites/Balls/sp_ball_cyan.png",
                "Assets/Art/Sprites/Balls/sp_ball_orange.png",
            };

            SerializedProperty ballSpritesProp = so.FindProperty("m_CustomBallSprites");
            ballSpritesProp.arraySize = ballPaths.Length;
            for (int i = 0; i < ballPaths.Length; i++)
            {
                Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(ballPaths[i]);
                ballSpritesProp.GetArrayElementAtIndex(i).objectReferenceValue = sp;
            }

            // Assign UI Sprites
            Sprite roundedSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_card_hud_container.png");
            if (roundedSp != null)
            {
                so.FindProperty("m_CustomRoundedSprite").objectReferenceValue = roundedSp;
            }

            Sprite glowSp = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/VFX/T_VFX_Glow_Halo_Ring.png");
            if (glowSp != null)
            {
                so.FindProperty("m_CustomGlowSprite").objectReferenceValue = glowSp;
            }

            so.FindProperty("m_BoardFrameSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_board_frame.png");
            so.FindProperty("m_CellSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_cell_recessed.png");
            so.FindProperty("m_TrayNextSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_tray_next_balls_recessed.png");
            so.FindProperty("m_ButtonSquareSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_button_square_neumorphic.png");
            so.FindProperty("m_UndoButtonSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_btn_action_undo.png");
            so.FindProperty("m_NewGameButtonSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_btn_action_newgame.png");
            so.FindProperty("m_DpadSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_btn_center_nav_dpad.png");
            so.FindProperty("m_IconGearSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_icon_settings_gear.png");
            so.FindProperty("m_IconBarsSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_icon_statistics_chart.png");
            so.FindProperty("m_IconCrownSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_icon_crown_gold.png");
            so.FindProperty("m_LogoQuadSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_brand_ball_quad.png");
            so.FindProperty("m_LogoTextSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/UI/ui_brand_logo_text.png");

            TMPro.TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font != null)
            {
                so.FindProperty("m_FontAsset").objectReferenceValue = font;
            }

            // Tune colors to complement high-fidelity pre-rendered sprites
            so.FindProperty("m_PanelBackgroundColor").colorValue = Color.white;
            so.FindProperty("m_BoardFrameColor").colorValue = Color.white;
            so.FindProperty("m_CellNormalColor").colorValue = Color.white;
            so.FindProperty("m_ButtonNormalTint").colorValue = Color.white;
            so.FindProperty("m_CrownTint").colorValue = new Color(1f, 0.72f, 0.05f);
            so.FindProperty("m_IconTint").colorValue = new Color(0.12f, 0.20f, 0.38f);
            so.FindProperty("m_SkyWashColor").colorValue = new Color(1f, 1f, 1f, 0.05f);

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(theme);
            Debug.Log("[Line98AssetPipeline] Line98ThemeData updated with high-fidelity assets!");
        }
    }
}
#endif

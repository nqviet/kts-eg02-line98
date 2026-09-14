using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace Line98.Editor
{
    public static class AudioMixerAuthoring
    {
        [MenuItem("Line98/Authoring/Build Main Mixer")]
        public static void BuildMainMixer()
        {
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/_Project/Content/Audio/MainMixer.mixer");
            if (mixer == null)
            {
                Debug.LogError("MainMixer.mixer not found!");
                return;
            }

            var t = mixer.GetType();
            var masterProp = t.GetProperty("masterGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var masterGroup = masterProp?.GetValue(mixer);
            Debug.Log($"Master group: {masterGroup}");

            var createGroupMethod = t.GetMethod("CreateNewGroup", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var addChildMethod = t.GetMethod("AddChildToParent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var cloneSnapshotMethod = t.GetMethod("CloneNewSnapshotFromTarget", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            string[] busNames = new[] { "Music", "SFX", "UI", "Ambience" };
            foreach (var bus in busNames)
            {
                var matchingGroups = mixer.FindMatchingGroups(bus);
                if (matchingGroups != null && matchingGroups.Length > 0)
                {
                    Debug.Log($"Group {bus} already exists!");
                    continue;
                }

                if (createGroupMethod != null && addChildMethod != null)
                {
                    var newGroup = createGroupMethod.Invoke(mixer, new object[] { bus, false });
                    Debug.Log($"Created group {bus}: {newGroup != null}");
                    addChildMethod.Invoke(mixer, new object[] { newGroup, masterGroup });
                    Debug.Log($"Added {bus} as child of masterGroup");
                }
            }

            // Snapshot: Check or create Zen snapshot
            var zenSnap = mixer.FindSnapshot("Zen");
            if (zenSnap == null)
            {
                var defaultSnap = mixer.FindSnapshot("Snapshot");
                Debug.Log($"defaultSnap: {defaultSnap}");
                if (cloneSnapshotMethod != null)
                {
                    Debug.Log($"cloneSnapshotMethod params: {string.Join(", ", System.Array.ConvertAll(cloneSnapshotMethod.GetParameters(), p => p.ParameterType.Name + " " + p.Name))}");
                    if (cloneSnapshotMethod.GetParameters().Length == 2)
                    {
                        cloneSnapshotMethod.Invoke(mixer, new object[] { defaultSnap, "Zen" });
                    }
                    else if (cloneSnapshotMethod.GetParameters().Length == 3)
                    {
                        cloneSnapshotMethod.Invoke(mixer, new object[] { defaultSnap, "Zen", false });
                    }
                }
            }
            else
            {
                Debug.Log("Zen snapshot already exists");
            }

            EditorUtility.SetDirty(mixer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Verify all groups in MainMixer
            var allGroups = mixer.FindMatchingGroups(string.Empty);
            Debug.Log($"Total groups in MainMixer: {allGroups.Length}");
            foreach (var g in allGroups)
            {
                Debug.Log($" - Group: {g.name}");
            }
        }

        [MenuItem("Line98/Authoring/Migrate Audio Catalog")]
        public static void MigrateAudioCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<Line98.Data.AudioCatalogSO>("Assets/_Project/Content/Definitions/AudioCatalog_Default.asset");
            if (catalog == null)
            {
                Debug.LogError("AudioCatalog_Default.asset not found!");
                return;
            }

            var entries = new System.Collections.Generic.List<Line98.Data.AudioEntry>
            {
                new Line98.Data.AudioEntry("sfx_ball_select", catalog.BallSelectClip, Line98.Data.AudioBusType.SFX, 1.0f, 0.05f),
                new Line98.Data.AudioEntry("sfx_ball_deselect", catalog.BallDeselectClip, Line98.Data.AudioBusType.SFX, 1.0f, 0.0f),
                new Line98.Data.AudioEntry("sfx_ball_move_flight", catalog.BallMoveFlightClip, Line98.Data.AudioBusType.SFX, 0.85f, 0.02f),
                new Line98.Data.AudioEntry("sfx_ball_place_settle", catalog.BallPlaceSettleClip, Line98.Data.AudioBusType.SFX, 1.0f, 0.02f),
                new Line98.Data.AudioEntry("sfx_ball_invalid", catalog.BallInvalidClip, Line98.Data.AudioBusType.UI, 0.9f, 0.0f),
                new Line98.Data.AudioEntry("sfx_spawn_pop", catalog.SpawnPopClip, Line98.Data.AudioBusType.SFX, 0.95f, 0.08f),
                new Line98.Data.AudioEntry("sfx_clear_tier1", catalog.ClearTier1Clip, Line98.Data.AudioBusType.SFX, 1.0f, 0.0f),
                new Line98.Data.AudioEntry("sfx_clear_tier2", catalog.ClearTier2Clip, Line98.Data.AudioBusType.SFX, 1.0f, 0.0f),
                new Line98.Data.AudioEntry("sfx_clear_tier3", catalog.ClearTier3Clip, Line98.Data.AudioBusType.SFX, 1.0f, 0.0f),
                new Line98.Data.AudioEntry("sfx_clear_tier4_perfect", catalog.ClearTier4Clip, Line98.Data.AudioBusType.SFX, 1.0f, 0.0f),
                new Line98.Data.AudioEntry("sfx_combo_up", catalog.ComboUpClip, Line98.Data.AudioBusType.SFX, 1.0f, 0.0f),
                new Line98.Data.AudioEntry("sfx_ui_button_click", catalog.ButtonClickClip, Line98.Data.AudioBusType.UI, 0.9f, 0.0f),
                new Line98.Data.AudioEntry("sfx_reward_earned", catalog.RewardEarnedClip, Line98.Data.AudioBusType.UI, 1.0f, 0.0f),
                new Line98.Data.AudioEntry("sfx_game_over", catalog.GameOverClip, Line98.Data.AudioBusType.SFX, 1.0f, 0.0f)
            };

            string[] musicPaths = new string[]
            {
                "Assets/Art/Audio/bgm_classic_main.ogg",
                "Assets/Art/Audio/bgm_zen_ambience.ogg"
            };

            foreach (var path in musicPaths)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null)
                {
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.Streaming;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.5f;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                    Debug.Log($"Configured AudioImporter for {path}: Streaming, Vorbis.");
                }
            }

            // Check if music files already exist
            var classicMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/bgm_classic_main.ogg") ??
                               AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Content/Audio/bgm_classic_main.ogg");
            if (classicMusic != null)
            {
                entries.Add(new Line98.Data.AudioEntry("bgm_classic_main", classicMusic, Line98.Data.AudioBusType.Music, 0.75f, 0.0f));
            }

            var zenMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/bgm_zen_ambience.ogg") ??
                           AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Content/Audio/bgm_zen_ambience.ogg");
            if (zenMusic != null)
            {
                entries.Add(new Line98.Data.AudioEntry("bgm_zen_ambience", zenMusic, Line98.Data.AudioBusType.Ambience, 0.75f, 0.0f));
            }

            catalog.SetEntriesForMigration(entries.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Successfully migrated {entries.Count} audio entries into AudioCatalog_Default.asset!");
        }

        [MenuItem("Line98/Authoring/Wire Presentation Root Audio")]
        public static void WirePresentationRootAudio()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (scene.name != "Game")
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/_Project/Content/Scenes/Game.unity");
            }

            var root = UnityEngine.Object.FindAnyObjectByType<Line98.Presentation.PresentationRoot>();
            if (root == null)
            {
                Debug.LogError("PresentationRoot not found in Game.unity!");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<Line98.Data.AudioCatalogSO>("Assets/_Project/Content/Definitions/AudioCatalog_Default.asset");
            var mixer = AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/_Project/Content/Audio/MainMixer.mixer");

            var so = new SerializedObject(root);
            var catalogProp = so.FindProperty("m_AudioCatalog");
            var mixerProp = so.FindProperty("m_MainMixer");

            if (catalogProp != null) catalogProp.objectReferenceValue = catalog;
            if (mixerProp != null) mixerProp.objectReferenceValue = mixer;

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(root);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(root.gameObject.scene);
            Debug.Log("Successfully wired m_AudioCatalog and m_MainMixer to PresentationRoot in Game.unity!");
        }
    }
}

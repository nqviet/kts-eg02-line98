using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Line98.Editor
{
    /// <summary>
    /// Idempotent editor setup utility that applies all Phase P1 (Platform baseline)
    /// and Phase P2 (Render pipeline switch) configuration per project_initial_setup.md.
    /// Can be executed via menu item or headless CI via -executeMethod Line98.Editor.ProjectSetup.Apply.
    /// </summary>
    public static class ProjectSetup
    {
        private const string s_RendererPath = "Assets/_Project/Content/Rendering/URP_UniversalRenderer_3D.asset";
        private const string s_UrpAssetPath = "Assets/Settings/UniversalRP.asset";

        [MenuItem("Line98/Setup/Apply Project Baseline")]
        public static void Apply()
        {
            Debug.Log("[ProjectSetup] Starting baseline application...");

            ApplyPlayerSettings();
            ApplyQualityAndRenderingSettings();
            CreateOrUpdateUniversal3DRenderer();
            CreateSceneStubs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[ProjectSetup] Baseline successfully applied.");
        }

        public static void ApplyPlayerSettings()
        {
            Debug.Log("[ProjectSetup] Configuring PlayerSettings...");

            // Product Identity
            PlayerSettings.productName = "LINE 98: Color Lines";
            PlayerSettings.companyName = "LINE 98";

            // Orientation: Portrait locked
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Standalone settings
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Low);

            // Android target settings
            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.High);
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
                EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ProjectSetup] Note: Android platform modules might not be fully active in Editor: {ex.Message}");
            }
        }

        public static void ApplyQualityAndRenderingSettings()
        {
            Debug.Log("[ProjectSetup] Configuring Quality & Rendering settings...");

            // vSync off for mobile framerate targeting
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        public static void CreateOrUpdateUniversal3DRenderer()
        {
            Debug.Log("[ProjectSetup] Checking Universal 3D Forward Renderer...");

            string renderingDir = Path.GetDirectoryName(s_RendererPath);
            if (!string.IsNullOrEmpty(renderingDir) && !Directory.Exists(renderingDir))
            {
                Directory.CreateDirectory(renderingDir);
            }

            UniversalRendererData rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(s_RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "URP_UniversalRenderer_3D";
                AssetDatabase.CreateAsset(rendererData, s_RendererPath);
                Debug.Log($"[ProjectSetup] Created 3D UniversalRendererData at {s_RendererPath}");
            }

            UniversalRenderPipelineAsset urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(s_UrpAssetPath);
            if (urpAsset != null)
            {
                SerializedObject serializedUrp = new SerializedObject(urpAsset);

                // Configure SRP batcher and shadows
                SerializedProperty srpBatcherProp = serializedUrp.FindProperty("m_UseSRPBatcher");
                if (srpBatcherProp != null) srpBatcherProp.boolValue = true;

                SerializedProperty shadowDistanceProp = serializedUrp.FindProperty("m_ShadowDistance");
                if (shadowDistanceProp != null) shadowDistanceProp.floatValue = 0f;

                SerializedProperty renderScaleProp = serializedUrp.FindProperty("m_RenderScale");
                if (renderScaleProp != null) renderScaleProp.floatValue = 1.0f;

                SerializedProperty depthTexProp = serializedUrp.FindProperty("m_SupportsCameraDepthTexture");
                if (depthTexProp != null) depthTexProp.boolValue = false;

                SerializedProperty opaqueTexProp = serializedUrp.FindProperty("m_SupportsCameraOpaqueTexture");
                if (opaqueTexProp != null) opaqueTexProp.boolValue = false;

                // Assign renderer to index 0
                SerializedProperty rendererDataList = serializedUrp.FindProperty("m_RendererDataList");
                if (rendererDataList != null)
                {
                    rendererDataList.arraySize = 1;
                    rendererDataList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
                }

                SerializedProperty defaultRendererIndex = serializedUrp.FindProperty("m_DefaultRendererIndex");
                if (defaultRendererIndex != null)
                {
                    defaultRendererIndex.intValue = 0;
                }

                serializedUrp.ApplyModifiedProperties();
                EditorUtility.SetDirty(urpAsset);
                Debug.Log("[ProjectSetup] UniversalRP asset updated with 3D Forward renderer and mobile settings.");
            }
        }

        public static void CreateSceneStubs()
        {
            string scenesDir = "Assets/_Project/Content/Scenes";
            if (!Directory.Exists(scenesDir))
            {
                Directory.CreateDirectory(scenesDir);
            }

            string bootScenePath = $"{scenesDir}/Boot.unity";
            string menuScenePath = $"{scenesDir}/MainMenu.unity";
            string gameScenePath = $"{scenesDir}/Game.unity";

            EnsureSceneExists(bootScenePath);
            EnsureSceneExists(menuScenePath);
            EnsureSceneExists(gameScenePath);

            SetupBootScene(bootScenePath);

            // Register in EditorBuildSettings
            EditorBuildSettingsScene[] scenes = new[]
            {
                new EditorBuildSettingsScene(bootScenePath, true),
                new EditorBuildSettingsScene(menuScenePath, true),
                new EditorBuildSettingsScene(gameScenePath, true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[ProjectSetup] Configured EditorBuildSettings scenes: Boot, MainMenu, Game.");
        }

        private static void SetupBootScene(string bootScenePath)
        {
            var bootScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(bootScenePath);
            bool modified = false;

            if (UnityEngine.Object.FindAnyObjectByType<Line98.App.AppRoot>() == null)
            {
                var appRootGo = new GameObject("[AppRoot]");
                appRootGo.AddComponent<Line98.App.AppRoot>();
                modified = true;
            }

            if (UnityEngine.Object.FindAnyObjectByType<Camera>() == null)
            {
                var cameraGo = new GameObject("Main Camera");
                var cam = cameraGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1.0f);
                cameraGo.tag = "MainCamera";
                cameraGo.AddComponent<UniversalAdditionalCameraData>();
                modified = true;
            }

            if (modified)
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(bootScene);
                Debug.Log("[ProjectSetup] Configured Boot scene with [AppRoot] and Main Camera.");
            }
        }

        private static void EnsureSceneExists(string scenePath)
        {
            if (!File.Exists(scenePath))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                    UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
                    UnityEditor.SceneManagement.NewSceneMode.Single);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            }
        }

        private class TestCallbacks : UnityEditor.TestTools.TestRunner.Api.ICallbacks
        {
            public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor testsToRun)
            {
                Debug.Log("[TestRunner] Suite started.");
            }

            public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor results)
            {
                if (results.FailCount > 0)
                {
                    Debug.LogError($"[TestRunner] Suite FAILED: {results.PassCount} passed, {results.FailCount} failed, {results.InconclusiveCount} inconclusive. Duration: {results.Duration:F2}s");
                }
                else
                {
                    Debug.Log($"[TestRunner] Suite PASSED: All {results.PassCount} tests passed! Duration: {results.Duration:F2}s");
                }
            }

            public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test)
            {
            }

            public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
            {
                if (result.TestStatus == UnityEditor.TestTools.TestRunner.Api.TestStatus.Failed)
                {
                    Debug.LogError($"[TestRunner] FAIL: {result.FullName} -> {result.Message}\n{result.StackTrace}");
                }
            }
        }

        [MenuItem("Line98/Tests/Run EditMode Tests")]
        public static void RunEditModeTests()
        {
            var api = ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
            var filter = new UnityEditor.TestTools.TestRunner.Api.Filter
            {
                testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode
            };

            api.RegisterCallbacks(new TestCallbacks());
            api.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(filter));
            Debug.Log("[TestRunner] Dispatched EditMode test run.");
        }

        [MenuItem("Line98/Tests/Run PlayMode Tests")]
        public static void RunPlayModeTests()
        {
            var api = ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
            var filter = new UnityEditor.TestTools.TestRunner.Api.Filter
            {
                testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode
            };

            api.RegisterCallbacks(new TestCallbacks());
            api.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(filter));
            Debug.Log("[TestRunner] Dispatched PlayMode test run.");
        }

        [MenuItem("Line98/Tests/Run All Tests")]
        public static void RunAllTests()
        {
            var api = ScriptableObject.CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();
            var filter = new UnityEditor.TestTools.TestRunner.Api.Filter
            {
                testMode = UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode | UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode
            };

            api.RegisterCallbacks(new TestCallbacks());
            api.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(filter));
            Debug.Log("[TestRunner] Dispatched All tests run.");
        }

        [MenuItem("Line98/Debug/Capture Screenshot In PlayMode")]
        public static void CaptureScreenshotInPlayMode()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "game_playmode_screen.png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[Screenshot] Capture dispatched to {path}");
        }
    }
}

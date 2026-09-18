using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Line98.Presentation;

namespace Line98.Editor
{
    /// <summary>
    /// Renders the in-game HUD to a PNG so layout and typography can be reviewed without entering
    /// play mode.
    /// <para>
    /// The canvases are switched to <see cref="RenderMode.WorldSpace"/> for the capture. Screen
    /// space would size the canvas from <c>Screen.width/height</c>, which in batch mode is the
    /// window Unity happens to have rather than the phone we are authoring for; world space lets
    /// the canvas rect be pinned to the 1080 x 1920 reference exactly, so a pixel in the capture is
    /// a canvas unit and measured sizes mean something.
    /// </para>
    /// The real <see cref="SafeAreaFitter"/> pass is driven afterwards, so the capture shows the
    /// sizes the game computes, not the sizes the prefab was authored with.
    /// </summary>
    public static class HudCaptureAuthoring
    {
        private const string UiRootPath = "Assets/_Project/Content/Prefabs/UI/Shell/UI_Root.prefab";
        private const int CaptureWidth = (int)HudLayoutSolver.ReferenceWidth;
        private const int CaptureHeight = (int)HudLayoutSolver.ReferenceHeight;

        /// <summary>Board area colour, so the empty middle does not read as a hole.</summary>
        private static readonly Color s_Background = new Color(0.937f, 0.953f, 0.965f, 1f);

        [MenuItem("Line98/Capture Game HUD")]
        public static void Capture()
        {
            string outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "HudCaptures");
            Directory.CreateDirectory(outputFolder);
            string outputPath = Path.Combine(outputFolder, "Hud_1080x1920.png");

            GameObject stage = new GameObject("HudCaptureStage");
            RenderTexture target = null;

            try
            {
                Camera camera = BuildCamera(stage, out target);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiRootPath);
                if (prefab == null)
                {
                    Debug.LogError($"[HudCaptureAuthoring] Could not load {UiRootPath}.");
                    return;
                }

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage.transform);
                instance.SetActive(true);

                PinCanvasesToReference(instance, camera);
                HidePopupLayer(instance);
                DriveResponsiveLayout(instance);
                ReportSizes(instance);

                Save(camera, target, outputPath);
                Debug.Log($"[HudCaptureAuthoring] Wrote {outputPath}");
            }
            finally
            {
                if (target != null)
                {
                    target.Release();
                    Object.DestroyImmediate(target);
                }

                Object.DestroyImmediate(stage);
            }
        }

        private static Camera BuildCamera(GameObject stage, out RenderTexture target)
        {
            GameObject cameraObject = new GameObject("Camera", typeof(Camera));
            cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.position = new Vector3(0f, 0f, -500f);

            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = s_Background;
            camera.orthographic = true;

            // Half the reference height: the canvas rect then fills the frame exactly.
            camera.orthographicSize = CaptureHeight * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 2000f;

            target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            return camera;
        }

        /// <summary>
        /// Puts every canvas on the same world-space plane at the reference size. Authored
        /// sortingOrder still decides what draws on top.
        /// </summary>
        private static void PinCanvasesToReference(GameObject instance, Camera camera)
        {
            Canvas[] canvases = instance.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;

                RectTransform rect = canvas.transform as RectTransform;
                if (rect == null) continue;

                rect.position = Vector3.zero;
                rect.rotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(CaptureWidth, CaptureHeight);
            }
        }

        private static void HidePopupLayer(GameObject instance)
        {
            Transform popups = FindByName(instance.transform, "Canvas_Popups");
            if (popups != null)
            {
                popups.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Runs the fitter exactly as the game does. Its reference caching lives in Awake, which
        /// edit-mode instantiation never calls, so it is invoked directly here -- the alternative
        /// would be duplicating the layout pass in this tool, where it could silently drift from
        /// the code it is supposed to be showing.
        /// </summary>
        private static void DriveResponsiveLayout(GameObject instance)
        {
            SafeAreaFitter[] fitters = instance.GetComponentsInChildren<SafeAreaFitter>(true);
            MethodInfo cacheReferences = typeof(SafeAreaFitter).GetMethod(
                "CacheReferences",
                BindingFlags.Instance | BindingFlags.NonPublic);

            for (int i = 0; i < fitters.Length; i++)
            {
                cacheReferences?.Invoke(fitters[i], null);
                fitters[i].RefreshSafeArea(
                    new Rect(0f, 0f, CaptureWidth, CaptureHeight),
                    CaptureWidth,
                    CaptureHeight);
            }

            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Logs the resolved size of every text, so the capture can be read numerically.</summary>
        private static void ReportSizes(GameObject instance)
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"[HudCaptureAuthoring] Resolved text sizes at {CaptureWidth}x{CaptureHeight} (scale 1.0):");

            TMP_Text[] texts = instance.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];
                if (!text.gameObject.activeInHierarchy) continue;

                string content = text.text != null && text.text.Length > 24
                    ? text.text.Substring(0, 24) + "..."
                    : text.text;
                report.AppendLine(
                    $"  {HierarchyPath(text.transform, instance.transform)} = {text.fontSize:0.#} units " +
                    $"({text.fontSize / 3f:0.#}dp)  \"{content}\"");
            }

            Debug.Log(report.ToString());
        }

        private static string HierarchyPath(Transform target, Transform root)
        {
            string path = target.name;
            Transform cursor = target.parent;
            while (cursor != null && cursor != root)
            {
                path = cursor.name + "/" + path;
                cursor = cursor.parent;
            }

            return path;
        }

        private static Transform FindByName(Transform root, string name)
        {
            if (root.name == name) return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindByName(root.GetChild(i), name);
                if (found != null) return found;
            }

            return null;
        }

        private static void Save(Camera camera, RenderTexture target, string outputPath)
        {
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            RenderTexture.active = previous;
        }
    }
}

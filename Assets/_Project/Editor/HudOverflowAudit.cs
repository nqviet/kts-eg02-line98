using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Line98.Presentation;

namespace Line98.Editor
{
    /// <summary>
    /// Measures every HUD string against the box it has to fit in, at several window shapes.
    /// <para>
    /// The responsive pass scales rects by the layout scale but floors font sizes, so on a short or
    /// wide window a label keeps its readable size inside a box that kept shrinking. This tool
    /// reports the resulting overflow numerically instead of by eye: for each text it drives the
    /// real <see cref="SafeAreaFitter"/>, then compares TMP's preferred size with the text's own
    /// rect and with the card that contains it.
    /// </para>
    /// </summary>
    public static class HudOverflowAudit
    {
        private const string UiRootPath = "Assets/_Project/Content/Prefabs/UI/Shell/UI_Root.prefab";

        /// <summary>Sub-unit differences are TMP rounding, not layout defects.</summary>
        private const float Tolerance = 0.5f;

        /// <summary>
        /// Window shapes in device pixels. The 9:16 and 9:20 phones sit at layout scale 1; the
        /// shorter and wider windows are where the scale drops and the font floor takes over.
        /// </summary>
        private static readonly Vector2Int[] s_Windows =
        {
            new Vector2Int(1080, 1920),
            new Vector2Int(1080, 2400),
            new Vector2Int(1080, 1440),
            new Vector2Int(1080, 1080),
            new Vector2Int(1920, 1080)
        };

        [MenuItem("Line98/Audit HUD Text Overflow")]
        public static void Audit()
        {
            var report = new StringBuilder();
            report.AppendLine("[HudOverflowAudit] Text that does not fit its box:");
            int total = 0;

            for (int i = 0; i < s_Windows.Length; i++)
            {
                total += AuditWindow(s_Windows[i], report);
            }

            report.AppendLine($"  ---- {total} overflowing text(s) across {s_Windows.Length} window shapes.");
            Debug.Log(report.ToString());
        }

        private static int AuditWindow(Vector2Int window, StringBuilder report)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiRootPath);
            if (prefab == null)
            {
                Debug.LogError($"[HudOverflowAudit] Could not load {UiRootPath}.");
                return 0;
            }

            GameObject stage = new GameObject("HudOverflowStage");
            int found = 0;

            try
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stage.transform);
                instance.SetActive(true);

                GetCanvasSize(instance, window, out float canvasWidth, out float canvasHeight);
                PinCanvases(instance, canvasWidth, canvasHeight);
                DriveResponsiveLayout(instance, window);

                float scale = HudLayoutSolver.Solve(canvasWidth, canvasHeight).Scale;
                report.AppendLine(
                    $"  {window.x}x{window.y} px -> canvas {canvasWidth:0}x{canvasHeight:0}, layout scale {scale:0.###}");

                var texts = new List<TMP_Text>(instance.GetComponentsInChildren<TMP_Text>(true));
                for (int i = 0; i < texts.Count; i++)
                {
                    if (Measure(texts[i], instance.transform, report))
                    {
                        found++;
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(stage);
            }

            return found;
        }

        private static bool Measure(TMP_Text text, Transform root, StringBuilder report)
        {
            if (text == null || !text.gameObject.activeInHierarchy) return false;
            if (string.IsNullOrEmpty(text.text)) return false;

            text.ForceMeshUpdate();

            Rect rect = text.rectTransform.rect;
            // Unconstrained: what the string wants on one line, before wrapping or clipping.
            Vector2 preferred = text.GetPreferredValues(text.text, Mathf.Infinity, Mathf.Infinity);

            float widthOver = preferred.x - rect.width;
            float heightOver = text.textBounds.size.y - rect.height;

            // The visible defect is a string crossing the card that frames it, so the container is
            // measured too: a text rect can be wide enough while the card behind it is not.
            RectTransform parent = text.rectTransform.parent as RectTransform;
            float parentOver = parent != null ? preferred.x - parent.rect.width : float.NegativeInfinity;

            bool overflows = widthOver > Tolerance || heightOver > Tolerance || parentOver > Tolerance;
            if (!overflows) return false;

            report.AppendLine(
                $"    {HierarchyPath(text.transform, root)} \"{Trim(text.text)}\"" +
                $" font {text.fontSize:0.#}" +
                $" | rect {rect.width:0.#}x{rect.height:0.#}" +
                $" | wants {preferred.x:0.#}x{preferred.y:0.#}" +
                $" | over rect {widthOver:0.#}w/{heightOver:0.#}h" +
                (parent != null ? $" | over {parent.name} {parentOver:0.#}w" : string.Empty) +
                $" | wrap {text.textWrappingMode} overflow {text.overflowMode} autosize {text.enableAutoSizing}");

            return true;
        }

        /// <summary>
        /// Mirrors <see cref="SafeAreaFitter"/>'s canvas-size derivation so the pinned world-space
        /// canvas matches the canvas the fitter lays out against. Without this the two disagree and
        /// every measured rect is off by the scaler's factor.
        /// </summary>
        private static void GetCanvasSize(GameObject instance, Vector2Int window, out float canvasWidth, out float canvasHeight)
        {
            canvasWidth = window.x;
            canvasHeight = window.y;

            CanvasScaler scaler = instance.GetComponentInChildren<CanvasScaler>(true);
            if (scaler == null) return;

            float scaleFactor = scaler.scaleFactor;
            if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                scaler.referenceResolution.x > 0f &&
                scaler.referenceResolution.y > 0f)
            {
                float widthScale = window.x / scaler.referenceResolution.x;
                float heightScale = window.y / scaler.referenceResolution.y;
                float match = Mathf.Clamp01(scaler.matchWidthOrHeight);
                scaleFactor = Mathf.Pow(widthScale, 1f - match) * Mathf.Pow(heightScale, match);
            }

            scaleFactor = Mathf.Max(scaleFactor, Mathf.Epsilon);
            canvasWidth = window.x / scaleFactor;
            canvasHeight = window.y / scaleFactor;
        }

        private static void PinCanvases(GameObject instance, float canvasWidth, float canvasHeight)
        {
            Canvas[] canvases = instance.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = RenderMode.WorldSpace;

                RectTransform rect = canvases[i].transform as RectTransform;
                if (rect == null) continue;

                rect.position = Vector3.zero;
                rect.rotation = Quaternion.identity;
                rect.localScale = Vector3.one;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(canvasWidth, canvasHeight);
            }
        }

        /// <summary>
        /// Runs the shipping layout pass. Its reference caching lives in Awake, which edit-mode
        /// instantiation never calls, so it is invoked directly rather than reimplemented here.
        /// </summary>
        private static void DriveResponsiveLayout(GameObject instance, Vector2Int window)
        {
            SafeAreaFitter[] fitters = instance.GetComponentsInChildren<SafeAreaFitter>(true);
            MethodInfo cacheReferences = typeof(SafeAreaFitter).GetMethod(
                "CacheReferences",
                BindingFlags.Instance | BindingFlags.NonPublic);

            for (int i = 0; i < fitters.Length; i++)
            {
                cacheReferences?.Invoke(fitters[i], null);
                fitters[i].RefreshSafeArea(new Rect(0f, 0f, window.x, window.y), window.x, window.y);
            }

            Canvas.ForceUpdateCanvases();
        }

        private static string Trim(string value)
        {
            value = value.Replace("\n", " ");
            return value.Length > 20 ? value.Substring(0, 20) + "..." : value;
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
    }
}

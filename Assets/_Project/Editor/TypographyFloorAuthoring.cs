using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using Line98.Presentation;

namespace Line98.Editor
{
    /// <summary>
    /// Raises authored TMP font sizes in the UI prefabs up to <see cref="UiTypography.MinBody"/>.
    /// <para>
    /// The responsive pass floors text it scales at runtime, but most popups are authored once and
    /// never scaled, so a 22-unit string renders at roughly 7dp on a 3x phone. This sweep fixes the
    /// authored values. It is idempotent -- text already at or above the floor is left alone -- and
    /// it reports every change so the reflow can be reviewed.
    /// </para>
    /// World-space and particle text is skipped: those are scaled by their own transforms, where
    /// canvas-unit floors mean nothing.
    /// </summary>
    public static class TypographyFloorAuthoring
    {
        private static readonly string[] s_SearchFolders =
        {
            "Assets/_Project/Content/Prefabs/UI"
        };

        /// <summary>
        /// UI_Root nests the panel and HUD prefabs, so walking it would edit the same text twice --
        /// once in the source prefab and once as a prefab-instance override. The source prefabs are
        /// authored directly, so fixing those covers UI_Root too.
        /// </summary>
        private const string CompositePrefabName = "UI_Root";

        /// <summary>
        /// Only text clearly under the floor is raised. Strings already at 28-29 units sit near
        /// 9.5dp and are mostly button labels in tight caps, where a bump buys almost no
        /// readability and carries the most reflow risk.
        /// </summary>
        private const float SweepCeiling = 26f;

        [MenuItem("Line98/Audit Font Size Floors")]
        public static void Audit()
        {
            Run(apply: false);
        }

        [MenuItem("Line98/Apply Font Size Floors")]
        public static void Apply()
        {
            Run(apply: true);
        }

        private static void Run(bool apply)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", s_SearchFolders);
            var report = new List<string>();
            int changed = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                if (prefab.name == CompositePrefabName) continue;

                bool prefabDirty = false;
                TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);
                for (int t = 0; t < texts.Length; t++)
                {
                    TMP_Text text = texts[t];
                    if (text == null || ShouldSkip(text)) continue;

                    float authored = text.fontSize;
                    if (authored <= 0f || authored >= SweepCeiling) continue;

                    report.Add($"  {System.IO.Path.GetFileName(path)} / {text.name}: {authored} -> {UiTypography.MinBody}");
                    changed++;

                    if (apply)
                    {
                        text.fontSize = UiTypography.MinBody;
                        EditorUtility.SetDirty(text);
                        prefabDirty = true;
                    }
                }

                if (prefabDirty)
                {
                    PrefabUtility.SavePrefabAsset(prefab);
                }
            }

            if (apply)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            string verb = apply ? "Raised" : "Would raise";
            Debug.Log($"[TypographyFloorAuthoring] {verb} {changed} text(s) to the {UiTypography.MinBody}-unit " +
                      $"({UiTypography.MinBody / 3f:0.#}dp) floor:\n{string.Join("\n", report)}");
        }

        /// <summary>
        /// Skips text whose size is not in canvas units: world-space labels and anything under a
        /// particle system, where the floor would blow the effect up to an absurd size.
        /// </summary>
        private static bool ShouldSkip(TMP_Text text)
        {
            if (text is TextMeshProUGUI) return false;

            // A non-UGUI TMP_Text is world space.
            return true;
        }
    }
}

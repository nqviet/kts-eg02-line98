using UnityEngine;
using UnityEditor;
using Line98.Data;

namespace Line98.Editor
{
    public static class AccessibilityAuthoring
    {
        private const string TexturePath = "Assets/Art/Textures/T_Ball_Accessibility_Patterns.png";
        private const string ThemePath = "Assets/_Project/Content/Definitions/BallTheme_Crystal.asset";

        // UV Rects derived from 512x512 sprite sheet
        // GDD §2 / Concept Vocabularies §9 shape assignments:
        public static readonly Vector4 RectCircleRed   = new Vector4(0.07617f, 0.74219f, 0.18164f, 0.18164f); // Sprite 0
        public static readonly Vector4 RectCrossOrange = new Vector4(0.41602f, 0.74805f, 0.16992f, 0.16992f); // Sprite 1
        public static readonly Vector4 RectTriBlue     = new Vector4(0.75000f, 0.75586f, 0.16992f, 0.16797f); // Sprite 2
        public static readonly Vector4 RectDiamondGreen = new Vector4(0.08203f, 0.40625f, 0.16992f, 0.18555f); // Sprite 3
        public static readonly Vector4 RectStarYellow  = new Vector4(0.41406f, 0.42578f, 0.17188f, 0.16406f); // Sprite 4
        public static readonly Vector4 RectRingPurple  = new Vector4(0.74219f, 0.40625f, 0.18555f, 0.18555f); // Sprite 5
        public static readonly Vector4 RectHexCyan     = new Vector4(0.40820f, 0.08594f, 0.18555f, 0.16016f); // Sprite 6

        [MenuItem("Line98/Authoring/Setup Ball Accessibility Materials")]
        public static void SetupBallAccessibilityMaterials()
        {
            var patternTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (patternTex == null)
            {
                Debug.LogError($"Pattern texture not found at {TexturePath}");
                return;
            }

            ConfigureMaterial("Assets/Art/Materials/M_Ball_Red.mat", patternTex, RectCircleRed);
            ConfigureMaterial("Assets/Art/Materials/M_Ball_Orange.mat", patternTex, RectCrossOrange);
            ConfigureMaterial("Assets/Art/Materials/M_Ball_Blue.mat", patternTex, RectTriBlue);
            ConfigureMaterial("Assets/Art/Materials/M_Ball_Green.mat", patternTex, RectDiamondGreen);
            ConfigureMaterial("Assets/Art/Materials/M_Ball_Yellow.mat", patternTex, RectStarYellow);
            ConfigureMaterial("Assets/Art/Materials/M_Ball_Purple.mat", patternTex, RectRingPurple);
            ConfigureMaterial("Assets/Art/Materials/M_Ball_Cyan.mat", patternTex, RectHexCyan);

            var glowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Ball_GlowShell.mat");
            if (glowMat != null)
            {
                glowMat.SetTexture("_PatternTex", patternTex);
                glowMat.SetFloat("_PatternStrength", 0.0f);
                EditorUtility.SetDirty(glowMat);
            }

            var theme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(ThemePath);
            if (theme != null)
            {
                var so = new SerializedObject(theme);
                var propTex = so.FindProperty("m_AccessibilityPatterns");
                var propOn = so.FindProperty("m_PatternsOn");
                if (propTex != null) propTex.objectReferenceValue = patternTex;
                if (propOn != null) propOn.boolValue = false;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(theme);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Successfully configured all 7 ball materials and BallThemeSO with accessibility patterns!");
        }

        private static void ConfigureMaterial(string path, Texture2D tex, Vector4 rect)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Debug.LogWarning($"Material not found at {path}");
                return;
            }

            var shader = Shader.Find("Line98/PolishedBall");
            if (shader != null)
            {
                mat.shader = shader;
            }
            mat.SetTexture("_PatternTex", tex);
            mat.SetVector("_PatternRect", rect);
            mat.SetColor("_PatternColor", Color.white);
            mat.SetFloat("_PatternStrength", 0.0f); // Default OFF
            EditorUtility.SetDirty(mat);
            Debug.Log($"Configured {mat.name}: shader={mat.shader.name}, HasProp={mat.HasProperty("_PatternTex")}, Tex={mat.GetTexture("_PatternTex")?.name}");
        }

        public static void SetPatternsEnabled(bool enabled)
        {
            float strength = enabled ? 1.0f : 0.0f;
            string[] paths = new string[]
            {
                "Assets/Art/Materials/M_Ball_Red.mat",
                "Assets/Art/Materials/M_Ball_Orange.mat",
                "Assets/Art/Materials/M_Ball_Blue.mat",
                "Assets/Art/Materials/M_Ball_Green.mat",
                "Assets/Art/Materials/M_Ball_Yellow.mat",
                "Assets/Art/Materials/M_Ball_Purple.mat",
                "Assets/Art/Materials/M_Ball_Cyan.mat"
            };

            foreach (var path in paths)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat != null)
                {
                    mat.SetFloat("_PatternStrength", strength);
                    EditorUtility.SetDirty(mat);
                }
            }

            var theme = AssetDatabase.LoadAssetAtPath<BallThemeSO>(ThemePath);
            if (theme != null)
            {
                theme.PatternsOn = enabled;
                EditorUtility.SetDirty(theme);
            }

            AssetDatabase.SaveAssets();
        }
    }
}

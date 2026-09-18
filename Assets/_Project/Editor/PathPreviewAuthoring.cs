using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Line98.Presentation;

namespace Line98.Editor
{
    /// <summary>
    /// Authors the assets the reworked move-path preview needs, and the enlarged undo badge sprite.
    /// Run once via the Line98 menu (or the Unity CLI) after pulling these changes.
    /// <para>
    /// Everything here is generated rather than hand-painted, so re-running is safe and idempotent.
    /// </para>
    /// </summary>
    public static class PathPreviewAuthoring
    {
        private const string MaterialDir = "Assets/Art/Materials/Vfx";
        private const string TextureDir = "Assets/Art/Sprites/VFX";
        private const string BadgePath = "Assets/Art/Sprites/UI/Badges/ui_badge_undo_counter.png";
        private const string ScenePath = "Assets/_Project/Content/Scenes/Game.unity";

        private const string DotTexturePath = TextureDir + "/T_Path_Dots.png";
        private const string DotMaterialPath = MaterialDir + "/M_Path_Dots.mat";
        private const string RingMaterialPath = MaterialDir + "/M_Path_DestRing.mat";
        private const string GlowMaterialPath = MaterialDir + "/M_Path_DestGlow.mat";

        private const string RingTexturePath = TextureDir + "/T_VFX_Shockwave_Ring.png";
        private const string HaloTexturePath = TextureDir + "/T_VFX_Glow_Halo_Ring.png";

        private const string ShaderName = "Line98/PathAdditive";

        [MenuItem("Line98/Author Path Preview Assets")]
        public static void BuildAll()
        {
            if (!Directory.Exists(MaterialDir))
            {
                Directory.CreateDirectory(MaterialDir);
                AssetDatabase.Refresh();
            }

            BuildDotTexture();
            RebuildUndoBadgeSprite();
            AssetDatabase.Refresh();

            Material dots = BuildMaterial(DotMaterialPath, DotTexturePath, new Color(0.35f, 0.92f, 1f, 1f), 1.15f);
            Material ring = BuildMaterial(RingMaterialPath, RingTexturePath, new Color(0.30f, 0.90f, 1f, 1f), 1.30f);
            Material glow = BuildMaterial(GlowMaterialPath, HaloTexturePath, new Color(0.25f, 0.80f, 1f, 1f), 0.90f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssignToScene(dots, ring, glow);

            Debug.Log("[PathPreviewAuthoring] Authored path preview materials, dot strip, and 144px undo badge.");
        }

        /// <summary>
        /// Generates the marching-dot strip the LineRenderer tiles along the route. One soft dot per
        /// tile, padded so consecutive tiles read as separate dots rather than a dashed line.
        /// </summary>
        private static void BuildDotTexture()
        {
            const int width = 64;
            const int height = 16;
            const float dotRadius = 4.5f;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color32[width * height];
            Vector2 center = new Vector2(width * 0.25f, height * 0.5f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    // Soft one-pixel shoulder keeps the dot from aliasing as it scrolls.
                    float alpha = Mathf.Clamp01(1f - (distance - (dotRadius - 1f)));
                    byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                    pixels[y * width + x] = new Color32(255, 255, 255, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(ToAbsolute(DotTexturePath), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(DotTexturePath, ImportAssetOptions.ForceUpdate);
            ConfigureTexture(DotTexturePath, wrapU: TextureWrapMode.Repeat);
        }

        /// <summary>
        /// Re-exports the undo counter badge at 144x144 so the enlarged 72-unit badge stays crisp.
        /// The badge is a flat disc with a ring, so it regenerates exactly rather than upscaling.
        /// </summary>
        private static void RebuildUndoBadgeSprite()
        {
            const int size = 144;
            const float ringWidth = 6f;

            // Sampled from the original 48px sprite.
            Color32 fill = new Color32(0xE8, 0x2A, 0x2A, 255);
            Color32 ring = new Color32(0xFF, 0xFF, 0xFF, 255);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerRadius = size * 0.5f - 1f;
            float fillRadius = outerRadius - ringWidth;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);

                    float outerAlpha = Mathf.Clamp01(outerRadius - distance);
                    if (outerAlpha <= 0f)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    // Blend fill into ring across a one-pixel boundary for a clean edge.
                    float fillWeight = Mathf.Clamp01(fillRadius - distance);
                    Color blended = Color.Lerp(ring, fill, fillWeight);
                    blended.a = outerAlpha;
                    pixels[y * size + x] = blended;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            File.WriteAllBytes(ToAbsolute(BadgePath), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(BadgePath, ImportAssetOptions.ForceUpdate);
        }

        private static Material BuildMaterial(string path, string texturePath, Color tint, float intensity)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[PathPreviewAuthoring] Shader '{ShaderName}' not found. Did PathAdditive.shader compile?");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                Debug.LogWarning($"[PathPreviewAuthoring] Missing texture '{texturePath}'; '{path}' will render untextured.");
            }

            material.SetTexture("_MainTex", texture);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Intensity", intensity);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Wires the three materials onto the scene's PresentationRoot.</summary>
        private static void AssignToScene(Material dots, Material ring, Material glow)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Object.FindAnyObjectByType<PresentationRoot>();
            if (root == null)
            {
                Debug.LogWarning($"[PathPreviewAuthoring] No PresentationRoot in '{ScenePath}'; assign the materials by hand.");
                return;
            }

            var serialized = new SerializedObject(root);
            SetReference(serialized, "m_PathDotMaterial", dots);
            SetReference(serialized, "m_PathDestRingMaterial", ring);
            SetReference(serialized, "m_PathDestGlowMaterial", glow);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetReference(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[PathPreviewAuthoring] PresentationRoot has no '{propertyName}'.");
                return;
            }

            property.objectReferenceValue = value;
        }

        private static void ConfigureTexture(string path, TextureWrapMode wrapU)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.wrapModeU = wrapU;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static string ToAbsolute(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }
    }
}

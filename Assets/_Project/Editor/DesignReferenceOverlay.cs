using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Editor
{
    /// <summary>
    /// Editor-only alignment tool: renders Docs/Design/main_scene_mockup.png at 35% opacity
    /// across the 1080x1920 canvas to verify pixel alignment without polluting scene assets.
    /// Uses transient GameObject with HideFlags.DontSave so it is never saved to the scene.
    /// </summary>
    public static class DesignReferenceOverlay
    {
        private static readonly string[] s_ReferencePaths = new[]
        {
            "Docs/Design/main_scene_mockup.png",
            "Docs/Design/main_scene__crystal.png"
        };

        private const string OverlayGameObjectName = "[DesignReferenceOverlay_Transient]";
        private const float DefaultOpacity = 0.35f;

        private static GameObject s_OverlayInstance;
        private static Image s_OverlayImage;
        private static Texture2D[] s_CachedTextures;
        private static Sprite[] s_CachedSprites;
        private static int s_CurrentIndex = -1;

        [MenuItem("Line98/Dev/Show Design Reference Overlay _F9", false, 101)]
        public static void ToggleOverlay()
        {
            EnsureInstances();

            s_CurrentIndex++;
            if (s_CurrentIndex >= s_ReferencePaths.Length)
            {
                s_CurrentIndex = -1;
            }

            if (s_CurrentIndex == -1)
            {
                if (s_OverlayInstance != null)
                {
                    s_OverlayInstance.SetActive(false);
                }
                Debug.Log("[DesignReferenceOverlay] Overlay off.");
                return;
            }

            Sprite sprite = GetOrCreateSprite(s_CurrentIndex);
            if (sprite == null)
            {
                Debug.LogWarning($"[DesignReferenceOverlay] Could not load reference image at index {s_CurrentIndex} ({s_ReferencePaths[s_CurrentIndex]}).");
                return;
            }

            s_OverlayImage.sprite = sprite;
            s_OverlayInstance.SetActive(true);
            Debug.Log($"[DesignReferenceOverlay] Overlay showing [{s_CurrentIndex}]: '{s_ReferencePaths[s_CurrentIndex]}' at {DefaultOpacity * 100f}%.");
        }

        private static void EnsureInstances()
        {
            if (s_CachedTextures == null || s_CachedTextures.Length != s_ReferencePaths.Length)
            {
                s_CachedTextures = new Texture2D[s_ReferencePaths.Length];
                s_CachedSprites = new Sprite[s_ReferencePaths.Length];
            }

            if (s_OverlayInstance == null)
            {
                var existing = GameObject.Find(OverlayGameObjectName);
                if (existing != null)
                {
                    s_OverlayInstance = existing;
                    s_OverlayImage = s_OverlayInstance.GetComponentInChildren<Image>();
                }
                else
                {
                    CreateOverlayHierarchy();
                }
            }
        }

        private static Sprite GetOrCreateSprite(int index)
        {
            if (index < 0 || index >= s_ReferencePaths.Length)
            {
                return null;
            }

            if (s_CachedSprites[index] != null)
            {
                return s_CachedSprites[index];
            }

            string path = s_ReferencePaths[index];
            if (!File.Exists(path))
            {
                return null;
            }

            byte[] fileData = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = Path.GetFileNameWithoutExtension(path) + "_transient",
                hideFlags = HideFlags.DontSave
            };
            tex.LoadImage(fileData);
            s_CachedTextures[index] = tex;

            var spr = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            spr.name = tex.name + "_sprite";
            spr.hideFlags = HideFlags.DontSave;
            s_CachedSprites[index] = spr;

            return spr;
        }

        private static void CreateOverlayHierarchy()
        {
            s_OverlayInstance = new GameObject(OverlayGameObjectName)
            {
                hideFlags = HideFlags.DontSave
            };

            var canvas = s_OverlayInstance.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = s_OverlayInstance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            var imgGo = new GameObject("MockupImage")
            {
                hideFlags = HideFlags.DontSave
            };
            imgGo.transform.SetParent(s_OverlayInstance.transform, false);

            var rectTransform = imgGo.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            s_OverlayImage = imgGo.AddComponent<Image>();
            s_OverlayImage.color = new Color(1f, 1f, 1f, DefaultOpacity);
            s_OverlayImage.raycastTarget = false;
            s_OverlayImage.preserveAspect = true;

            s_OverlayInstance.SetActive(false);
        }
    }
}

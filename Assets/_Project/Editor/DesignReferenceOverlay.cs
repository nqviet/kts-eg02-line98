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
        private const string ReferenceImagePath = "Docs/Design/main_scene_mockup.png";
        private const string OverlayGameObjectName = "[DesignReferenceOverlay_Transient]";
        private const float DefaultOpacity = 0.35f;

        private static GameObject s_OverlayInstance;
        private static Texture2D s_CachedTexture;
        private static Sprite s_CachedSprite;

        [MenuItem("Line98/Dev/Show Design Reference Overlay _F9", false, 101)]
        public static void ToggleOverlay()
        {
            if (s_OverlayInstance != null)
            {
                bool nextState = !s_OverlayInstance.activeSelf;
                s_OverlayInstance.SetActive(nextState);
                Debug.Log($"[DesignReferenceOverlay] Overlay {(nextState ? "shown" : "hidden")}.");
                return;
            }

            // Check if existing orphaned transient object exists in active scene
            var existing = GameObject.Find(OverlayGameObjectName);
            if (existing != null)
            {
                s_OverlayInstance = existing;
                bool nextState = !s_OverlayInstance.activeSelf;
                s_OverlayInstance.SetActive(nextState);
                Debug.Log($"[DesignReferenceOverlay] Overlay {(nextState ? "shown" : "hidden")}.");
                return;
            }

            CreateOverlay();
        }

        private static void CreateOverlay()
        {
            if (!File.Exists(ReferenceImagePath))
            {
                Debug.LogWarning($"[DesignReferenceOverlay] Reference image not found at '{ReferenceImagePath}'.");
                return;
            }

            if (s_CachedTexture == null)
            {
                byte[] fileData = File.ReadAllBytes(ReferenceImagePath);
                s_CachedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "main_scene_mockup_transient",
                    hideFlags = HideFlags.DontSave
                };
                s_CachedTexture.LoadImage(fileData);
            }

            if (s_CachedSprite == null && s_CachedTexture != null)
            {
                s_CachedSprite = Sprite.Create(
                    s_CachedTexture,
                    new Rect(0f, 0f, s_CachedTexture.width, s_CachedTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                s_CachedSprite.name = "main_scene_mockup_sprite_transient";
                s_CachedSprite.hideFlags = HideFlags.DontSave;
            }

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

            var img = imgGo.AddComponent<Image>();
            img.sprite = s_CachedSprite;
            img.color = new Color(1f, 1f, 1f, DefaultOpacity);
            img.raycastTarget = false;
            img.preserveAspect = true;

            s_OverlayInstance.SetActive(true);
            Debug.Log("[DesignReferenceOverlay] Transient overlay created and displayed (never serialized into scene).");
        }
    }
}

#if UNITY_EDITOR || DEBUG
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>
    /// F9 debug overlay: renders main_scene_mockup at 35% opacity across the 1080x1920 canvas
    /// to verify 1:1 pixel-level alignment with the design spec.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MockupOverlayDebug : MonoBehaviour
    {
        [SerializeField] private Sprite m_MockupSprite;
        [SerializeField] private float m_Opacity = 0.35f;

        private GameObject m_OverlayCanvasGo;
        private Image m_OverlayImage;
        private bool m_IsVisible = false;

        private void Start()
        {
            EnsureOverlay();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.f9Key.wasPressedThisFrame)
            {
                ToggleOverlay();
            }
#endif
        }

        public void SetMockupSprite(Sprite sprite)
        {
            m_MockupSprite = sprite;
            if (m_OverlayImage != null)
            {
                m_OverlayImage.sprite = sprite;
            }
        }

        public void ToggleOverlay()
        {
            m_IsVisible = !m_IsVisible;
            if (m_OverlayCanvasGo != null)
            {
                m_OverlayCanvasGo.SetActive(m_IsVisible);
            }
        }

        private void EnsureOverlay()
        {
            if (m_OverlayCanvasGo != null) return;

            m_OverlayCanvasGo = new GameObject("[MockupOverlay_Debug]");
            m_OverlayCanvasGo.transform.SetParent(transform, false);

            var canvas = m_OverlayCanvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = m_OverlayCanvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(HudLayoutSolver.ReferenceWidth, HudLayoutSolver.ReferenceHeight);
            scaler.matchWidthOrHeight = 0f; // Match width

            var imgGo = new GameObject("MockupImage");
            imgGo.transform.SetParent(m_OverlayCanvasGo.transform, false);

            var rectTransform = imgGo.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            m_OverlayImage = imgGo.AddComponent<Image>();
            m_OverlayImage.sprite = m_MockupSprite;
            m_OverlayImage.color = new Color(1f, 1f, 1f, m_Opacity);
            m_OverlayImage.raycastTarget = false;
            m_OverlayImage.preserveAspect = true;

            m_OverlayCanvasGo.SetActive(m_IsVisible);
        }
    }
}
#endif

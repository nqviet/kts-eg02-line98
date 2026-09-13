using System;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Monitors Screen.safeArea changes and applies safe margins to UI components.
    /// Exposes normalized safe insets and reference canvas pixel deltas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool m_ApplyToTransform = false;

        private RectTransform m_RectTransform;
        private Rect m_LastSafeArea = Rect.zero;
        private Vector2Int m_LastScreenSize = Vector2Int.zero;
        private float m_SafeTopCanvas;
        private float m_SafeBottomCanvas;

        public event Action<float, float> OnSafeAreaChanged;

        public float SafeTopCanvas => m_SafeTopCanvas;
        public float SafeBottomCanvas => m_SafeBottomCanvas;

        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            RefreshSafeArea(force: true);
        }

        private void Update()
        {
            if (Screen.safeArea != m_LastSafeArea || Screen.width != m_LastScreenSize.x || Screen.height != m_LastScreenSize.y)
            {
                RefreshSafeArea();
            }
        }

        public void RefreshSafeArea(bool force = false)
        {
            Rect safeArea = Screen.safeArea;
            int screenW = Screen.width;
            int screenH = Screen.height;

            if (!force && safeArea == m_LastSafeArea && screenW == m_LastScreenSize.x && screenH == m_LastScreenSize.y)
            {
                return;
            }

            m_LastSafeArea = safeArea;
            m_LastScreenSize = new Vector2Int(screenW, screenH);

            if (screenW <= 0 || screenH <= 0) return;

            // Insets in normalized screen space
            float topInsetNorm = (screenH - safeArea.yMax) / screenH;
            float bottomInsetNorm = safeArea.yMin / screenH;

            // Scaled into 1080x1920 reference coordinate space
            float canvasHeight = screenH * HudLayoutSolver.ReferenceWidth / screenW;
            m_SafeTopCanvas = topInsetNorm * canvasHeight;
            m_SafeBottomCanvas = bottomInsetNorm * canvasHeight;

            if (m_ApplyToTransform && m_RectTransform != null)
            {
                Vector2 anchorMin = safeArea.position;
                Vector2 anchorMax = safeArea.position + safeArea.size;
                anchorMin.x /= screenW;
                anchorMin.y /= screenH;
                anchorMax.x /= screenW;
                anchorMax.y /= screenH;

                m_RectTransform.anchorMin = anchorMin;
                m_RectTransform.anchorMax = anchorMax;
                m_RectTransform.offsetMin = Vector2.zero;
                m_RectTransform.offsetMax = Vector2.zero;
            }

            ApplyHudLayout(canvasHeight);
            OnSafeAreaChanged?.Invoke(m_SafeTopCanvas, m_SafeBottomCanvas);
        }

        private void ApplyHudLayout(float canvasHeight)
        {
            var layout = HudLayoutSolver.Solve(HudLayoutSolver.ReferenceWidth, canvasHeight, m_SafeTopCanvas, m_SafeBottomCanvas);
            Place("Canvas_StaticHUD/BrandBar", layout.BrandRect);
            Place("Canvas_StaticHUD/CardRow", layout.HudRect);
            Place("Canvas_StaticHUD/ActionBar", layout.ActionRect);
            Place("Canvas_DynamicHUD/ScoreValue", new Rect(layout.ScoreCardRect.center.x - 150f, layout.HudRect.y + 64f, 300f, 80f));
            Place("Canvas_DynamicHUD/BestValue", new Rect(layout.BestCardRect.center.x - 150f, layout.HudRect.y + 83f, 300f, 80f));
            Place("Canvas_DynamicHUD/PreviewQueue", new Rect(410f, layout.HudRect.y + 70f, 260f, 80f));
            Place("Canvas_DynamicHUD/UndoBadge", new Rect(329f, layout.UndoBtnRect.y - 24f, 48f, 48f));
            var rig = UnityEngine.Object.FindAnyObjectByType<CameraRig>();
            if (rig != null) rig.SetTargetViewport(layout.BoardViewportRect.min, layout.BoardViewportRect.max);
        }

        private void Place(string path, Rect bounds)
        {
            var rect = transform.Find(path) as RectTransform;
            if (rect == null) return;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(bounds.x, -bounds.y);
            rect.sizeDelta = bounds.size;
        }
    }
}

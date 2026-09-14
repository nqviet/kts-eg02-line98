using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Converts the device safe area into canvas units and applies the responsive HUD layout.
    /// References are resolved once in Awake so a resize performs no hierarchy searches.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        public enum FitMode
        {
            Hud,
            Overlay
        }

        private readonly struct ResponsiveRect
        {
            public readonly RectTransform Rect;
            public readonly Vector2 AnchoredPosition;
            public readonly Vector2 SizeDelta;

            public ResponsiveRect(RectTransform rect)
            {
                Rect = rect;
                AnchoredPosition = rect.anchoredPosition;
                SizeDelta = rect.sizeDelta;
            }
        }

        private readonly struct ResponsiveText
        {
            public readonly TMP_Text Text;
            public readonly float FontSize;

            public ResponsiveText(TMP_Text text)
            {
                Text = text;
                FontSize = text.fontSize;
            }
        }

        [SerializeField] private FitMode m_Mode = FitMode.Hud;
        [SerializeField] private bool m_ApplyToTransform = false;
        [SerializeField] private RectTransform m_OverlayRoot;

        private readonly List<ResponsiveRect> m_StaticElements = new List<ResponsiveRect>();
        private readonly List<ResponsiveRect> m_PreviewElements = new List<ResponsiveRect>();
        private readonly List<ResponsiveRect> m_BadgeElements = new List<ResponsiveRect>();
        private readonly List<ResponsiveText> m_StaticTexts = new List<ResponsiveText>();
        private readonly List<ResponsiveText> m_DynamicTexts = new List<ResponsiveText>();

        private RectTransform m_RectTransform;
        [Header("HUD Slots")]
        [SerializeField] private RectTransform m_StaticCanvas;
        [SerializeField] private RectTransform m_DynamicCanvas;
        [SerializeField] private RectTransform m_BrandBar;
        [SerializeField] private RectTransform m_CardRow;
        [SerializeField] private RectTransform m_ActionBar;
        [SerializeField] private RectTransform m_ScoreValue;
        [SerializeField] private RectTransform m_BestValue;
        [SerializeField] private RectTransform m_PreviewQueue;
        [SerializeField] private RectTransform m_UndoBadge;
        private UnityEngine.UI.CanvasScaler m_CanvasScaler;
        private CameraRig m_CameraRig;

        private readonly List<UiResponsiveModal> m_ResponsivePopups = new List<UiResponsiveModal>();

        private Rect m_LastSafeArea = Rect.zero;
        private Vector2Int m_LastScreenSize = Vector2Int.zero;
        private float m_SafeTopCanvas;
        private float m_SafeBottomCanvas;
        private float m_SafeLeftCanvas;
        private float m_SafeRightCanvas;
        private HudLayoutSolver.LayoutResult m_CurrentLayout;

        public event Action<float, float> OnSafeAreaChanged;

        public float SafeTopCanvas => m_SafeTopCanvas;
        public float SafeBottomCanvas => m_SafeBottomCanvas;
        public float SafeLeftCanvas => m_SafeLeftCanvas;
        public float SafeRightCanvas => m_SafeRightCanvas;
        public HudLayoutSolver.LayoutResult CurrentLayout => m_CurrentLayout;
        public FitMode Mode => m_Mode;

        public void SetCameraRig(CameraRig cameraRig)
        {
            if (m_Mode == FitMode.Hud)
            {
                m_CameraRig = cameraRig;
            }
        }

        /// <summary>
        /// Registers a placed or runtime-created popup with the current safe-area solution.
        /// Runtime popup factories must call this once after instantiation.
        /// </summary>
        public void RegisterPopup(PopupView popup)
        {
            if (popup == null)
            {
                return;
            }

            UiResponsiveModal responsiveModal = popup.GetComponent<UiResponsiveModal>();
            if (responsiveModal == null)
            {
                responsiveModal = popup.gameObject.AddComponent<UiResponsiveModal>();
            }

            RegisterResponsivePopup(responsiveModal);
        }

        public void UnregisterPopup(PopupView popup)
        {
            if (popup == null)
            {
                return;
            }

            UiResponsiveModal responsiveModal = popup.GetComponent<UiResponsiveModal>();
            if (responsiveModal != null)
            {
                m_ResponsivePopups.Remove(responsiveModal);
            }
        }

        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            CacheReferences();
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
            RefreshSafeArea(Screen.safeArea, Screen.width, Screen.height, force);
        }

        /// <summary>
        /// Test seam for applying a supplied device safe area without requiring a physical device.
        /// </summary>
        public void RefreshSafeArea(Rect safeArea, int screenWidth, int screenHeight)
        {
            RefreshSafeArea(safeArea, screenWidth, screenHeight, force: true);
        }

        private void RefreshSafeArea(Rect safeArea, int screenWidth, int screenHeight, bool force)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                return;
            }

            if (!force && safeArea == m_LastSafeArea && screenWidth == m_LastScreenSize.x && screenHeight == m_LastScreenSize.y)
            {
                return;
            }

            m_LastSafeArea = safeArea;
            m_LastScreenSize = new Vector2Int(screenWidth, screenHeight);

            GetCanvasSize(screenWidth, screenHeight, out float canvasWidth, out float canvasHeight);
            m_SafeLeftCanvas = Mathf.Clamp(safeArea.xMin, 0f, screenWidth) / screenWidth * canvasWidth;
            m_SafeRightCanvas = Mathf.Clamp(screenWidth - safeArea.xMax, 0f, screenWidth) / screenWidth * canvasWidth;
            m_SafeTopCanvas = Mathf.Clamp(screenHeight - safeArea.yMax, 0f, screenHeight) / screenHeight * canvasHeight;
            m_SafeBottomCanvas = Mathf.Clamp(safeArea.yMin, 0f, screenHeight) / screenHeight * canvasHeight;

            if (m_Mode == FitMode.Overlay)
            {
                ApplyOverlaySafeArea(safeArea, screenWidth, screenHeight);
                OnSafeAreaChanged?.Invoke(m_SafeTopCanvas, m_SafeBottomCanvas);
                return;
            }

            if (m_ApplyToTransform && m_RectTransform != null)
            {
                Vector2 anchorMin = safeArea.position / new Vector2(screenWidth, screenHeight);
                Vector2 anchorMax = (safeArea.position + safeArea.size) / new Vector2(screenWidth, screenHeight);
                m_RectTransform.anchorMin = Vector2.Max(Vector2.zero, Vector2.Min(Vector2.one, anchorMin));
                m_RectTransform.anchorMax = Vector2.Max(Vector2.zero, Vector2.Min(Vector2.one, anchorMax));
                m_RectTransform.offsetMin = Vector2.zero;
                m_RectTransform.offsetMax = Vector2.zero;
            }

            ApplyHudLayout(canvasWidth, canvasHeight);
            OnSafeAreaChanged?.Invoke(m_SafeTopCanvas, m_SafeBottomCanvas);
        }

        private void ApplyOverlaySafeArea(Rect safeArea, int screenWidth, int screenHeight)
        {
            RectTransform target = m_OverlayRoot != null ? m_OverlayRoot : m_RectTransform;
            if (target == null)
            {
                return;
            }

            Vector2 screenSize = new Vector2(screenWidth, screenHeight);
            target.anchorMin = Vector2.Max(Vector2.zero, Vector2.Min(Vector2.one, safeArea.position / screenSize));
            target.anchorMax = Vector2.Max(Vector2.zero, Vector2.Min(Vector2.one, (safeArea.position + safeArea.size) / screenSize));
            target.offsetMin = Vector2.zero;
            target.offsetMax = Vector2.zero;
        }

        private void CacheReferences()
        {
            if (m_Mode == FitMode.Overlay)
            {
                return;
            }

            ValidateSlots();

            if (m_StaticCanvas != null)
            {
                m_CanvasScaler = m_StaticCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            }

            CacheResponsiveChildren(m_BrandBar, m_StaticElements);
            CacheResponsiveChildren(m_CardRow, m_StaticElements);
            CacheResponsiveChildren(m_ActionBar, m_StaticElements);
            CacheResponsiveChildren(m_PreviewQueue, m_PreviewElements);
            CacheResponsiveChildren(m_UndoBadge, m_BadgeElements);
            CacheTexts(m_StaticCanvas, m_StaticTexts);
            CacheTexts(m_DynamicCanvas, m_DynamicTexts);

            m_CameraRig = UnityEngine.Object.FindAnyObjectByType<CameraRig>();
        }

        private void ValidateSlots()
        {
            if (m_StaticCanvas == null || m_DynamicCanvas == null || m_BrandBar == null ||
                m_CardRow == null || m_ActionBar == null || m_ScoreValue == null ||
                m_BestValue == null || m_PreviewQueue == null || m_UndoBadge == null)
            {
                Debug.LogError("[SafeAreaFitter] One or more serialized HUD slots are not assigned.", this);
            }
        }

        private void RegisterResponsivePopup(UiResponsiveModal responsiveModal)
        {
            if (responsiveModal == null || m_ResponsivePopups.Contains(responsiveModal))
            {
                return;
            }

            m_ResponsivePopups.Add(responsiveModal);
            responsiveModal.ApplyResponsiveLayout(m_CurrentLayout.LayoutRect.width, m_CurrentLayout.MiddleRect.height);
        }

        private static void CacheResponsiveChildren(RectTransform root, List<ResponsiveRect> elements)
        {
            if (root == null)
            {
                return;
            }

            RectTransform[] descendants = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                if (descendants[i] != root)
                {
                    elements.Add(new ResponsiveRect(descendants[i]));
                }
            }
        }

        private static void CacheTexts(RectTransform root, List<ResponsiveText> texts)
        {
            if (root == null)
            {
                return;
            }

            TMP_Text[] descendants = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                texts.Add(new ResponsiveText(descendants[i]));
            }
        }

        private void GetCanvasSize(int screenWidth, int screenHeight, out float canvasWidth, out float canvasHeight)
        {
            float scaleFactor = 1f;
            if (m_CanvasScaler != null)
            {
                if (m_CanvasScaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                    m_CanvasScaler.referenceResolution.x > 0f &&
                    m_CanvasScaler.referenceResolution.y > 0f)
                {
                    float widthScale = screenWidth / m_CanvasScaler.referenceResolution.x;
                    float heightScale = screenHeight / m_CanvasScaler.referenceResolution.y;
                    float match = Mathf.Clamp01(m_CanvasScaler.matchWidthOrHeight);
                    scaleFactor = Mathf.Pow(widthScale, 1f - match) * Mathf.Pow(heightScale, match);
                }
                else
                {
                    scaleFactor = m_CanvasScaler.scaleFactor;
                }
            }

            scaleFactor = Mathf.Max(scaleFactor, Mathf.Epsilon);
            canvasWidth = screenWidth / scaleFactor;
            canvasHeight = screenHeight / scaleFactor;
        }

        private void ApplyHudLayout(float canvasWidth, float canvasHeight)
        {
            HudLayoutSolver.LayoutResult layout = HudLayoutSolver.Solve(
                canvasWidth,
                canvasHeight,
                m_SafeTopCanvas,
                m_SafeBottomCanvas,
                m_SafeLeftCanvas,
                m_SafeRightCanvas);
            m_CurrentLayout = layout;

            PlaceTopStretch(m_BrandBar, layout.BrandRect, canvasWidth);
            PlaceTopStretch(m_CardRow, layout.HudRect, canvasWidth);
            PlaceBottomStretch(m_ActionBar, layout.ActionRect, canvasWidth, canvasHeight);
            ApplyScale(m_StaticElements, layout.Scale);
            ApplyTextScale(m_StaticTexts, layout.Scale);

            float contentInset = 9f * layout.Scale;
            PlaceTopCenter(m_ScoreValue, new Rect(
                layout.ScoreCardRect.xMin + contentInset,
                layout.HudRect.yMin + 64f * layout.Scale,
                Mathf.Max(0f, layout.ScoreCardRect.width - contentInset * 2f),
                80f * layout.Scale), canvasWidth);
            PlaceTopCenter(m_BestValue, new Rect(
                layout.BestCardRect.xMin + contentInset,
                layout.HudRect.yMin + 83f * layout.Scale,
                Mathf.Max(0f, layout.BestCardRect.width - contentInset * 2f),
                80f * layout.Scale), canvasWidth);
            PlaceTopCenter(m_PreviewQueue, new Rect(
                layout.NextCardRect.center.x - 130f * layout.Scale,
                layout.HudRect.yMin + 70f * layout.Scale,
                260f * layout.Scale,
                80f * layout.Scale), canvasWidth);
            PlaceTopCenter(m_UndoBadge, new Rect(
                layout.UndoBtnRect.xMax - 38f * layout.Scale,
                layout.UndoBtnRect.yMin - 24f * layout.Scale,
                48f * layout.Scale,
                48f * layout.Scale), canvasWidth);
            ApplyScale(m_PreviewElements, layout.Scale);
            ApplyScale(m_BadgeElements, layout.Scale);
            ApplyTextScale(m_DynamicTexts, layout.Scale);

            if (m_CameraRig != null)
            {
                m_CameraRig.SetTargetViewport(layout.BoardViewportRect.min, layout.BoardViewportRect.max);
            }

            for (int i = m_ResponsivePopups.Count - 1; i >= 0; i--)
            {
                UiResponsiveModal responsivePopup = m_ResponsivePopups[i];
                if (responsivePopup == null)
                {
                    m_ResponsivePopups.RemoveAt(i);
                    continue;
                }

                responsivePopup.ApplyResponsiveLayout(layout.LayoutRect.width, layout.MiddleRect.height);
            }
        }

        private static void PlaceTopStretch(RectTransform rect, Rect bounds, float canvasWidth)
        {
            if (rect == null)
            {
                return;
            }

            GetHorizontalAnchors(bounds, canvasWidth, out float minX, out float maxX);
            rect.anchorMin = new Vector2(minX, 1f);
            rect.anchorMax = new Vector2(maxX, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -bounds.yMin);
            rect.sizeDelta = new Vector2(0f, bounds.height);
        }

        private static void PlaceBottomStretch(RectTransform rect, Rect bounds, float canvasWidth, float canvasHeight)
        {
            if (rect == null)
            {
                return;
            }

            GetHorizontalAnchors(bounds, canvasWidth, out float minX, out float maxX);
            rect.anchorMin = new Vector2(minX, 0f);
            rect.anchorMax = new Vector2(maxX, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, Mathf.Max(0f, canvasHeight - bounds.yMax));
            rect.sizeDelta = new Vector2(0f, bounds.height);
        }

        private static void PlaceTopCenter(RectTransform rect, Rect bounds, float canvasWidth)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(bounds.center.x - canvasWidth * 0.5f, -bounds.center.y);
            rect.sizeDelta = bounds.size;
        }

        private static void GetHorizontalAnchors(Rect bounds, float canvasWidth, out float minX, out float maxX)
        {
            if (canvasWidth <= 0f)
            {
                minX = 0f;
                maxX = 0f;
                return;
            }

            minX = Mathf.Clamp01(bounds.xMin / canvasWidth);
            maxX = Mathf.Clamp01(bounds.xMax / canvasWidth);
        }

        private static void ApplyScale(List<ResponsiveRect> elements, float scale)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                ResponsiveRect element = elements[i];
                if (element.Rect == null)
                {
                    continue;
                }

                element.Rect.anchoredPosition = element.AnchoredPosition * scale;
                element.Rect.sizeDelta = element.SizeDelta * scale;
            }
        }

        private static void ApplyTextScale(List<ResponsiveText> texts, float scale)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                ResponsiveText text = texts[i];
                if (text.Text != null)
                {
                    text.Text.fontSize = text.FontSize * scale;
                }
            }
        }
    }
}

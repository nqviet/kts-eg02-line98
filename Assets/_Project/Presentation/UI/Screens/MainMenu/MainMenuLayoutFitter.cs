using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Fits the authored 1080x1920 main menu column into the safe screen rect with one uniform scale,
    /// and keeps the 3D showcase board framed inside the column's board slot on every aspect ratio.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class MainMenuLayoutFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform m_Column;
        [SerializeField] private Vector2 m_ReferenceSize = new Vector2(1080f, 1920f);
        [SerializeField, Min(0.05f)] private float m_MaxScale = 1f;

        [Tooltip("Board slot in column units, measured from the column's top-left corner.")]
        [SerializeField] private Rect m_BoardSlot = new Rect(226.8f, 528f, 626.4f, 564.5f);

        private RectTransform m_RectTransform;
        private Canvas m_Canvas;
        private MenuShowcaseRig m_ShowcaseRig;
        private Vector2 m_LastHostSize;
        private Vector2Int m_LastScreenSize;
        private bool m_LastRigInitialized;

        public RectTransform Column => m_Column;
        public Rect BoardSlot => m_BoardSlot;

        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            if (m_Column == null && transform.childCount > 0)
            {
                m_Column = transform.GetChild(0) as RectTransform;
            }
        }

        private void OnEnable()
        {
            m_LastHostSize = Vector2.zero;
            if (m_ShowcaseRig == null)
            {
                m_ShowcaseRig = FindAnyObjectByType<MenuShowcaseRig>();
            }
        }

        private void LateUpdate()
        {
            bool rigInitialized = m_ShowcaseRig != null && m_ShowcaseRig.IsInitialized;
            Vector2 hostSize = m_RectTransform.rect.size;
            Vector2Int screenSize = new Vector2Int(Screen.width, Screen.height);
            if (hostSize == m_LastHostSize && screenSize == m_LastScreenSize && rigInitialized == m_LastRigInitialized)
            {
                return;
            }

            m_LastHostSize = hostSize;
            m_LastScreenSize = screenSize;
            m_LastRigInitialized = rigInitialized;
            Apply();
        }

        /// <summary>Scales the column into the host rect and re-frames the showcase camera.</summary>
        public void Apply()
        {
            if (m_Column == null || m_RectTransform == null)
            {
                return;
            }

            // The host is already inset to the safe area by the overlay SafeAreaFitter.
            ReferenceFitSolver.FitResult fit = ReferenceFitSolver.Solve(
                m_RectTransform.rect.size, default, m_ReferenceSize, 0.05f, m_MaxScale);
            if (!fit.IsValid)
            {
                return;
            }

            m_Column.anchorMin = new Vector2(0.5f, 0.5f);
            m_Column.anchorMax = new Vector2(0.5f, 0.5f);
            m_Column.pivot = new Vector2(0.5f, 0.5f);
            m_Column.sizeDelta = m_ReferenceSize;
            m_Column.anchoredPosition = Vector2.zero;
            m_Column.localScale = new Vector3(fit.Scale, fit.Scale, 1f);

            ApplyShowcaseViewport();
        }

        private void ApplyShowcaseViewport()
        {
            if (m_ShowcaseRig == null || !m_ShowcaseRig.IsInitialized || m_ShowcaseRig.CameraRig == null ||
                Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            if (m_Canvas == null)
            {
                m_Canvas = m_Column.GetComponentInParent<Canvas>();
            }

            Camera uiCamera = m_Canvas != null && m_Canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? m_Canvas.worldCamera
                : null;

            // Column local space is centre-pivoted with Y up; the slot is authored top-left with Y down.
            Vector2 half = m_ReferenceSize * 0.5f;
            Vector3 localMin = new Vector3(m_BoardSlot.xMin - half.x, half.y - m_BoardSlot.yMax, 0f);
            Vector3 localMax = new Vector3(m_BoardSlot.xMax - half.x, half.y - m_BoardSlot.yMin, 0f);
            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(uiCamera, m_Column.TransformPoint(localMin));
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(uiCamera, m_Column.TransformPoint(localMax));
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            m_ShowcaseRig.CameraRig.SetTargetViewport(screenMin / screen, screenMax / screen);
        }
    }
}

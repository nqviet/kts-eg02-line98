using UnityEngine;

namespace Line98.Views
{
    /// <summary>
    /// Presentation component that handles device notch / safe area anchoring
    /// and scales the design root maintaining aspect ratio.
    /// </summary>
    public sealed class Line98SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private RectTransform m_SafeAreaRect;
        [SerializeField] private RectTransform m_DesignRootRect;
        [SerializeField] private float m_DesignWidth = 768f;
        [SerializeField] private float m_DesignHeight = 1366f;

        private Rect m_LastSafeArea;

        public void Initialize(RectTransform safeAreaRect, RectTransform designRootRect, float designWidth, float designHeight)
        {
            m_SafeAreaRect = safeAreaRect;
            m_DesignRootRect = designRootRect;
            m_DesignWidth = designWidth;
            m_DesignHeight = designHeight;
            ApplySafeArea();
        }

        private void Start()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (m_LastSafeArea != Screen.safeArea)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (m_SafeAreaRect == null || m_DesignRootRect == null)
            {
                return;
            }

            Rect safeArea = Screen.safeArea;
            m_LastSafeArea = safeArea;
            m_SafeAreaRect.anchorMin = safeArea.position / new Vector2(Screen.width, Screen.height);
            m_SafeAreaRect.anchorMax = (safeArea.position + safeArea.size) / new Vector2(Screen.width, Screen.height);
            m_SafeAreaRect.offsetMin = Vector2.zero;
            m_SafeAreaRect.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();

            float scale = Mathf.Min(m_SafeAreaRect.rect.width / m_DesignWidth, m_SafeAreaRect.rect.height / m_DesignHeight);
            m_DesignRootRect.localScale = new Vector3(scale, scale, 1f);
            m_DesignRootRect.anchoredPosition = Vector2.zero;
        }
    }
}

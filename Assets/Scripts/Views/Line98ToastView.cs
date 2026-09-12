using TMPro;
using UnityEngine;

namespace Line98.Views
{
    /// <summary>
    /// Presentation component for animated gameplay notification toasts.
    /// Handles timing and alpha fading.
    /// </summary>
    public sealed class Line98ToastView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup m_ToastGroup;
        [SerializeField] private TextMeshProUGUI m_ToastText;

        private float m_ToastUntil;
        private float m_ToastDuration = 1.35f;
        private float m_FadeDuration = 0.28f;

        public void Initialize(CanvasGroup group, TextMeshProUGUI text, float toastDuration, float fadeDuration)
        {
            m_ToastGroup = group;
            m_ToastText = text;
            m_ToastDuration = toastDuration;
            m_FadeDuration = fadeDuration;

            if (m_ToastGroup != null)
            {
                m_ToastGroup.alpha = 0f;
            }
        }

        public void ShowToast(string message)
        {
            if (m_ToastText != null)
            {
                m_ToastText.SetText(message);
            }

            m_ToastUntil = Time.unscaledTime + m_ToastDuration;

            if (m_ToastGroup != null)
            {
                m_ToastGroup.alpha = 1f;
            }
        }

        private void Update()
        {
            if (m_ToastGroup == null || m_ToastGroup.alpha <= 0f)
            {
                return;
            }

            float remaining = m_ToastUntil - Time.unscaledTime;
            m_ToastGroup.alpha = Mathf.Clamp01(remaining / m_FadeDuration);
        }
    }
}

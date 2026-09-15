using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Owns responsive modal application independently from popup navigation lifecycle.</summary>
    [DisallowMultipleComponent]
    public sealed class UiResponsiveModal : MonoBehaviour
    {
        [SerializeField] private PopupView m_Popup;

        private void Awake()
        {
            if (m_Popup == null) m_Popup = GetComponent<PopupView>();
        }

        public void ApplyResponsiveLayout(float layoutWidth, float middleHeight)
        {
            m_Popup?.ApplyResponsiveLayout(layoutWidth, middleHeight);
        }

        public void ApplyResponsiveLayout(float layoutWidth, float middleHeight, float layoutHeight)
        {
            if (m_Popup == null) m_Popup = GetComponent<PopupView>();
            if (m_Popup == null)
            {
                return;
            }

            float availableHeight = m_Popup.UsesFullLayoutHeight ? layoutHeight : middleHeight;
            m_Popup.ApplyResponsiveLayout(layoutWidth, availableHeight);
        }
    }
}

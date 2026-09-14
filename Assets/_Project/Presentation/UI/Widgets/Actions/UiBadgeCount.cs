using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>Reusable number-or-AD badge view shared by action and reward controls.</summary>
    [DisallowMultipleComponent]
    public sealed class UiBadgeCount : MonoBehaviour
    {
        [SerializeField] private Image m_Background;
        [SerializeField] private TMP_Text m_Label;

        private void Awake()
        {
            if (m_Background == null) m_Background = GetComponent<Image>();
            if (m_Label == null) m_Label = GetComponentInChildren<TMP_Text>(true);
        }

        public void SetCount(int count)
        {
            SetVisible(true);
            if (m_Label != null) m_Label.text = Mathf.Max(0, count).ToString();
        }

        public void SetAd()
        {
            SetVisible(true);
            if (m_Label != null) m_Label.text = "AD";
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}

using TMPro;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Data-facing view of a labelled score card and optional crown slot.</summary>
    [DisallowMultipleComponent]
    public sealed class UiValueCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Label;
        [SerializeField] private RollingNumber m_Value;
        [SerializeField] private RectTransform m_Adornment;

        public RollingNumber Value => m_Value;
        public RectTransform Adornment => m_Adornment;

        public void SetValue(int value, bool animate)
        {
            m_Value?.SetValue(value, animate);
        }
    }
}

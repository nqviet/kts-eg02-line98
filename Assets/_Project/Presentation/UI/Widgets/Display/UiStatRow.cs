using TMPro;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Reusable label/value row used by popup statistics.</summary>
    [DisallowMultipleComponent]
    public sealed class UiStatRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Label;
        [SerializeField] private TMP_Text m_Value;

        public void Set(string label, string value)
        {
            if (m_Label != null) m_Label.text = label;
            if (m_Value != null) m_Value.text = value;
        }
    }
}

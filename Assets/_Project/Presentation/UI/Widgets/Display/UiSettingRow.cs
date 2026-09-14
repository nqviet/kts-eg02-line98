using TMPro;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Reusable labelled toggle row used by popup settings.</summary>
    [DisallowMultipleComponent]
    public sealed class UiSettingRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Label;
        [SerializeField] private UnityEngine.UI.Toggle m_Toggle;

        public void Set(string label, bool value)
        {
            if (m_Label != null) m_Label.text = label;
            if (m_Toggle != null) m_Toggle.SetIsOnWithoutNotify(value);
        }
    }
}

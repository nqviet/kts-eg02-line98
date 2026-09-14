using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>Prefab-owned visual frame for a popup modal; lifecycle remains in UiPopupBase.</summary>
    [DisallowMultipleComponent]
    public sealed class UiModalPanel : MonoBehaviour
    {
        [SerializeField] private Image m_Frame;

        public Image Frame => m_Frame;

        private void Awake()
        {
            if (m_Frame == null) m_Frame = GetComponent<Image>();
        }
    }
}

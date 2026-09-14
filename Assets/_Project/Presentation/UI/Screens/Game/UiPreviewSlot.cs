using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>Single visual slot in a preview tray.</summary>
    [DisallowMultipleComponent]
    public sealed class UiPreviewSlot : MonoBehaviour
    {
        [SerializeField] private Image m_Image;
        [SerializeField] private CanvasGroup m_CanvasGroup;

        private void Awake()
        {
            if (m_Image == null) m_Image = GetComponent<Image>();
            if (m_CanvasGroup == null) m_CanvasGroup = GetComponent<CanvasGroup>();
        }

        public void SetSprite(Sprite sprite, float alpha)
        {
            if (m_Image == null)
            {
                return;
            }

            m_Image.sprite = sprite;
            m_Image.enabled = sprite != null;
            m_Image.preserveAspect = true;
            if (m_CanvasGroup != null) m_CanvasGroup.alpha = alpha;
        }
    }
}

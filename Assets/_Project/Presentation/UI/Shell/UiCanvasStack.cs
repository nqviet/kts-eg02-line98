using UnityEngine;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Owns the three UI canvas layers and the shared modal scrim.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiCanvasStack : MonoBehaviour
    {
        [SerializeField] private Canvas m_StaticCanvas;
        [SerializeField] private Canvas m_DynamicCanvas;
        [SerializeField] private Canvas m_PopupCanvas;
        [SerializeField] private UnityEngine.UI.Image m_ScrimImage;
        [SerializeField] private CanvasGroup m_ScrimGroup;

        private TweenRunner m_TweenRunner;

        public Canvas StaticCanvas => m_StaticCanvas;
        public Canvas DynamicCanvas => m_DynamicCanvas;
        public Canvas PopupCanvas => m_PopupCanvas;
        public CanvasGroup ScrimGroup => m_ScrimGroup;

        public void Configure(
            Canvas staticCanvas,
            Canvas dynamicCanvas,
            Canvas popupCanvas,
            UnityEngine.UI.Image scrimImage,
            CanvasGroup scrimGroup)
        {
            m_StaticCanvas = staticCanvas;
            m_DynamicCanvas = dynamicCanvas;
            m_PopupCanvas = popupCanvas;
            m_ScrimImage = scrimImage;
            m_ScrimGroup = scrimGroup;
            EnsureSortingOrders();
        }

        public void Initialize(TweenRunner tweenRunner)
        {
            m_TweenRunner = tweenRunner;
            EnsureSortingOrders();

            if (m_ScrimGroup != null)
            {
                m_ScrimGroup.alpha = 0f;
                m_ScrimGroup.blocksRaycasts = false;
                m_ScrimGroup.interactable = false;
            }
        }

        public void EnsureSortingOrders()
        {
            if (m_StaticCanvas != null) m_StaticCanvas.sortingOrder = 0;
            if (m_DynamicCanvas != null) m_DynamicCanvas.sortingOrder = 10;
            if (m_PopupCanvas != null) m_PopupCanvas.sortingOrder = 20;
        }

        public void SetModalVisible(bool visible, bool dimScrim = true)
        {
            if (m_ScrimGroup == null)
            {
                return;
            }

            if (m_ScrimImage != null && visible)
            {
                m_ScrimImage.color = dimScrim
                    ? new Color(0.039f, 0.078f, 0.157f, 0.61f)
                    : new Color(0.96f, 0.97f, 0.98f, 0.75f);
            }

            m_ScrimGroup.blocksRaycasts = visible;
            m_ScrimGroup.interactable = visible;

            if (m_TweenRunner != null)
            {
                m_TweenRunner.CancelByOwner(m_ScrimGroup);
                Tween tween = new Tween
                {
                    From = m_ScrimGroup.alpha,
                    To = visible ? 1f : 0f,
                    Duration = 0.20f,
                    Ease = Easing.OutCubic,
                    Owner = m_ScrimGroup,
                    OnUpdate = value => m_ScrimGroup.alpha = value
                };
                m_TweenRunner.Play(in tween);
            }
            else
            {
                m_ScrimGroup.alpha = visible ? 1f : 0f;
            }
        }
    }
}

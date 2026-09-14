using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Screen-level bridge that keeps the established HudPresenter API prefab-safe.</summary>
    [DisallowMultipleComponent]
    public sealed class UiHudScreen : UiScreenBase
    {
        [SerializeField] private HudPresenter m_Presenter;

        public HudPresenter Presenter => m_Presenter;

        private void Awake()
        {
            if (m_Presenter == null) m_Presenter = GetComponent<HudPresenter>();
        }

        public override void Initialize(UiServices services)
        {
            if (m_Presenter != null && services != null)
            {
                m_Presenter.Initialize(services.Session, services.Router, services.TweenRunner, services.Theme);
            }
        }
    }
}

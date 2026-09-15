using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Scene-owned main menu screen root. Navigation is composed from child UiNavigationButton widgets.
    /// Presenter binds persistent stats (Best Score, Daily Streak) to UI cards.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuScreen : UiScreenBase
    {
        [SerializeField] private MainMenuPresenter m_Presenter;

        public override string ScreenId => string.IsNullOrWhiteSpace(base.ScreenId) ? "MainMenu" : base.ScreenId;
        public MainMenuPresenter Presenter => m_Presenter;

        public void Configure(MainMenuPresenter presenter)
        {
            m_Presenter = presenter;
        }

        private void Awake()
        {
            if (m_Presenter == null)
            {
                m_Presenter = GetComponentInChildren<MainMenuPresenter>(true);
            }
        }
    }
}

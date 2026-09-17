using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Line98.Presentation
{
    /// <summary>
    /// Binds semantic navigation buttons to scene destinations and shell-level popup intents.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiSceneNavigator : MonoBehaviour
    {
        [SerializeField] private string m_MainMenuScene = "MainMenu";
        [SerializeField] private string m_GameScene = "Game";
        [SerializeField] private UiNavigationButton[] m_NavigationButtons = System.Array.Empty<UiNavigationButton>();

        private readonly List<UiNavigationButton> m_BoundButtons = new List<UiNavigationButton>();
        private UiShell m_Shell;
        private bool m_IsLoading;

        public bool IsLoading => m_IsLoading;

        private void Awake()
        {
            m_Shell = GetComponentInParent<UiShell>();
            if (m_Shell == null)
            {
                m_Shell = FindAnyObjectByType<UiShell>();
            }
            if (m_NavigationButtons == null || m_NavigationButtons.Length == 0)
            {
                m_NavigationButtons = GetComponentsInChildren<UiNavigationButton>(true);
            }
        }

        private void OnEnable()
        {
            if (m_Shell != null)
            {
                m_Shell.OnDailyPlayRequested -= HandleDailyPlayRequested;
                m_Shell.OnDailyPlayRequested += HandleDailyPlayRequested;
            }
            for (int i = 0; i < m_NavigationButtons.Length; i++)
            {
                UiNavigationButton button = m_NavigationButtons[i];
                if (button == null || m_BoundButtons.Contains(button))
                {
                    continue;
                }

                button.OnNavigate += HandleNavigate;
                m_BoundButtons.Add(button);
            }
        }

        private void OnDisable()
        {
            if (m_Shell != null) m_Shell.OnDailyPlayRequested -= HandleDailyPlayRequested;
            for (int i = 0; i < m_BoundButtons.Count; i++)
            {
                if (m_BoundButtons[i] != null)
                {
                    m_BoundButtons[i].OnNavigate -= HandleNavigate;
                }
            }

            m_BoundButtons.Clear();
        }

        public enum GameModeRequest
        {
            None,
            Classic,
            Daily,
            Zen
        }

        public static GameModeRequest PendingModeRequest { get; set; } = GameModeRequest.None;

        public void Navigate(UiNavigationButton.UiDestination destination)
        {
            if (destination == UiNavigationButton.UiDestination.Settings)
            {
                m_Shell?.OpenSettings();
                return;
            }

            if (destination == UiNavigationButton.UiDestination.Statistics)
            {
                m_Shell?.OpenStatistics();
                return;
            }

            if (destination == UiNavigationButton.UiDestination.Daily)
            {
                m_Shell?.OpenDailyChallenge();
                return;
            }
            else if (destination == UiNavigationButton.UiDestination.Zen)
            {
                PendingModeRequest = GameModeRequest.Zen;
                destination = UiNavigationButton.UiDestination.Game;
            }
            else if (destination == UiNavigationButton.UiDestination.Game)
            {
                PendingModeRequest = GameModeRequest.Classic;
            }

            string sceneName = destination == UiNavigationButton.UiDestination.Menu
                ? m_MainMenuScene
                : destination == UiNavigationButton.UiDestination.Game ? m_GameScene : null;
            if (string.IsNullOrWhiteSpace(sceneName) || m_IsLoading || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                return;
            }

            m_IsLoading = true;
            AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (load == null)
            {
                m_IsLoading = false;
                return;
            }

            load.completed += _ => m_IsLoading = false;
        }

        private void HandleNavigate(UiNavigationButton.UiDestination destination)
        {
            Navigate(destination);
        }

        private void HandleDailyPlayRequested()
        {
            PendingModeRequest = GameModeRequest.Daily;
            Navigate(UiNavigationButton.UiDestination.Game);
            PendingModeRequest = GameModeRequest.Daily;
        }
    }
}

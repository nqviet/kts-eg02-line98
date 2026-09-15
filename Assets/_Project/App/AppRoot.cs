using System;
using Line98.Core;
using Line98.Gameplay;
using Line98.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Line98.App
{
    /// <summary>
    /// Master application composition root.
    /// Manages service registry, session lifecycle, single Update loop dispatch,
    /// and pause/quit autosave triggers per Architecture §2.
    /// </summary>
    public sealed class AppRoot : MonoBehaviour
    {
        private static AppRoot s_Instance;
        public static AppRoot Instance => s_Instance;

        [Header("Configuration Assets")]
        [SerializeField] private Line98.Data.ScoreTableSO m_ScoreTable;
        [SerializeField] private Line98.Data.SpawnColorPolicySO m_SpawnColorPolicy;
        [SerializeField] private Line98.Data.GameConfigSO m_GameConfig;
        [SerializeField] private Line98.Data.ThemeCatalogSO m_ThemeCatalog;

        private ServiceRegistry m_Registry;
        private ConfigService m_ConfigService;
        private GameSession m_Session;
        private GameManager m_GameManager;
        private SaveService m_SaveService;
        private StatisticsService m_StatsService;
        private AchievementService m_AchievementService;
        private IAnalyticsService m_AnalyticsService;
        private IAdService m_AdService;
        private IIapService m_IapService;
        private ICosmeticService m_CosmeticService;
        private CosmeticThemeSelector m_ThemeSelector;
        private Presentation.PresentationRoot m_BoundPresentationRoot;
        private Presentation.UiShell m_BoundShell;
        private SettingsPresenter m_SettingsPresenter;
        private bool m_IsThemeChangeBound;
        private bool m_IsLoadingMainMenuFromBoot;

        public ConfigService Config => m_ConfigService;
        public GameSession Session => m_Session;
        public Presentation.IThemeSelector ThemeSelector => m_ThemeSelector;
        public Presentation.PresentationRoot PresentationRoot => m_BoundPresentationRoot;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            InitializeServices();
            InitializeGameplay();

#if DEBUG || UNITY_EDITOR
            var debugHud = gameObject.GetComponent<DebugHud>();
            if (debugHud == null)
            {
                debugHud = gameObject.AddComponent<DebugHud>();
            }
            debugHud.Initialize(m_Session);
#endif
        }

        private void Start()
        {
            m_GameManager.StartClassicGame();

            LoadMainMenuFromBoot(SceneManager.GetActiveScene());
        }

        private void InitializeServices()
        {
            m_Registry = ServiceRegistry.Instance;
            m_Registry.Clear();

#if UNITY_EDITOR
            if (m_ScoreTable == null)
            {
                m_ScoreTable = UnityEditor.AssetDatabase.LoadAssetAtPath<Line98.Data.ScoreTableSO>("Assets/_Project/Content/Definitions/ScoreTable_Default.asset");
            }
            if (m_SpawnColorPolicy == null)
            {
                m_SpawnColorPolicy = UnityEditor.AssetDatabase.LoadAssetAtPath<Line98.Data.SpawnColorPolicySO>("Assets/_Project/Content/Definitions/SpawnColorPolicy_Default.asset");
            }
            if (m_GameConfig == null)
            {
                m_GameConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<Line98.Data.GameConfigSO>("Assets/_Project/Content/Definitions/GameConfig_Default.asset");
            }
            if (m_ThemeCatalog == null)
            {
                m_ThemeCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Line98.Data.ThemeCatalogSO>("Assets/_Project/Content/Definitions/ThemeCatalog_Default.asset");
            }
#endif

            m_ConfigService = new ConfigService(m_ScoreTable, m_SpawnColorPolicy, m_GameConfig);
            m_SaveService = new SaveService(new FileSaveBackend());
            m_StatsService = new StatisticsService();
            m_AchievementService = new AchievementService();
            m_AnalyticsService = new DebugAnalyticsService();
            m_AdService = new NoOpAdService();
            m_IapService = new EditorStubIapService();
            m_CosmeticService = new CosmeticService(m_ThemeCatalog, new FileSaveBackend());
            m_ThemeSelector = new CosmeticThemeSelector(m_CosmeticService);

            m_Registry.Register<ConfigService>(m_ConfigService);
            m_Registry.Register<SaveService>(m_SaveService);
            m_Registry.Register<StatisticsService>(m_StatsService);
            m_Registry.Register<AchievementService>(m_AchievementService);
            m_Registry.Register<IAnalyticsService>(m_AnalyticsService);
            m_Registry.Register<IAdService>(m_AdService);
            m_Registry.Register<IIapService>(m_IapService);
            m_Registry.Register<ICosmeticService>(m_CosmeticService);
            m_Registry.Register<IThemeProvider>(m_CosmeticService);
            m_Registry.Register<Presentation.IThemeSelector>(m_ThemeSelector);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            BindPresentationRoot();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (m_CosmeticService != null && m_IsThemeChangeBound)
            {
                m_CosmeticService.OnThemeChanged -= HandleThemeChanged;
            }

            m_IsThemeChangeBound = false;
            m_SettingsPresenter?.Dispose();
            m_SettingsPresenter = null;
            m_BoundPresentationRoot = null;
            m_BoundShell = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Boot")
            {
                LoadMainMenuFromBoot(scene);
                return;
            }

            m_IsLoadingMainMenuFromBoot = false;
            BindPresentationRoot();
        }

        private void LoadMainMenuFromBoot(Scene scene)
        {
            if (scene.name != "Boot" || m_IsLoadingMainMenuFromBoot || !Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                return;
            }

            m_IsLoadingMainMenuFromBoot = true;
            SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
        }

        private void BindPresentationRoot()
        {
            BindThemeChanges();

            var presRoot = FindAnyObjectByType<Presentation.PresentationRoot>();
            if (presRoot != m_BoundPresentationRoot)
            {
                m_BoundPresentationRoot = presRoot;
                if (m_BoundPresentationRoot != null)
                {
                    m_BoundPresentationRoot.ThemeSelector = m_ThemeSelector;

                    if (m_CosmeticService?.ActiveTheme != null)
                    {
                        m_BoundPresentationRoot.ApplyTheme(m_CosmeticService.ActiveTheme);
                    }
                }
            }

            BindShell();
        }

        private void BindThemeChanges()
        {
            if (m_CosmeticService == null || m_IsThemeChangeBound)
            {
                return;
            }

            m_CosmeticService.OnThemeChanged += HandleThemeChanged;
            m_IsThemeChangeBound = true;
        }

        private void BindShell()
        {
            var shell = FindAnyObjectByType<Presentation.UiShell>();
            if (shell == m_BoundShell)
            {
                return;
            }

            m_BoundShell = shell;
            if (m_BoundShell == null)
            {
                return;
            }

            m_BoundShell.SetThemeSelector(m_ThemeSelector);
            m_BoundShell.SetThemeCatalog(m_ThemeCatalog);
            if (m_CosmeticService?.ActiveTheme != null)
            {
                m_BoundShell.ApplyTheme(m_CosmeticService.ActiveTheme.UiTheme, m_CosmeticService.ActiveTheme.ThemeId);
            }
        }

        private void HandleThemeChanged(ThemeChange change)
        {
            if (m_BoundPresentationRoot != null && m_CosmeticService?.ActiveTheme != null)
            {
                m_BoundPresentationRoot.ApplyTheme(m_CosmeticService.ActiveTheme);
            }
            else if (m_BoundShell != null && m_CosmeticService?.ActiveTheme != null)
            {
                m_BoundShell.ApplyTheme(m_CosmeticService.ActiveTheme.UiTheme, m_CosmeticService.ActiveTheme.ThemeId);
            }
        }

        private void InitializeGameplay()
        {
            m_Session = new GameSession();
            m_GameManager = new GameManager(m_Session, m_ConfigService);
            m_Registry.Register<GameSession>(m_Session);
            m_Registry.Register<GameManager>(m_GameManager);

            m_Session.OnMoveCommitted += OnMoveCommitted;
            m_Session.OnGameOver += OnGameOver;

            BindPresentationRoot();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Tick(dt);
        }

        public void Tick(float dt)
        {
            TryBindSettingsPresenter();
        }

        private void TryBindSettingsPresenter()
        {
            if (m_BoundShell == null || m_BoundShell.SettingsPopup == null || m_BoundPresentationRoot == null || !m_BoundPresentationRoot.IsInitialized)
            {
                return;
            }

            if (m_SettingsPresenter != null &&
                m_SettingsPresenter.View == m_BoundShell.SettingsPopup &&
                m_SettingsPresenter.AudioService == m_BoundPresentationRoot.AudioService &&
                m_SettingsPresenter.IapService == m_IapService)
            {
                return;
            }

            m_SettingsPresenter?.Dispose();
            m_SettingsPresenter = new SettingsPresenter(
                m_BoundShell.SettingsPopup,
                m_BoundPresentationRoot.AudioService,
                m_IapService,
                m_BoundShell);
        }

        private void OnMoveCommitted(Core.MoveResult result)
        {
            m_StatsService?.RecordMove();
            if (result.Outcome == Core.MoveOutcome.Cleared)
            {
                m_StatsService?.RecordLineClear(result.Cleared.LongestRun, result.Cleared.RunCount, result.ScoreDelta);
                m_AchievementService?.CheckLine(result.Cleared.LongestRun);
                m_AchievementService?.CheckScore(m_Session.Score);
            }

            Autosave();
        }

        private void OnGameOver(SessionSummary summary)
        {
            m_StatsService?.RecordGameOver(summary.FinalScore);
            Autosave();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Autosave();
            }
        }

        private void OnApplicationQuit()
        {
            Autosave();
        }

        private void Autosave()
        {
            if (m_SaveService == null || m_Session == null) return;

            SaveData data = new SaveData
            {
                BoardCells = m_Session.Board.ExportCells(),
                PreviewColors = Array.ConvertAll(m_Session.PreviewQueue.ToArray(), c => (int)c),
                S0 = m_Session.Rng.S0,
                S1 = m_Session.Rng.S1,
                S2 = m_Session.Rng.S2,
                S3 = m_Session.Rng.S3,
                Score = m_Session.Score,
                MoveCount = m_Session.MoveCount,
                LinesCleared = m_Session.LinesCleared,
                FreeUndos = m_Session.FreeUndosRemaining,
                BestScore = m_StatsService.Stats.BestScore,
                TotalGamesPlayed = m_StatsService.Stats.GamesPlayed,
                LongestLine = m_StatsService.Stats.LongestLine
            };

            m_SaveService.SaveGame(data);
        }
    }
}

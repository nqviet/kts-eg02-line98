using System;
using Line98.Core;
using Line98.Data;
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
        private static Func<ISaveBackend> s_SaveBackendFactory;
        public static AppRoot Instance => s_Instance;
        public static Func<ISaveBackend> SaveBackendFactory
        {
            get => s_SaveBackendFactory;
            set => s_SaveBackendFactory = value;
        }

        [Header("Configuration Assets")]
        [SerializeField] private Line98.Data.ScoreTableSO m_ScoreTable;
        [SerializeField] private Line98.Data.SpawnColorPolicySO m_SpawnColorPolicy;
        [SerializeField] private Line98.Data.GameConfigSO m_GameConfig;
        [SerializeField] private Line98.Data.ThemeCatalogSO m_ThemeCatalog;
        [SerializeField] private Line98.Data.AudioCatalogSO m_AudioCatalog;
        [SerializeField] private UnityEngine.Audio.AudioMixer m_MainMixer;

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
        private Presentation.Audio.AudioService m_AudioService;
        private Presentation.PresentationRoot m_BoundPresentationRoot;
        private Presentation.MenuShowcaseRig m_BoundShowcaseRig;
        private Presentation.MainMenuPresenter m_BoundMenuPresenter;
        private Presentation.MainMenuEntryMotion m_BoundEntryMotion;
        private Presentation.UiShell m_BoundShell;
        private SettingsPresenter m_SettingsPresenter;
        private bool m_IsThemeChangeBound;
        private bool m_IsLoadingMainMenuFromBoot;

        public ConfigService Config => m_ConfigService;
        public GameSession Session => m_Session;
        public Presentation.Audio.AudioService AudioService => m_AudioService;
        public Presentation.IThemeSelector ThemeSelector => m_ThemeSelector;
        public Presentation.PresentationRoot PresentationRoot => m_BoundPresentationRoot;
        public Presentation.MenuShowcaseRig ShowcaseRig => m_BoundShowcaseRig;

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

            StartCoroutine(PlayBootMusic());

            LoadMainMenuFromBoot(SceneManager.GetActiveScene());
        }

        private System.Collections.IEnumerator PlayBootMusic()
        {
            yield return null;
            if (m_AudioService != null && !m_AudioService.IsMusicPlaying && !m_AudioService.IsAmbiencePlaying)
            {
                m_AudioService.PlayMusic("bgm_classic_main");
            }
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
            if (m_AudioCatalog == null)
            {
                m_AudioCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Line98.Data.AudioCatalogSO>("Assets/_Project/Content/Definitions/AudioCatalog_Default.asset");
            }
            if (m_MainMixer == null)
            {
                m_MainMixer = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Audio.AudioMixer>("Assets/_Project/Content/Audio/MainMixer.mixer");
            }
#endif

            m_ConfigService = new ConfigService(m_ScoreTable, m_SpawnColorPolicy, m_GameConfig);
            m_SaveService = new SaveService(CreateSaveBackend());
            SaveData loadedSave = m_SaveService.LoadGame();
            PlayerStats initialStats = new PlayerStats
            {
                BestScore = loadedSave != null ? loadedSave.BestScore : 0,
                CurrentDailyStreak = loadedSave != null ? loadedSave.DailyStreak : 0,
                TotalLinesCleared = loadedSave != null ? loadedSave.LinesCleared : 0,
                TotalMoves = loadedSave != null ? loadedSave.MoveCount : 0,
                GamesPlayed = loadedSave != null ? loadedSave.TotalGamesPlayed : 0,
                LongestLine = loadedSave != null ? loadedSave.LongestLine : 0
            };
            m_StatsService = new StatisticsService(initialStats);
            m_AchievementService = new AchievementService();
            m_AnalyticsService = new DebugAnalyticsService();
            m_AdService = new NoOpAdService();
            m_IapService = new EditorStubIapService();
            m_CosmeticService = new CosmeticService(m_ThemeCatalog, CreateSaveBackend());
            m_ThemeSelector = new CosmeticThemeSelector(m_CosmeticService);

            if (m_AudioCatalog != null && m_MainMixer != null)
            {
                m_AudioService = new Presentation.Audio.AudioService(m_AudioCatalog, m_MainMixer, transform);
                m_Registry.Register<Presentation.Audio.AudioService>(m_AudioService);
            }

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
            m_BoundShowcaseRig = null;
            m_BoundMenuPresenter = null;
            m_BoundEntryMotion = null;
            m_BoundShell = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "Boot")
            {
                LoadMainMenuFromBoot(scene);
                return;
            }

            if (scene.name == "Game")
            {
                switch (Presentation.UiSceneNavigator.PendingModeRequest)
                {
                    case Presentation.UiSceneNavigator.GameModeRequest.Daily:
                        m_GameManager?.StartDailyChallenge();
                        m_AudioService?.StopAmbience();
                        if (m_AudioService != null && !m_AudioService.IsMusicPlaying)
                        {
                            m_AudioService.PlayMusic("bgm_classic_main");
                        }
                        break;
                    case Presentation.UiSceneNavigator.GameModeRequest.Zen:
                        m_GameManager?.StartZenMode();
                        m_AudioService?.StopMusic(1.0f);
                        m_AudioService?.PlayAmbience("bgm_zen_ambience");
                        break;
                    case Presentation.UiSceneNavigator.GameModeRequest.Classic:
                    default:
                        m_GameManager?.StartClassicGame();
                        m_AudioService?.StopAmbience();
                        if (m_AudioService != null && !m_AudioService.IsMusicPlaying)
                        {
                            m_AudioService.PlayMusic("bgm_classic_main");
                        }
                        break;
                }
                Presentation.UiSceneNavigator.PendingModeRequest = Presentation.UiSceneNavigator.GameModeRequest.None;
            }
            else if (scene.name == "MainMenu")
            {
                m_AudioService?.StopAmbience();
                if (m_AudioService != null && !m_AudioService.IsMusicPlaying)
                {
                    m_AudioService.PlayMusic("bgm_classic_main");
                }
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

                    if (!m_BoundPresentationRoot.IsInitialized && m_Session != null)
                    {
                        m_BoundPresentationRoot.Initialize(
                            m_Session,
                            themeSelector: m_ThemeSelector,
                            ballTheme: m_CosmeticService?.ActiveBallTheme,
                            boardTheme: m_CosmeticService?.ActiveBoardTheme,
                            uiTheme: m_CosmeticService?.ActiveUiTheme,
                            audioService: m_AudioService);
                    }

                    if (m_CosmeticService != null)
                    {
                        m_BoundPresentationRoot.ApplyThemeSelection(CurrentThemeSelection());
                    }
                }
            }

            var showcaseRig = FindAnyObjectByType<Presentation.MenuShowcaseRig>();
            if (showcaseRig != m_BoundShowcaseRig)
            {
                m_BoundShowcaseRig = showcaseRig;
                if (m_BoundShowcaseRig != null)
                {
                    m_BoundShowcaseRig.SetReducedMotion(SettingsPresenter.IsReduceEffectsEnabled);
                    if (m_CosmeticService != null)
                    {
                        m_BoundShowcaseRig.ApplyTheme(m_CosmeticService.ActiveBoardTheme, m_CosmeticService.ActiveBallTheme);
                    }
                }
            }

            var entryMotion = FindAnyObjectByType<Presentation.MainMenuEntryMotion>();
            if (entryMotion != m_BoundEntryMotion)
            {
                m_BoundEntryMotion = entryMotion;
                if (m_BoundEntryMotion != null)
                {
                    m_BoundEntryMotion.Play(SettingsPresenter.IsReduceEffectsEnabled);
                }
            }

            var menuPresenter = FindAnyObjectByType<Presentation.MainMenuPresenter>();
            if (menuPresenter != null)
            {
                m_BoundMenuPresenter = menuPresenter;
                int best = m_StatsService?.Stats?.BestScore ?? 0;
                int streak = m_StatsService?.Stats?.CurrentDailyStreak ?? 0;
                m_BoundMenuPresenter.SetStats(best, streak, m_CosmeticService?.ActiveUiTheme);
            }

            BindShell();

            if (m_CosmeticService?.ActiveBallTheme != null)
            {
                ApplyBallThemeEverywhere(m_CosmeticService.ActiveBallTheme);
            }
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
            if (m_CosmeticService?.ActiveUiTheme != null)
            {
                m_BoundShell.ApplyTheme(m_CosmeticService.ActiveUiTheme);
            }

            if (m_CosmeticService?.ActiveBallTheme != null)
            {
                m_BoundShell.ApplyBallTheme(m_CosmeticService.ActiveBallTheme);
            }
        }

        private Presentation.ThemeSelection CurrentThemeSelection()
        {
            return new Presentation.ThemeSelection(
                m_CosmeticService.ActiveBallTheme,
                m_CosmeticService.ActiveBoardTheme,
                m_CosmeticService.ActiveUiTheme,
                m_CosmeticService.ActiveClearEffect);
        }

        /// <summary>
        /// Any change re-reads the service's full selection and pushes it down. The service is the
        /// single source of truth, so a Board change and its paired Ui change are both safe to re-apply.
        /// </summary>
        private void HandleThemeChanged(ThemeChange change)
        {
            if (m_CosmeticService == null) return;

            Presentation.ThemeSelection selection = CurrentThemeSelection();

            // Unity objects can retain a managed wrapper after their scene is unloaded.
            // Use Unity's overloaded null check so stale scene bindings are not invoked.
            if (m_BoundPresentationRoot != null)
            {
                m_BoundPresentationRoot.ApplyThemeSelection(selection);
            }
            if (m_BoundShowcaseRig != null)
            {
                m_BoundShowcaseRig.ApplyTheme(selection.Board, selection.Ball);
            }
            if (m_BoundMenuPresenter != null)
            {
                if (selection.Ui != null) m_BoundMenuPresenter.ApplyTheme(selection.Ui);
                if (selection.Ball != null) m_BoundMenuPresenter.ApplyBallTheme(selection.Ball);
            }
            if (m_BoundShell != null)
            {
                if (selection.Ui != null) m_BoundShell.ApplyTheme(selection.Ui);
                if (selection.Ball != null) m_BoundShell.ApplyBallTheme(selection.Ball);
                m_BoundShell.RefreshCosmeticsSelection();
            }
        }

        private void ApplyBallThemeEverywhere(BallThemeSO ball)
        {
            if (ball == null) return;

            // Unity objects can retain a managed wrapper after their scene is unloaded.
            // Use Unity's overloaded null check so stale scene bindings are not invoked.
            if (m_BoundPresentationRoot != null)
            {
                m_BoundPresentationRoot.ApplyBallTheme(ball);
            }
            if (m_BoundShowcaseRig != null)
            {
                m_BoundShowcaseRig.ApplyBallTheme(ball);
            }
            if (m_BoundMenuPresenter != null)
            {
                m_BoundMenuPresenter.ApplyBallTheme(ball);
            }
            if (m_BoundShell != null)
            {
                m_BoundShell.ApplyBallTheme(ball);
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
            m_AudioService?.Tick(dt);
            if (m_BoundShowcaseRig != null)
            {
                m_BoundShowcaseRig.Tick(dt);
            }
            if (m_BoundEntryMotion != null)
            {
                m_BoundEntryMotion.Tick(dt);
            }
        }

        private void TryBindSettingsPresenter()
        {
            if (m_BoundShell == null || m_BoundShell.SettingsPopup == null)
            {
                return;
            }

            var audioService = m_AudioService
                ?? (m_BoundPresentationRoot != null && m_BoundPresentationRoot.IsInitialized ? m_BoundPresentationRoot.AudioService : null);

            if (m_SettingsPresenter != null &&
                m_SettingsPresenter.View == m_BoundShell.SettingsPopup &&
                m_SettingsPresenter.AudioService == audioService &&
                m_SettingsPresenter.IapService == m_IapService)
            {
                return;
            }

            m_SettingsPresenter?.Dispose();
            m_SettingsPresenter = new SettingsPresenter(
                m_BoundShell.SettingsPopup,
                audioService,
                m_IapService,
                m_BoundShell);
            m_SettingsPresenter.OnReduceEffectsChanged += HandleReduceEffectsChanged;
        }

        private void HandleReduceEffectsChanged(bool enabled)
        {
            if (m_BoundShowcaseRig != null)
            {
                m_BoundShowcaseRig.SetReducedMotion(enabled);
            }
            if (enabled && m_BoundEntryMotion != null)
            {
                m_BoundEntryMotion.Snap();
            }
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
            if (m_Session?.Mode is DailyChallengeMode)
            {
                m_StatsService?.RecordDailyCompletion();
            }
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

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }

            m_AudioService?.Dispose();
            m_AudioService = null;
        }

        private static ISaveBackend CreateSaveBackend()
        {
            return s_SaveBackendFactory?.Invoke() ?? new FileSaveBackend();
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
                LongestLine = m_StatsService.Stats.LongestLine,
                DailyStreak = m_StatsService.Stats.CurrentDailyStreak
            };

            m_SaveService.SaveGame(data);
        }
    }
}

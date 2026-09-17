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
        [SerializeField] private Line98.Data.BrandConfigSO m_BrandConfig;
        [SerializeField] private Line98.Data.DailyChallengeConfigSO m_DailyConfig;
        [SerializeField] private Line98.Data.AchievementCatalogSO m_AchievementCatalog;

        private ServiceRegistry m_Registry;
        private ConfigService m_ConfigService;
        private GameSession m_Session;
        private GameManager m_GameManager;
        private SaveService m_SaveService;
        private StatisticsService m_StatsService;
        private AchievementService m_AchievementService;
        private SettingsService m_SettingsService;
        private DailyChallengeService m_DailyChallengeService;
        private MenuService m_MenuService;
        private SessionEventRouter m_SessionEventRouter;
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
        private SessionSave m_PendingResume;

        public ConfigService Config => m_ConfigService;
        public GameSession Session => m_Session;
        public Presentation.Audio.AudioService AudioService => m_AudioService;
        public Presentation.IThemeSelector ThemeSelector => m_ThemeSelector;
        public Presentation.PresentationRoot PresentationRoot => m_BoundPresentationRoot;
        public Presentation.MenuShowcaseRig ShowcaseRig => m_BoundShowcaseRig;
        public Line98.Data.ThemeCatalogSO ThemeCatalog => m_ThemeCatalog;
        public Line98.Data.BrandConfigSO BrandConfig => m_BrandConfig;
        public Line98.Data.DailyChallengeConfigSO DailyConfig => m_DailyConfig;
        public SettingsService SettingsService => m_SettingsService;
        public DailyChallengeService DailyChallengeService => m_DailyChallengeService;
        public MenuService MenuService => m_MenuService;

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
            // No session is started here: the session stays in Boot until a mode is chosen,
            // so autosave cannot overwrite a pending resume with an idle board.
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
            if (m_BrandConfig == null)
            {
                m_BrandConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<Line98.Data.BrandConfigSO>("Assets/_Project/Content/Definitions/BrandConfig_Default.asset");
            }
            if (m_DailyConfig == null)
            {
                m_DailyConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<Line98.Data.DailyChallengeConfigSO>("Assets/_Project/Content/Definitions/DailyChallengeConfig_Default.asset");
            }
            if (m_AchievementCatalog == null)
            {
                m_AchievementCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<Line98.Data.AchievementCatalogSO>("Assets/_Project/Content/Definitions/AchievementCatalog_Default.asset");
            }
#endif

            m_ConfigService = new ConfigService(m_ScoreTable, m_SpawnColorPolicy, m_GameConfig);
            m_SaveService = new SaveService(CreateSaveBackend());
            SaveData loadedSave = m_SaveService.LoadGame();
            m_PendingResume = loadedSave?.Session;

            m_StatsService = new StatisticsService();
            if (loadedSave != null)
            {
                m_StatsService.LoadFrom(loadedSave.Stats);
            }

            m_AchievementService = new AchievementService(m_AchievementCatalog);
            if (loadedSave?.Progress != null)
            {
                m_AchievementService.LoadState(loadedSave.Progress.UnlockedAchievementIds);
            }

            m_SettingsService = new SettingsService();
            if (loadedSave?.Settings != null)
            {
                m_SettingsService.Load(loadedSave.Settings);
            }

            m_DailyChallengeService = new DailyChallengeService(m_DailyConfig, loadedSave?.Daily);

            string todayStr = m_DailyChallengeService.Today;
            ResumeDecision resumeDecision = ResumeEvaluator.Evaluate(loadedSave, todayStr);
            m_MenuService = new MenuService(m_StatsService, m_DailyChallengeService, resumeDecision);

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
            m_Registry.Register<ISaveService>(m_SaveService);
            m_Registry.Register<StatisticsService>(m_StatsService);
            m_Registry.Register<AchievementService>(m_AchievementService);
            m_Registry.Register<IAchievementService>(m_AchievementService);
            m_Registry.Register<SettingsService>(m_SettingsService);
            m_Registry.Register<ISettingsService>(m_SettingsService);
            m_Registry.Register<DailyChallengeService>(m_DailyChallengeService);
            m_Registry.Register<MenuService>(m_MenuService);
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
            m_SessionEventRouter?.Dispose();
            m_SessionEventRouter = null;
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
                        ResumeOrStartSession("daily", () => m_GameManager?.StartDailyChallenge(m_DailyChallengeService?.Today));
                        m_AudioService?.StopAmbience();
                        if (m_AudioService != null && !m_AudioService.IsMusicPlaying)
                        {
                            m_AudioService.PlayMusic("bgm_classic_main");
                        }
                        break;
                    case Presentation.UiSceneNavigator.GameModeRequest.Zen:
                        ResumeOrStartSession("zen", () => m_GameManager?.StartZenMode());
                        m_AudioService?.StopMusic(1.0f);
                        m_AudioService?.PlayAmbience("bgm_zen_ambience");
                        break;
                    case Presentation.UiSceneNavigator.GameModeRequest.Classic:
                    default:
                        ResumeOrStartSession("classic", () => m_GameManager?.StartClassicGame());
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
                RefreshResumeDecision();
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
                int best = m_StatsService?.BestScore ?? 0;
                int streak = m_DailyChallengeService != null ? m_DailyChallengeService.CurrentStreak : (m_StatsService?.CurrentStreak ?? 0);
                m_BoundMenuPresenter.SetStats(best, streak, m_CosmeticService?.ActiveUiTheme);

                MenuSnapshot menu = m_MenuService != null ? m_MenuService.GetSnapshot() : default;
                bool offerClassic = menu.HasResumableGame && menu.ResumeModeId == "classic";
                m_BoundMenuPresenter.SetResumeOffer(offerClassic, menu.ResumeScore, menu.ResumeMoveCount);
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
            m_BoundShell.ConfigureProductLayer(BuildDailyPopupPayload, BuildProgressPopupPayload);
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

        private Presentation.DailyPopupPayload BuildDailyPopupPayload()
        {
            DailyReadModel model = m_DailyChallengeService != null
                ? m_DailyChallengeService.BuildReadModel(m_StatsService?.BestScore ?? 0)
                : default;

            var payload = new Presentation.DailyPopupPayload
            {
                Date = model.Date,
                CurrentStreak = model.CurrentStreak,
                BestScore = model.BestScore,
                PlayedToday = model.PlayedToday,
                CompletedToday = model.CompletedToday,
                TodayScore = model.TodayScore,
                CanPlayToday = model.CanPlayToday,
                Board = new BallColor[BoardModel.CellCount],
                Week = new Presentation.DailyDayVisual[model.Week?.Length ?? 0]
            };

            if (model.Preview.Valid && model.Preview.Board != null)
            {
                for (int i = 0; i < BoardModel.CellCount; i++) payload.Board[i] = model.Preview.Board.ColorAt(i);
            }

            for (int i = 0; i < payload.Week.Length; i++)
            {
                DailyDayCell cell = model.Week[i];
                payload.Week[i] = new Presentation.DailyDayVisual(
                    cell.Date,
                    (Presentation.DailyDayVisualState)(byte)cell.State,
                    cell.IsToday);
            }

            return payload;
        }

        private Presentation.ProgressPopupPayload BuildProgressPopupPayload()
        {
            var sessionContext = m_Session != null
                ? new SessionContext(
                    liveScore: m_Session.Score,
                    liveMoves: m_Session.MoveCount,
                    liveLineLength: m_Session.LongestLine,
                    bestSessionScore: m_Session.Score,
                    bestSessionLineLength: m_Session.LongestLine,
                    bestSessionMoves: m_Session.MoveCount)
                : SessionContext.Empty;
            int streak = m_DailyChallengeService?.CurrentStreak ?? 0;
            ProgressMetrics metrics = m_StatsService?.BuildMetrics(in sessionContext, streak) ?? default;
            StatisticsReadModel stats = m_StatsService?.BuildReadModel(in sessionContext, streak) ?? default;
            AchievementReadModel achievements = m_AchievementService?.BuildReadModel(in metrics, featuredCount: 10) ?? default;
            AchievementProgress[] ordered = achievements.Ordered ?? Array.Empty<AchievementProgress>();
            var rows = new Presentation.AchievementPopupRow[ordered.Length];
            for (int i = 0; i < ordered.Length; i++)
            {
                AchievementProgress item = ordered[i];
                string displayName = item.Definition != null && !string.IsNullOrWhiteSpace(item.Definition.DisplayName)
                    ? item.Definition.DisplayName
                    : HumanizeAchievementId(item.Id);
                rows[i] = new Presentation.AchievementPopupRow(
                    item.Id,
                    displayName,
                    item.Definition != null ? item.Definition.Icon : null,
                    item.Unlocked,
                    item.Current,
                    item.Threshold,
                    item.Normalized);
            }

            return new Presentation.ProgressPopupPayload
            {
                BestScore = stats.BestScore,
                GamesPlayed = stats.GamesPlayed,
                TotalScore = stats.TotalScore,
                TotalLinesCleared = stats.TotalLinesCleared,
                LongestLine = stats.LongestLine,
                HighestCombo = stats.HighestCombo,
                CurrentStreak = stats.CurrentStreak,
                AchievementsUnlocked = achievements.Unlocked,
                AchievementsTotal = achievements.Total,
                AchievementRatio = achievements.Ratio,
                Achievements = rows
            };
        }

        private static string HumanizeAchievementId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "ACHIEVEMENT";
            return id.Replace('_', ' ').ToUpperInvariant();
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

            // The session router is attached when a Game scene starts or resumes a session
            BindPresentationRoot();
        }

        private void AttachSessionRouter()
        {
            m_SessionEventRouter?.Dispose();
            m_SessionEventRouter = new SessionEventRouter(
                m_Session,
                m_StatsService,
                m_AchievementService,
                m_DailyChallengeService,
                m_SaveService,
                m_MenuService,
                m_AnalyticsService,
                m_Session.Mode);
        }

        /// <summary>
        /// Enters the Game scene: continues the live or persisted session when it matches the
        /// requested mode, otherwise starts a fresh one. Only fresh sessions count as a game start.
        /// </summary>
        private void ResumeOrStartSession(string modeId, Action startFresh)
        {
            // Detach first so the outgoing router does not observe the replacement
            m_SessionEventRouter?.Dispose();
            m_SessionEventRouter = null;

            if (TryResumeSession(modeId))
            {
                AttachSessionRouter();
            }
            else
            {
                startFresh?.Invoke();
                AttachSessionRouter();
                m_SessionEventRouter.RecordSessionStart();
                m_SaveService?.TrySaveSession(m_Session.CaptureState());
            }

            m_PendingResume = null;
            m_MenuService?.NotifyChanged();
        }

        private bool TryResumeSession(string modeId)
        {
            if (m_Session == null || m_GameManager == null) return false;

            SessionSave candidate = CurrentSessionSave();
            ResumeDecision decision = EvaluateResume(candidate);
            if (decision.Kind != ResumeDecisionKind.Offer || !string.Equals(decision.ModeId, modeId, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // A live, settled session already holds the authoritative state (including its undo stack)
            if (m_Session.Phase == GamePhase.Playing)
            {
                return true;
            }

            // Cold start or a move interrupted mid-flight: restore through the save mapper,
            // which normalises the phase to Playing and drops the uncommitted move.
            return SessionSaveMapper.ToState(candidate, out SessionState state) && m_GameManager.TryResumeGame(in state);
        }

        /// <summary>The live session once one exists, otherwise the session loaded from disk at boot.</summary>
        private SessionSave CurrentSessionSave()
        {
            if (m_Session != null && m_Session.Phase != GamePhase.Boot && m_Session.Phase != GamePhase.Menu)
            {
                return SessionSaveMapper.ToSave(m_Session.CaptureState(), DateTime.UtcNow.Ticks);
            }

            return m_PendingResume;
        }

        private ResumeDecision EvaluateResume(SessionSave session)
        {
            string today = m_DailyChallengeService != null ? m_DailyChallengeService.Today : DateTime.UtcNow.ToString("yyyy-MM-dd");
            return ResumeEvaluator.Evaluate(new SaveData { Session = session }, today);
        }

        private void RefreshResumeDecision()
        {
            m_MenuService?.SetResumeDecision(EvaluateResume(CurrentSessionSave()));
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
                m_SettingsPresenter.IapService == m_IapService &&
                m_SettingsPresenter.SettingsService == m_SettingsService)
            {
                return;
            }

            m_SettingsPresenter?.Dispose();
            m_SettingsPresenter = new SettingsPresenter(
                m_BoundShell.SettingsPopup,
                audioService,
                m_IapService,
                m_BoundShell,
                m_SettingsService,
                m_BrandConfig);
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

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Autosave();
            }
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

            m_SessionEventRouter?.Dispose();
            m_SessionEventRouter = null;

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
                BestScore = m_StatsService?.BestScore ?? 0,
                TotalGamesPlayed = m_StatsService?.GamesPlayed ?? 0,
                LongestLine = m_StatsService?.LongestLine ?? 0,
                DailyStreak = m_DailyChallengeService != null ? m_DailyChallengeService.CurrentStreak : (m_StatsService?.CurrentStreak ?? 0),
                Stats = m_StatsService?.Snapshot() ?? new StatsSave(),
                Progress = new ProgressSave { UnlockedAchievementIds = m_AchievementService?.SaveState() ?? new System.Collections.Generic.List<string>() },
                Settings = m_SettingsService?.Snapshot() ?? new SettingsSave(),
                Daily = m_DailyChallengeService?.SaveState() ?? new DailySave()
            };

            if (m_Session.Phase == GamePhase.Boot || m_Session.Phase == GamePhase.Menu)
            {
                // Preserve an unconsumed resume offer instead of overwriting it with an idle board
                data.Session = m_SaveService.LoadGame()?.Session;
            }
            else
            {
                data.Session = SessionSaveMapper.ToSave(m_Session.CaptureState(), DateTime.UtcNow.Ticks);
            }
            m_SaveService.SaveGame(data);
        }
    }
}

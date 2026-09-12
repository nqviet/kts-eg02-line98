using System;
using Line98.Gameplay;
using Line98.Services;
using UnityEngine;

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

        private ServiceRegistry m_Registry;
        private GameSession m_Session;
        private GameManager m_GameManager;
        private SaveService m_SaveService;
        private StatisticsService m_StatsService;
        private AchievementService m_AchievementService;
        private IAnalyticsService m_AnalyticsService;
        private IAdService m_AdService;
        private IIapService m_IapService;

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);

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
            // Start default classic game on boot
            m_GameManager.StartClassicGame();
        }

        private void InitializeServices()
        {
            m_Registry = ServiceRegistry.Instance;
            m_Registry.Clear();

            m_SaveService = new SaveService(new FileSaveBackend());
            m_StatsService = new StatisticsService();
            m_AchievementService = new AchievementService();
            m_AnalyticsService = new DebugAnalyticsService();
            m_AdService = new NoOpAdService();
            m_IapService = new EditorStubIapService();

            m_Registry.Register<SaveService>(m_SaveService);
            m_Registry.Register<StatisticsService>(m_StatsService);
            m_Registry.Register<AchievementService>(m_AchievementService);
            m_Registry.Register<IAnalyticsService>(m_AnalyticsService);
            m_Registry.Register<IAdService>(m_AdService);
            m_Registry.Register<IIapService>(m_IapService);
        }

        private void InitializeGameplay()
        {
            m_Session = new GameSession();
            m_GameManager = new GameManager(m_Session);
            m_Registry.Register<GameSession>(m_Session);
            m_Registry.Register<GameManager>(m_GameManager);

            m_Session.OnMoveCommitted += OnMoveCommitted;
            m_Session.OnGameOver += OnGameOver;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            Tick(dt);
        }

        public void Tick(float dt)
        {
            // Single Update dispatch point for active animators and systems
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

        private void OnGameOver()
        {
            m_StatsService?.RecordGameOver(m_Session.Score);
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

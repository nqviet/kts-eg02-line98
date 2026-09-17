using System;
using UnityEngine;
using UnityEngine.Audio;
using Line98.Core;
using Line98.Data;
using Line98.Gameplay;
using Line98.Presentation.Animation;
using Line98.Presentation.Audio;
using Line98.Presentation.Vfx;

namespace Line98.Presentation
{
    /// <summary>
    /// Master visual orchestrator for Line 98.
    /// Manages the visual board, pooled ball instances, procedural tween runner,
    /// 3D orthogonal camera rig, mathematical input router, and move pacing.
    /// Dispatches a single unified Tick(dt) across all presentation sub-systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PresentationRoot : MonoBehaviour, ITickable, ISlowMoController
    {
        [Header("Profiles & Definitions")]
        [SerializeField] private MotionProfileSO m_MotionProfile;
        [SerializeField] private FeedbackProfileSO m_FeedbackProfile;
        [SerializeField] private CameraProfileSO m_CameraProfile;
        [SerializeField] private BallThemeSO m_BallTheme;
        [SerializeField] private BoardThemeSO m_BoardTheme;
        [SerializeField] private VfxCatalogSO m_VfxCatalog;
        [SerializeField] private AudioCatalogSO m_AudioCatalog;
        [SerializeField] private AudioMixer m_MainMixer;

        [Header("Art Assets")]
        [SerializeField] private Renderer m_Backdrop;
        [SerializeField] private Mesh m_CellMesh;
        [SerializeField] private Mesh m_FrameMesh;
        [SerializeField] private Mesh m_BallMesh;
        [SerializeField] private Mesh m_ShadowMesh;
        [SerializeField] private Material m_CellMaterial;
        [SerializeField] private Material m_FrameMaterial;
        [SerializeField] private Material m_ShadowMaterial;
        [SerializeField] private Material m_GlowMaterial;
        [SerializeField] private Material[] m_BallMaterials;

        [Header("Runtime Hierarchies")]
        [SerializeField] private CameraRig m_CameraRig;
        [SerializeField] private BoardView m_BoardView;
        [SerializeField] private InputRouter m_InputRouter;
        [SerializeField] private UiShell m_UIRouter;
        [SerializeField] private HudPresenter m_HudPresenter;
        [SerializeField] private UiThemeSO m_UiTheme;

        private GameSession m_Session;
        private TweenRunner m_TweenRunner;
        private BallViewManager m_BallManager;
        private MoveAnimator m_MoveAnimator;
        private BoardAnimator m_BoardAnimator;
        private MovePacer m_MovePacer;
        private PathPreviewView m_PathPreviewView;
        private VfxService m_VfxService;
        private AudioService m_AudioService;
        private bool m_OwnsAudioService = true;
        private ThemeSwapController m_ThemeSwapController;
        private IThemeSelector m_ThemeSelector;
        private bool m_IsInitialized;
        private float m_ClockScale = 1.0f;
        private float m_SlowMoRemaining;

        public bool IsInitialized => m_IsInitialized;
        public float ClockScale => m_ClockScale;
        public CameraRig CameraRig => m_CameraRig;
        public BoardView BoardView => m_BoardView;
        public BallViewManager BallManager => m_BallManager;
        public TweenRunner TweenRunner => m_TweenRunner;
        public MovePacer MovePacer => m_MovePacer;

        public void RequestSlowMo(float scale, float durationSeconds)
        {
            if (durationSeconds <= 0f) return;
            m_ClockScale = Mathf.Clamp(scale, 0.05f, 1.0f);
            m_SlowMoRemaining = durationSeconds;
        }
        public PathPreviewView PathPreviewView => m_PathPreviewView;
        public VfxService VfxService => m_VfxService;
        public AudioService AudioService => m_AudioService;
        public AudioCatalogSO AudioCatalog => m_AudioCatalog;
        public AudioMixer MainMixer => m_MainMixer;
        public InputRouter InputRouter => m_InputRouter;
        public UiShell UIRouter => m_UIRouter;
        public HudPresenter HudPresenter => m_HudPresenter;
        public GameSession Session => m_Session;
        private ThemeDefinitionSO m_ActiveTheme;
        public ThemeDefinitionSO ActiveTheme => m_ActiveTheme;
        public ThemeSwapController ThemeSwapController => m_ThemeSwapController;
        public IThemeSelector ThemeSelector
        {
            get => m_ThemeSelector;
            set
            {
                m_ThemeSelector = value;
                if (m_UIRouter != null)
                {
                    m_UIRouter.ThemeSelector = value;
                }
            }
        }

        public void Initialize(
            GameSession session,
            MotionProfileSO motionProfile = null,
            FeedbackProfileSO feedbackProfile = null,
            CameraProfileSO cameraProfile = null,
            BallThemeSO ballTheme = null,
            BoardThemeSO boardTheme = null,
            VfxCatalogSO vfxCatalog = null,
            AudioCatalogSO audioCatalog = null,
            AudioMixer mainMixer = null,
            IThemeSelector themeSelector = null,
            UiThemeSO uiTheme = null,
            AudioService audioService = null)
        {
            m_Session = session;
            if (motionProfile != null) m_MotionProfile = motionProfile;
            if (feedbackProfile != null) m_FeedbackProfile = feedbackProfile;
            if (cameraProfile != null) m_CameraProfile = cameraProfile;
            if (ballTheme != null) m_BallTheme = ballTheme;
            if (boardTheme != null) m_BoardTheme = boardTheme;
            if (vfxCatalog != null) m_VfxCatalog = vfxCatalog;
            if (audioCatalog != null) m_AudioCatalog = audioCatalog;
            if (mainMixer != null) m_MainMixer = mainMixer;
            if (uiTheme != null) m_UiTheme = uiTheme;
            if (themeSelector != null)
            {
                ThemeSelector = themeSelector;
            }
            else if (m_ThemeSelector == null && m_UIRouter != null && m_UIRouter.ThemeCatalog != null)
            {
                ThemeSelector = new CatalogThemeSelector(m_UIRouter.ThemeCatalog, ApplyTheme, this);
            }

            // 1. Initialize procedural TweenRunner
            m_TweenRunner = new TweenRunner();

            // 1b. Initialize VfxService
            var vfxRoot = transform.Find("VFX_Root");
            if (vfxRoot == null)
            {
                var vfxGo = new GameObject("VFX_Root");
                vfxGo.transform.SetParent(transform, false);
                vfxRoot = vfxGo.transform;
            }
            m_VfxService = new VfxService(m_VfxCatalog, vfxRoot);
            m_VfxService.PrewarmShaders();

            // 1c. Initialize AudioService
            if (audioService != null)
            {
                m_AudioService = audioService;
                m_OwnsAudioService = false;
            }
            else
            {
                m_AudioService = new AudioService(m_AudioCatalog, m_MainMixer, transform);
                m_OwnsAudioService = true;
            }

            // 2. Initialize BoardView
            if (m_BoardView == null)
            {
                var boardGo = new GameObject("BoardView");
                boardGo.transform.SetParent(transform, false);
                m_BoardView = boardGo.AddComponent<BoardView>();
            }
            m_BoardView.Initialize(m_BoardTheme, m_CellMesh, m_CellMaterial, m_FrameMesh, m_FrameMaterial);

            // 3. Initialize CameraRig
            if (m_CameraRig == null)
            {
                var camGo = new GameObject("CameraRig");
                camGo.transform.SetParent(transform, false);
                m_CameraRig = camGo.AddComponent<CameraRig>();
            }
            m_CameraRig.Initialize(m_CameraProfile, m_BoardView.transform.position, m_BoardView.BoardExtent + 0.4f,
                m_BoardView.BoardDepth + 0.4f * m_BoardView.RowPitchScale);
            m_CameraRig.SetBackdrop(m_Backdrop);

            // SafeAreaFitter owns the one authoritative HUD solve and supplies the matching viewport.
            SafeAreaFitter safeAreaFitter = m_HudPresenter != null
                ? m_HudPresenter.GetComponent<SafeAreaFitter>()
                : null;
            if (safeAreaFitter != null)
            {
                safeAreaFitter.SetCameraRig(m_CameraRig);
                safeAreaFitter.RefreshSafeArea(force: true);
            }

            // 4. Initialize BallViewManager & prewarm 81 instances
            var ballsRoot = transform.Find("Balls_Root");
            if (ballsRoot == null)
            {
                var go = new GameObject("Balls_Root");
                go.transform.SetParent(transform, false);
                ballsRoot = go.transform;
            }
            m_BallManager = new BallViewManager();
            Mesh initialBallMesh = m_BallTheme != null && m_BallTheme.BallMesh != null
                ? m_BallTheme.BallMesh
                : m_BallMesh;
            Material[] initialBallMaterials = m_BallTheme != null && m_BallTheme.BallMaterials != null && m_BallTheme.BallMaterials.Length > 0
                ? m_BallTheme.BallMaterials
                : m_BallMaterials;
            m_BallManager.Initialize(
                initialBallMesh,
                m_ShadowMesh,
                initialBallMaterials,
                m_GlowMaterial,
                m_ShadowMaterial,
                ballsRoot,
                m_TweenRunner,
                m_BoardView.CellPitch,
                m_BallTheme);

            // 5. Initialize Animators
            if (m_MotionProfile == null)
            {
                Debug.LogWarning("[PresentationRoot] m_MotionProfile is null, falling back to MotionProfileSO.Default.");
            }
            if (m_FeedbackProfile == null)
            {
                Debug.LogWarning("[PresentationRoot] m_FeedbackProfile is null, falling back to FeedbackProfileSO.Default.");
            }

            FeedbackRules feedbackRules = m_FeedbackProfile != null ? m_FeedbackProfile.ToRules() : FeedbackRules.Default;
            m_MoveAnimator = new MoveAnimator(m_BoardView, m_BallManager, m_TweenRunner, m_MotionProfile ?? MotionProfileSO.Default, m_VfxService, m_AudioService);
            m_BoardAnimator = new BoardAnimator(m_BoardView, m_BallManager, m_TweenRunner, m_CameraRig.CamShake, feedbackRules, m_GlowMaterial, m_VfxService, m_AudioService, m_MotionProfile ?? MotionProfileSO.Default, this);

            // 6. Initialize InputRouter
            if (m_InputRouter == null)
            {
                var inputGo = new GameObject("InputRouter");
                inputGo.transform.SetParent(transform, false);
                m_InputRouter = inputGo.AddComponent<InputRouter>();
            }
            m_InputRouter.Initialize(m_CameraRig.Camera, m_BoardView, m_Session);
            m_InputRouter.OnInvalidMoveAttempted += HandleInvalidMoveAttempted;

            // 7. Initialize PathPreviewView and MovePacer and bind to GameSession
            m_PathPreviewView = new PathPreviewView(m_BoardView, null, null, transform);
            m_MovePacer = new MovePacer(m_MoveAnimator, m_BoardAnimator, m_InputRouter, m_PathPreviewView, m_MotionProfile ?? MotionProfileSO.Default);
            if (m_Session != null)
            {
                m_Session.Pacer = m_MovePacer;
                m_Session.OnBallSelected += HandleBallSelected;
                m_Session.OnBallDeselected += HandleBallDeselected;
                m_Session.OnMoveCommitted += HandleMoveCommitted;
                m_Session.OnGameOver += HandleGameOver;

                // Sync initial board state (e.g. 5 initial balls)
                m_BallManager.SyncFromBoard(m_Session.Board, m_BoardView);
            }

            // 8. Initialize UI Router and HUD Presenter
            if (m_UIRouter != null)
            {
                m_UIRouter.Initialize(m_TweenRunner, m_AudioService, m_UiTheme, m_Session);
            }

            if (m_HudPresenter != null && m_Session != null)
            {
                m_HudPresenter.Initialize(m_Session, m_UIRouter, m_TweenRunner, m_UiTheme, m_PathPreviewView);
                if (m_BallTheme != null)
                {
                    m_HudPresenter.ApplyTheme(m_UiTheme, m_BallTheme);
                }
            }

            // 9. Initialize ThemeSwapController
            m_ThemeSwapController = new ThemeSwapController(m_BoardView, m_BallManager, m_UIRouter, m_HudPresenter, m_BoardAnimator);

            m_IsInitialized = true;
        }

        public void ApplyTheme(ThemeDefinitionSO theme)
        {
            ApplyTheme(theme, null);
        }

        public void ApplyTheme(ThemeDefinitionSO theme, BallThemeSO ballTheme)
        {
            if (theme == null) return;
            m_ActiveTheme = theme;
            m_BallTheme = ballTheme != null ? ballTheme : theme.BallTheme;
            m_BoardTheme = theme.BoardTheme;
            m_UiTheme = theme.UiTheme;
            m_ThemeSwapController?.ApplyTheme(theme, m_BallTheme);
        }

        public void ApplyBallTheme(BallThemeSO ballTheme)
        {
            if (ballTheme == null) return;
            m_BallTheme = ballTheme;
            m_BallMaterials = ballTheme.BallMaterials;
            m_ThemeSwapController?.ApplyBallTheme(ballTheme);
        }

        public void ApplyBoardTheme(BoardThemeSO boardTheme)
        {
            if (boardTheme == null) return;
            m_BoardTheme = boardTheme;
            m_ThemeSwapController?.ApplyBoardTheme(boardTheme);
        }

        public void ApplyClearEffect(ClearEffectSO clearEffect)
        {
            if (clearEffect == null) return;
            m_ThemeSwapController?.ApplyClearEffect(clearEffect);
        }

        public void ApplyUiTheme(UiThemeSO uiTheme, string themeId)
        {
            if (uiTheme == null) return;
            m_UiTheme = uiTheme;
            m_ThemeSwapController?.ApplyUiTheme(uiTheme, themeId);
        }

        public void ApplyCategoryTheme(ThemeCategory category, ThemeDefinitionSO theme)
        {
            if (theme == null) return;
            switch (category)
            {
                case ThemeCategory.Ball:
                    ApplyBallTheme(theme.BallTheme);
                    break;
                case ThemeCategory.Board:
                    ApplyBoardTheme(theme.BoardTheme);
                    break;
                case ThemeCategory.ClearEffect:
                    ApplyClearEffect(theme.ClearEffect);
                    break;
                case ThemeCategory.Ui:
                    ApplyUiTheme(theme.UiTheme, theme.ThemeId);
                    break;
            }
        }

        public void ApplyCategoryTheme(ThemeCategory category, string partId)
        {
            var catalog = m_UIRouter != null ? m_UIRouter.ThemeCatalog : null;
            if (catalog == null) return;

            switch (category)
            {
                case ThemeCategory.Ball:
                    if (catalog.TryGetBallTheme(partId, out var bt)) ApplyBallTheme(bt);
                    break;
                case ThemeCategory.Board:
                    if (catalog.TryGetBoardTheme(partId, out var brdt)) ApplyBoardTheme(brdt);
                    break;
                case ThemeCategory.ClearEffect:
                    for (int i = 0; i < catalog.Count; i++)
                    {
                        var t = catalog.ThemeAt(i);
                        if (t != null && t.ClearEffect != null && string.Equals(t.ClearEffect.ThemeId, partId, StringComparison.OrdinalIgnoreCase))
                        {
                            ApplyClearEffect(t.ClearEffect);
                            break;
                        }
                    }
                    break;
                case ThemeCategory.Ui:
                    for (int i = 0; i < catalog.Count; i++)
                    {
                        var t = catalog.ThemeAt(i);
                        if (t != null && t.UiTheme != null && string.Equals(t.UiTheme.ThemeId, partId, StringComparison.OrdinalIgnoreCase))
                        {
                            ApplyUiTheme(t.UiTheme, t.ThemeId);
                            break;
                        }
                    }
                    break;
            }
        }

        private sealed class CatalogThemeSelector : IThemeSelector
        {
            private readonly ThemeCatalogSO m_Catalog;
            private readonly Action<ThemeDefinitionSO> m_ApplyTheme;
            private readonly PresentationRoot m_Root;

            public CatalogThemeSelector(ThemeCatalogSO catalog, Action<ThemeDefinitionSO> applyTheme, PresentationRoot root = null)
            {
                m_Catalog = catalog;
                m_ApplyTheme = applyTheme;
                m_Root = root;
            }

            public string ActiveThemeId => m_Root != null && m_Root.m_ActiveTheme != null ? m_Root.m_ActiveTheme.ThemeId : m_Catalog?.DefaultTheme?.ThemeId ?? ThemeIds.Classic;
            public BallThemeSO ActiveBallTheme => m_Root != null && m_Root.m_BallTheme != null ? m_Root.m_BallTheme : m_Catalog?.DefaultTheme?.BallTheme;
            public string ActiveBallThemeId => m_Root != null && m_Root.m_BallTheme != null ? m_Root.m_BallTheme.ThemeId : m_Catalog?.DefaultTheme?.BallTheme?.ThemeId ?? ThemeIds.Classic;
            public string ActiveBoardThemeId => m_Root != null && m_Root.m_BoardTheme != null ? m_Root.m_BoardTheme.ThemeId : m_Catalog?.DefaultTheme?.BoardTheme?.ThemeId ?? ThemeIds.Classic;
            public string ActiveClearEffectThemeId => m_Catalog?.DefaultTheme?.ClearEffect?.ThemeId ?? ThemeIds.Classic;

            public void RequestTheme(string themeId)
            {
                if (m_Catalog != null && m_Catalog.TryGetTheme(themeId, out ThemeDefinitionSO theme))
                {
                    m_ApplyTheme?.Invoke(theme);
                }
            }

            public void RequestTheme(ThemeCategory category, string partThemeId)
            {
                if (m_Root != null)
                {
                    m_Root.ApplyCategoryTheme(category, partThemeId);
                }
                else if (m_Catalog != null && m_Catalog.TryGetTheme(partThemeId, out ThemeDefinitionSO theme))
                {
                    m_ApplyTheme?.Invoke(theme);
                }
            }
        }

        private void Start()
        {
            if (!m_IsInitialized)
            {
                var session = new GameSession();
                session.StartNewGame();
                Initialize(session);
            }
        }

        private GridPos m_LastSelectedPos;
        private bool m_HasSelectedBall;

        private void HandleBallSelected(GridPos pos)
        {
            if (m_HasSelectedBall && m_LastSelectedPos != pos)
            {
                var prevBall = m_BallManager?.GetBallAt(m_LastSelectedPos);
                if (prevBall != null)
                {
                    prevBall.SetSelected(false);
                }
            }

            m_LastSelectedPos = pos;
            m_HasSelectedBall = true;
            var ball = m_BallManager?.GetBallAt(pos);
            if (ball != null)
            {
                ball.SetSelected(true);
                m_VfxService?.PlayVfx("BallSelect", m_BoardView.GridToWorld(pos));
                m_AudioService?.PlaySfx("sfx_ball_select");
            }
        }

        private void HandleBallDeselected()
        {
            if (m_HasSelectedBall)
            {
                var ball = m_BallManager?.GetBallAt(m_LastSelectedPos);
                if (ball != null)
                {
                    ball.SetSelected(false);
                }
                m_HasSelectedBall = false;
                m_AudioService?.PlaySfx("sfx_ball_deselect");
            }
        }

        private void HandleInvalidMoveAttempted(GridPos pos)
        {
            var ball = m_BallManager?.GetBallAt(pos);
            if (ball != null)
            {
                Vector3 shakeAxis = m_CameraRig != null ? m_CameraRig.YawRightAxis : Vector3.right;
                ball.PlayShake(shakeAxis);
            }
            if (m_VfxService != null && m_BoardView != null)
            {
                m_VfxService.PlayVfx("InvalidShake", m_BoardView.GridToWorld(pos));
            }
            m_AudioService?.PlaySfx("sfx_ball_invalid");
        }

        private void HandleMoveCommitted(MoveResult result)
        {
            // Visual commit sync if not handled by pacer
        }

        private void HandleGameOver(SessionSummary summary)
        {
            Vector3 center = m_BoardView != null ? m_BoardView.transform.position : Vector3.zero;
            m_VfxService?.PlayVfx("GameOverFrost", center);
            m_AudioService?.PlaySfx("sfx_game_over");
        }

        public void Tick(float dt)
        {
            if (m_SlowMoRemaining > 0f)
            {
                m_SlowMoRemaining -= dt;
                if (m_SlowMoRemaining <= 0f)
                {
                    m_SlowMoRemaining = 0f;
                    m_ClockScale = 1.0f;
                }
            }

            if (!m_IsInitialized || m_TweenRunner == null) return;

            float presentationDt = dt * m_ClockScale;

            m_TweenRunner.Tick(presentationDt);
            m_PathPreviewView?.Tick(presentationDt);
            m_CameraRig.Tick(presentationDt);
            m_MovePacer.Tick(presentationDt);
            m_InputRouter.Tick(dt);
            m_UIRouter?.Tick(presentationDt);
            m_VfxService?.Tick(presentationDt);
            if (m_OwnsAudioService)
            {
                m_AudioService?.Tick(dt);
            }
        }

        private void Update()
        {
            // Standalone fallback if AppRoot does not drive Tick directly
            Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (m_InputRouter != null)
            {
                m_InputRouter.OnInvalidMoveAttempted -= HandleInvalidMoveAttempted;
            }
            if (m_Session != null)
            {
                m_Session.OnBallSelected -= HandleBallSelected;
                m_Session.OnBallDeselected -= HandleBallDeselected;
                m_Session.OnMoveCommitted -= HandleMoveCommitted;
                m_Session.OnGameOver -= HandleGameOver;
            }
            if (m_OwnsAudioService)
            {
                m_AudioService?.Dispose();
            }
            m_BallManager?.Dispose();
        }
    }
}

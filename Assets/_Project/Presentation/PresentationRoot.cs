using System;
using UnityEngine;
using Line98.Core;
using Line98.Data;
using Line98.Gameplay;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Master visual orchestrator for Line 98.
    /// Manages the visual board, pooled ball instances, procedural tween runner,
    /// 3D orthogonal camera rig, mathematical input router, and move pacing.
    /// Dispatches a single unified Tick(dt) across all presentation sub-systems.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PresentationRoot : MonoBehaviour, ITickable
    {
        [Header("Profiles & Definitions")]
        [SerializeField] private MotionProfileSO m_MotionProfile;
        [SerializeField] private FeedbackProfileSO m_FeedbackProfile;
        [SerializeField] private CameraProfileSO m_CameraProfile;
        [SerializeField] private BallThemeSO m_BallTheme;
        [SerializeField] private BoardThemeSO m_BoardTheme;

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
        [SerializeField] private UIRouter m_UIRouter;
        [SerializeField] private HudPresenter m_HudPresenter;
        [SerializeField] private UiThemeSO m_UiTheme;

        private GameSession m_Session;
        private TweenRunner m_TweenRunner;
        private BallViewManager m_BallManager;
        private MoveAnimator m_MoveAnimator;
        private BoardAnimator m_BoardAnimator;
        private MovePacer m_MovePacer;
        private bool m_IsInitialized;

        public bool IsInitialized => m_IsInitialized;
        public CameraRig CameraRig => m_CameraRig;
        public BoardView BoardView => m_BoardView;
        public BallViewManager BallManager => m_BallManager;
        public TweenRunner TweenRunner => m_TweenRunner;
        public MovePacer MovePacer => m_MovePacer;
        public InputRouter InputRouter => m_InputRouter;
        public UIRouter UIRouter => m_UIRouter;
        public HudPresenter HudPresenter => m_HudPresenter;
        public GameSession Session => m_Session;

        public void Initialize(
            GameSession session,
            MotionProfileSO motionProfile = null,
            FeedbackProfileSO feedbackProfile = null,
            CameraProfileSO cameraProfile = null,
            BallThemeSO ballTheme = null,
            BoardThemeSO boardTheme = null)
        {
            m_Session = session;
            if (motionProfile != null) m_MotionProfile = motionProfile;
            if (feedbackProfile != null) m_FeedbackProfile = feedbackProfile;
            if (cameraProfile != null) m_CameraProfile = cameraProfile;
            if (ballTheme != null) m_BallTheme = ballTheme;
            if (boardTheme != null) m_BoardTheme = boardTheme;

            // 1. Initialize procedural TweenRunner
            m_TweenRunner = new TweenRunner();

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

            // Fit camera into UI solver board viewport
            var layout = HudLayoutSolver.Solve(HudLayoutSolver.ReferenceWidth, HudLayoutSolver.ReferenceHeight);
            m_CameraRig.SetTargetViewport(layout.BoardViewportRect.min, layout.BoardViewportRect.max);
            if (m_HudPresenter != null)
                m_HudPresenter.GetComponent<SafeAreaFitter>()?.RefreshSafeArea(force: true);

            // 4. Initialize BallViewManager & prewarm 81 instances
            var ballsRoot = transform.Find("Balls_Root");
            if (ballsRoot == null)
            {
                var go = new GameObject("Balls_Root");
                go.transform.SetParent(transform, false);
                ballsRoot = go.transform;
            }
            m_BallManager = new BallViewManager();
            m_BallManager.Initialize(
                m_BallMesh,
                m_ShadowMesh,
                m_BallMaterials,
                m_GlowMaterial,
                m_ShadowMaterial,
                ballsRoot,
                m_TweenRunner,
                m_BoardView.CellPitch);

            // 5. Initialize Animators
            FeedbackRules feedbackRules = m_FeedbackProfile != null ? m_FeedbackProfile.ToRules() : FeedbackRules.Default;
            m_MoveAnimator = new MoveAnimator(m_BoardView, m_BallManager, m_TweenRunner, m_MotionProfile);
            m_BoardAnimator = new BoardAnimator(m_BoardView, m_BallManager, m_TweenRunner, m_CameraRig.CamShake, feedbackRules);

            // 6. Initialize InputRouter
            if (m_InputRouter == null)
            {
                var inputGo = new GameObject("InputRouter");
                inputGo.transform.SetParent(transform, false);
                m_InputRouter = inputGo.AddComponent<InputRouter>();
            }
            m_InputRouter.Initialize(m_CameraRig.Camera, m_BoardView, m_Session);

            // 7. Initialize MovePacer and bind to GameSession
            m_MovePacer = new MovePacer(m_MoveAnimator, m_BoardAnimator, m_InputRouter);
            if (m_Session != null)
            {
                m_Session.Pacer = m_MovePacer;
                m_Session.OnBallSelected += HandleBallSelected;
                m_Session.OnBallDeselected += HandleBallDeselected;
                m_Session.OnMoveCommitted += HandleMoveCommitted;

                // Sync initial board state (e.g. 5 initial balls)
                m_BallManager.SyncFromBoard(m_Session.Board, m_BoardView);
            }

            // 8. Initialize UI Router and HUD Presenter
            if (m_UIRouter != null)
            {
                m_UIRouter.Initialize(m_TweenRunner);
            }

            if (m_HudPresenter != null && m_Session != null)
            {
                m_HudPresenter.Initialize(m_Session, m_UIRouter, m_TweenRunner, m_UiTheme);
            }

            m_IsInitialized = true;
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
            }
        }

        private void HandleMoveCommitted(MoveResult result)
        {
            // Visual commit sync if not handled by pacer
        }

        public void Tick(float dt)
        {
            if (!m_IsInitialized) return;

            m_TweenRunner.Tick(dt);
            m_CameraRig.Tick(dt);
            m_MovePacer.Tick(dt);
            m_InputRouter.Tick(dt);
            m_UIRouter?.Tick(dt);
        }

        private void Update()
        {
            // Standalone fallback if AppRoot does not drive Tick directly
            Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (m_Session != null)
            {
                m_Session.OnBallSelected -= HandleBallSelected;
                m_Session.OnBallDeselected -= HandleBallDeselected;
                m_Session.OnMoveCommitted -= HandleMoveCommitted;
            }
            m_BallManager?.Dispose();
        }
    }
}

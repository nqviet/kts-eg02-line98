using System;
using UnityEngine;
using Line98.Core;
using Line98.Data;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Lightweight presentation rig for the MainMenu 3D showcase board.
    /// Composes CameraRig, BoardView, and BallViewManager without binding
    /// active GameSession gameplay logic, input routers, or HUD presenters.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MenuShowcaseRig : MonoBehaviour
    {
        [Header("Hierarchies")]
        [SerializeField] private CameraRig m_CameraRig;
        [SerializeField] private BoardView m_BoardView;
        [SerializeField] private Transform m_BallsRoot;
        [SerializeField] private Renderer m_Backdrop;

        [Header("Assets & Themes")]
        [SerializeField] private BoardThemeSO m_BoardTheme;
        [SerializeField] private BallThemeSO m_BallTheme;
        [SerializeField] private CameraProfileSO m_CameraProfile;
        [SerializeField] private MenuShowcaseLayoutSO m_Layout;
        [SerializeField] private Mesh m_ShadowMesh;
        [SerializeField] private Material m_GlowMaterial;

        [Header("Viewport")]
        [SerializeField] private Rect m_TargetViewport = new Rect(0.225f, 0.411f, 0.596f, 0.338f);
        [SerializeField, Min(0.1f)] private float m_ShowcaseBallScale = 1.85f;
        [SerializeField] private bool m_UseUiGemShowcase = true;

        private TweenRunner m_TweenRunner;
        private BallViewManager m_BallManager;
        private Vector3 m_BoardBaseLocalPosition;
        private float m_Timer;
        private bool m_IsReducedMotion;
        private bool m_IsInitialized;

        public CameraRig CameraRig => m_CameraRig;
        public BoardView BoardView => m_BoardView;
        public BallViewManager BallManager => m_BallManager;
        public TweenRunner TweenRunner => m_TweenRunner;
        public bool IsInitialized => m_IsInitialized;
        public bool IsReducedMotion => m_IsReducedMotion;

        public void SetReducedMotion(bool reducedMotion)
        {
            m_IsReducedMotion = reducedMotion;
        }

        private void Start()
        {
            if (!m_IsInitialized)
            {
                Initialize();
            }
        }

        private bool m_IsDrivenExternally;

        private void Update()
        {
            // Only self-tick when not driven externally by AppRoot
            if (!m_IsDrivenExternally)
            {
                Tick(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            m_BallManager?.Dispose();
        }

        public void Initialize(
            BoardThemeSO boardTheme = null,
            BallThemeSO ballTheme = null,
            CameraProfileSO cameraProfile = null,
            Renderer backdrop = null,
            Rect viewport = default,
            MenuShowcaseLayoutSO layout = null)
        {
            if (boardTheme != null) m_BoardTheme = boardTheme;
            if (ballTheme != null) m_BallTheme = ballTheme;
            if (cameraProfile != null) m_CameraProfile = cameraProfile;
            if (backdrop != null) m_Backdrop = backdrop;
            if (layout != null) m_Layout = layout;

            if (viewport.width > 0f && viewport.height > 0f)
            {
                m_TargetViewport = viewport;
            }

            EnsureHierarchy();

            m_TweenRunner = new TweenRunner();

            // 1. Initialize BoardView with theme meshes & materials (D19)
            if (m_BoardView != null && m_BoardTheme != null)
            {
                m_BoardView.Initialize(
                    m_BoardTheme,
                    m_BoardTheme.BoardCellMesh,
                    m_BoardTheme.BoardCellMaterial,
                    m_BoardTheme.BoardFrameMesh,
                    m_BoardTheme.BoardFrameMaterial);
            }

            m_BoardBaseLocalPosition = m_BoardView != null ? m_BoardView.transform.localPosition : Vector3.zero;

            // 2. Initialize BallViewManager
            if (m_BallTheme != null && m_BoardView != null)
            {
                m_BallManager = new BallViewManager();
                m_BallManager.Initialize(
                    m_BallTheme.BallMesh,
                    m_ShadowMesh,
                    m_BallTheme.BallMaterials,
                    m_GlowMaterial,
                    m_BallTheme.BlobShadowMaterial,
                    m_BallsRoot,
                    m_TweenRunner,
                    m_BoardView.CellPitch,
                    m_BallTheme);

                SpawnShowcaseBalls();
            }

            // 3. Initialize CameraRig
            if (m_CameraRig != null && m_CameraProfile != null && m_BoardView != null)
            {
                m_CameraRig.Initialize(
                    m_CameraProfile,
                    m_BoardView.transform.position,
                    m_BoardView.BoardExtent,
                    m_BoardView.BoardDepth);

                if (m_Backdrop != null)
                {
                    m_CameraRig.SetBackdrop(m_Backdrop);
                }

                m_CameraRig.SetTargetViewport(
                    new Vector2(m_TargetViewport.xMin, m_TargetViewport.yMin),
                    new Vector2(m_TargetViewport.xMax, m_TargetViewport.yMax));
            }

            m_IsInitialized = true;
        }

        private void EnsureHierarchy()
        {
            if (m_BoardView == null)
            {
                m_BoardView = GetComponentInChildren<BoardView>();
                if (m_BoardView == null)
                {
                    var boardGo = new GameObject("BoardView");
                    boardGo.transform.SetParent(transform, false);
                    m_BoardView = boardGo.AddComponent<BoardView>();
                }
            }

            if (m_BallsRoot == null)
            {
                var existing = m_BoardView.transform.Find("Balls_Root");
                if (existing != null)
                {
                    m_BallsRoot = existing;
                }
                else
                {
                    var ballsGo = new GameObject("Balls_Root");
                    ballsGo.transform.SetParent(m_BoardView.transform, false);
                    m_BallsRoot = ballsGo.transform;
                }
            }
            else if (m_BallsRoot.parent != m_BoardView.transform)
            {
                m_BallsRoot.SetParent(m_BoardView.transform, true);
            }

            if (m_CameraRig == null)
            {
                m_CameraRig = GetComponentInChildren<CameraRig>();
            }
        }

        private void SpawnShowcaseBalls()
        {
            if (m_Layout == null || m_Layout.Placements == null || m_BallManager == null || m_BoardView == null)
            {
                return;
            }

            m_BallManager.ClearAll();

            foreach (var placement in m_Layout.Placements)
            {
                GridPos pos = placement.Position;
                Vector3 worldFloorPos = m_BoardView.GridToWorld(pos);
                BallView ball = m_BallManager.SpawnBall(pos, placement.Color, worldFloorPos);
                if (ball != null)
                {
                    ball.transform.localScale = Vector3.one * m_ShowcaseBallScale;
                    if (m_UseUiGemShowcase)
                    {
                        var renderers = ball.GetComponentsInChildren<MeshRenderer>(true);
                        for (int i = 0; i < renderers.Length; i++)
                        {
                            renderers[i].enabled = false;
                        }
                    }
                }
            }

            if (m_UseUiGemShowcase && m_BallsRoot != null)
            {
                var renderers = m_BallsRoot.GetComponentsInChildren<MeshRenderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = false;
                }
            }
        }

        public void ApplyBallTheme(BallThemeSO ballTheme)
        {
            if (ballTheme == null) return;
            m_BallTheme = ballTheme;
            m_BallManager?.ApplyTheme(m_BallTheme);
        }

        public void ApplyTheme(BoardThemeSO boardTheme, BallThemeSO ballTheme)
        {
            if (boardTheme != null)
            {
                m_BoardTheme = boardTheme;
                m_BoardView?.ApplyTheme(m_BoardTheme);
            }

            if (ballTheme != null)
            {
                m_BallTheme = ballTheme;
                m_BallManager?.ApplyTheme(m_BallTheme);
            }

            // Reposition showcase balls in case RowPitchScale changed
            if (m_Layout != null && m_Layout.Placements != null && m_BallManager != null && m_BoardView != null)
            {
                foreach (var placement in m_Layout.Placements)
                {
                    var ball = m_BallManager.GetBallAt(placement.Position);
                    if (ball != null)
                    {
                        ball.transform.position = m_BoardView.GridToWorld(placement.Position);
                    }
                }
            }
        }

        public void Tick(float dt)
        {
            m_IsDrivenExternally = true;
            if (!m_IsInitialized) return;

            m_TweenRunner?.Tick(dt);
            m_CameraRig?.Tick(dt);

            m_Timer += dt;

            if (m_BoardView != null)
            {
                float drift = 0f;
                Vector3 parallax = Vector3.zero;

                if (!m_IsReducedMotion && m_Layout != null)
                {
                    float freq = m_Layout.IdleDriftFrequency;
                    float amp = m_Layout.IdleDriftAmplitude;
                    drift = Mathf.Sin(m_Timer * freq * Mathf.PI * 2f) * amp;

                    if (m_Layout.ParallaxFactor > 0f)
                    {
                        Vector2 mouseNorm = Vector2.zero;
                        var pointer = UnityEngine.InputSystem.Pointer.current;
                        if (pointer != null && Screen.width > 0 && Screen.height > 0)
                        {
                            Vector2 mouse = pointer.position.ReadValue();
                            mouseNorm = new Vector2(
                                (mouse.x / Screen.width) - 0.5f,
                                (mouse.y / Screen.height) - 0.5f);
                        }
                        float pf = m_Layout.ParallaxFactor;
                        parallax = new Vector3(mouseNorm.x * pf, 0f, mouseNorm.y * pf);
                    }
                }

                m_BoardView.transform.localPosition = m_BoardBaseLocalPosition + new Vector3(parallax.x, drift, parallax.z);
            }
        }
    }
}

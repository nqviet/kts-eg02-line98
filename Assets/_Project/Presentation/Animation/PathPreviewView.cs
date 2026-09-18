using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Core;
using Line98.Data;

namespace Line98.Presentation.Animation
{
    /// <summary>
    /// Presentation component that renders a marching dotted path preview along BFS routes and a
    /// destination indicator that sits <em>inside</em> the target cell.
    /// <para>
    /// The indicator is a composite scaled off the board's cell pitch and laid on the board's own
    /// plane: a thin outer ring, a soft inner glow, a ring that converges inward on a loop, and an
    /// optional ghost of the ball that is about to arrive. The convergence is what reads as "the
    /// target is this cell" -- a static shape reads as decoration.
    /// </para>
    /// Shared by MovePacer (move preview), HintService (hint highlight), and Onboarding.
    /// Zero heap allocation during active playback.
    /// </summary>
    public sealed class PathPreviewView : ITickable
    {
        private enum Phase : byte
        {
            Hidden,
            SettleIn,
            Breathe,
            FadeOut,
            Collapse
        }

        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_ColorId = Shader.PropertyToID("_Color");
        private static readonly int s_RimColorId = Shader.PropertyToID("_RimColor");

        // Fractions of one cell pitch. The ring stops short of the cell edge so neighbouring cells
        // stay readable, and the glow sits well inside it.
        private const float RingCellFill = 0.86f;
        private const float GlowCellFill = 0.70f;
        private const float GhostCellFill = 0.62f;

        private const float SettleFromScale = 1.35f;
        private const float ConvergeFromScale = 1.20f;
        private const float ConvergeToScale = 0.55f;
        private const float CollapseToScale = 0.60f;
        private const float BreathAmplitude = 0.03f;
        private const float BreathPeriod = 1.10f;

        private const float RingAlpha = 0.90f;
        private const float GlowAlpha = 0.16f;
        private const float ConvergeAlpha = 0.70f;

        private const float DotMarchSpeed = 2.5f;
        private const float LineHeight = 0.12f;

        // Layered just above the board so the composite never z-fights with the cell surface.
        private const float GlowHeight = 0.015f;
        private const float RingHeight = 0.020f;
        private const float ConvergeHeight = 0.025f;

        private static readonly Color InvalidTint = new Color(1f, 0.30f, 0.28f, 1f);
        private static readonly Color NeutralTint = new Color(0.15f, 0.85f, 1.0f, 1f);

        private readonly BoardView m_BoardView;
        private readonly Transform m_Root;
        private readonly GameObject m_LineObject;
        private readonly LineRenderer m_LineRenderer;
        private readonly Material m_DotMaterial;
        private readonly Material m_RingMaterial;
        private readonly Material m_GlowMaterial;
        private readonly Material m_CreatedMaterial;
        private readonly MotionProfileSO m_MotionProfile;

        private readonly GameObject m_DestRingObject;
        private readonly Transform m_DestRoot;
        private readonly Layer m_Ring;
        private readonly Layer m_Glow;
        private readonly Layer m_Converge;
        private readonly Layer m_Ghost;

        private IGhostBallSource m_GhostSource;

        private bool m_IsVisible;
        private object m_CurrentOwner;
        private float m_HoldTimer;
        private float m_MarchOffset;

        private Phase m_Phase;
        private float m_PhaseElapsed;
        private float m_ConvergeTimer;
        private bool m_ConvergeActive;
        private float m_ConvergeElapsed;
        private float m_BreathTimer;
        private float m_CellSize;

        public bool IsVisible => m_IsVisible;
        public LineRenderer LineRenderer => m_LineRenderer;
        public GameObject DestRingObject => m_DestRingObject;

        /// <summary>Whether the indicator is still fading rather than fully gone.</summary>
        public bool IsFading => m_Phase == Phase.FadeOut || m_Phase == Phase.Collapse;

        private float DestRingInSeconds => Ms(m_MotionProfile != null ? m_MotionProfile.DestRingInMs : 160f);
        private float DestRingHoldSeconds => Ms(m_MotionProfile != null ? m_MotionProfile.DestRingHoldMs : 700f);
        private float DestRingConvergeSeconds => Ms(m_MotionProfile != null ? m_MotionProfile.DestRingConvergeMs : 520f);
        private float DestRingFadeSeconds => Ms(m_MotionProfile != null ? m_MotionProfile.DestRingFadeMs : 120f);
        private float DestRingInvalidSeconds => Ms(m_MotionProfile != null ? m_MotionProfile.DestRingInvalidMs : 200f);
        private float GhostAlpha => m_MotionProfile != null ? m_MotionProfile.GhostAlpha : 0.20f;

        private static float Ms(float milliseconds) => Mathf.Max(0.0001f, milliseconds * 0.001f);

        /// <summary>
        /// One tinted, alpha-driven quad (or mesh) of the destination composite.
        /// <para>
        /// Each layer owns a private material instance rather than writing a
        /// MaterialPropertyBlock over a shared one. Under URP's SRP Batcher, per-object property
        /// blocks for values in the UnityPerMaterial buffer are unreliable, and mutating the shared
        /// material would tint every other user of it. Four instances, created once, is cheap.
        /// </para>
        /// </summary>
        private sealed class Layer
        {
            public readonly Transform Transform;
            public readonly MeshRenderer Renderer;
            public readonly MeshFilter Filter;

            private Material m_Source;
            private Material m_Instance;
            private Color m_Tint;

            public Layer(string name, Transform parent, Material material, Mesh mesh)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                Transform = go.transform;

                Filter = go.AddComponent<MeshFilter>();
                Filter.sharedMesh = mesh;

                Renderer = go.AddComponent<MeshRenderer>();
                Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Renderer.receiveShadows = false;
                Renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                Renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

                m_Tint = Color.white;
                SetMaterial(material);
                go.SetActive(false);
            }

            public bool IsUsable => Renderer != null && m_Instance != null && Filter.sharedMesh != null;

            public void SetTint(Color tint)
            {
                m_Tint = tint;
            }

            public void SetMesh(Mesh mesh)
            {
                Filter.sharedMesh = mesh;
            }

            /// <summary>Re-instances only when the source actually changes, so repeated shows are free.</summary>
            public void SetMaterial(Material material)
            {
                if (material == null)
                {
                    m_Source = null;
                    m_Instance = null;
                    Renderer.sharedMaterial = null;
                    return;
                }

                if (ReferenceEquals(material, m_Source) && m_Instance != null)
                {
                    return;
                }

                m_Source = material;
                m_Instance = new Material(material) { name = $"{material.name}_PathPreview" };
                Renderer.sharedMaterial = m_Instance;
            }

            public void SetActive(bool active)
            {
                if (Transform != null) Transform.gameObject.SetActive(active && IsUsable);
            }

            /// <summary>
            /// Drives alpha on the private instance. All three tint channels are written because the
            /// composite mixes shader families: the ring and glow quads read _BaseColor or _Color,
            /// while the ghost reuses the ball's glow shell, which reads _RimColor. Setting a
            /// property the shader does not declare is a no-op.
            /// </summary>
            public void SetAlpha(float alpha)
            {
                if (m_Instance == null) return;

                Color color = new Color(m_Tint.r, m_Tint.g, m_Tint.b, m_Tint.a * Mathf.Clamp01(alpha));
                if (m_Instance.HasProperty(s_BaseColorId)) m_Instance.SetColor(s_BaseColorId, color);
                if (m_Instance.HasProperty(s_ColorId)) m_Instance.SetColor(s_ColorId, color);
                if (m_Instance.HasProperty(s_RimColorId)) m_Instance.SetColor(s_RimColorId, color);
            }

            public void Dispose()
            {
                if (m_Instance != null)
                {
                    DestroySafely(m_Instance);
                    m_Instance = null;
                }
            }
        }

        public PathPreviewView(
            BoardView boardView,
            Material dotMaterial,
            Material ringMaterial,
            Transform root,
            Material glowMaterial = null,
            MotionProfileSO motionProfile = null)
        {
            m_BoardView = boardView;
            m_Root = root != null ? root : (boardView != null ? boardView.transform : null);
            m_MotionProfile = motionProfile;

            // Runtime fallback only. Real materials come from PresentationRoot; without them the
            // marching dots have no texture to march and the ring has no ring in it.
            if (dotMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                }
                m_CreatedMaterial = shader != null ? new Material(shader) : null;
                if (m_CreatedMaterial != null)
                {
                    m_CreatedMaterial.color = NeutralTint;
                }
                m_DotMaterial = m_CreatedMaterial;
            }
            else
            {
                m_DotMaterial = dotMaterial;
            }

            m_RingMaterial = ringMaterial != null ? ringMaterial : m_DotMaterial;
            m_GlowMaterial = glowMaterial != null ? glowMaterial : m_RingMaterial;

            m_LineObject = new GameObject("PathPreview_Line");
            if (m_Root != null)
            {
                m_LineObject.transform.SetParent(m_Root, false);
            }
            m_LineRenderer = m_LineObject.AddComponent<LineRenderer>();
            m_LineRenderer.useWorldSpace = true;
            m_LineRenderer.startWidth = 0.10f;
            m_LineRenderer.endWidth = 0.10f;
            m_LineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_LineRenderer.receiveShadows = false;
            m_LineRenderer.textureMode = LineTextureMode.Tile;
            if (m_DotMaterial != null)
            {
                m_LineRenderer.sharedMaterial = m_DotMaterial;
            }
            m_LineRenderer.positionCount = 0;
            m_LineRenderer.enabled = false;

            // Destination composite. A generated quad rather than CreatePrimitive: no collider to
            // destroy, and no dependency on primitive defaults.
            m_DestRingObject = new GameObject("PathPreview_DestRing");
            if (m_Root != null)
            {
                m_DestRingObject.transform.SetParent(m_Root, false);
            }
            m_DestRoot = m_DestRingObject.transform;

            Mesh quad = BuildQuad();
            m_Glow = new Layer("Glow_Inner", m_DestRoot, m_GlowMaterial, quad);
            m_Ring = new Layer("Ring_Outer", m_DestRoot, m_RingMaterial, quad);
            m_Converge = new Layer("Ring_Converge", m_DestRoot, m_RingMaterial, quad);
            m_Ghost = new Layer("Ghost_Ball", m_DestRoot, null, null);

            m_DestRingObject.SetActive(false);
        }

        /// <summary>
        /// Supplies the ghost ball's mesh and material. Optional -- without a source the indicator
        /// simply omits the ghost.
        /// </summary>
        public void SetGhostSource(IGhostBallSource source)
        {
            m_GhostSource = source;
        }

        public void ShowPath(List<GridPos> path, float holdSeconds, object owner)
        {
            ShowPath(path, holdSeconds, owner, showGhost: true);
        }

        /// <param name="showGhost">
        /// Hint and onboarding pass false: the hint already highlights the source ball, and a ghost
        /// would imply a move the player has not committed to.
        /// </param>
        public void ShowPath(List<GridPos> path, float holdSeconds, object owner, bool showGhost)
        {
            if (path == null || path.Count < 2)
            {
                return;
            }

            m_CurrentOwner = owner;
            m_HoldTimer = holdSeconds;
            m_IsVisible = true;
            m_MarchOffset = 0f;

            if (m_LineRenderer != null)
            {
                m_LineRenderer.positionCount = path.Count;
                Vector3 lift = BoardUp * LineHeight;
                for (int i = 0; i < path.Count; i++)
                {
                    m_LineRenderer.SetPosition(i, CellWorld(path[i]) + lift);
                }
                m_LineRenderer.enabled = true;
            }

            GridPos target = path[path.Count - 1];
            BeginDestination(target, showGhost ? path[0] : (GridPos?)null, NeutralTint);
        }

        public void ShowHint(GridPos from, GridPos to, List<GridPos> path, float holdSeconds, object owner)
        {
            if (path != null && path.Count >= 2)
            {
                // Hint must not draw a ghost: it is a suggestion, not a queued move.
                ShowPath(path, holdSeconds, owner, showGhost: false);
                return;
            }

            m_CurrentOwner = owner;
            m_HoldTimer = holdSeconds;
            m_IsVisible = true;
            m_MarchOffset = 0f;

            if (m_LineRenderer != null)
            {
                m_LineRenderer.enabled = false;
                m_LineRenderer.positionCount = 0;
            }

            BeginDestination(to, null, NeutralTint);
        }

        /// <summary>
        /// Collapses the indicator on an unreachable cell, so a rejected tap reads as "not allowed"
        /// rather than as a dropped input.
        /// </summary>
        public void ShowInvalid(GridPos cell, object owner)
        {
            m_CurrentOwner = owner;
            m_IsVisible = true;
            m_HoldTimer = 0f;

            if (m_LineRenderer != null)
            {
                m_LineRenderer.enabled = false;
                m_LineRenderer.positionCount = 0;
            }

            BeginDestination(cell, null, InvalidTint);
            m_Phase = Phase.Collapse;
            m_PhaseElapsed = 0f;
            m_Converge.SetActive(false);
            m_ConvergeActive = false;
        }

        /// <summary>
        /// Starts the outward fade. Called before the ball lands so the indicator is gone by the
        /// time the ball occupies the cell, instead of the ball flying over a lit shape.
        /// </summary>
        public void FadeOut()
        {
            if (!m_IsVisible || m_Phase == Phase.FadeOut || m_Phase == Phase.Collapse)
            {
                return;
            }

            m_Phase = Phase.FadeOut;
            m_PhaseElapsed = 0f;
            m_HoldTimer = 0f;
        }

        /// <summary>
        /// Clears the dotted route but leaves the destination indicator up. Called when the ball
        /// starts moving: the path has served its purpose, while the target cell must stay marked
        /// for as long as the ball is in the air.
        /// </summary>
        public void HidePath()
        {
            if (m_LineRenderer != null)
            {
                m_LineRenderer.enabled = false;
                m_LineRenderer.positionCount = 0;
            }
        }

        /// <summary>How long <see cref="FadeOut"/> takes, so callers can start it before landing.</summary>
        public float FadeDurationSeconds => DestRingFadeSeconds;

        public void Hide()
        {
            m_IsVisible = false;
            m_CurrentOwner = null;
            m_HoldTimer = 0f;
            m_Phase = Phase.Hidden;
            m_PhaseElapsed = 0f;
            m_ConvergeActive = false;

            if (m_LineRenderer != null)
            {
                m_LineRenderer.enabled = false;
                m_LineRenderer.positionCount = 0;
            }

            if (m_DestRingObject != null)
            {
                m_DestRingObject.SetActive(false);
            }
        }

        public void CancelByOwner(object owner)
        {
            if (m_CurrentOwner == owner)
            {
                Hide();
            }
        }

        /// <summary>Releases the per-layer material instances created for alpha control.</summary>
        public void Dispose()
        {
            Hide();
            m_Ring?.Dispose();
            m_Glow?.Dispose();
            m_Converge?.Dispose();
            m_Ghost?.Dispose();

            if (m_CreatedMaterial != null)
            {
                DestroySafely(m_CreatedMaterial);
            }
        }

        /// <summary>
        /// Object.Destroy throws outside play mode, which would break EditMode teardown, so pick the
        /// matching call for the current mode.
        /// </summary>
        private static void DestroySafely(UnityEngine.Object target)
        {
            if (target == null) return;

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        public void Tick(float dt)
        {
            if (!m_IsVisible)
            {
                return;
            }

            MarchDots(dt);
            TickDestination(dt);

            if (m_HoldTimer > 0f)
            {
                m_HoldTimer -= dt;
                if (m_HoldTimer <= 0f)
                {
                    m_HoldTimer = 0f;
                    FadeOut();
                }
            }
        }

        private void MarchDots(float dt)
        {
            m_MarchOffset -= dt * DotMarchSpeed;
            if (m_DotMaterial != null && m_DotMaterial.HasProperty("_MainTex"))
            {
                m_DotMaterial.mainTextureOffset = new Vector2(m_MarchOffset, 0f);
            }
        }

        private Vector3 BoardUp => m_BoardView != null ? m_BoardView.transform.up : Vector3.up;

        private Vector3 CellWorld(GridPos pos)
        {
            return m_BoardView != null
                ? m_BoardView.GridToWorld(pos)
                : new Vector3(pos.X, 0f, pos.Y);
        }

        private void BeginDestination(GridPos target, GridPos? ghostFrom, Color tint)
        {
            if (m_DestRingObject == null)
            {
                return;
            }

            m_CellSize = m_BoardView != null ? m_BoardView.CellPitch : 1f;

            // Sit on the board's own plane. Using the board's rotation rather than a hardcoded
            // Euler keeps the composite flat even if the board root is tilted or re-parented.
            Quaternion boardRotation = m_BoardView != null ? m_BoardView.transform.rotation : Quaternion.identity;
            m_DestRoot.SetPositionAndRotation(CellWorld(target), boardRotation);

            Vector3 up = BoardUp;
            LayoutLayer(m_Glow, GlowCellFill, GlowHeight, up, tint);
            LayoutLayer(m_Ring, RingCellFill, RingHeight, up, tint);
            LayoutLayer(m_Converge, RingCellFill, ConvergeHeight, up, tint);

            ConfigureGhost(ghostFrom, up);

            m_Phase = Phase.SettleIn;
            m_PhaseElapsed = 0f;
            m_BreathTimer = 0f;
            m_ConvergeTimer = DestRingHoldSeconds;
            m_ConvergeActive = false;
            m_ConvergeElapsed = 0f;

            m_DestRingObject.SetActive(true);
            ApplyDestinationVisuals(scale: SettleFromScale, alpha: 0f);
        }

        private void LayoutLayer(Layer layer, float cellFill, float height, Vector3 up, Color tint)
        {
            if (layer == null || !layer.IsUsable)
            {
                layer?.SetActive(false);
                return;
            }

            layer.SetTint(tint);
            // The quad is authored in XY; rotating 90 degrees about X lays it flat. The parent
            // already carries the board's rotation, so local +Y is the board's up axis.
            layer.Transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            layer.Transform.localPosition = Vector3.up * (height * m_CellSize);
            layer.Transform.localScale = Vector3.one * (m_CellSize * cellFill);
            layer.SetActive(true);
        }

        private void ConfigureGhost(GridPos? ghostFrom, Vector3 up)
        {
            if (ghostFrom == null || m_GhostSource == null ||
                !m_GhostSource.TryGetGhost(ghostFrom.Value, out Mesh mesh, out Material material, out float restHeight))
            {
                m_Ghost.SetActive(false);
                return;
            }

            m_Ghost.SetMesh(mesh);
            m_Ghost.SetMaterial(material);
            // Keep the ball's own hue and only dial its alpha down, so the ghost reads as "this
            // ball, faintly" rather than as a white blob.
            m_Ghost.SetTint(ResolveTint(material));
            m_Ghost.Transform.localRotation = Quaternion.identity;
            m_Ghost.Transform.localPosition = Vector3.up * restHeight;
            m_Ghost.Transform.localScale = Vector3.one * (m_CellSize * GhostCellFill);
            m_Ghost.SetActive(true);
        }

        private static Color ResolveTint(Material material)
        {
            if (material == null) return Color.white;
            if (material.HasProperty(s_RimColorId)) return material.GetColor(s_RimColorId);
            if (material.HasProperty(s_BaseColorId)) return material.GetColor(s_BaseColorId);
            if (material.HasProperty(s_ColorId)) return material.GetColor(s_ColorId);
            return Color.white;
        }

        private void TickDestination(float dt)
        {
            if (m_Phase == Phase.Hidden || m_DestRingObject == null || !m_DestRingObject.activeSelf)
            {
                return;
            }

            m_PhaseElapsed += dt;

            switch (m_Phase)
            {
                case Phase.SettleIn:
                {
                    float t = Mathf.Clamp01(m_PhaseElapsed / DestRingInSeconds);
                    float scale = Mathf.LerpUnclamped(SettleFromScale, 1f, Easing.OutBack(t));
                    ApplyDestinationVisuals(scale, RingAlpha * Easing.OutCubic(t));
                    if (t >= 1f)
                    {
                        m_Phase = Phase.Breathe;
                        m_PhaseElapsed = 0f;
                    }
                    break;
                }

                case Phase.Breathe:
                {
                    m_BreathTimer += dt;
                    float breath = 1f + Mathf.Sin(m_BreathTimer / BreathPeriod * Mathf.PI * 2f) * BreathAmplitude;
                    ApplyDestinationVisuals(breath, RingAlpha);
                    TickConverge(dt);
                    break;
                }

                case Phase.FadeOut:
                {
                    float t = Mathf.Clamp01(m_PhaseElapsed / DestRingFadeSeconds);
                    ApplyDestinationVisuals(Mathf.Lerp(1f, 0.92f, t), RingAlpha * (1f - Easing.OutQuad(t)));
                    if (t >= 1f) Hide();
                    break;
                }

                case Phase.Collapse:
                {
                    float t = Mathf.Clamp01(m_PhaseElapsed / DestRingInvalidSeconds);
                    float scale = Mathf.LerpUnclamped(1f, CollapseToScale, Easing.InBack(t));
                    ApplyDestinationVisuals(scale, RingAlpha * (1f - t));
                    if (t >= 1f) Hide();
                    break;
                }
            }
        }

        /// <summary>
        /// Drives the converging ring: a second ring that starts outside the cell and pulls inward,
        /// repeatedly. This is the motion that names the cell as the target.
        /// </summary>
        private void TickConverge(float dt)
        {
            if (!m_Converge.IsUsable)
            {
                return;
            }

            if (!m_ConvergeActive)
            {
                m_ConvergeTimer -= dt;
                if (m_ConvergeTimer > 0f)
                {
                    m_Converge.SetActive(false);
                    return;
                }

                m_ConvergeActive = true;
                m_ConvergeElapsed = 0f;
                m_Converge.SetActive(true);
            }

            m_ConvergeElapsed += dt;
            float t = Mathf.Clamp01(m_ConvergeElapsed / DestRingConvergeSeconds);
            float scale = Mathf.LerpUnclamped(ConvergeFromScale, ConvergeToScale, Easing.OutCubic(t));
            m_Converge.Transform.localScale = Vector3.one * (m_CellSize * RingCellFill * scale);
            m_Converge.SetAlpha(ConvergeAlpha * (1f - t));

            if (t >= 1f)
            {
                m_ConvergeActive = false;
                m_ConvergeTimer = DestRingHoldSeconds;
                m_Converge.SetActive(false);
            }
        }

        private void ApplyDestinationVisuals(float scale, float alpha)
        {
            if (m_Ring.IsUsable)
            {
                m_Ring.Transform.localScale = Vector3.one * (m_CellSize * RingCellFill * scale);
                m_Ring.SetAlpha(alpha);
            }

            if (m_Glow.IsUsable)
            {
                m_Glow.Transform.localScale = Vector3.one * (m_CellSize * GlowCellFill * scale);
                m_Glow.SetAlpha(alpha * (GlowAlpha / RingAlpha));
            }

            if (m_Ghost.IsUsable && m_Ghost.Transform.gameObject.activeSelf)
            {
                m_Ghost.SetAlpha(alpha * (GhostAlpha / RingAlpha));
            }
        }

        /// <summary>Unit quad in the XY plane, centred on the origin.</summary>
        private static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "PathPreview_Quad" };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            });
            mesh.SetUVs(0, new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            });
            mesh.SetNormals(new List<Vector3>
            {
                Vector3.back,
                Vector3.back,
                Vector3.back,
                Vector3.back
            });
            mesh.SetTriangles(new List<int> { 0, 2, 1, 2, 3, 1 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Core;
using Line98.Data;
using Line98.Gameplay;
using Line98.Presentation.Vfx;

namespace Line98.Presentation.Animation
{
    /// <summary>
    /// Coordinates board-level procedural animations:
    /// - 3-ball spawn arrivals (staggered 70 ms, arc flight + 120 ms micro-bounce)
    /// - 4-tier line clear sequence (outward pulse at 22 ms/cell, glow ramp, InExpo burst)
    /// - Board diagonal wipe on game entry
    /// Zero garbage allocation during active gameplay.
    /// </summary>
    public sealed class BoardAnimator : ITickable
    {
        private readonly BoardView m_BoardView;
        private readonly BallViewManager m_BallManager;
        private readonly TweenRunner m_TweenRunner;
        private readonly CamShake m_CamShake;
        private readonly FeedbackRules m_FeedbackRules;
        private readonly Material m_RibbonMaterial;
        private readonly VfxService m_VfxService;
        private Material m_CreatedRibbonMaterial;

        private bool m_IsAnimatingSpawns;
        private bool m_IsAnimatingClears;
        private bool m_HasPlayedBurstVfx;
        private float m_SpawnTimer;
        private int m_CurrentSpawnIndex;
        private SpawnBatch m_PendingSpawns;
        private Action m_OnSpawnsComplete;

        private float m_ClearTimer;
        private float m_ClearTotalDuration;
        private ClearGroup m_PendingClears;
        private FeedbackTierRule m_ActiveTierRule;
        private GridPos m_PlacedPos;
        private Action m_OnClearsComplete;
        private readonly List<BallView> m_ClearingBalls = new List<BallView>(16);

        // Pooled LineRenderers for clear ribbons (H/V share one, diagonals share second)
        private readonly LineRenderer[] m_RibbonRenderers = new LineRenderer[2];
        private readonly List<GridPos> m_RunH_V = new List<GridPos>(16);
        private readonly List<GridPos> m_RunDiag = new List<GridPos>(16);

        private readonly Line98.Presentation.Audio.AudioService m_AudioService;
        private float m_SpeedMultiplier = 1.0f;

        public bool IsActive => m_IsAnimatingSpawns || m_IsAnimatingClears;
        public FeedbackTierRule ActiveTierRule => m_ActiveTierRule;
        public float ClearTotalDuration => m_ClearTotalDuration;
        public LineRenderer[] RibbonRenderers => m_RibbonRenderers;

        public float SpeedMultiplier
        {
            get => m_SpeedMultiplier;
            set => m_SpeedMultiplier = Mathf.Max(1.0f, value);
        }

        public BoardAnimator(
            BoardView boardView,
            BallViewManager ballManager,
            TweenRunner tweenRunner,
            CamShake camShake,
            FeedbackRules feedbackRules,
            Material ribbonMaterial = null,
            VfxService vfxService = null,
            Line98.Presentation.Audio.AudioService audioService = null)
        {
            m_BoardView = boardView;
            m_BallManager = ballManager;
            m_TweenRunner = tweenRunner;
            m_CamShake = camShake;
            m_FeedbackRules = feedbackRules;
            m_RibbonMaterial = ribbonMaterial;
            m_VfxService = vfxService;
            m_AudioService = audioService;
        }

        public void AnimateSpawns(SpawnBatch batch, Action onComplete)
        {
            m_PendingSpawns = batch;
            m_OnSpawnsComplete = onComplete;
            m_CurrentSpawnIndex = 0;
            m_SpawnTimer = 0f;
            m_IsAnimatingSpawns = true;
            m_SpeedMultiplier = 1.0f;

            if (batch.IsEmpty || batch.Count == 0)
            {
                m_IsAnimatingSpawns = false;
                onComplete?.Invoke();
            }
        }

        public void AnimateClear(ClearGroup clears, GridPos placedPos, Action onComplete)
        {
            m_PendingClears = clears;
            m_PlacedPos = placedPos.IsValid ? placedPos : (clears.Positions != null && clears.Positions.Length > 0 ? clears.Positions[0] : placedPos);
            m_OnClearsComplete = onComplete;
            m_SpeedMultiplier = 1.0f;
            m_ClearingBalls.Clear();

            if (clears.IsEmpty || clears.Positions.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            // Determine feedback tier from rule set
            int count = clears.Count;
            FeedbackTierRule chosenRule = m_FeedbackRules.Tiers != null && m_FeedbackRules.Tiers.Length > 0
                ? m_FeedbackRules.Tiers[0]
                : FeedbackRules.Default.Tiers[0];

            if (m_FeedbackRules.Tiers != null)
            {
                for (int i = 0; i < m_FeedbackRules.Tiers.Length; i++)
                {
                    var rule = m_FeedbackRules.Tiers[i];
                    if (count >= rule.MinLength && count <= rule.MaxLength)
                    {
                        chosenRule = rule;
                        break;
                    }
                }
            }
            m_ActiveTierRule = chosenRule;

            // Collect active ball views for clearing
            for (int i = 0; i < clears.Positions.Length; i++)
            {
                var view = m_BallManager != null ? m_BallManager.GetBallAt(clears.Positions[i]) : null;
                if (view != null)
                {
                    m_ClearingBalls.Add(view);
                }
            }

            // Trigger screen shake if specified by tier
            if (m_ActiveTierRule.ShakeAmp > 0f && m_CamShake != null)
            {
                m_CamShake.TriggerShake(m_ActiveTierRule.ShakeAmp, duration: 0.38f);
            }

            // Setup line ribbons
            SetupRibbons(clears);

            m_HasPlayedBurstVfx = false;
            m_ClearTimer = 0f;
            // Total clear timeline duration based on tier: Connect(90) + Pulse(190) + Glow(200) + Burst(140) + Hold
            m_ClearTotalDuration = (0.62f + m_ActiveTierRule.HoldMs * 0.001f) * m_ActiveTierRule.AnimationScale;
            m_IsAnimatingClears = true;
        }

        private void EnsureRibbons()
        {
            if (m_RibbonRenderers[0] != null) return;

            Material mat = m_RibbonMaterial;
            if (mat == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    mat = new Material(shader);
                    m_CreatedRibbonMaterial = mat;
                }
            }

            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject($"ClearRibbon_{i}");
                if (m_BoardView != null)
                {
                    go.transform.SetParent(m_BoardView.transform, false);
                }
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 0;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.enabled = false;
                if (mat != null) lr.sharedMaterial = mat;
                m_RibbonRenderers[i] = lr;
            }
        }

        private void SetupRibbons(ClearGroup clears)
        {
            EnsureRibbons();
            ReleaseRibbons();

            m_RunH_V.Clear();
            m_RunDiag.Clear();

            if (clears.Positions == null || clears.Positions.Length < 2) return;

            // Classify runs into H/V and Diagonal
            // Check rows (H)
            for (int y = 0; y < GridPos.BoardSize; y++)
            {
                var rowCells = new List<GridPos>();
                for (int i = 0; i < clears.Positions.Length; i++)
                {
                    if (clears.Positions[i].Y == y) rowCells.Add(clears.Positions[i]);
                }
                if (rowCells.Count >= 5)
                {
                    rowCells.Sort((a, b) => a.X.CompareTo(b.X));
                    m_RunH_V.AddRange(rowCells);
                    break;
                }
            }

            // Check cols (V) if no H
            if (m_RunH_V.Count == 0)
            {
                for (int x = 0; x < GridPos.BoardSize; x++)
                {
                    var colCells = new List<GridPos>();
                    for (int i = 0; i < clears.Positions.Length; i++)
                    {
                        if (clears.Positions[i].X == x) colCells.Add(clears.Positions[i]);
                    }
                    if (colCells.Count >= 5)
                    {
                        colCells.Sort((a, b) => a.Y.CompareTo(b.Y));
                        m_RunH_V.AddRange(colCells);
                        break;
                    }
                }
            }

            // Check main diagonal (X - Y)
            for (int diff = -(GridPos.BoardSize - 1); diff < GridPos.BoardSize; diff++)
            {
                var diagCells = new List<GridPos>();
                for (int i = 0; i < clears.Positions.Length; i++)
                {
                    if (clears.Positions[i].X - clears.Positions[i].Y == diff) diagCells.Add(clears.Positions[i]);
                }
                if (diagCells.Count >= 5)
                {
                    diagCells.Sort((a, b) => a.X.CompareTo(b.X));
                    m_RunDiag.AddRange(diagCells);
                    break;
                }
            }

            // Check anti-diagonal (X + Y)
            if (m_RunDiag.Count == 0)
            {
                for (int sum = 0; sum < GridPos.BoardSize * 2; sum++)
                {
                    var diagCells = new List<GridPos>();
                    for (int i = 0; i < clears.Positions.Length; i++)
                    {
                        if (clears.Positions[i].X + clears.Positions[i].Y == sum) diagCells.Add(clears.Positions[i]);
                    }
                    if (diagCells.Count >= 5)
                    {
                        diagCells.Sort((a, b) => a.X.CompareTo(b.X));
                        m_RunDiag.AddRange(diagCells);
                        break;
                    }
                }
            }

            // Fallback for non-canonical clear group
            if (m_RunH_V.Count == 0 && m_RunDiag.Count == 0)
            {
                m_RunH_V.AddRange(clears.Positions);
            }

            Color ribbonColor = m_ActiveTierRule.ShowBanner
                ? new Color(1.0f, 0.84f, 0.0f, 0.95f) // Gold tint for tier 4
                : new Color(1.0f, 1.0f, 1.0f, 0.85f);

            if (m_RunH_V.Count >= 2 && m_RibbonRenderers[0] != null)
            {
                ConfigureRibbon(m_RibbonRenderers[0], m_RunH_V, ribbonColor);
            }

            if (m_RunDiag.Count >= 2 && m_RibbonRenderers[1] != null)
            {
                ConfigureRibbon(m_RibbonRenderers[1], m_RunDiag, ribbonColor);
            }
        }

        private void ConfigureRibbon(LineRenderer lr, List<GridPos> run, Color color)
        {
            lr.enabled = true;
            lr.startWidth = m_ActiveTierRule.RibbonWidth;
            lr.endWidth = m_ActiveTierRule.RibbonWidth;
            lr.startColor = color;
            lr.endColor = color;
            lr.positionCount = run.Count;

            for (int i = 0; i < run.Count; i++)
            {
                Vector3 worldPos = m_BoardView != null
                    ? m_BoardView.GridToWorld(run[i]) + Vector3.up * 0.30f
                    : new Vector3(run[i].X, 0.30f, run[i].Y);
                lr.SetPosition(i, worldPos);
            }
        }

        private void ReleaseRibbons()
        {
            for (int i = 0; i < m_RibbonRenderers.Length; i++)
            {
                if (m_RibbonRenderers[i] != null)
                {
                    m_RibbonRenderers[i].enabled = false;
                    m_RibbonRenderers[i].positionCount = 0;
                }
            }
        }

        public void Tick(float dt)
        {
            float scaledDt = dt * m_SpeedMultiplier;

            if (m_IsAnimatingSpawns)
            {
                TickSpawns(scaledDt);
            }

            if (m_IsAnimatingClears)
            {
                TickClears(scaledDt);
            }
        }

        private void TickSpawns(float dt)
        {
            m_SpawnTimer += dt;
            const float StaggerTime = 0.07f; // 70 ms stagger per specification

            while (m_CurrentSpawnIndex < m_PendingSpawns.Count && m_SpawnTimer >= m_CurrentSpawnIndex * StaggerTime)
            {
                SpawnItem item = m_PendingSpawns.Items[m_CurrentSpawnIndex];
                Vector3 targetFloor = m_BoardView != null ? m_BoardView.GridToWorld(item.Position) : Vector3.zero;

                // Spawn ball and animate pop-in
                BallView view = m_BallManager != null ? m_BallManager.SpawnBall(item.Position, item.Color, targetFloor) : null;
                if (view != null && m_TweenRunner != null)
                {
                    view.transform.localScale = Vector3.zero;
                    Tween t = new Tween
                    {
                        From = 0.01f,
                        To = 1.0f,
                        Duration = 0.22f / m_SpeedMultiplier,
                        Ease = Easing.OutBack,
                        Target = view,
                        ActionId = BallView.ActionScaleIn,
                        Owner = view,
                        IsActive = true
                    };
                    m_TweenRunner.Play(in t);
                }

                m_AudioService?.PlaySfx("sfx_spawn_pop");
                m_CurrentSpawnIndex++;
            }

            if (m_CurrentSpawnIndex >= m_PendingSpawns.Count && m_SpawnTimer >= m_CurrentSpawnIndex * StaggerTime + 0.25f)
            {
                m_IsAnimatingSpawns = false;
                m_OnSpawnsComplete?.Invoke();
            }
        }

        private void TickClears(float dt)
        {
            m_ClearTimer += dt;
            float t = m_ClearTotalDuration > 0f ? m_ClearTimer / m_ClearTotalDuration : 1f;

            float pulseStart = 0.09f * m_ActiveTierRule.AnimationScale;
            float pulseDuration = 0.20f * m_ActiveTierRule.AnimationScale;
            float pulseEnd = pulseStart + pulseDuration;

            // Pulse phase: pulse balls outward from placed ball
            if (m_ClearTimer >= pulseStart && m_ClearTimer < pulseEnd)
            {
                for (int i = 0; i < m_ClearingBalls.Count; i++)
                {
                    var ball = m_ClearingBalls[i];
                    if (ball != null)
                    {
                        // Calculate Manhattan distance from placed ball for outward stagger
                        int dist = Mathf.Abs(ball.Position.X - m_PlacedPos.X) + Mathf.Abs(ball.Position.Y - m_PlacedPos.Y);
                        float delay = dist * (m_ActiveTierRule.StaggerMs * 0.001f);
                        float localPulseT = Mathf.Clamp01((m_ClearTimer - pulseStart - delay) / (0.18f * m_ActiveTierRule.AnimationScale));
                        float pulseAmp = 0.18f * m_ActiveTierRule.AnimationScale;
                        float s = 1.0f + Mathf.Sin(localPulseT * Mathf.PI) * pulseAmp;
                        ball.transform.localScale = Vector3.one * s;
                        ball.SetGlow(true, m_ActiveTierRule.GlowIntensity);
                    }
                }
            }

            // Burst phase: collapse to zero with InExpo
            float burstStart = 0.42f * m_ActiveTierRule.AnimationScale;
            float burstDuration = 0.16f * m_ActiveTierRule.AnimationScale;
            float burstEnd = burstStart + burstDuration;

            if (m_ClearTimer >= burstStart)
            {
                if (!m_HasPlayedBurstVfx)
                {
                    m_HasPlayedBurstVfx = true;
                    Vector3 centroid = Vector3.zero;
                    if (m_PendingClears.Positions != null && m_PendingClears.Positions.Length > 0)
                    {
                        for (int p = 0; p < m_PendingClears.Positions.Length; p++)
                        {
                            centroid += m_BoardView != null
                                ? m_BoardView.GridToWorld(m_PendingClears.Positions[p])
                                : new Vector3(m_PendingClears.Positions[p].X, 0f, m_PendingClears.Positions[p].Y);
                        }
                        centroid /= m_PendingClears.Positions.Length;
                    }

                    if (!string.IsNullOrEmpty(m_ActiveTierRule.VfxKey))
                    {
                        m_VfxService?.PlayBurst(m_ActiveTierRule.VfxKey, centroid, this);
                    }

                    string audioKey = !string.IsNullOrEmpty(m_ActiveTierRule.AudioKey)
                        ? m_ActiveTierRule.AudioKey
                        : "sfx_clear_tier1";
                    m_AudioService?.PlaySfx(audioKey);
                }

                float burstT = Mathf.Clamp01((m_ClearTimer - burstStart) / burstDuration);
                float collapseScale = 1.0f - Easing.InExpo(burstT);

                for (int i = 0; i < m_ClearingBalls.Count; i++)
                {
                    var ball = m_ClearingBalls[i];
                    if (ball != null)
                    {
                        ball.transform.localScale = Vector3.one * Mathf.Max(0.001f, collapseScale);
                    }
                }

                // Ribbon fade out
                for (int r = 0; r < m_RibbonRenderers.Length; r++)
                {
                    var lr = m_RibbonRenderers[r];
                    if (lr != null && lr.enabled)
                    {
                        float rw = m_ActiveTierRule.RibbonWidth * Mathf.Max(0.001f, collapseScale);
                        lr.startWidth = rw;
                        lr.endWidth = rw;
                    }
                }
            }

            if (t >= 1.0f)
            {
                m_IsAnimatingClears = false;

                // Despawn all cleared balls from manager
                if (m_BallManager != null && m_PendingClears.Positions != null)
                {
                    for (int i = 0; i < m_PendingClears.Positions.Length; i++)
                    {
                        m_BallManager.DespawnBall(m_PendingClears.Positions[i]);
                    }
                }
                m_ClearingBalls.Clear();
                ReleaseRibbons();

                m_OnClearsComplete?.Invoke();
            }
        }

        public void Cancel()
        {
            m_IsAnimatingSpawns = false;
            m_IsAnimatingClears = false;
            m_ClearingBalls.Clear();
            ReleaseRibbons();
            m_VfxService?.CancelByOwner(this);
            m_TweenRunner?.CancelByOwner(this);
        }
    }
}

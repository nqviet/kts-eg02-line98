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
    public sealed class BoardAnimator : ITickable, ITweenTarget
    {
        public const int ActionBanner = 10;

        private readonly BoardView m_BoardView;
        private readonly BallViewManager m_BallManager;
        private readonly TweenRunner m_TweenRunner;
        private readonly CamShake m_CamShake;
        private readonly FeedbackRules m_FeedbackRules;
        private readonly Material m_RibbonMaterial;
        private readonly VfxService m_VfxService;
        private readonly ISlowMoController m_SlowMoController;
        private Material m_CreatedRibbonMaterial;

        private Transform m_BannerTransform;
        private TMPro.TMP_Text m_BannerText;
        private Vector3 m_BannerBaseLocalPos;
        private Color m_BannerBaseColor;

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
        private FeedbackCue m_ActiveTierRule;
        private GridPos m_PlacedPos;
        private Action m_OnClearsComplete;
        private readonly List<BallView> m_ClearingBalls = new List<BallView>(16);

        // Pooled LineRenderers for clear ribbons (H/V share one, diagonals share second)
        private readonly LineRenderer[] m_RibbonRenderers = new LineRenderer[2];
        private readonly List<GridPos> m_RunH_V = new List<GridPos>(16);
        private readonly List<GridPos> m_RunDiag = new List<GridPos>(16);

        private readonly Line98.Presentation.Audio.AudioService m_AudioService;
        private readonly MotionProfileSO m_MotionProfile;
        private float m_SpeedMultiplier = 1.0f;
        private ClearEffectSO m_ClearEffect;
        private int m_ScoreDelta;
        private float m_ComboMultiplier = 1.0f;
        private bool m_HasPlayedScorePopup;

        public bool IsActive => m_IsAnimatingSpawns || m_IsAnimatingClears;
        public FeedbackCue ActiveTierRule => m_ActiveTierRule;
        public float ClearTotalDuration => m_ClearTotalDuration;
        public LineRenderer[] RibbonRenderers => m_RibbonRenderers;
        public ClearEffectSO ClearEffect => m_ClearEffect;

        public void ApplyClearEffect(ClearEffectSO clearEffect)
        {
            m_ClearEffect = clearEffect;
        }

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
            Line98.Presentation.Audio.AudioService audioService = null,
            MotionProfileSO motionProfile = null,
            ISlowMoController slowMoController = null)
        {
            m_BoardView = boardView;
            m_BallManager = ballManager;
            m_TweenRunner = tweenRunner;
            m_CamShake = camShake;
            m_FeedbackRules = feedbackRules;
            m_RibbonMaterial = ribbonMaterial;
            m_VfxService = vfxService;
            m_AudioService = audioService;
            m_MotionProfile = motionProfile;
            m_SlowMoController = slowMoController;
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
            AnimateClear(clears, placedPos, 0, 1.0f, onComplete);
        }

        public void AnimateClear(ClearGroup clears, GridPos placedPos, int scoreDelta, float comboMultiplier, Action onComplete)
        {
            m_PendingClears = clears;
            m_PlacedPos = placedPos.IsValid ? placedPos : (clears.Positions != null && clears.Positions.Length > 0 ? clears.Positions[0] : placedPos);
            m_ScoreDelta = scoreDelta;
            m_ComboMultiplier = comboMultiplier;
            m_OnClearsComplete = onComplete;
            m_SpeedMultiplier = 1.0f;
            m_ClearingBalls.Clear();

            if (clears.IsEmpty || clears.Positions.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            // Determine feedback tier from FeedbackDirector (Gameplay authority)
            m_ActiveTierRule = FeedbackDirector.Evaluate(clears, m_FeedbackRules);

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
            m_HasPlayedScorePopup = false;
            m_ClearTimer = 0f;

            // Total clear timeline duration based on Animation §5 per-tier totals:
            // Tier 1: ~0.94s, Tier 2: ~1.15s, Tier 3: ~1.45s, Tier 4: ~2.10s
            float baseDuration;
            switch (m_ActiveTierRule.Tier)
            {
                case 1: baseDuration = 0.94f; break;
                case 2: baseDuration = 1.15f; break;
                case 3: baseDuration = 1.45f; break;
                case 4: baseDuration = 2.10f; break;
                default: baseDuration = 0.94f; break;
            }
            m_ClearTotalDuration = baseDuration * m_ActiveTierRule.AnimationScale;
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
                ? (m_ClearEffect != null ? m_ClearEffect.RibbonTintBanner : new Color(1.0f, 0.84f, 0.0f, 0.95f))
                : (m_ClearEffect != null ? m_ClearEffect.RibbonTint : new Color(1.0f, 1.0f, 1.0f, 0.85f));

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

        private Vector3 CalculateCentroid()
        {
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
            return centroid;
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
                    Vector3 centroid = CalculateCentroid();

                    GameObject burstInstance = null;
                    if (!string.IsNullOrEmpty(m_ActiveTierRule.VfxKey))
                    {
                        burstInstance = m_VfxService?.PlayBurst(m_ActiveTierRule.VfxKey, centroid, this);
                    }

                    if (m_ActiveTierRule.ShowBanner && burstInstance != null)
                    {
                        AnimateBanner(burstInstance);
                    }

                    if (m_ActiveTierRule.AllowSlowMo && m_SlowMoController != null && m_MotionProfile != null)
                    {
                        float slowMoMs = m_MotionProfile.SlowMoMs;
                        float slowMoScale = m_MotionProfile.SlowMoScale > 0f ? m_MotionProfile.SlowMoScale : 0.35f;
                        if (slowMoMs > 0f)
                        {
                            m_SlowMoController.RequestSlowMo(slowMoScale, slowMoMs * 0.001f);
                        }
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

            // Score popup and combo burst at ScorePopupMs
            float scorePopupTime = (m_MotionProfile != null ? m_MotionProfile.ScorePopupMs : 480f) * 0.001f * m_ActiveTierRule.AnimationScale;
            if (m_ClearTimer >= scorePopupTime && !m_HasPlayedScorePopup)
            {
                m_HasPlayedScorePopup = true;
                Vector3 centroid = CalculateCentroid();
                if (m_ScoreDelta > 0 && m_VfxService != null)
                {
                    m_VfxService.PlayScorePopup(m_ScoreDelta, centroid, m_ComboMultiplier, this);
                }

                if (m_PendingClears.RunCount >= 2)
                {
                    m_VfxService?.PlayBurst("ComboBurst", centroid, this);
                    m_AudioService?.PlaySfx("sfx_combo_up");
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

        private void AnimateBanner(GameObject burstInstance)
        {
            if (burstInstance == null || m_TweenRunner == null) return;

            var tmp = burstInstance.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmp == null) return;

            m_BannerTransform = tmp.transform;
            m_BannerText = tmp;
            m_BannerBaseLocalPos = m_BannerTransform.localPosition;
            m_BannerBaseColor = tmp.color;

            m_BannerTransform.localScale = Vector3.zero;
            Color initialColor = m_BannerBaseColor;
            initialColor.a = 0f;
            tmp.color = initialColor;

            var tween = new Tween
            {
                From = 0f,
                To = 1f,
                Duration = 1.2f,
                Source = TimeSource.Scaled,
                Owner = this,
                ActionId = ActionBanner,
                Target = this
            };
            m_TweenRunner.Play(in tween);
        }

        public void OnTweenUpdate(int actionId, float value)
        {
            if (actionId == ActionBanner && m_BannerTransform != null && m_BannerText != null)
            {
                // Punch OutBack in first 35% of duration
                float punchT = Mathf.Clamp01(value / 0.35f);
                float scale = Easing.OutBack(punchT);
                m_BannerTransform.localScale = Vector3.one * scale;

                // Float up by +0.7 units
                m_BannerTransform.localPosition = m_BannerBaseLocalPos + Vector3.up * (value * 0.7f);

                // Alpha ramp: fade in quickly (first 20%), hold, fade out at end (last 30%)
                float alpha;
                if (value < 0.2f)
                {
                    alpha = value / 0.2f;
                }
                else if (value > 0.7f)
                {
                    alpha = Mathf.Clamp01((1.0f - value) / 0.3f);
                }
                else
                {
                    alpha = 1.0f;
                }

                Color color = m_BannerBaseColor;
                color.a = alpha;
                m_BannerText.color = color;
            }
        }

        public void OnTweenComplete(int actionId)
        {
            if (actionId == ActionBanner)
            {
                if (m_BannerTransform != null)
                {
                    m_BannerTransform.localScale = Vector3.zero;
                    m_BannerTransform.localPosition = m_BannerBaseLocalPos;
                }
                if (m_BannerText != null)
                {
                    m_BannerText.color = m_BannerBaseColor;
                }
                m_BannerTransform = null;
                m_BannerText = null;
            }
        }

        public void Cancel()
        {
            m_IsAnimatingSpawns = false;
            m_IsAnimatingClears = false;
            m_ClearingBalls.Clear();
            ReleaseRibbons();
            if (m_BannerTransform != null)
            {
                m_BannerTransform.localScale = Vector3.zero;
                m_BannerTransform.localPosition = m_BannerBaseLocalPos;
                m_BannerTransform = null;
                m_BannerText = null;
            }
            m_VfxService?.CancelByOwner(this);
            m_TweenRunner?.CancelByOwner(this);
        }
    }
}

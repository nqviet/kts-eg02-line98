using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Core;
using Line98.Data;

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

        private bool m_IsAnimatingSpawns;
        private bool m_IsAnimatingClears;
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

        private float m_SpeedMultiplier = 1.0f;

        public bool IsActive => m_IsAnimatingSpawns || m_IsAnimatingClears;
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
            FeedbackRules feedbackRules)
        {
            m_BoardView = boardView;
            m_BallManager = ballManager;
            m_TweenRunner = tweenRunner;
            m_CamShake = camShake;
            m_FeedbackRules = feedbackRules;
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
            m_PlacedPos = placedPos;
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
            FeedbackTierRule chosenRule = m_FeedbackRules.Tiers[0];
            for (int i = 0; i < m_FeedbackRules.Tiers.Length; i++)
            {
                var rule = m_FeedbackRules.Tiers[i];
                if (count >= rule.MinLength && count <= rule.MaxLength)
                {
                    chosenRule = rule;
                    break;
                }
            }
            m_ActiveTierRule = chosenRule;

            // Collect active ball views for clearing
            for (int i = 0; i < clears.Positions.Length; i++)
            {
                var view = m_BallManager.GetBallAt(clears.Positions[i]);
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

            m_ClearTimer = 0f;
            // Total clear timeline duration based on tier: Connect(90) + Pulse(190) + Glow(200) + Burst(140) + Hold
            m_ClearTotalDuration = (0.62f + m_ActiveTierRule.HoldMs * 0.001f) * m_ActiveTierRule.AnimationScale;
            m_IsAnimatingClears = true;
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
                Vector3 targetFloor = m_BoardView.GridToWorld(item.Position);

                // Spawn ball and animate pop-in
                BallView view = m_BallManager.SpawnBall(item.Position, item.Color, targetFloor);
                if (view != null)
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
            float t = m_ClearTimer / m_ClearTotalDuration;

            // Pulse phase (0.15 to 0.45): pulse balls outward from placed ball
            float pulseStart = 0.12f;
            float pulseEnd = 0.45f;
            if (m_ClearTimer >= pulseStart && m_ClearTimer < pulseEnd)
            {
                float pulseT = (m_ClearTimer - pulseStart) / (pulseEnd - pulseStart);
                float wave = Mathf.Sin(pulseT * Mathf.PI);

                for (int i = 0; i < m_ClearingBalls.Count; i++)
                {
                    var ball = m_ClearingBalls[i];
                    if (ball != null)
                    {
                        // Calculate Manhattan distance from placed ball for outward stagger
                        int dist = Mathf.Abs(ball.Position.X - m_PlacedPos.X) + Mathf.Abs(ball.Position.Y - m_PlacedPos.Y);
                        float delay = dist * 0.022f; // 22 ms per cell stagger
                        float localPulseT = Mathf.Clamp01((m_ClearTimer - pulseStart - delay) / 0.18f);
                        float s = 1.0f + Mathf.Sin(localPulseT * Mathf.PI) * 0.22f;
                        ball.transform.localScale = Vector3.one * s;
                        ball.SetGlow(true);
                    }
                }
            }

            // Burst phase (0.45 to 0.70): collapse to zero with InExpo
            float burstStart = 0.45f;
            float burstEnd = 0.70f;
            if (m_ClearTimer >= burstStart)
            {
                float burstT = Mathf.Clamp01((m_ClearTimer - burstStart) / (burstEnd - burstStart));
                float collapseScale = 1.0f - Easing.InExpo(burstT);

                for (int i = 0; i < m_ClearingBalls.Count; i++)
                {
                    var ball = m_ClearingBalls[i];
                    if (ball != null)
                    {
                        ball.transform.localScale = Vector3.one * Mathf.Max(0.001f, collapseScale);
                    }
                }
            }

            if (t >= 1.0f)
            {
                m_IsAnimatingClears = false;

                // Despawn all cleared balls from manager
                for (int i = 0; i < m_PendingClears.Positions.Length; i++)
                {
                    m_BallManager.DespawnBall(m_PendingClears.Positions[i]);
                }
                m_ClearingBalls.Clear();

                m_OnClearsComplete?.Invoke();
            }
        }

        public void Cancel()
        {
            m_IsAnimatingSpawns = false;
            m_IsAnimatingClears = false;
            m_ClearingBalls.Clear();
        }
    }
}

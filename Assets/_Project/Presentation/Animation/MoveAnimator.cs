using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Core;
using Line98.Data;
using Line98.Presentation.Vfx;

namespace Line98.Presentation.Animation
{
    /// <summary>
    /// Animates ball movement along a grid path:
    /// - Anticipation pull-back (-8% reverse hop, 40 ms)
    /// - Segmented flight with Catmull-Rom arc hops (42-85 ms per step)
    /// - Landing soft bounce and volume-preserving squash (160 ms DampedSine)
    /// Guarantees 0 B GC allocation per flight using cached waypoint buffers.
    /// </summary>
    public sealed class MoveAnimator : ITickable
    {
        private const int MaxWaypoints = 32;

        private readonly BoardView m_BoardView;
        private readonly BallViewManager m_BallManager;
        private readonly TweenRunner m_TweenRunner;
        private readonly MotionProfileSO m_MotionProfile;
        private readonly VfxService m_VfxService;
        private readonly Line98.Presentation.Audio.AudioService m_AudioService;

        private readonly Vector3[] m_WaypointBuffer = new Vector3[MaxWaypoints];
        private int m_WaypointCount;
        private BallView m_ActiveFlightBall;
        private Action m_OnLandingCallback;
        private Action m_OnCompleteCallback;

        private float m_StepDuration = 0.06f;
        private float m_FlightElapsed;
        private float m_TotalFlightDuration;
        private float m_SpeedMultiplier = 1.0f;
        private bool m_IsFlying;

        // Anticipation pull-back state
        private bool m_IsAnticipating;
        private float m_AnticipationElapsed;
        private float m_AnticipationDuration = 0.04f;
        private Vector3 m_AnticipationOrigin;
        private Vector3 m_AnticipationPullBack;

        // Landing bounce state
        private bool m_IsLanding;
        private float m_LandingElapsed;
        private float m_LandingDuration = 0.16f;
        private Vector3 m_LandingWorldPos;

        public bool IsActive => m_IsAnticipating || m_IsFlying || m_IsLanding;
        public bool IsAnticipating => m_IsAnticipating;
        public MotionProfileSO Profile => m_MotionProfile;
        public float StepDuration => m_StepDuration;
        public float TotalFlightDuration => m_TotalFlightDuration;
        public float LandingDuration => m_LandingDuration;
        public int WaypointCount => m_WaypointCount;

        public float SpeedMultiplier
        {
            get => m_SpeedMultiplier;
            set => m_SpeedMultiplier = Mathf.Max(1.0f, value);
        }

        public MoveAnimator(
            BoardView boardView,
            BallViewManager ballManager,
            TweenRunner tweenRunner,
            MotionProfileSO motionProfile = null,
            VfxService vfxService = null,
            Line98.Presentation.Audio.AudioService audioService = null)
        {
            m_BoardView = boardView;
            m_BallManager = ballManager;
            m_TweenRunner = tweenRunner;
            m_MotionProfile = motionProfile ?? MotionProfileSO.Default;
            m_LandingDuration = m_MotionProfile.LandingMs * 0.001f * m_MotionProfile.AnimationScale;
            m_VfxService = vfxService;
            m_AudioService = audioService;
        }

        public void AnimateMove(
            GridPos from,
            GridPos to,
            List<GridPos> path,
            Action onLanding,
            Action onComplete)
        {
            m_OnLandingCallback = onLanding;
            m_OnCompleteCallback = onComplete;
            m_SpeedMultiplier = 1.0f;

            m_ActiveFlightBall = m_BallManager.GetBallAt(from);
            if (m_ActiveFlightBall == null)
            {
                // Fallback: trigger landing and completion immediately
                onLanding?.Invoke();
                onComplete?.Invoke();
                return;
            }

            // Populate cached waypoint buffer with decimation if path > 12 cells (Animation §6.1 #8)
            m_WaypointCount = 0;
            if (path != null && path.Count > 0)
            {
                int maxWaypoints = Mathf.Clamp(m_MotionProfile.MaxFlightWaypoints, 2, MaxWaypoints);
                if (path.Count > 12)
                {
                    int targetCount = Mathf.Min(maxWaypoints, path.Count);
                    for (int i = 0; i < targetCount; i++)
                    {
                        int idx = Mathf.RoundToInt(i * (path.Count - 1f) / (targetCount - 1f));
                        m_WaypointBuffer[m_WaypointCount++] = m_BoardView.GridToWorld(path[idx]);
                    }
                }
                else
                {
                    int count = Mathf.Min(path.Count, MaxWaypoints);
                    for (int i = 0; i < count; i++)
                    {
                        m_WaypointBuffer[m_WaypointCount++] = m_BoardView.GridToWorld(path[i]);
                    }
                }
            }
            else
            {
                m_WaypointBuffer[m_WaypointCount++] = m_BoardView.GridToWorld(from);
                m_WaypointBuffer[m_WaypointCount++] = m_BoardView.GridToWorld(to);
            }

            // Calculate timing from motion profile: clamp(MoveBaseMs / steps, PerStepMinMs, PerStepMaxMs) * AnimationScale
            int stepCount = Mathf.Max(1, m_WaypointCount - 1);
            float perStepMs = Mathf.Clamp(
                m_MotionProfile.MoveBaseMs / stepCount,
                m_MotionProfile.PerStepMinMs,
                m_MotionProfile.PerStepMaxMs) * m_MotionProfile.AnimationScale;
            m_StepDuration = perStepMs * 0.001f;
            m_TotalFlightDuration = stepCount * m_StepDuration;
            m_LandingDuration = m_MotionProfile.LandingMs * 0.001f * m_MotionProfile.AnimationScale;
            m_FlightElapsed = 0f;

            m_AnticipationDuration = m_MotionProfile.AnticipationMs * 0.001f * m_MotionProfile.AnimationScale;
            m_AnticipationElapsed = 0f;

            if (m_AnticipationDuration > 0.001f && m_WaypointCount >= 2)
            {
                m_IsAnticipating = true;
                m_IsFlying = false;
                m_IsLanding = false;
                m_AnticipationOrigin = m_WaypointBuffer[0];
                Vector3 seg0Direction = (m_WaypointBuffer[1] - m_WaypointBuffer[0]).normalized;
                float cellPitch = m_BoardView != null ? m_BoardView.CellPitch : 1.0f;
                m_AnticipationPullBack = -seg0Direction * (0.08f * cellPitch);
            }
            else
            {
                m_IsAnticipating = false;
                m_IsFlying = true;
                m_IsLanding = false;
            }

            // Relocate spatial reference in manager immediately
            m_BallManager.RelocateBall(from, to);

            // Attach ball trail for flight only (Animation §6.1 #8, T5.4)
            if (m_VfxService != null && m_ActiveFlightBall != null)
            {
                Color tint = GetBallColor(m_ActiveFlightBall.Color);
                m_VfxService.AttachTrail(m_ActiveFlightBall.transform, tint, this);
            }

            m_AudioService?.PlaySfx("sfx_ball_move_flight");
        }

        private static Color GetBallColor(BallColor color)
        {
            return color switch
            {
                BallColor.Red => new Color(0.95f, 0.2f, 0.2f),
                BallColor.Orange => new Color(1.0f, 0.55f, 0.1f),
                BallColor.Yellow => new Color(1.0f, 0.9f, 0.15f),
                BallColor.Green => new Color(0.2f, 0.85f, 0.3f),
                BallColor.Cyan => new Color(0.1f, 0.85f, 0.95f),
                BallColor.Purple => new Color(0.65f, 0.2f, 0.95f),
                BallColor.Blue => new Color(0.2f, 0.4f, 0.95f),
                _ => Color.white
            };
        }

        public void Tick(float dt)
        {
            float scaledDt = dt * m_SpeedMultiplier;

            if (m_IsAnticipating)
            {
                float timeToFinish = Mathf.Max(0f, m_AnticipationDuration - m_AnticipationElapsed);
                if (scaledDt >= timeToFinish)
                {
                    TickAnticipation(timeToFinish);
                    scaledDt -= timeToFinish;
                }
                else
                {
                    TickAnticipation(scaledDt);
                    scaledDt = 0f;
                }
            }

            if (scaledDt > 0f && m_IsFlying)
            {
                float timeToFinish = Mathf.Max(0f, m_TotalFlightDuration - m_FlightElapsed);
                if (scaledDt >= timeToFinish)
                {
                    TickFlight(timeToFinish);
                    scaledDt -= timeToFinish;
                }
                else
                {
                    TickFlight(scaledDt);
                    scaledDt = 0f;
                }
            }

            if (scaledDt > 0f && m_IsLanding)
            {
                TickLanding(scaledDt);
            }
        }

        private void TickAnticipation(float dt)
        {
            m_AnticipationElapsed += dt;
            float t = m_AnticipationDuration > 0f ? Mathf.Clamp01(m_AnticipationElapsed / m_AnticipationDuration) : 1f;

            // Offset −0.08 × CellPitch along reverse of segment 0 and return smoothly
            float curve = Mathf.Sin(t * Mathf.PI);
            if (m_ActiveFlightBall != null)
            {
                m_ActiveFlightBall.transform.position = m_AnticipationOrigin + m_AnticipationPullBack * curve;
            }

            if (t >= 1.0f)
            {
                m_IsAnticipating = false;
                m_IsFlying = true;
                m_FlightElapsed = 0f;
                if (m_ActiveFlightBall != null)
                {
                    m_ActiveFlightBall.transform.position = m_AnticipationOrigin;
                }
            }
        }

        private void TickFlight(float dt)
        {
            m_FlightElapsed += dt;
            float normalizedT = Mathf.Clamp01(m_FlightElapsed / m_TotalFlightDuration);

            if (m_WaypointCount >= 2 && m_ActiveFlightBall != null)
            {
                float totalSegments = m_WaypointCount - 1;
                float currentSegmentFloat = normalizedT * totalSegments;
                int segIndex = Mathf.Min((int)currentSegmentFloat, m_WaypointCount - 2);
                float segT = currentSegmentFloat - segIndex;

                // InOutQuad easing within segment
                float easedSegT = m_MotionProfile.InOutQuad != null
                    ? m_MotionProfile.InOutQuad.Evaluate(segT)
                    : Easing.InOutQuad(segT);

                Vector3 start = m_WaypointBuffer[segIndex];
                Vector3 end = m_WaypointBuffer[segIndex + 1];
                Vector3 currentGround = Vector3.Lerp(start, end, easedSegT);

                // Arc hop: parabolic lift per step (height 0.12u)
                float hopNormalized = m_MotionProfile.Hop != null
                    ? m_MotionProfile.Hop.Evaluate(segT)
                    : Mathf.Sin(easedSegT * Mathf.PI);
                float hopLift = hopNormalized * 0.12f * m_BoardView.CellPitch;

                m_ActiveFlightBall.transform.position = currentGround;
                m_ActiveFlightBall.SetHeightLift(hopLift);
            }

            if (normalizedT >= 1.0f)
            {
                m_IsFlying = false;

                // Reached destination -> invoke landing callback immediately!
                m_LandingWorldPos = m_WaypointBuffer[m_WaypointCount - 1];
                if (m_ActiveFlightBall != null)
                {
                    m_ActiveFlightBall.transform.position = m_LandingWorldPos;
                    m_ActiveFlightBall.SetHeightLift(0f);
                    m_VfxService?.DetachTrail(m_ActiveFlightBall.transform);
                }

                m_VfxService?.PlayBurst("PlacementSettle", m_LandingWorldPos, this);
                m_AudioService?.PlaySfx("sfx_ball_place_settle");
                m_OnLandingCallback?.Invoke();

                // Start landing bounce
                m_IsLanding = true;
                m_LandingElapsed = 0f;
                m_LandingDuration = m_MotionProfile.LandingMs * 0.001f * m_MotionProfile.AnimationScale;
            }
        }

        private void TickLanding(float dt)
        {
            m_LandingElapsed += dt;
            float t = m_LandingDuration > 0f ? m_LandingElapsed / m_LandingDuration : 1f;

            if (t < 1.0f && m_ActiveFlightBall != null)
            {
                // Soft Bounce: DampedSine with A=0.09u, lambda=LandingDecay, omega=LandingFrequency
                float amp = 0.09f * m_BoardView.CellPitch;
                float bounce = Easing.DampedSine(m_LandingElapsed, amp, m_MotionProfile.LandingDecay, m_MotionProfile.LandingFrequency);

                // Positive bounce lifts visual; negative bounce compresses into squash
                if (bounce >= 0f)
                {
                    m_ActiveFlightBall.SetHeightLift(bounce);
                    m_ActiveFlightBall.SetSquash(1f, 1f, 1f);
                }
                else
                {
                    m_ActiveFlightBall.SetHeightLift(0f);
                    float sy = Mathf.Clamp(1f - (Mathf.Abs(bounce) / amp) * 0.35f, 0.65f, 1f);
                    float sHoriz = 1f / Mathf.Sqrt(sy);
                    m_ActiveFlightBall.SetSquash(sHoriz, sy, sHoriz);
                }
            }
            else
            {
                m_IsLanding = false;
                if (m_ActiveFlightBall != null)
                {
                    m_ActiveFlightBall.ResetVisuals();
                }
                m_OnCompleteCallback?.Invoke();
            }
        }

        public void Cancel()
        {
            m_IsAnticipating = false;
            m_IsFlying = false;
            m_IsLanding = false;
            if (m_ActiveFlightBall != null)
            {
                m_VfxService?.DetachTrail(m_ActiveFlightBall.transform);
                m_ActiveFlightBall.ResetVisuals();
            }
            m_VfxService?.CancelByOwner(this);
        }
    }
}

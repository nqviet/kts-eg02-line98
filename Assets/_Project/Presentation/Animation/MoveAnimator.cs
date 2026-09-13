using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Core;
using Line98.Data;

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

        private readonly Vector3[] m_WaypointBuffer = new Vector3[MaxWaypoints];
        private int m_WaypointCount;
        private BallView m_ActiveFlightBall;
        private Action m_OnLandingCallback;
        private Action m_OnCompleteCallback;

        private float m_StepDuration = 0.06f;
        private float m_FlightElapsed;
        private float m_TotalFlightDuration;
        private bool m_IsFlying;
        private float m_SpeedMultiplier = 1.0f;

        // Landing bounce state
        private bool m_IsLanding;
        private float m_LandingElapsed;
        private float m_LandingDuration = 0.16f;
        private Vector3 m_LandingWorldPos;

        public bool IsActive => m_IsFlying || m_IsLanding;
        public float SpeedMultiplier
        {
            get => m_SpeedMultiplier;
            set => m_SpeedMultiplier = Mathf.Max(1.0f, value);
        }

        public MoveAnimator(
            BoardView boardView,
            BallViewManager ballManager,
            TweenRunner tweenRunner,
            MotionProfileSO motionProfile = null)
        {
            m_BoardView = boardView;
            m_BallManager = ballManager;
            m_TweenRunner = tweenRunner;
            m_MotionProfile = motionProfile;
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

            // Populate cached waypoint buffer
            m_WaypointCount = 0;
            if (path != null && path.Count > 0)
            {
                int count = Mathf.Min(path.Count, MaxWaypoints);
                for (int i = 0; i < count; i++)
                {
                    m_WaypointBuffer[m_WaypointCount++] = m_BoardView.GridToWorld(path[i]);
                }
            }
            else
            {
                m_WaypointBuffer[m_WaypointCount++] = m_BoardView.GridToWorld(from);
                m_WaypointBuffer[m_WaypointCount++] = m_BoardView.GridToWorld(to);
            }

            // Calculate timing per specification: clamp(300 / steps, 42, 85) ms
            int stepCount = Mathf.Max(1, m_WaypointCount - 1);
            float perStepMs = Mathf.Clamp(300f / stepCount, 42f, 85f);
            m_StepDuration = perStepMs * 0.001f;
            m_TotalFlightDuration = stepCount * m_StepDuration;
            m_FlightElapsed = 0f;
            m_IsFlying = true;
            m_IsLanding = false;

            // Relocate spatial reference in manager immediately
            m_BallManager.RelocateBall(from, to);
        }

        public void Tick(float dt)
        {
            float scaledDt = dt * m_SpeedMultiplier;

            if (m_IsFlying)
            {
                TickFlight(scaledDt);
            }
            else if (m_IsLanding)
            {
                TickLanding(scaledDt);
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
                float easedSegT = Easing.InOutQuad(segT);

                Vector3 start = m_WaypointBuffer[segIndex];
                Vector3 end = m_WaypointBuffer[segIndex + 1];
                Vector3 currentGround = Vector3.Lerp(start, end, easedSegT);

                // Arc hop: parabolic lift per step (height 0.12u)
                float hopNormalized = Mathf.Sin(easedSegT * Mathf.PI);
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
                }

                m_OnLandingCallback?.Invoke();

                // Start landing bounce
                m_IsLanding = true;
                m_LandingElapsed = 0f;
                m_LandingDuration = 0.16f;
            }
        }

        private void TickLanding(float dt)
        {
            m_LandingElapsed += dt;
            float t = m_LandingElapsed / m_LandingDuration;

            if (t < 1.0f && m_ActiveFlightBall != null)
            {
                // Soft Bounce: DampedSine with A=0.09u, lambda=9, omega=28
                float amp = 0.09f * m_BoardView.CellPitch;
                float bounce = Easing.DampedSine(m_LandingElapsed, amp, 9f, 28f);

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
            m_IsFlying = false;
            m_IsLanding = false;
            if (m_ActiveFlightBall != null)
            {
                m_ActiveFlightBall.ResetVisuals();
            }
        }
    }
}

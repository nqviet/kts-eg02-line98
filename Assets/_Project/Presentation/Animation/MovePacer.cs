using System;
using UnityEngine;
using Line98.Core;
using Line98.Data;
using Line98.Gameplay;

namespace Line98.Presentation.Animation
{
    /// <summary>
    /// Paces move execution between Gameplay and Presentation.
    /// Crucially invokes commitCallback AT THE LANDING FRAME, not at timeline end,
    /// so the model becomes authoritative while cosmetic tails (clears, spawns) finish.
    /// Provides 3x fast-forward on pointer tap during tails.
    /// </summary>
    public sealed class MovePacer : IMovePacer, ITickable
    {
        private readonly MoveAnimator m_MoveAnimator;
        private readonly BoardAnimator m_BoardAnimator;
        private readonly InputRouter m_InputRouter;
        private readonly PathPreviewView m_PathPreviewView;
        private readonly MotionProfileSO m_MotionProfile;

        private MovePlan m_CurrentPlan;
        private Action m_PendingCommit;
        private bool m_IsPacing;
        private float m_PathPreviewDelay;
        private float m_DestFadeDelay;

        public bool IsPacing => m_IsPacing;

        public MovePacer(
            MoveAnimator moveAnimator,
            BoardAnimator boardAnimator,
            InputRouter inputRouter,
            PathPreviewView pathPreviewView = null,
            MotionProfileSO motionProfile = null)
        {
            m_MoveAnimator = moveAnimator;
            m_BoardAnimator = boardAnimator;
            m_InputRouter = inputRouter;
            m_PathPreviewView = pathPreviewView;
            m_MotionProfile = motionProfile;

            if (m_InputRouter != null)
            {
                m_InputRouter.OnFastForwardRequested += HandleFastForwardRequested;
            }
        }

        public void Play(MovePlan plan, Action commitCallback)
        {
            m_CurrentPlan = plan;
            m_PendingCommit = commitCallback;
            m_IsPacing = true;

            // Lock user input during motion
            m_InputRouter?.LockInput();

            float previewSeconds = (m_MotionProfile != null ? m_MotionProfile.PathPreviewMs : 140f) * 0.001f;
            if (previewSeconds > 0.001f && m_PathPreviewView != null && plan.Path != null && plan.Path.Count > 1)
            {
                m_PathPreviewDelay = previewSeconds;
                // Hold of 0 means "no self-expiry": the pacer owns the indicator's lifetime, so it
                // survives the route dwell and stays on the target cell for the whole flight.
                m_PathPreviewView.ShowPath(plan.Path, 0f, this);
            }
            else
            {
                m_PathPreviewDelay = 0f;
                StartFlight();
            }
        }

        private void StartFlight()
        {
            if (m_CurrentPlan == null) return;

            // The route has been read; from here the destination indicator carries the intent.
            m_PathPreviewView?.HidePath();

            m_MoveAnimator.AnimateMove(
                m_CurrentPlan.From,
                m_CurrentPlan.To,
                m_CurrentPlan.Path,
                onLanding: OnBallLanded,
                onComplete: OnFlightSettleComplete);

            ScheduleDestinationFade();
        }

        /// <summary>
        /// Starts the destination fade so it completes just as the ball arrives, instead of the ball
        /// landing on top of a lit indicator. Flight duration is only known once AnimateMove has
        /// planned the waypoints, so this has to run after it.
        /// </summary>
        private void ScheduleDestinationFade()
        {
            if (m_PathPreviewView == null || m_MoveAnimator == null)
            {
                m_DestFadeDelay = 0f;
                return;
            }

            float fadeSeconds = m_PathPreviewView.FadeDurationSeconds;
            m_DestFadeDelay = Mathf.Max(0f, m_MoveAnimator.TotalFlightDuration - fadeSeconds);

            // Flight shorter than the fade: fade immediately rather than overshoot the landing.
            if (m_DestFadeDelay <= 0f)
            {
                m_PathPreviewView.FadeOut();
            }
        }

        private void OnBallLanded()
        {
            // CRITICAL: Model commit happens right on landing frame
            if (m_PendingCommit != null)
            {
                var callback = m_PendingCommit;
                m_PendingCommit = null;
                callback.Invoke();
            }
        }

        private void OnFlightSettleComplete()
        {
            if (m_CurrentPlan == null)
            {
                FinishPacing();
                return;
            }

            if (!m_CurrentPlan.Cleared.IsEmpty)
            {
                // Clear sequence (4-tier pulse, glow, burst)
                m_BoardAnimator.AnimateClear(
                    m_CurrentPlan.Cleared,
                    m_CurrentPlan.To,
                    m_CurrentPlan.ScoreDelta,
                    m_CurrentPlan.ComboMultiplier,
                    onComplete: FinishPacing);
            }
            else if (!m_CurrentPlan.Spawned.IsEmpty && m_CurrentPlan.Spawned.Count > 0)
            {
                // Spawn sequence (3 balls arriving at destinations)
                m_BoardAnimator.AnimateSpawns(
                    m_CurrentPlan.Spawned,
                    onComplete: FinishPacing);
            }
            else
            {
                FinishPacing();
            }
        }

        private void FinishPacing()
        {
            m_IsPacing = false;
            m_CurrentPlan = null;
            m_PendingCommit = null;
            m_PathPreviewDelay = 0f;
            m_DestFadeDelay = 0f;
            m_PathPreviewView?.CancelByOwner(this);

            if (m_MoveAnimator != null) m_MoveAnimator.SpeedMultiplier = 1.0f;
            if (m_BoardAnimator != null) m_BoardAnimator.SpeedMultiplier = 1.0f;

            m_InputRouter?.UnlockInput();
        }

        /// <summary>
        /// Aborts pacing without committing, releasing input and resetting speed multipliers.
        /// Used when the session state is replaced wholesale mid-flight.
        /// </summary>
        public void CancelPacing()
        {
            if (!m_IsPacing) return;

            // Never commit a plan whose visuals were discarded
            FinishPacing();
        }

        private void HandleFastForwardRequested()
        {
            if (m_IsPacing)
            {
                if (m_PathPreviewDelay > 0f)
                {
                    m_PathPreviewDelay = 0f;
                    StartFlight();
                }

                // Skilled player tapped during tail -> fast-forward at 3x
                if (m_MoveAnimator != null) m_MoveAnimator.SpeedMultiplier = 3.0f;
                if (m_BoardAnimator != null) m_BoardAnimator.SpeedMultiplier = 3.0f;
            }
        }

        public void Tick(float dt)
        {
            if (m_PathPreviewDelay > 0f)
            {
                m_PathPreviewDelay -= dt;
                if (m_PathPreviewDelay <= 0f)
                {
                    m_PathPreviewDelay = 0f;
                    StartFlight();
                }
            }
            else if (m_DestFadeDelay > 0f)
            {
                m_DestFadeDelay -= dt;
                if (m_DestFadeDelay <= 0f)
                {
                    m_DestFadeDelay = 0f;
                    m_PathPreviewView?.FadeOut();
                }
            }

            m_MoveAnimator?.Tick(dt);
            m_BoardAnimator?.Tick(dt);
        }
    }
}

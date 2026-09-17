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
                m_PathPreviewView.ShowPath(plan.Path, previewSeconds, this);
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

            m_MoveAnimator.AnimateMove(
                m_CurrentPlan.From,
                m_CurrentPlan.To,
                m_CurrentPlan.Path,
                onLanding: OnBallLanded,
                onComplete: OnFlightSettleComplete);
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
                    m_PathPreviewView?.Hide();
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
                    m_PathPreviewView?.Hide();
                    StartFlight();
                }
            }

            m_MoveAnimator?.Tick(dt);
            m_BoardAnimator?.Tick(dt);
        }
    }
}

using System;
using UnityEngine;
using Line98.Core;
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

        private MovePlan m_CurrentPlan;
        private Action m_PendingCommit;
        private bool m_IsPacing;

        public bool IsPacing => m_IsPacing;

        public MovePacer(
            MoveAnimator moveAnimator,
            BoardAnimator boardAnimator,
            InputRouter inputRouter)
        {
            m_MoveAnimator = moveAnimator;
            m_BoardAnimator = boardAnimator;
            m_InputRouter = inputRouter;

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

            // Start anticipation and flight
            m_MoveAnimator.AnimateMove(
                plan.From,
                plan.To,
                plan.Path,
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

            if (m_MoveAnimator != null) m_MoveAnimator.SpeedMultiplier = 1.0f;
            if (m_BoardAnimator != null) m_BoardAnimator.SpeedMultiplier = 1.0f;

            m_InputRouter?.UnlockInput();
        }

        private void HandleFastForwardRequested()
        {
            if (m_IsPacing)
            {
                // Skilled player tapped during tail -> fast-forward at 3x
                if (m_MoveAnimator != null) m_MoveAnimator.SpeedMultiplier = 3.0f;
                if (m_BoardAnimator != null) m_BoardAnimator.SpeedMultiplier = 3.0f;
            }
        }

        public void Tick(float dt)
        {
            m_MoveAnimator?.Tick(dt);
            m_BoardAnimator?.Tick(dt);
        }
    }
}

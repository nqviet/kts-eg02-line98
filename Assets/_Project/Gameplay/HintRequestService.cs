using System;
using Line98.Core;

namespace Line98.Gameplay
{
    public enum HintDenialReason : byte
    {
        None,
        NotPlaying,
        ModeDisallows,
        NoLegalMove,
        RewardUnavailable,
        RewardCancelled
    }

    public readonly struct HintRequest
    {
        public readonly IGameModeStrategy Mode;
        public readonly GamePhase Phase;

        public HintRequest(IGameModeStrategy mode, GamePhase phase)
        {
            Mode = mode;
            Phase = phase;
        }
    }

    public readonly struct HintOutcome
    {
        public readonly bool Success;
        public readonly HintDenialReason Denial;
        public readonly HintSuggestion Suggestion;

        public HintOutcome(bool success, HintDenialReason denial, HintSuggestion suggestion)
        {
            Success = success;
            Denial = denial;
            Suggestion = suggestion;
        }

        public static HintOutcome Denied(HintDenialReason reason) => new HintOutcome(false, reason, default);
    }

    public sealed class HintRequestService
    {
        private readonly IAdGate m_Gate;
        private readonly int m_FreePerRun;
        private int m_FreeHintsRemaining;
        private HintDenialReason m_LastDenial;

        public int FreeHintsRemaining => m_FreeHintsRemaining;
        public HintDenialReason LastDenial => m_LastDenial;

        public HintRequestService(IAdGate gate = null, int freePerRun = 0)
        {
            m_Gate = gate ?? new AlwaysGrantAdGate();
            m_FreePerRun = freePerRun;
            ResetForNewRun();
        }

        public void ResetForNewRun()
        {
            m_FreeHintsRemaining = m_FreePerRun;
            m_LastDenial = HintDenialReason.None;
        }

        public bool CanRequest(in HintRequest request)
        {
            if (request.Phase != GamePhase.Playing) return false;
            if (request.Mode == null || !request.Mode.HintsAllowed) return false;
            if (m_FreeHintsRemaining > 0) return true;
            return m_Gate.IsRewardAvailable(AdRewardKind.Hint);
        }

        public bool TryRequest(GameSession session, in HintRequest request, out HintSuggestion suggestion)
        {
            suggestion = default;
            if (session == null || session.Board == null)
            {
                m_LastDenial = HintDenialReason.NotPlaying;
                return false;
            }

            if (request.Phase != GamePhase.Playing)
            {
                m_LastDenial = HintDenialReason.NotPlaying;
                return false;
            }

            if (request.Mode == null || !request.Mode.HintsAllowed)
            {
                m_LastDenial = HintDenialReason.ModeDisallows;
                return false;
            }

            if (m_FreeHintsRemaining > 0)
            {
                suggestion = HintService.FindBestMove(session.Board, session.PreviewQueue, request.Mode.GetScoreRules());
                if (suggestion.Tier == HintTier.None)
                {
                    m_LastDenial = HintDenialReason.NoLegalMove;
                    return false;
                }

                m_FreeHintsRemaining--;
                m_LastDenial = HintDenialReason.None;
                return true;
            }

            if (!m_Gate.IsRewardAvailable(AdRewardKind.Hint))
            {
                m_LastDenial = HintDenialReason.RewardUnavailable;
                return false;
            }

            bool granted = false;
            m_Gate.RequestReward(AdRewardKind.Hint, success => granted = success);

            if (granted)
            {
                suggestion = HintService.FindBestMove(session.Board, session.PreviewQueue, request.Mode.GetScoreRules());
                if (suggestion.Tier == HintTier.None)
                {
                    m_LastDenial = HintDenialReason.NoLegalMove;
                    return false;
                }

                m_LastDenial = HintDenialReason.None;
                return true;
            }

            m_LastDenial = HintDenialReason.RewardCancelled;
            return false;
        }

        public void TryRequest(GameSession session, in HintRequest request, Action<HintOutcome> onComplete)
        {
            if (TryRequest(session, in request, out var suggestion))
            {
                onComplete?.Invoke(new HintOutcome(true, HintDenialReason.None, suggestion));
            }
            else
            {
                onComplete?.Invoke(HintOutcome.Denied(m_LastDenial));
            }
        }
    }
}

using System;
using Line98.Core;

namespace Line98.Gameplay
{
    public enum UndoDenialReason : byte
    {
        None,
        NotPlaying,
        NothingToUndo,
        ModeDisallows,
        FreeExhausted,
        AdUnavailable,
        AdCancelled
    }

    public readonly struct UndoResult
    {
        public readonly bool Success;
        public readonly UndoDenialReason Denial;
        public readonly bool UsedFreeUndo;
        public readonly bool UsedRewardedUndo;
        public readonly int FreeUndosRemaining;
        public readonly int RewardedUndosRemaining;
        public readonly GameSnapshot Restored;

        public UndoResult(
            bool success,
            UndoDenialReason denial,
            bool usedFreeUndo,
            bool usedRewardedUndo,
            int freeUndosRemaining,
            int rewardedUndosRemaining,
            GameSnapshot restored)
        {
            Success = success;
            Denial = denial;
            UsedFreeUndo = usedFreeUndo;
            UsedRewardedUndo = usedRewardedUndo;
            FreeUndosRemaining = freeUndosRemaining;
            RewardedUndosRemaining = rewardedUndosRemaining;
            Restored = restored;
        }

        public static UndoResult Denied(UndoDenialReason reason, int freeRemaining, int rewardedRemaining)
        {
            return new UndoResult(false, reason, false, false, freeRemaining, rewardedRemaining, null);
        }
    }

    public readonly struct UndoAvailability
    {
        public readonly GamePhase Phase;
        public readonly bool ModeAllowsUndo;
        public readonly bool HasSnapshot;
        public readonly bool UnlimitedUndo;

        public UndoAvailability(GamePhase phase, bool modeAllowsUndo, bool hasSnapshot, bool unlimitedUndo = false)
        {
            Phase = phase;
            ModeAllowsUndo = modeAllowsUndo;
            HasSnapshot = hasSnapshot;
            UnlimitedUndo = unlimitedUndo;
        }
    }

    public readonly struct UndoRules
    {
        public readonly int FreeCount;
        public readonly int MaxRewarded;

        public UndoRules(int freeCount = 3, int maxRewarded = 3)
        {
            FreeCount = freeCount;
            MaxRewarded = maxRewarded;
        }

        public static UndoRules Default => new UndoRules(3, 3);
    }

    public sealed class UndoService
    {
        private readonly UndoRules m_Rules;
        private readonly IAdGate m_Gate;

        private int m_FreeUndosRemaining;
        private int m_RewardedUndosRemaining;

        public event Action<UndoResult> OnUndoResolved;

        public int FreeAllowance => m_Rules.FreeCount;
        public int MaxRewarded => m_Rules.MaxRewarded;
        public int FreeUndosRemaining => m_FreeUndosRemaining;
        public int RewardedUndosRemaining => m_RewardedUndosRemaining;

        public UndoService(UndoRules? rules = null, IAdGate gate = null)
        {
            m_Rules = rules ?? UndoRules.Default;
            m_Gate = gate ?? new AlwaysGrantAdGate();
            ResetForNewRun();
        }

        public void ResetForNewRun()
        {
            m_FreeUndosRemaining = m_Rules.FreeCount;
            m_RewardedUndosRemaining = m_Rules.MaxRewarded;
        }

        public void LoadState(int freeRemaining, int rewardedUsed)
        {
            m_FreeUndosRemaining = Math.Max(0, freeRemaining);
            m_RewardedUndosRemaining = Math.Max(0, m_Rules.MaxRewarded - rewardedUsed);
        }

        public bool CanUndo(in UndoAvailability ctx)
        {
            if (ctx.Phase != GamePhase.Playing) return false;
            if (!ctx.ModeAllowsUndo || !ctx.HasSnapshot) return false;
            if (ctx.UnlimitedUndo) return true;

            return m_FreeUndosRemaining > 0 || (m_RewardedUndosRemaining > 0 && m_Gate.IsRewardAvailable(AdRewardKind.Undo));
        }

        public bool RequiresReward(in UndoAvailability ctx)
        {
            if (ctx.UnlimitedUndo) return false;
            return m_FreeUndosRemaining <= 0;
        }

        public UndoResult Request(in UndoAvailability ctx, Func<GameSnapshot> executeRestore)
        {
            if (ctx.Phase == GamePhase.Resolving)
            {
                return EmitResult(UndoResult.Denied(UndoDenialReason.NotPlaying, m_FreeUndosRemaining, m_RewardedUndosRemaining));
            }
            if (ctx.Phase != GamePhase.Playing)
            {
                return EmitResult(UndoResult.Denied(UndoDenialReason.NotPlaying, m_FreeUndosRemaining, m_RewardedUndosRemaining));
            }
            if (!ctx.ModeAllowsUndo)
            {
                return EmitResult(UndoResult.Denied(UndoDenialReason.ModeDisallows, m_FreeUndosRemaining, m_RewardedUndosRemaining));
            }
            if (!ctx.HasSnapshot)
            {
                return EmitResult(UndoResult.Denied(UndoDenialReason.NothingToUndo, m_FreeUndosRemaining, m_RewardedUndosRemaining));
            }

            if (ctx.UnlimitedUndo)
            {
                GameSnapshot snapshot = executeRestore?.Invoke();
                if (snapshot == null)
                {
                    return EmitResult(UndoResult.Denied(UndoDenialReason.NothingToUndo, m_FreeUndosRemaining, m_RewardedUndosRemaining));
                }
                return EmitResult(new UndoResult(true, UndoDenialReason.None, false, false, m_FreeUndosRemaining, m_RewardedUndosRemaining, snapshot));
            }

            if (m_FreeUndosRemaining > 0)
            {
                GameSnapshot snapshot = executeRestore?.Invoke();
                if (snapshot == null)
                {
                    return EmitResult(UndoResult.Denied(UndoDenialReason.NothingToUndo, m_FreeUndosRemaining, m_RewardedUndosRemaining));
                }

                m_FreeUndosRemaining--;
                return EmitResult(new UndoResult(true, UndoDenialReason.None, true, false, m_FreeUndosRemaining, m_RewardedUndosRemaining, snapshot));
            }

            if (m_RewardedUndosRemaining <= 0)
            {
                return EmitResult(UndoResult.Denied(UndoDenialReason.FreeExhausted, m_FreeUndosRemaining, m_RewardedUndosRemaining));
            }

            if (!m_Gate.IsRewardAvailable(AdRewardKind.Undo))
            {
                return EmitResult(UndoResult.Denied(UndoDenialReason.AdUnavailable, m_FreeUndosRemaining, m_RewardedUndosRemaining));
            }

            bool granted = false;
            m_Gate.RequestReward(AdRewardKind.Undo, success => granted = success);

            if (granted)
            {
                GameSnapshot snapshot = executeRestore?.Invoke();
                if (snapshot == null)
                {
                    return EmitResult(UndoResult.Denied(UndoDenialReason.NothingToUndo, m_FreeUndosRemaining, m_RewardedUndosRemaining));
                }

                m_RewardedUndosRemaining--;
                return EmitResult(new UndoResult(true, UndoDenialReason.None, false, true, m_FreeUndosRemaining, m_RewardedUndosRemaining, snapshot));
            }

            return EmitResult(UndoResult.Denied(UndoDenialReason.AdCancelled, m_FreeUndosRemaining, m_RewardedUndosRemaining));
        }

        public void Request(in UndoAvailability ctx, Func<GameSnapshot> executeRestore, Action<UndoResult> onComplete)
        {
            if (ctx.Phase == GamePhase.Resolving)
            {
                var denied = UndoResult.Denied(UndoDenialReason.NotPlaying, m_FreeUndosRemaining, m_RewardedUndosRemaining);
                EmitResult(denied);
                onComplete?.Invoke(denied);
                return;
            }
            if (ctx.Phase != GamePhase.Playing)
            {
                var denied = UndoResult.Denied(UndoDenialReason.NotPlaying, m_FreeUndosRemaining, m_RewardedUndosRemaining);
                EmitResult(denied);
                onComplete?.Invoke(denied);
                return;
            }
            if (!ctx.ModeAllowsUndo)
            {
                var denied = UndoResult.Denied(UndoDenialReason.ModeDisallows, m_FreeUndosRemaining, m_RewardedUndosRemaining);
                EmitResult(denied);
                onComplete?.Invoke(denied);
                return;
            }
            if (!ctx.HasSnapshot)
            {
                var denied = UndoResult.Denied(UndoDenialReason.NothingToUndo, m_FreeUndosRemaining, m_RewardedUndosRemaining);
                EmitResult(denied);
                onComplete?.Invoke(denied);
                return;
            }

            if (ctx.UnlimitedUndo)
            {
                GameSnapshot snapshot = executeRestore?.Invoke();
                var result = snapshot != null
                    ? new UndoResult(true, UndoDenialReason.None, false, false, m_FreeUndosRemaining, m_RewardedUndosRemaining, snapshot)
                    : UndoResult.Denied(UndoDenialReason.NothingToUndo, m_FreeUndosRemaining, m_RewardedUndosRemaining);
                EmitResult(result);
                onComplete?.Invoke(result);
                return;
            }

            if (m_FreeUndosRemaining > 0)
            {
                GameSnapshot snapshot = executeRestore?.Invoke();
                if (snapshot != null)
                {
                    m_FreeUndosRemaining--;
                    var res = new UndoResult(true, UndoDenialReason.None, true, false, m_FreeUndosRemaining, m_RewardedUndosRemaining, snapshot);
                    EmitResult(res);
                    onComplete?.Invoke(res);
                }
                else
                {
                    var res = UndoResult.Denied(UndoDenialReason.NothingToUndo, m_FreeUndosRemaining, m_RewardedUndosRemaining);
                    EmitResult(res);
                    onComplete?.Invoke(res);
                }
                return;
            }

            if (m_RewardedUndosRemaining <= 0)
            {
                var res = UndoResult.Denied(UndoDenialReason.FreeExhausted, m_FreeUndosRemaining, m_RewardedUndosRemaining);
                EmitResult(res);
                onComplete?.Invoke(res);
                return;
            }

            if (!m_Gate.IsRewardAvailable(AdRewardKind.Undo))
            {
                var res = UndoResult.Denied(UndoDenialReason.AdUnavailable, m_FreeUndosRemaining, m_RewardedUndosRemaining);
                EmitResult(res);
                onComplete?.Invoke(res);
                return;
            }

            m_Gate.RequestReward(AdRewardKind.Undo, success =>
            {
                if (success)
                {
                    GameSnapshot snapshot = executeRestore?.Invoke();
                    if (snapshot != null)
                    {
                        m_RewardedUndosRemaining--;
                        var res = new UndoResult(true, UndoDenialReason.None, false, true, m_FreeUndosRemaining, m_RewardedUndosRemaining, snapshot);
                        EmitResult(res);
                        onComplete?.Invoke(res);
                    }
                    else
                    {
                        var res = UndoResult.Denied(UndoDenialReason.NothingToUndo, m_FreeUndosRemaining, m_RewardedUndosRemaining);
                        EmitResult(res);
                        onComplete?.Invoke(res);
                    }
                }
                else
                {
                    var res = UndoResult.Denied(UndoDenialReason.AdCancelled, m_FreeUndosRemaining, m_RewardedUndosRemaining);
                    EmitResult(res);
                    onComplete?.Invoke(res);
                }
            });
        }

        private UndoResult EmitResult(UndoResult result)
        {
            OnUndoResolved?.Invoke(result);
            return result;
        }
    }
}

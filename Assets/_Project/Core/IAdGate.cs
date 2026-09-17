using System;

namespace Line98.Core
{
    public enum AdRewardKind : byte
    {
        Undo,
        Continue,
        Hint
    }

    public interface IAdGate
    {
        bool IsRewardAvailable(AdRewardKind kind);
        void RequestReward(AdRewardKind kind, Action<bool> onRewarded);
    }

    /// <summary>
    /// Phase 3 default: grants immediately, no SDK. Used by tests, editor, and offline dev builds.
    /// </summary>
    public sealed class AlwaysGrantAdGate : IAdGate
    {
        public bool IsRewardAvailable(AdRewardKind kind) => true;

        public void RequestReward(AdRewardKind kind, Action<bool> onRewarded)
        {
            onRewarded?.Invoke(true);
        }
    }

    /// <summary>
    /// Offline / build-time default. Rewards degrade gracefully to the free path (base §22).
    /// </summary>
    public sealed class NullAdGate : IAdGate
    {
        public bool IsRewardAvailable(AdRewardKind kind) => false;

        public void RequestReward(AdRewardKind kind, Action<bool> onRewarded)
        {
            onRewarded?.Invoke(false);
        }
    }
}

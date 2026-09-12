using System;

namespace Line98.Services
{
    public interface IAdService
    {
        bool IsInterstitialReady { get; }
        bool IsRewardedReady { get; }
        void ShowInterstitial(Action onClosed);
        void ShowRewarded(Action onRewardEarned, Action onClosed);
    }

    public sealed class NoOpAdService : IAdService
    {
        public bool IsInterstitialReady => false;
        public bool IsRewardedReady => false;

        public void ShowInterstitial(Action onClosed) => onClosed?.Invoke();
        public void ShowRewarded(Action onRewardEarned, Action onClosed)
        {
            onRewardEarned?.Invoke();
            onClosed?.Invoke();
        }
    }
}

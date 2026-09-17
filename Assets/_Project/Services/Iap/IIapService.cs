using System;

namespace Line98.Services
{
    public enum PurchaseState : byte
    {
        Unknown,
        NotOwned,
        Pending,
        Owned,
        Unavailable
    }

    public interface IIapService
    {
        bool HasRemovedAds { get; }
        PurchaseState RemoveAdsState { get; }
        event Action<PurchaseState> OnRemoveAdsStateChanged;
        bool CanRestorePurchases { get; }

        void BuyRemoveAds(Action<bool> onComplete);
        void RestorePurchases(Action<bool> onComplete);
    }

    public interface IPurchaseService : IIapService
    {
    }

    public sealed class EditorStubIapService : IPurchaseService, IIapService
    {
        private bool m_HasRemovedAds;
        private PurchaseState m_RemoveAdsState = PurchaseState.NotOwned;

        public bool HasRemovedAds => m_HasRemovedAds;
        public PurchaseState RemoveAdsState => m_RemoveAdsState;
        public bool CanRestorePurchases => true;

        public event Action<PurchaseState> OnRemoveAdsStateChanged;

        public void BuyRemoveAds(Action<bool> onComplete)
        {
            m_HasRemovedAds = true;
            m_RemoveAdsState = PurchaseState.Owned;
            OnRemoveAdsStateChanged?.Invoke(m_RemoveAdsState);
            onComplete?.Invoke(true);
        }

        public void RestorePurchases(Action<bool> onComplete)
        {
            onComplete?.Invoke(true);
        }
    }
}

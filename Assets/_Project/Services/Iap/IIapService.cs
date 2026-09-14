using System;

namespace Line98.Services
{
    public interface IIapService
    {
        bool HasRemovedAds { get; }
        void BuyRemoveAds(Action<bool> onComplete);
        void RestorePurchases(Action<bool> onComplete);
    }

    public interface IPurchaseService : IIapService
    {
    }

    public sealed class EditorStubIapService : IPurchaseService, IIapService
    {
        private bool m_HasRemovedAds;

        public bool HasRemovedAds => m_HasRemovedAds;

        public void BuyRemoveAds(Action<bool> onComplete)
        {
            m_HasRemovedAds = true;
            onComplete?.Invoke(true);
        }

        public void RestorePurchases(Action<bool> onComplete)
        {
            onComplete?.Invoke(true);
        }
    }
}

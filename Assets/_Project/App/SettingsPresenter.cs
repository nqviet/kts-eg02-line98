using System;
using Line98.Data;
using Line98.Presentation;
using Line98.Presentation.Audio;
using Line98.Services;
using UnityEngine;

namespace Line98.App
{
    /// <summary>Binds the settings view to presentation, purchase, and preference services.</summary>
    public sealed class SettingsPresenter : IDisposable
    {
        private const string VibrationKey = "line98_pref_vibration";
        public const string ReduceEffectsKey = "line98_pref_reduce_fx";
        private const string DefaultPrivacyUrl = "https://line98game.com/privacy";
        private const string DefaultSupportEmail = "mailto:support@line98game.com";

        private static ISettingsService s_ActiveSettingsService;

        private readonly SettingsPopup m_View;
        private readonly AudioService m_AudioService;
        private readonly IIapService m_IapService;
        private readonly UiShell m_UiShell;
        private readonly ISettingsService m_SettingsService;
        private readonly IBrandConfig m_BrandConfig;

        /// <summary>Raised after the Reduce Effects preference is persisted.</summary>
        public event Action<bool> OnReduceEffectsChanged;

        public static bool IsReduceEffectsEnabled =>
            s_ActiveSettingsService != null
                ? s_ActiveSettingsService.Current.ReduceEffects
                : PlayerPrefs.GetInt(ReduceEffectsKey, 0) == 1;

        public SettingsPopup View => m_View;
        public AudioService AudioService => m_AudioService;
        public IIapService IapService => m_IapService;
        public UiShell UiShell => m_UiShell;
        public ISettingsService SettingsService => m_SettingsService;
        public IBrandConfig BrandConfig => m_BrandConfig;

        public SettingsPresenter(
            SettingsPopup view,
            AudioService audioService,
            IIapService iapService,
            UiShell uiShell,
            ISettingsService settingsService = null,
            IBrandConfig brandConfig = null)
        {
            m_View = view;
            m_AudioService = audioService;
            m_IapService = iapService;
            m_UiShell = uiShell;
            m_SettingsService = settingsService;
            m_BrandConfig = brandConfig;

            s_ActiveSettingsService = m_SettingsService;

            InitializeView();
            BindEvents();
        }

        private void InitializeView()
        {
            if (m_View == null) return;

            if (m_SettingsService != null)
            {
                var readModel = m_SettingsService.BuildReadModel(m_IapService, m_BrandConfig);
                m_View.SetInitialStates(
                    readModel.Music,
                    readModel.Sfx,
                    readModel.Vibration,
                    readModel.ReduceEffects,
                    readModel.CurrentLocale.DisplayName,
                    readModel.VersionLabel);
                // TODO(P4.1): restore SetRemoveAdsAvailable(readModel.RemoveAdsState == PurchaseState.NotOwned).
                m_View.SetPurchasesComingSoon();
            }
            else
            {
                bool music = m_AudioService?.IsMusicEnabled ?? true;
                bool sfx = m_AudioService?.IsSfxEnabled ?? true;
                bool vibration = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
                bool reduceEffects = IsReduceEffectsEnabled;
                string version = m_BrandConfig?.VersionLabel ?? Application.version;
                m_View.SetInitialStates(music, sfx, vibration, reduceEffects, "ENGLISH", version);
                // TODO(P4.1): restore SetRemoveAdsAvailable(!m_IapService.HasRemovedAds).
                m_View.SetPurchasesComingSoon();
            }

            // TODO(P5.2): drop this once Localization ships and the locale picker can open (GDD P5.2).
            m_View.SetLanguageComingSoon();
        }

        private void BindEvents()
        {
            if (m_View == null) return;

            m_View.OnBackClicked += HandleBack;
            m_View.OnMusicToggled += HandleMusicToggled;
            m_View.OnSfxToggled += HandleSfxToggled;
            m_View.OnVibrationToggled += HandleVibrationToggled;
            m_View.OnReduceEffectsToggled += HandleReduceEffectsToggled;
            m_View.OnLanguageClicked += HandleLanguageClicked;
            m_View.OnRemoveAdsClicked += HandleRemoveAds;
            m_View.OnRestorePurchasesClicked += HandleRestorePurchases;
            m_View.OnPrivacyPolicyClicked += HandlePrivacyPolicy;
            m_View.OnContactSupportClicked += HandleContactSupport;
        }

        private void HandleBack() => m_UiShell?.PopPopup();

        private void HandleMusicToggled(bool enabled)
        {
            m_SettingsService?.SetMusic(enabled);
            m_AudioService?.SetMusicEnabled(enabled);
        }

        private void HandleSfxToggled(bool enabled)
        {
            m_SettingsService?.SetSfx(enabled);
            m_AudioService?.SetSfxEnabled(enabled);
        }

        private void HandleVibrationToggled(bool enabled)
        {
            m_SettingsService?.SetVibration(enabled);
            PlayerPrefs.SetInt(VibrationKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void HandleReduceEffectsToggled(bool enabled)
        {
            m_SettingsService?.SetReduceEffects(enabled);
            PlayerPrefs.SetInt(ReduceEffectsKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnReduceEffectsChanged?.Invoke(enabled);
        }

        // The three handlers below are deliberately inert. Their controls are grayed out and
        // non-interactable, so they should never fire -- but they must not touch a service even if
        // something re-enables the control, because the only implementations available are stubs
        // that would fake a result (EditorStubIapService instantly "grants" Remove Ads).

        private void HandleLanguageClicked()
        {
            // TODO(P5.2): open the locale picker once Localization ships (GDD P5.2).
        }

        private void HandleRemoveAds()
        {
            // TODO(P4.1): call IIapService.BuyRemoveAds once a real store implementation replaces
            // EditorStubIapService, then re-enable the button via SetRemoveAdsAvailable.
        }

        private void HandleRestorePurchases()
        {
            // TODO(P4.1): same as Remove Ads -- there is no store to restore from yet.
        }

        private void HandlePrivacyPolicy()
        {
            string url = m_BrandConfig?.PrivacyUrl ?? DefaultPrivacyUrl;
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }

        private void HandleContactSupport()
        {
            string url = m_BrandConfig?.SupportUrl ?? m_BrandConfig?.SupportEmail ?? DefaultSupportEmail;
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
        }

        public void Dispose()
        {
            if (s_ActiveSettingsService == m_SettingsService)
            {
                s_ActiveSettingsService = null;
            }

            OnReduceEffectsChanged = null;
            if (m_View == null) return;

            m_View.OnBackClicked -= HandleBack;
            m_View.OnMusicToggled -= HandleMusicToggled;
            m_View.OnSfxToggled -= HandleSfxToggled;
            m_View.OnVibrationToggled -= HandleVibrationToggled;
            m_View.OnReduceEffectsToggled -= HandleReduceEffectsToggled;
            m_View.OnLanguageClicked -= HandleLanguageClicked;
            m_View.OnRemoveAdsClicked -= HandleRemoveAds;
            m_View.OnRestorePurchasesClicked -= HandleRestorePurchases;
            m_View.OnPrivacyPolicyClicked -= HandlePrivacyPolicy;
            m_View.OnContactSupportClicked -= HandleContactSupport;
        }
    }
}

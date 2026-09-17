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
                m_View.SetRemoveAdsAvailable(readModel.RemoveAdsState == PurchaseState.NotOwned);
            }
            else
            {
                bool music = m_AudioService?.IsMusicEnabled ?? true;
                bool sfx = m_AudioService?.IsSfxEnabled ?? true;
                bool vibration = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
                bool reduceEffects = IsReduceEffectsEnabled;
                string version = m_BrandConfig?.VersionLabel ?? Application.version;
                m_View.SetInitialStates(music, sfx, vibration, reduceEffects, "ENGLISH", version);
                m_View.SetRemoveAdsAvailable(!(m_IapService?.HasRemovedAds ?? false));
            }
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

        private void HandleLanguageClicked()
        {
            Debug.Log("[Settings] Language selector requested. Connect a localization provider to handle language changes.");
        }

        private void HandleRemoveAds()
        {
            m_IapService?.BuyRemoveAds(success =>
            {
                if (success) m_View?.SetRemoveAdsAvailable(false);
            });
        }

        private void HandleRestorePurchases()
        {
            m_IapService?.RestorePurchases(success => Debug.Log($"[Settings] Restore purchases completed: {success}."));
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

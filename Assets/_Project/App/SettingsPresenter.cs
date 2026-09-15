using System;
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
        private const string ReduceEffectsKey = "line98_pref_reduce_fx";
        private const string PrivacyUrl = "https://line98game.com/privacy";
        private const string SupportEmail = "mailto:support@line98game.com";

        private readonly SettingsPopup m_View;
        private readonly AudioService m_AudioService;
        private readonly IIapService m_IapService;
        private readonly UiShell m_UiShell;

        public SettingsPopup View => m_View;
        public AudioService AudioService => m_AudioService;
        public IIapService IapService => m_IapService;
        public UiShell UiShell => m_UiShell;

        public SettingsPresenter(SettingsPopup view, AudioService audioService, IIapService iapService, UiShell uiShell)
        {
            m_View = view;
            m_AudioService = audioService;
            m_IapService = iapService;
            m_UiShell = uiShell;

            InitializeView();
            BindEvents();
        }

        private void InitializeView()
        {
            if (m_View == null) return;

            bool music = m_AudioService?.IsMusicEnabled ?? true;
            bool sfx = m_AudioService?.IsSfxEnabled ?? true;
            bool vibration = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            bool reduceEffects = PlayerPrefs.GetInt(ReduceEffectsKey, 0) == 1;
            m_View.SetInitialStates(music, sfx, vibration, reduceEffects, "ENGLISH", Application.version);
            m_View.SetRemoveAdsAvailable(!(m_IapService?.HasRemovedAds ?? false));
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
        private void HandleMusicToggled(bool enabled) => m_AudioService?.SetMusicEnabled(enabled);
        private void HandleSfxToggled(bool enabled) => m_AudioService?.SetSfxEnabled(enabled);

        private static void HandleVibrationToggled(bool enabled)
        {
            PlayerPrefs.SetInt(VibrationKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void HandleReduceEffectsToggled(bool enabled)
        {
            PlayerPrefs.SetInt(ReduceEffectsKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
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

        private static void HandlePrivacyPolicy() => Application.OpenURL(PrivacyUrl);
        private static void HandleContactSupport() => Application.OpenURL(SupportEmail);

        public void Dispose()
        {
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

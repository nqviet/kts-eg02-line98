using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>
    /// In-game Crystal settings popup. All service decisions remain outside the view.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsPopup : UiPopupBase
    {
        [Header("Header")]
        [SerializeField] private Button m_BackButton;

        [Header("Audio & Feedback")]
        [SerializeField] private UiToggle m_MusicToggle;
        [SerializeField] private UiToggle m_SfxToggle;
        [SerializeField] private UiToggle m_VibrationToggle;
        [SerializeField] private UiToggle m_ReduceEffectsToggle;

        [Header("Language")]
        [SerializeField] private UiSettingRow m_LanguageRow;

        [Header("Purchases")]
        [SerializeField] private Button m_RemoveAdsButton;
        [SerializeField] private UiSettingRow m_RestorePurchasesRow;

        [Header("Support")]
        [SerializeField] private UiSettingRow m_PrivacyPolicyRow;
        [SerializeField] private UiSettingRow m_ContactSupportRow;

        [Header("Footer")]
        [SerializeField] private TMP_Text m_VersionText;
        [SerializeField] private UnityEngine.UI.Image[] m_GemSlots = System.Array.Empty<UnityEngine.UI.Image>();

        [Header("Compatibility")]
        [SerializeField] private Button m_CosmeticsButton;

        public event Action OnBackClicked;
        public event Action<bool> OnMusicToggled;
        public event Action<bool> OnSfxToggled;
        public event Action<bool> OnVibrationToggled;
        public event Action<bool> OnReduceEffectsToggled;
        public event Action OnLanguageClicked;
        public event Action OnRemoveAdsClicked;
        public event Action OnRestorePurchasesClicked;
        public event Action OnPrivacyPolicyClicked;
        public event Action OnContactSupportClicked;
        public event Action OnCosmeticsClicked;

        public UiToggle MusicToggle => m_MusicToggle;
        public UiToggle SfxToggle => m_SfxToggle;
        public UiToggle VibrationToggle => m_VibrationToggle;
        public UiToggle ReduceEffectsToggle => m_ReduceEffectsToggle;
        public Button CosmeticsButton => m_CosmeticsButton;
        public UnityEngine.UI.Image[] GemSlots => m_GemSlots;
        public Line98.Data.BallThemeSO BallTheme => m_BallTheme;
        public override bool UsesFullLayoutHeight => true;

        private Line98.Data.UiThemeSO m_UiTheme;
        private Line98.Data.BallThemeSO m_BallTheme;

        protected override void Awake()
        {
            base.Awake();

            BindListeners();
        }

        public void SetInitialStates(bool music, bool sfx, bool haptics, bool reduceEffects, string language, string version)
        {
            m_MusicToggle?.SetState(music, notify: false, animate: false);
            m_SfxToggle?.SetState(sfx, notify: false, animate: false);
            m_VibrationToggle?.SetState(haptics, notify: false, animate: false);
            m_ReduceEffectsToggle?.SetState(reduceEffects, notify: false, animate: false);
            m_LanguageRow?.Configure(null, language, "TIẾNG VIỆT AVAILABLE");
            if (m_VersionText != null) m_VersionText.text = $"VERSION {version}";
        }

        public void SetRemoveAdsAvailable(bool available)
        {
            if (m_RemoveAdsButton != null) m_RemoveAdsButton.interactable = available;
        }

        public void ApplyTheme(Line98.Data.UiThemeSO theme)
        {
            if (theme == null) return;
            m_UiTheme = theme;

            var appliers = GetComponentsInChildren<UiThemeApplier>(true);
            for (int i = 0; i < appliers.Length; i++)
            {
                appliers[i].Apply(theme);
            }

            m_MusicToggle?.ApplyTheme(theme);
            m_SfxToggle?.ApplyTheme(theme);
            m_VibrationToggle?.ApplyTheme(theme);
            m_ReduceEffectsToggle?.ApplyTheme(theme);

            RefreshGemSlots();
        }

        public void ApplyBallTheme(Line98.Data.BallThemeSO ballTheme)
        {
            m_BallTheme = ballTheme;
            RefreshGemSlots();
        }

        private void RefreshGemSlots()
        {
            var spriteSet = m_BallTheme?.PreviewSpriteSet ?? m_UiTheme?.PreviewSpriteSet;
            if (m_GemSlots == null || spriteSet == null) return;

            var colors = new[]
            {
                Line98.Core.BallColor.Red,
                Line98.Core.BallColor.Orange,
                Line98.Core.BallColor.Yellow,
                Line98.Core.BallColor.Green,
                Line98.Core.BallColor.Cyan,
                Line98.Core.BallColor.Purple,
                Line98.Core.BallColor.Blue
            };
            for (int i = 0; i < m_GemSlots.Length && i < colors.Length; i++)
            {
                if (m_GemSlots[i] != null)
                {
                    m_GemSlots[i].sprite = spriteSet.GetSprite(colors[i]);
                }
            }
        }

        private void BindListeners()
        {
            if (m_BackButton != null) m_BackButton.onClick.AddListener(HandleBackClicked);
            if (m_MusicToggle != null) m_MusicToggle.OnToggled += HandleMusicToggled;
            if (m_SfxToggle != null) m_SfxToggle.OnToggled += HandleSfxToggled;
            if (m_VibrationToggle != null) m_VibrationToggle.OnToggled += HandleVibrationToggled;
            if (m_ReduceEffectsToggle != null) m_ReduceEffectsToggle.OnToggled += HandleReduceEffectsToggled;
            if (m_LanguageRow?.ActionButton != null) m_LanguageRow.ActionButton.onClick.AddListener(HandleLanguageClicked);
            if (m_RemoveAdsButton != null) m_RemoveAdsButton.onClick.AddListener(HandleRemoveAdsClicked);
            if (m_RestorePurchasesRow?.ActionButton != null) m_RestorePurchasesRow.ActionButton.onClick.AddListener(HandleRestorePurchasesClicked);
            if (m_PrivacyPolicyRow?.ActionButton != null) m_PrivacyPolicyRow.ActionButton.onClick.AddListener(HandlePrivacyPolicyClicked);
            if (m_ContactSupportRow?.ActionButton != null) m_ContactSupportRow.ActionButton.onClick.AddListener(HandleContactSupportClicked);
            if (m_CosmeticsButton != null) m_CosmeticsButton.onClick.AddListener(HandleCosmeticsClicked);
        }

        private void HandleBackClicked() => OnBackClicked?.Invoke();
        private void HandleMusicToggled(bool value) => OnMusicToggled?.Invoke(value);
        private void HandleSfxToggled(bool value) => OnSfxToggled?.Invoke(value);
        private void HandleVibrationToggled(bool value) => OnVibrationToggled?.Invoke(value);
        private void HandleReduceEffectsToggled(bool value) => OnReduceEffectsToggled?.Invoke(value);
        private void HandleLanguageClicked() => OnLanguageClicked?.Invoke();
        private void HandleRemoveAdsClicked() => OnRemoveAdsClicked?.Invoke();
        private void HandleRestorePurchasesClicked() => OnRestorePurchasesClicked?.Invoke();
        private void HandlePrivacyPolicyClicked() => OnPrivacyPolicyClicked?.Invoke();
        private void HandleContactSupportClicked() => OnContactSupportClicked?.Invoke();
        private void HandleCosmeticsClicked() => OnCosmeticsClicked?.Invoke();

        protected override void OnDestroy()
        {
            if (m_BackButton != null) m_BackButton.onClick.RemoveListener(HandleBackClicked);
            if (m_MusicToggle != null) m_MusicToggle.OnToggled -= HandleMusicToggled;
            if (m_SfxToggle != null) m_SfxToggle.OnToggled -= HandleSfxToggled;
            if (m_VibrationToggle != null) m_VibrationToggle.OnToggled -= HandleVibrationToggled;
            if (m_ReduceEffectsToggle != null) m_ReduceEffectsToggle.OnToggled -= HandleReduceEffectsToggled;
            if (m_LanguageRow?.ActionButton != null) m_LanguageRow.ActionButton.onClick.RemoveListener(HandleLanguageClicked);
            if (m_RemoveAdsButton != null) m_RemoveAdsButton.onClick.RemoveListener(HandleRemoveAdsClicked);
            if (m_RestorePurchasesRow?.ActionButton != null) m_RestorePurchasesRow.ActionButton.onClick.RemoveListener(HandleRestorePurchasesClicked);
            if (m_PrivacyPolicyRow?.ActionButton != null) m_PrivacyPolicyRow.ActionButton.onClick.RemoveListener(HandlePrivacyPolicyClicked);
            if (m_ContactSupportRow?.ActionButton != null) m_ContactSupportRow.ActionButton.onClick.RemoveListener(HandleContactSupportClicked);
            if (m_CosmeticsButton != null) m_CosmeticsButton.onClick.RemoveListener(HandleCosmeticsClicked);
            base.OnDestroy();
        }
    }
}

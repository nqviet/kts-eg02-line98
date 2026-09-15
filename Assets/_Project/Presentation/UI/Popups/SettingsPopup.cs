using System;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>
    /// In-game Settings popup with Audio toggles (Music, SFX) and close button.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsPopup : UiPopupBase
    {
        [SerializeField] private Toggle m_MusicToggle;
        [SerializeField] private Toggle m_SfxToggle;
        [SerializeField] private Button m_CosmeticsButton;

        public event Action<bool> OnMusicToggled;
        public event Action<bool> OnSfxToggled;
        public event Action OnCosmeticsClicked;

        public Toggle MusicToggle => m_MusicToggle;
        public Toggle SfxToggle => m_SfxToggle;
        public Button CosmeticsButton => m_CosmeticsButton;

        protected override void Awake()
        {
            base.Awake();

            if (m_MusicToggle != null)
            {
                m_MusicToggle.onValueChanged.AddListener(val => OnMusicToggled?.Invoke(val));
            }

            if (m_SfxToggle != null)
            {
                m_SfxToggle.onValueChanged.AddListener(val => OnSfxToggled?.Invoke(val));
            }

            if (m_CosmeticsButton != null)
            {
                m_CosmeticsButton.onClick.AddListener(() => OnCosmeticsClicked?.Invoke());
            }
        }
    }
}

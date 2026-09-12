using System;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Views
{
    /// <summary>
    /// Presentation component for header elements and action buttons (Settings, Stats).
    /// </summary>
    public sealed class Line98HeaderView : MonoBehaviour
    {
        [SerializeField] private Button m_SettingsButton;
        [SerializeField] private Button m_StatsButton;

        public event Action OnSettingsClicked;
        public event Action OnStatsClicked;

        public void Initialize(Button settingsButton, Button statsButton)
        {
            m_SettingsButton = settingsButton;
            m_StatsButton = statsButton;

            if (m_SettingsButton != null)
            {
                m_SettingsButton.onClick.RemoveAllListeners();
                m_SettingsButton.onClick.AddListener(OnSettingsButtonPressed);
            }

            if (m_StatsButton != null)
            {
                m_StatsButton.onClick.RemoveAllListeners();
                m_StatsButton.onClick.AddListener(OnStatsButtonPressed);
            }
        }

        public void OnSettingsButtonPressed()
        {
            OnSettingsClicked?.Invoke();
        }

        public void OnStatsButtonPressed()
        {
            OnStatsClicked?.Invoke();
        }
    }
}

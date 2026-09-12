using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Views
{
    /// <summary>
    /// Presentation component for Modal Overlays (Settings and Stats dialogs).
    /// </summary>
    public sealed class Line98ModalView : MonoBehaviour
    {
        [SerializeField] private GameObject m_Overlay;
        [SerializeField] private TextMeshProUGUI m_TitleText;
        [SerializeField] private TextMeshProUGUI m_BodyText;
        [SerializeField] private TextMeshProUGUI m_PrimaryLabel;
        [SerializeField] private TextMeshProUGUI m_SecondaryLabel;
        [SerializeField] private GameObject m_PrimaryButtonObject;
        [SerializeField] private GameObject m_SecondaryButtonObject;
        [SerializeField] private Button m_PrimaryButton;
        [SerializeField] private Button m_SecondaryButton;

        public bool IsOpen => m_Overlay != null && m_Overlay.activeSelf;

        public void Initialize(
            GameObject overlay,
            TextMeshProUGUI titleText,
            TextMeshProUGUI bodyText,
            TextMeshProUGUI primaryLabel,
            TextMeshProUGUI secondaryLabel,
            GameObject primaryButtonObject,
            GameObject secondaryButtonObject)
        {
            m_Overlay = overlay;
            m_TitleText = titleText;
            m_BodyText = bodyText;
            m_PrimaryLabel = primaryLabel;
            m_SecondaryLabel = secondaryLabel;
            m_PrimaryButtonObject = primaryButtonObject;
            m_SecondaryButtonObject = secondaryButtonObject;

            if (m_PrimaryButtonObject != null)
            {
                m_PrimaryButton = m_PrimaryButtonObject.GetComponent<Button>();
            }

            if (m_SecondaryButtonObject != null)
            {
                m_SecondaryButton = m_SecondaryButtonObject.GetComponent<Button>();
            }

            if (m_Overlay != null)
            {
                m_Overlay.SetActive(false);
            }
        }

        public void ShowSettings(bool soundEnabled, Action onToggleSound, Action onClose)
        {
            if (m_Overlay == null)
            {
                return;
            }

            m_Overlay.SetActive(true);
            m_TitleText.SetText("Settings");
            m_BodyText.SetText("Choose a sound feedback setting for this session.");
            UpdateSoundLabel(soundEnabled);
            m_SecondaryLabel.SetText("Close");
            m_SecondaryButtonObject.SetActive(true);

            if (m_PrimaryButton != null)
            {
                m_PrimaryButton.onClick.RemoveAllListeners();
                m_PrimaryButton.onClick.AddListener(() => onToggleSound?.Invoke());
            }

            if (m_SecondaryButton != null)
            {
                m_SecondaryButton.onClick.RemoveAllListeners();
                m_SecondaryButton.onClick.AddListener(() => onClose?.Invoke());
            }
        }

        public void UpdateSoundLabel(bool soundEnabled)
        {
            if (m_PrimaryLabel != null)
            {
                m_PrimaryLabel.SetText(soundEnabled ? "Sound: On" : "Sound: Off");
            }
        }

        public void ShowStats(int score, int moves, int bestScore, Action onClose)
        {
            if (m_Overlay == null)
            {
                return;
            }

            m_Overlay.SetActive(true);
            m_TitleText.SetText("Session Stats");
            m_BodyText.SetText("Score  " + score.ToString("00000") + "\nMoves  " + moves + "\nBest   " + bestScore.ToString("00000"));
            m_PrimaryLabel.SetText("Close");
            m_SecondaryButtonObject.SetActive(false);

            if (m_PrimaryButton != null)
            {
                m_PrimaryButton.onClick.RemoveAllListeners();
                m_PrimaryButton.onClick.AddListener(() => onClose?.Invoke());
            }
        }

        public void Close()
        {
            if (m_Overlay != null)
            {
                m_Overlay.SetActive(false);
            }
        }
    }
}

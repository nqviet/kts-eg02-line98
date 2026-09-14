using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Line98.Core;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Master UI router governing the 3-tier canvas stack (StaticHUD 0, DynamicHUD 10, Popups 20),
    /// modal scrim fading, popup lifecycle, and navigation back handling.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIRouter : MonoBehaviour, ITickable
    {
        [Header("Canvases")]
        [SerializeField] private Canvas m_StaticCanvas;
        [SerializeField] private Canvas m_DynamicCanvas;
        [SerializeField] private Canvas m_PopupCanvas;

        [Header("Modal Scrim")]
        [SerializeField] private Image m_ScrimImage;
        [SerializeField] private CanvasGroup m_ScrimGroup;

        [Header("Popups")]
        [SerializeField] private GameOverPopup m_GameOverPopup;
        [SerializeField] private ConfirmPopup m_ConfirmPopup;
        [SerializeField] private SettingsPopup m_SettingsPopup;
        [SerializeField] private StatisticsPopup m_StatisticsPopup;

        private TweenRunner m_TweenRunner;
        private readonly Stack<PopupView> m_PopupStack = new Stack<PopupView>();

        public Canvas StaticCanvas => m_StaticCanvas;
        public Canvas DynamicCanvas => m_DynamicCanvas;
        public Canvas PopupCanvas => m_PopupCanvas;

        public GameOverPopup GameOverPopup => m_GameOverPopup;
        public ConfirmPopup ConfirmPopup => m_ConfirmPopup;
        public SettingsPopup SettingsPopup => m_SettingsPopup;
        public StatisticsPopup StatisticsPopup => m_StatisticsPopup;

        public bool IsAnyPopupOpen => m_PopupStack.Count > 0;

        private void Awake()
        {
            EnsureCanvasSortingOrders();
            if (m_ScrimGroup != null)
            {
                m_ScrimGroup.alpha = 0f;
                m_ScrimGroup.blocksRaycasts = false;
            }
        }

        public void Initialize(TweenRunner tweenRunner)
        {
            m_TweenRunner = tweenRunner;
            EnsureCanvasSortingOrders();

            if (m_GameOverPopup != null) m_GameOverPopup.Initialize(tweenRunner);
            if (m_ConfirmPopup != null) m_ConfirmPopup.Initialize(tweenRunner);
            if (m_SettingsPopup != null) m_SettingsPopup.Initialize(tweenRunner);
            if (m_StatisticsPopup != null) m_StatisticsPopup.Initialize(tweenRunner);
        }

        public void EnsureCanvasSortingOrders()
        {
            if (m_StaticCanvas != null) m_StaticCanvas.sortingOrder = 0;
            if (m_DynamicCanvas != null) m_DynamicCanvas.sortingOrder = 10;
            if (m_PopupCanvas != null) m_PopupCanvas.sortingOrder = 20;
        }

        public void OpenGameOver(in SessionSummary summary)
        {
            if (m_GameOverPopup == null) return;
            m_GameOverPopup.Populate(summary);
            PushPopup(m_GameOverPopup);
        }

        public void OpenGameOver(int score, int bestScore, int lines, bool canContinue)
        {
            OpenGameOver(new SessionSummary(score, bestScore, 0, lines, 0, canContinue));
        }

        public void OpenConfirm(string title, string body, Action onConfirm)
        {
            if (m_ConfirmPopup == null) return;
            m_ConfirmPopup.SetContent(title, body);

            if (m_ConfirmPopup.ConfirmButton != null)
            {
                m_ConfirmPopup.ConfirmButton.onClick.RemoveAllListeners();
                m_ConfirmPopup.ConfirmButton.onClick.AddListener(() =>
                {
                    m_ConfirmPopup.Hide();
                    onConfirm?.Invoke();
                });
            }

            PushPopup(m_ConfirmPopup);
        }

        public void OpenSettings()
        {
            if (m_SettingsPopup == null) return;
            PushPopup(m_SettingsPopup);
        }

        public void OpenStatistics(int gamesPlayed, int bestScore, int totalLines, int avgScore)
        {
            if (m_StatisticsPopup == null) return;
            m_StatisticsPopup.Populate(gamesPlayed, bestScore, totalLines, avgScore);
            PushPopup(m_StatisticsPopup);
        }

        private void PushPopup(PopupView popup)
        {
            if (popup == null) return;

            m_PopupStack.Push(popup);
            UpdateScrim(true);
            popup.Show();
        }

        public void PopPopup()
        {
            if (m_PopupStack.Count == 0) return;

            PopupView popup = m_PopupStack.Pop();
            popup.Hide(() =>
            {
                if (m_PopupStack.Count == 0)
                {
                    UpdateScrim(false);
                }
            });
        }

        public void CloseAllPopups()
        {
            while (m_PopupStack.Count > 0)
            {
                var popup = m_PopupStack.Pop();
                popup.Hide();
            }
            UpdateScrim(false);
        }

        private void UpdateScrim(bool show)
        {
            if (m_ScrimGroup == null) return;

            m_ScrimGroup.blocksRaycasts = show;

            if (m_TweenRunner != null)
            {
                m_TweenRunner.CancelByOwner(m_ScrimGroup);
                var tween = new Tween
                {
                    From = m_ScrimGroup.alpha,
                    To = show ? 1f : 0f,
                    Duration = 0.20f,
                    Ease = Easing.OutCubic,
                    Owner = m_ScrimGroup,
                    OnUpdate = val => m_ScrimGroup.alpha = val
                };
                m_TweenRunner.Play(in tween);
            }
            else
            {
                m_ScrimGroup.alpha = show ? 1f : 0f;
            }
        }

        public void Tick(float dt)
        {
            // Back button / Android escape key handling
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && m_PopupStack.Count > 0)
            {
                PopPopup();
            }
#endif
        }
    }
}

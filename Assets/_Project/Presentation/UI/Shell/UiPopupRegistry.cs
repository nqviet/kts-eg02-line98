using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Core;
using Line98.Data;

namespace Line98.Presentation
{
    public enum UiPopupId
    {
        GameOver,
        Confirm,
        Settings,
        Statistics,
        Cosmetics
    }

    public interface IUiPopupPayload
    {
    }

    public readonly struct CosmeticsPopupPayload : IUiPopupPayload
    {
        public readonly ThemeCatalogSO Catalog;
        public readonly string ActiveThemeId;
        public readonly IThemeSelector Selector;

        public CosmeticsPopupPayload(ThemeCatalogSO catalog, string activeThemeId, IThemeSelector selector)
        {
            Catalog = catalog;
            ActiveThemeId = activeThemeId;
            Selector = selector;
        }
    }

    public readonly struct GameOverPopupPayload : IUiPopupPayload
    {
        public readonly SessionSummary Summary;

        public GameOverPopupPayload(in SessionSummary summary)
        {
            Summary = summary;
        }
    }

    public readonly struct ConfirmPopupPayload : IUiPopupPayload
    {
        public readonly string Title;
        public readonly string Body;
        public readonly Action OnConfirm;

        public ConfirmPopupPayload(string title, string body, Action onConfirm)
        {
            Title = title;
            Body = body;
            OnConfirm = onConfirm;
        }
    }

    public readonly struct StatisticsPopupPayload : IUiPopupPayload
    {
        public readonly int GamesPlayed;
        public readonly int BestScore;
        public readonly int TotalLines;
        public readonly int AverageScore;

        public StatisticsPopupPayload(int gamesPlayed, int bestScore, int totalLines, int averageScore)
        {
            GamesPlayed = gamesPlayed;
            BestScore = bestScore;
            TotalLines = totalLines;
            AverageScore = averageScore;
        }
    }

    /// <summary>
    /// Resolves popup ids to placed or lazily-instantiated popup views.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiPopupRegistry : MonoBehaviour
    {
        [Serializable]
        private struct PopupCatalogEntry
        {
            public UiPopupId Id;
            public PopupView Prefab;
        }

        [SerializeField] private PopupCatalogEntry[] m_Catalog = Array.Empty<PopupCatalogEntry>();

        private readonly Dictionary<UiPopupId, PopupView> m_Instances = new Dictionary<UiPopupId, PopupView>();
        private UiShell m_Shell;
        private SafeAreaFitter m_SafeAreaFitter;

        public int RegisteredCount => m_Instances.Count;

        public void Initialize(UiShell shell, SafeAreaFitter safeAreaFitter)
        {
            m_Shell = shell;
            m_SafeAreaFitter = safeAreaFitter;
            RegisterExistingPopups();
        }

        public void Register(UiPopupId id, PopupView popup)
        {
            if (popup == null)
            {
                return;
            }

            m_Instances[id] = popup;
            m_SafeAreaFitter?.RegisterPopup(popup);
        }

        public bool TryGet(UiPopupId id, out PopupView popup)
        {
            popup = Resolve(id);
            return popup != null;
        }

        /// <summary>
        /// Returns an already-registered popup without instantiating a catalog prefab.
        /// Use this for state inspection where the lazy-instantiate side effect of TryGet is unwanted.
        /// </summary>
        public bool TryGetRegistered(UiPopupId id, out PopupView popup)
        {
            if (m_Instances.TryGetValue(id, out popup) && popup != null)
            {
                return true;
            }

            popup = null;
            return false;
        }

        public bool Open(UiPopupId id, IUiPopupPayload payload = null)
        {
            if (m_Shell == null)
            {
                Debug.LogWarning($"[UiPopupRegistry] Cannot open {id}: no UiShell is initialized.");
                return false;
            }

            PopupView popup = Resolve(id);
            if (popup == null)
            {
                Debug.LogWarning($"[UiPopupRegistry] Cannot open {id}: no placed popup or catalog prefab is configured.");
                return false;
            }

            ApplyPayload(popup, payload);
            m_Shell.PushPopup(popup);
            return true;
        }

        private void RegisterExistingPopups()
        {
            PopupView[] popups = GetComponentsInChildren<PopupView>(true);
            for (int i = 0; i < popups.Length; i++)
            {
                if (TryGetId(popups[i], out UiPopupId id))
                {
                    Register(id, popups[i]);
                }
            }
        }

        private PopupView Resolve(UiPopupId id)
        {
            if (m_Instances.TryGetValue(id, out PopupView popup) && popup != null)
            {
                return popup;
            }

            for (int i = 0; i < m_Catalog.Length; i++)
            {
                PopupCatalogEntry entry = m_Catalog[i];
                if (entry.Id != id || entry.Prefab == null || m_Shell == null)
                {
                    continue;
                }

                Transform parent = m_Shell.PopupCanvas != null ? m_Shell.PopupCanvas.transform : transform;
                PopupView instance = Instantiate(entry.Prefab, parent);
                instance.name = entry.Prefab.name;
                instance.gameObject.SetActive(false);
                Register(id, instance);
                return instance;
            }

            return null;
        }

        private void ApplyPayload(PopupView popup, IUiPopupPayload payload)
        {
            if (payload is GameOverPopupPayload gameOverPayload && popup is GameOverPopup gameOverPopup)
            {
                gameOverPopup.Populate(gameOverPayload.Summary);
            }
            else if (payload is ConfirmPopupPayload confirmPayload && popup is ConfirmPopup confirmPopup)
            {
                confirmPopup.SetContent(confirmPayload.Title, confirmPayload.Body);
                m_Shell.PrepareConfirm(confirmPopup, confirmPayload.OnConfirm);
            }
            else if (payload is StatisticsPopupPayload statisticsPayload && popup is StatisticsPopup statisticsPopup)
            {
                statisticsPopup.Populate(
                    statisticsPayload.GamesPlayed,
                    statisticsPayload.BestScore,
                    statisticsPayload.TotalLines,
                    statisticsPayload.AverageScore);
            }
            else if (payload is CosmeticsPopupPayload cosmeticsPayload && popup is CosmeticsPopup cosmeticsPopup)
            {
                cosmeticsPopup.Populate(cosmeticsPayload.Catalog, cosmeticsPayload.ActiveThemeId, cosmeticsPayload.Selector);
            }
        }

        private static bool TryGetId(PopupView popup, out UiPopupId id)
        {
            if (popup is GameOverPopup)
            {
                id = UiPopupId.GameOver;
                return true;
            }

            if (popup is ConfirmPopup)
            {
                id = UiPopupId.Confirm;
                return true;
            }

            if (popup is SettingsPopup)
            {
                id = UiPopupId.Settings;
                return true;
            }

            if (popup is StatisticsPopup)
            {
                id = UiPopupId.Statistics;
                return true;
            }

            if (popup is CosmeticsPopup)
            {
                id = UiPopupId.Cosmetics;
                return true;
            }

            id = default;
            return false;
        }
    }
}

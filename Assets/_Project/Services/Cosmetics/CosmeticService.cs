using System;
using UnityEngine;
using Line98.Data;

namespace Line98.Services
{
    /// <summary>
    /// Core cosmetic service: resolves themes, manages persistence across restarts,
    /// and dispatches notifications on theme changes.
    /// </summary>
    public sealed class CosmeticService : ICosmeticService
    {
        private const string s_SaveKey = "line98_cosmetics";

        private readonly ThemeCatalogSO m_Catalog;
        private readonly ISaveBackend m_Backend;
        private CosmeticSettings m_Settings;
        private ThemeDefinitionSO m_ActiveTheme;

        public event Action<ThemeChange> OnThemeChanged;

        public CosmeticService(ThemeCatalogSO catalog, ISaveBackend backend = null)
        {
            m_Catalog = catalog;
            m_Backend = backend ?? new FileSaveBackend();

            LoadAndInitialize();
        }

        public string ActiveThemeId => m_ActiveTheme != null ? m_ActiveTheme.ThemeId : (m_Settings?.ThemeId ?? "classic");

        public ThemeDefinitionSO ActiveTheme => m_ActiveTheme;

        public string ActiveBallThemeId
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Settings?.BallOverrideId))
                {
                    return m_Settings.BallOverrideId;
                }
                return m_ActiveTheme?.BallTheme?.ThemeId ?? "classic";
            }
        }

        public string ActiveBoardThemeId
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Settings?.BoardOverrideId))
                {
                    return m_Settings.BoardOverrideId;
                }
                return m_ActiveTheme?.BoardTheme?.ThemeId ?? "classic";
            }
        }

        public void SetTheme(string themeId)
        {
            var resolved = ThemeResolver.Resolve(m_Catalog, themeId);
            if (resolved == null)
            {
                Debug.LogWarning($"[CosmeticService] Cannot set theme '{themeId ?? "<null>"}' because it could not be resolved.");
                return;
            }

            bool isSameTheme = m_ActiveTheme != null && string.Equals(m_ActiveTheme.ThemeId, resolved.ThemeId, StringComparison.OrdinalIgnoreCase);
            bool hasOverrides = !string.IsNullOrEmpty(m_Settings.BallOverrideId)
                || !string.IsNullOrEmpty(m_Settings.BoardOverrideId)
                || !string.IsNullOrEmpty(m_Settings.UiOverrideId)
                || !string.IsNullOrEmpty(m_Settings.ClearEffectOverrideId);

            if (isSameTheme && !hasOverrides)
            {
                // Idempotent call — no change and no event
                return;
            }

            m_ActiveTheme = resolved;
            m_Settings.ThemeId = resolved.ThemeId;
            m_Settings.BallOverrideId = null;
            m_Settings.BoardOverrideId = null;
            m_Settings.UiOverrideId = null;
            m_Settings.ClearEffectOverrideId = null;

            SaveSettings();

            // Notify subscribers after persistence is committed
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Ball, resolved.ThemeId));
        }

        public void SetBallTheme(string ballThemeId)
        {
            var resolvedBall = ThemeResolver.ResolveBall(m_Catalog, ballThemeId, m_ActiveTheme);
            if (resolvedBall == null) return;

            string targetId = resolvedBall.ThemeId;
            if (string.Equals(ActiveBallThemeId, targetId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            m_Settings.BallOverrideId = targetId;
            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Ball, targetId));
        }

        public void SetBoardTheme(string boardThemeId)
        {
            var resolvedBoard = ThemeResolver.ResolveBoard(m_Catalog, boardThemeId, m_ActiveTheme);
            if (resolvedBoard == null) return;

            string targetId = resolvedBoard.ThemeId;
            if (string.Equals(ActiveBoardThemeId, targetId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            m_Settings.BoardOverrideId = targetId;
            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Board, targetId));
        }

        public void SetUiTheme(string uiThemeId)
        {
            if (string.Equals(m_Settings.UiOverrideId, uiThemeId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            m_Settings.UiOverrideId = uiThemeId;
            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Ui, uiThemeId));
        }

        public void SetClearEffectTheme(string clearEffectId)
        {
            if (string.Equals(m_Settings.ClearEffectOverrideId, clearEffectId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            m_Settings.ClearEffectOverrideId = clearEffectId;
            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.ClearEffect, clearEffectId));
        }

        public void ResetToDefault()
        {
            string defaultId = m_Catalog != null ? m_Catalog.DefaultThemeId : "classic";
            SetTheme(defaultId);
        }

        private void LoadAndInitialize()
        {
            string json = m_Backend?.Load(s_SaveKey);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    m_Settings = JsonUtility.FromJson<CosmeticSettings>(json) ?? new CosmeticSettings();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CosmeticService] Failed to parse cosmetic settings: {ex.Message}");
                    m_Settings = new CosmeticSettings();
                }
            }
            else
            {
                m_Settings = new CosmeticSettings();
            }

            m_ActiveTheme = ThemeResolver.Resolve(m_Catalog, m_Settings.ThemeId);
            if (m_ActiveTheme != null)
            {
                m_Settings.ThemeId = m_ActiveTheme.ThemeId;
            }
        }

        private void SaveSettings()
        {
            if (m_Backend != null && m_Settings != null)
            {
                string json = JsonUtility.ToJson(m_Settings, true);
                m_Backend.Save(s_SaveKey, json);
            }
        }
    }
}

using System;
using System.Collections.Generic;
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
        private BallThemeSO m_ActiveBallTheme;
        private readonly HashSet<ThemeCategory> m_WarnedDivergentOverrides = new HashSet<ThemeCategory>();

        public event Action<ThemeChange> OnThemeChanged;

        public CosmeticService(ThemeCatalogSO catalog, ISaveBackend backend = null)
        {
            m_Catalog = catalog;
            m_Backend = backend ?? new FileSaveBackend();

            LoadAndInitialize();
        }

        public string ActiveThemeId => m_ActiveTheme != null ? m_ActiveTheme.ThemeId : (m_Settings?.ThemeId ?? ThemeIds.Classic);

        public ThemeDefinitionSO ActiveTheme => m_ActiveTheme;

        public BallThemeSO ActiveBallTheme => m_ActiveBallTheme;

        public string ActiveBallThemeId => m_ActiveBallTheme != null ? m_ActiveBallTheme.ThemeId : ThemeIds.Classic;

        public string ActiveBoardThemeId
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Settings?.BoardOverrideId))
                {
                    return m_Settings.BoardOverrideId;
                }
                return m_ActiveTheme?.BoardTheme?.ThemeId ?? ThemeIds.Classic;
            }
        }

        public string ActiveClearEffectThemeId
        {
            get
            {
                if (!string.IsNullOrEmpty(m_Settings?.ClearEffectOverrideId))
                {
                    return m_Settings.ClearEffectOverrideId;
                }
                return m_ActiveTheme?.ClearEffect?.ThemeId ?? ThemeIds.Classic;
            }
        }

        public string BallOverrideId => m_Settings?.BallOverrideId;
        public string BoardOverrideId => m_Settings?.BoardOverrideId;

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

            RefreshEffectiveBallTheme();
            SaveSettings();

            // Notify subscribers after persistence is committed
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Ball, resolved.ThemeId));
        }

        public void SetBallTheme(string ballThemeId)
        {
            var resolvedBall = ThemeResolver.ResolveBall(m_Catalog, ballThemeId, m_ActiveTheme);
            if (resolvedBall == null) return;

            string targetId = resolvedBall.ThemeId;
            string bundlePartId = m_ActiveTheme?.BallTheme?.ThemeId;
            string overrideId = IdsMatch(targetId, bundlePartId) ? null : targetId;
            if (IdsMatch(m_Settings.BallOverrideId, overrideId) && IdsMatch(ActiveBallThemeId, targetId))
            {
                return;
            }

            m_Settings.BallOverrideId = overrideId;
            RefreshEffectiveBallTheme();
            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Ball, targetId));
        }

        public void SetBoardTheme(string boardThemeId)
        {
            var resolvedBoard = ThemeResolver.ResolveBoard(m_Catalog, boardThemeId, m_ActiveTheme);
            if (resolvedBoard == null) return;

            string targetId = resolvedBoard.ThemeId;
            string bundlePartId = m_ActiveTheme?.BoardTheme?.ThemeId;
            string overrideId = IdsMatch(targetId, bundlePartId) ? null : targetId;
            if (IdsMatch(m_Settings.BoardOverrideId, overrideId) && IdsMatch(ActiveBoardThemeId, targetId))
            {
                return;
            }

            m_Settings.BoardOverrideId = overrideId;
            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Board, targetId));
        }

        public void SetUiTheme(string uiThemeId)
        {
            string bundlePartId = m_ActiveTheme?.UiTheme?.ThemeId;
            string overrideId = IdsMatch(uiThemeId, bundlePartId) ? null : uiThemeId;
            if (IdsMatch(m_Settings.UiOverrideId, overrideId))
            {
                return;
            }

            m_Settings.UiOverrideId = overrideId;
            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Ui, overrideId ?? bundlePartId));
        }

        public void SetClearEffectTheme(string clearEffectId)
        {
            string bundlePartId = m_ActiveTheme?.ClearEffect?.ThemeId;
            string overrideId = IdsMatch(clearEffectId, bundlePartId) ? null : clearEffectId;
            if (IdsMatch(m_Settings.ClearEffectOverrideId, overrideId))
            {
                return;
            }

            m_Settings.ClearEffectOverrideId = overrideId;
            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.ClearEffect, overrideId ?? bundlePartId));
        }

        public void ResetCategoryOverride(ThemeCategory category)
        {
            string effectiveId;
            switch (category)
            {
                case ThemeCategory.Ball:
                    if (string.IsNullOrEmpty(m_Settings.BallOverrideId)) return;
                    m_Settings.BallOverrideId = null;
                    RefreshEffectiveBallTheme();
                    effectiveId = m_ActiveBallTheme?.ThemeId;
                    break;
                case ThemeCategory.Board:
                    if (string.IsNullOrEmpty(m_Settings.BoardOverrideId)) return;
                    m_Settings.BoardOverrideId = null;
                    effectiveId = m_ActiveTheme?.BoardTheme?.ThemeId;
                    break;
                case ThemeCategory.Ui:
                    if (string.IsNullOrEmpty(m_Settings.UiOverrideId)) return;
                    m_Settings.UiOverrideId = null;
                    effectiveId = m_ActiveTheme?.UiTheme?.ThemeId;
                    break;
                case ThemeCategory.ClearEffect:
                    if (string.IsNullOrEmpty(m_Settings.ClearEffectOverrideId)) return;
                    m_Settings.ClearEffectOverrideId = null;
                    effectiveId = m_ActiveTheme?.ClearEffect?.ThemeId;
                    break;
                default:
                    return;
            }

            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(category, effectiveId));
        }

        public void ResetToDefault()
        {
            string defaultId = m_Catalog != null ? m_Catalog.DefaultThemeId : ThemeIds.Classic;
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

            bool didMigrate = MigrateSettingsIfNeeded();
            WarnForDivergentOverrides();
            RefreshEffectiveBallTheme();

            if (didMigrate)
            {
                SaveSettings();
            }
        }

        private void RefreshEffectiveBallTheme()
        {
            m_ActiveBallTheme = ThemeResolver.ResolveBall(m_Catalog, m_Settings?.BallOverrideId, m_ActiveTheme);
        }

        private void SaveSettings()
        {
            if (m_Backend != null && m_Settings != null)
            {
                string json = JsonUtility.ToJson(m_Settings, true);
                m_Backend.Save(s_SaveKey, json);
            }
        }

        private bool MigrateSettingsIfNeeded()
        {
            if (m_Settings == null || m_Settings.Version >= CosmeticSettings.CurrentVersion)
            {
                return false;
            }

            int sourceVersion = m_Settings.Version;
            var droppedOverrides = new List<string>(4);
            DropDivergentLegacyOverride(
                ThemeCategory.Ball,
                ref m_Settings.BallOverrideId,
                m_ActiveTheme?.BallTheme?.ThemeId,
                droppedOverrides);
            DropDivergentLegacyOverride(
                ThemeCategory.Board,
                ref m_Settings.BoardOverrideId,
                m_ActiveTheme?.BoardTheme?.ThemeId,
                droppedOverrides);
            DropDivergentLegacyOverride(
                ThemeCategory.Ui,
                ref m_Settings.UiOverrideId,
                m_ActiveTheme?.UiTheme?.ThemeId,
                droppedOverrides);
            DropDivergentLegacyOverride(
                ThemeCategory.ClearEffect,
                ref m_Settings.ClearEffectOverrideId,
                m_ActiveTheme?.ClearEffect?.ThemeId,
                droppedOverrides);

            m_Settings.Version = CosmeticSettings.CurrentVersion;
            string dropped = droppedOverrides.Count > 0 ? string.Join(", ", droppedOverrides) : "none";
            Debug.LogWarning($"[CosmeticService] Migrated cosmetic settings v{sourceVersion} to v{CosmeticSettings.CurrentVersion}; dropped divergent overrides: {dropped}.");
            return true;
        }

        private static void DropDivergentLegacyOverride(
            ThemeCategory category,
            ref string overrideId,
            string bundlePartId,
            List<string> droppedOverrides)
        {
            if (string.IsNullOrEmpty(overrideId) || IdsMatch(overrideId, bundlePartId))
            {
                return;
            }

            droppedOverrides.Add($"{category}='{overrideId}'");
            overrideId = null;
        }

        private void WarnForDivergentOverrides()
        {
            WarnForDivergentOverride(ThemeCategory.Ball, m_Settings?.BallOverrideId, m_ActiveTheme?.BallTheme?.ThemeId);
            WarnForDivergentOverride(ThemeCategory.Board, m_Settings?.BoardOverrideId, m_ActiveTheme?.BoardTheme?.ThemeId);
            WarnForDivergentOverride(ThemeCategory.Ui, m_Settings?.UiOverrideId, m_ActiveTheme?.UiTheme?.ThemeId);
            WarnForDivergentOverride(ThemeCategory.ClearEffect, m_Settings?.ClearEffectOverrideId, m_ActiveTheme?.ClearEffect?.ThemeId);
        }

        private void WarnForDivergentOverride(ThemeCategory category, string overrideId, string bundlePartId)
        {
            if (string.IsNullOrEmpty(overrideId) || IdsMatch(overrideId, bundlePartId) || !m_WarnedDivergentOverrides.Add(category))
            {
                return;
            }

            Debug.LogWarning(
                $"[CosmeticService] Persisted {category} override '{overrideId}' diverges from bundle '{m_ActiveTheme?.ThemeId ?? "<none>"}' part '{bundlePartId ?? "<none>"}'.");
        }

        private static bool IdsMatch(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}

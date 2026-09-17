using System;
using UnityEngine;
using Line98.Data;

namespace Line98.Services
{
    /// <summary>
    /// Core cosmetic service: owns three independent selection axes (Ball, Board, ClearEffect),
    /// derives the UI theme from the selected board's pack, persists across restarts,
    /// and dispatches notifications on changes.
    /// </summary>
    public sealed class CosmeticService : ICosmeticService
    {
        private const string s_SaveKey = "line98_cosmetics";

        private readonly ThemeCatalogSO m_Catalog;
        private readonly ISaveBackend m_Backend;
        private CosmeticSettings m_Settings;
        private BallThemeSO m_ActiveBallTheme;
        private BoardThemeSO m_ActiveBoardTheme;
        private UiThemeSO m_ActiveUiTheme;
        private ThemeDefinitionSO m_ActivePack;
        private ClearEffectSO m_ActiveClearEffect;

        public event Action<ThemeChange> OnThemeChanged;

        public CosmeticService(ThemeCatalogSO catalog, ISaveBackend backend = null)
        {
            m_Catalog = catalog;
            m_Backend = backend ?? new FileSaveBackend();

            LoadAndInitialize();
        }

        public BallThemeSO ActiveBallTheme => m_ActiveBallTheme;
        public string ActiveBallThemeId => m_ActiveBallTheme != null ? m_ActiveBallTheme.ThemeId : DefaultPartId(ThemeCategory.Ball);

        public BoardThemeSO ActiveBoardTheme => m_ActiveBoardTheme;
        public string ActiveBoardThemeId => m_ActiveBoardTheme != null ? m_ActiveBoardTheme.ThemeId : DefaultPartId(ThemeCategory.Board);

        /// <summary>UI theme of the pack that owns the active board. Never selected independently.</summary>
        public UiThemeSO ActiveUiTheme => m_ActiveUiTheme;
        public string ActiveUiThemeId => m_ActiveUiTheme != null ? m_ActiveUiTheme.ThemeId : DefaultPartId(ThemeCategory.Ui);

        public ClearEffectSO ActiveClearEffect => m_ActiveClearEffect;
        public string ActiveClearEffectThemeId => m_ActiveClearEffect != null ? m_ActiveClearEffect.ThemeId : DefaultPartId(ThemeCategory.ClearEffect);

        public void SetBallTheme(string ballThemeId)
        {
            var resolved = ThemeResolver.ResolveBall(m_Catalog, ballThemeId, m_Catalog?.DefaultTheme);
            if (resolved == null || resolved == m_ActiveBallTheme) return;

            m_ActiveBallTheme = resolved;
            m_Settings.BallThemeId = resolved.ThemeId;
            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Ball, resolved.ThemeId));
        }

        /// <summary>
        /// Selects a board and atomically switches the UI theme to the board's pack.
        /// This is the only place the UI theme changes. Emits Board, then Ui (if it changed).
        /// </summary>
        public void SetBoardTheme(string boardThemeId)
        {
            if (!ThemeResolver.ResolveBoardWithUi(m_Catalog, boardThemeId, out var board, out var ui, out var pack))
            {
                return;
            }

            bool boardChanged = board != m_ActiveBoardTheme;
            bool uiChanged = ui != m_ActiveUiTheme;
            if (!boardChanged && !uiChanged) return;

            m_ActiveBoardTheme = board;
            m_ActiveUiTheme = ui;
            m_ActivePack = pack;
            m_Settings.BoardThemeId = board.ThemeId;
            SaveSettings();

            // Notify after persistence and after both parts are committed, so any subscriber
            // reading the service sees a consistent board/UI pair.
            if (boardChanged)
            {
                OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Board, board.ThemeId));
            }
            if (uiChanged)
            {
                OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.Ui, ActiveUiThemeId));
            }
        }

        public void SetClearEffectTheme(string clearEffectId)
        {
            var resolved = ThemeResolver.ResolveClearEffect(m_Catalog, clearEffectId, m_Catalog?.DefaultTheme);
            if (resolved == null || resolved == m_ActiveClearEffect) return;

            m_ActiveClearEffect = resolved;
            m_Settings.ClearEffectThemeId = resolved.ThemeId;
            SaveSettings();
            OnThemeChanged?.Invoke(new ThemeChange(ThemeCategory.ClearEffect, resolved.ThemeId));
        }

        public void ResetToDefault()
        {
            SetBoardTheme(DefaultPartId(ThemeCategory.Board));
            SetBallTheme(DefaultPartId(ThemeCategory.Ball));
            SetClearEffectTheme(DefaultPartId(ThemeCategory.ClearEffect));
        }

        private void LoadAndInitialize()
        {
            string json = m_Backend?.Load(s_SaveKey);
            bool didMigrate = false;
            m_Settings = null;

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var legacy = JsonUtility.FromJson<LegacyCosmeticSettings>(json);
                    if (legacy != null && legacy.Version < CosmeticSettings.CurrentVersion)
                    {
                        m_Settings = MigrateLegacy(legacy);
                        didMigrate = true;
                    }
                    else
                    {
                        m_Settings = JsonUtility.FromJson<CosmeticSettings>(json);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CosmeticService] Failed to parse cosmetic settings: {ex.Message}");
                }
            }

            m_Settings ??= new CosmeticSettings();
            ResolveActiveParts();

            if (didMigrate)
            {
                SaveSettings();
            }
        }

        private void ResolveActiveParts()
        {
            ThemeDefinitionSO defaultPack = m_Catalog?.DefaultTheme;

            m_ActiveBallTheme = ThemeResolver.ResolveBall(m_Catalog, m_Settings.BallThemeId, defaultPack);
            ThemeResolver.ResolveBoardWithUi(m_Catalog, m_Settings.BoardThemeId, out m_ActiveBoardTheme, out m_ActiveUiTheme, out m_ActivePack);
            m_ActiveClearEffect = ThemeResolver.ResolveClearEffect(m_Catalog, m_Settings.ClearEffectThemeId, defaultPack);

            // Normalize persisted ids to what actually resolved (e.g. unknown id -> default).
            if (m_ActiveBallTheme != null) m_Settings.BallThemeId = m_ActiveBallTheme.ThemeId;
            if (m_ActiveBoardTheme != null) m_Settings.BoardThemeId = m_ActiveBoardTheme.ThemeId;
            if (m_ActiveClearEffect != null) m_Settings.ClearEffectThemeId = m_ActiveClearEffect.ThemeId;
        }

        /// <summary>
        /// v1/v2 -> v3: flatten "bundle + per-category override" into three independent part ids.
        /// The legacy UI override is dropped: UI now always follows the board's pack.
        /// </summary>
        private CosmeticSettings MigrateLegacy(LegacyCosmeticSettings legacy)
        {
            ThemeDefinitionSO bundle = ThemeResolver.Resolve(m_Catalog, legacy.ThemeId);

            var migrated = new CosmeticSettings
            {
                BallThemeId = FirstNonEmpty(legacy.BallOverrideId, ThemeCatalogSO.PartId(bundle, ThemeCategory.Ball), DefaultPartId(ThemeCategory.Ball)),
                BoardThemeId = FirstNonEmpty(legacy.BoardOverrideId, ThemeCatalogSO.PartId(bundle, ThemeCategory.Board), DefaultPartId(ThemeCategory.Board)),
                ClearEffectThemeId = FirstNonEmpty(legacy.ClearEffectOverrideId, ThemeCatalogSO.PartId(bundle, ThemeCategory.ClearEffect), DefaultPartId(ThemeCategory.ClearEffect))
            };

            if (!string.IsNullOrEmpty(legacy.UiOverrideId))
            {
                Debug.LogWarning(
                    $"[CosmeticService] Dropped legacy UI override '{legacy.UiOverrideId}' during v{legacy.Version}->v{CosmeticSettings.CurrentVersion} migration; UI now follows board '{migrated.BoardThemeId}'.");
            }

            return migrated;
        }

        private void SaveSettings()
        {
            if (m_Backend != null && m_Settings != null)
            {
                string json = JsonUtility.ToJson(m_Settings, true);
                m_Backend.Save(s_SaveKey, json);
            }
        }

        private string DefaultPartId(ThemeCategory category)
        {
            return m_Catalog != null ? m_Catalog.DefaultPartId(category) : ThemeIds.Classic;
        }

        private static string FirstNonEmpty(string a, string b, string c)
        {
            if (!string.IsNullOrEmpty(a)) return a;
            if (!string.IsNullOrEmpty(b)) return b;
            return c;
        }

        /// <summary>Read-only shape of v1/v2 saves, used only for migration.</summary>
        [Serializable]
        private sealed class LegacyCosmeticSettings
        {
            // Saves predating the Version field are treated as v1.
            public int Version = 1;
            public string ThemeId;
            public string BallOverrideId;
            public string BoardOverrideId;
            public string UiOverrideId;
            public string ClearEffectOverrideId;
        }
    }
}

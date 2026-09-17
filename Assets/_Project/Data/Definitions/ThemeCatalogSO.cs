using System;
using UnityEngine;

namespace Line98.Data
{
    /// <summary>
    /// Registry of all available theme definitions in the game.
    /// </summary>
    [CreateAssetMenu(menuName = "Line98/Definitions/Theme Catalog", fileName = "ThemeCatalog_Default")]
    public sealed class ThemeCatalogSO : ScriptableObject
    {
        [SerializeField] private ThemeDefinitionSO[] m_Themes = Array.Empty<ThemeDefinitionSO>();
        [SerializeField] private string m_DefaultThemeId = ThemeIds.Crystal;

        public int Count => m_Themes != null ? m_Themes.Length : 0;
        public string DefaultThemeId => m_DefaultThemeId;

        public ThemeDefinitionSO ThemeAt(int index)
        {
            if (m_Themes != null && index >= 0 && index < m_Themes.Length)
            {
                return m_Themes[index];
            }
            return null;
        }

        public ThemeDefinitionSO DefaultTheme
        {
            get
            {
                if (TryGetTheme(m_DefaultThemeId, out var def))
                {
                    return def;
                }
                return Count > 0 ? m_Themes[0] : null;
            }
        }

        public bool TryGetTheme(string themeId, out ThemeDefinitionSO theme)
        {
            theme = null;
            if (string.IsNullOrEmpty(themeId) || m_Themes == null)
            {
                return false;
            }

            for (int i = 0; i < m_Themes.Length; i++)
            {
                var candidate = m_Themes[i];
                if (candidate != null && string.Equals(candidate.ThemeId, themeId, StringComparison.OrdinalIgnoreCase))
                {
                    theme = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetBallTheme(string themeId, out BallThemeSO ballTheme)
        {
            ballTheme = null;
            if (string.IsNullOrEmpty(themeId) || m_Themes == null)
            {
                return false;
            }

            for (int i = 0; i < m_Themes.Length; i++)
            {
                var themeDef = m_Themes[i];
                if (themeDef != null)
                {
                    var bt = themeDef.BallTheme;
                    if (bt != null && string.Equals(bt.ThemeId, themeId, StringComparison.OrdinalIgnoreCase))
                    {
                        ballTheme = bt;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetBoardTheme(string themeId, out BoardThemeSO boardTheme)
        {
            boardTheme = null;
            if (string.IsNullOrEmpty(themeId) || m_Themes == null)
            {
                return false;
            }

            for (int i = 0; i < m_Themes.Length; i++)
            {
                var themeDef = m_Themes[i];
                if (themeDef != null)
                {
                    var bt = themeDef.BoardTheme;
                    if (bt != null && string.Equals(bt.ThemeId, themeId, StringComparison.OrdinalIgnoreCase))
                    {
                        boardTheme = bt;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetUiTheme(string themeId, out UiThemeSO uiTheme)
        {
            uiTheme = null;
            if (TryGetPackForPart(ThemeCategory.Ui, themeId, out var pack))
            {
                uiTheme = pack.UiTheme;
            }
            return uiTheme != null;
        }

        public bool TryGetClearEffect(string themeId, out ClearEffectSO clearEffect)
        {
            clearEffect = null;
            if (TryGetPackForPart(ThemeCategory.ClearEffect, themeId, out var pack))
            {
                clearEffect = pack.ClearEffect;
            }
            return clearEffect != null;
        }

        /// <summary>
        /// Authoritative part→pack lookup: returns the first pack declaring a part with this id.
        /// The catalog audit guarantees any later match references the same asset.
        /// </summary>
        public bool TryGetPackForPart(ThemeCategory category, string partId, out ThemeDefinitionSO pack)
        {
            pack = null;
            if (string.IsNullOrEmpty(partId) || m_Themes == null)
            {
                return false;
            }

            for (int i = 0; i < m_Themes.Length; i++)
            {
                var candidate = m_Themes[i];
                if (candidate != null && string.Equals(PartId(candidate, category), partId, StringComparison.OrdinalIgnoreCase))
                {
                    pack = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPartIds(ThemeDefinitionSO pack, out ThemePartIds ids)
        {
            ids = default;
            if (pack == null)
            {
                return false;
            }

            ids = new ThemePartIds(
                PartId(pack, ThemeCategory.Ball),
                PartId(pack, ThemeCategory.Board),
                PartId(pack, ThemeCategory.Ui),
                PartId(pack, ThemeCategory.ClearEffect));
            return true;
        }

        public string DefaultPartId(ThemeCategory category)
        {
            return PartId(DefaultTheme, category) ?? ThemeIds.Classic;
        }

        public static string PartId(ThemeDefinitionSO pack, ThemeCategory category)
        {
            if (pack == null) return null;
            return category switch
            {
                ThemeCategory.Ball => pack.BallTheme != null ? pack.BallTheme.ThemeId : null,
                ThemeCategory.Board => pack.BoardTheme != null ? pack.BoardTheme.ThemeId : null,
                ThemeCategory.Ui => pack.UiTheme != null ? pack.UiTheme.ThemeId : null,
                ThemeCategory.ClearEffect => pack.ClearEffect != null ? pack.ClearEffect.ThemeId : null,
                _ => null
            };
        }

        public int UnlockedCount
        {
            get
            {
                if (m_Themes == null) return 0;
                int count = 0;
                for (int i = 0; i < m_Themes.Length; i++)
                {
                    if (m_Themes[i] != null && m_Themes[i].UnlockedByDefault)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
    }
}

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
        [SerializeField] private string m_DefaultThemeId = "classic";

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

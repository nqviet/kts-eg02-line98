using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Data;

namespace Line98.Services
{
    /// <summary>
    /// Static resolution rules for theme identifiers, inheritance, and catalog fallbacks.
    /// Fallback chain: requested -> catalog default -> classic -> catalog[0] -> null + warn-once.
    /// </summary>
    public static class ThemeResolver
    {
        private static readonly HashSet<string> s_WarnedMessages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static ThemeDefinitionSO Resolve(ThemeCatalogSO catalog, string requestedId)
        {
            if (catalog == null || catalog.Count == 0)
            {
                WarnOnce("ThemeResolver: Theme catalog is null or empty.");
                return null;
            }

            if (!string.IsNullOrEmpty(requestedId) && catalog.TryGetTheme(requestedId, out var theme) && theme != null)
            {
                return theme;
            }

            if (!string.IsNullOrEmpty(requestedId))
            {
                WarnOnce($"ThemeResolver: Requested theme '{requestedId}' not found in catalog. Falling back to default.");
            }

            if (catalog.DefaultTheme != null)
            {
                return catalog.DefaultTheme;
            }

            if (catalog.TryGetTheme("classic", out var classicTheme) && classicTheme != null)
            {
                return classicTheme;
            }

            var first = catalog.ThemeAt(0);
            if (first != null)
            {
                return first;
            }

            WarnOnce("ThemeResolver: Unable to resolve any valid theme from catalog.");
            return null;
        }

        public static BallThemeSO ResolveBall(ThemeCatalogSO catalog, string requestedBallId, ThemeDefinitionSO contextTheme = null)
        {
            if (catalog != null && !string.IsNullOrEmpty(requestedBallId) && catalog.TryGetBallTheme(requestedBallId, out var ballTheme))
            {
                return ballTheme;
            }

            if (contextTheme != null && contextTheme.BallTheme != null)
            {
                return contextTheme.BallTheme;
            }

            var resolvedTheme = Resolve(catalog, null);
            return resolvedTheme?.BallTheme;
        }

        public static BoardThemeSO ResolveBoard(ThemeCatalogSO catalog, string requestedBoardId, ThemeDefinitionSO contextTheme = null)
        {
            if (catalog != null && !string.IsNullOrEmpty(requestedBoardId) && catalog.TryGetBoardTheme(requestedBoardId, out var boardTheme))
            {
                return boardTheme;
            }

            if (contextTheme != null && contextTheme.BoardTheme != null)
            {
                return contextTheme.BoardTheme;
            }

            var resolvedTheme = Resolve(catalog, null);
            return resolvedTheme?.BoardTheme;
        }

        public static void ResetWarnings()
        {
            s_WarnedMessages.Clear();
        }

        private static void WarnOnce(string msg)
        {
            if (s_WarnedMessages.Add(msg))
            {
                Debug.LogWarning(msg);
            }
        }
    }
}

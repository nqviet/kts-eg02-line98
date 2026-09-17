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
            if (!string.IsNullOrEmpty(requestedBallId))
            {
                if (catalog != null && catalog.TryGetBallTheme(requestedBallId, out var ballTheme) && ballTheme != null)
                {
                    return ballTheme;
                }

                WarnOnce($"ThemeResolver: Requested ball theme '{requestedBallId}' was not found. Falling back to the default pack.");
            }

            if (contextTheme != null && contextTheme.BallTheme != null)
            {
                return contextTheme.BallTheme;
            }

            if (contextTheme != null)
            {
                WarnOnce($"ThemeResolver: Bundle '{contextTheme.ThemeId}' has no ball theme. Falling back to the catalog default ball theme.");
            }
            else
            {
                WarnOnce("ThemeResolver: No bundle context was supplied for ball resolution. Falling back to the catalog default ball theme.");
            }

            BallThemeSO defaultBallTheme = catalog?.DefaultTheme?.BallTheme;
            if (defaultBallTheme != null)
            {
                return defaultBallTheme;
            }

            WarnOnce("ThemeResolver: Unable to resolve a ball theme because the catalog default bundle has no ball theme.");
            return null;
        }

        public static BoardThemeSO ResolveBoard(ThemeCatalogSO catalog, string requestedBoardId, ThemeDefinitionSO contextTheme = null)
        {
            if (!string.IsNullOrEmpty(requestedBoardId))
            {
                if (catalog != null && catalog.TryGetBoardTheme(requestedBoardId, out var boardTheme) && boardTheme != null)
                {
                    return boardTheme;
                }

                WarnOnce($"ThemeResolver: Requested board theme '{requestedBoardId}' was not found. Falling back to the default pack.");
            }

            if (contextTheme != null && contextTheme.BoardTheme != null)
            {
                return contextTheme.BoardTheme;
            }

            if (contextTheme != null)
            {
                WarnOnce($"ThemeResolver: Bundle '{contextTheme.ThemeId}' has no board theme. Falling back to the catalog default board theme.");
            }
            else
            {
                WarnOnce("ThemeResolver: No bundle context was supplied for board resolution. Falling back to the catalog default board theme.");
            }

            BoardThemeSO defaultBoardTheme = catalog?.DefaultTheme?.BoardTheme;
            if (defaultBoardTheme != null)
            {
                return defaultBoardTheme;
            }

            WarnOnce("ThemeResolver: Unable to resolve a board theme because the catalog default bundle has no board theme.");
            return null;
        }

        /// <summary>
        /// Resolves a board and the UI theme of the pack that owns it, as one atomic pair.
        /// UI is never resolved by its own id: the board's pack is the single source of truth.
        /// </summary>
        public static bool ResolveBoardWithUi(
            ThemeCatalogSO catalog,
            string requestedBoardId,
            out BoardThemeSO board,
            out UiThemeSO ui,
            out ThemeDefinitionSO pack)
        {
            board = ResolveBoard(catalog, requestedBoardId, catalog?.DefaultTheme);
            ui = null;
            pack = null;
            if (board == null)
            {
                return false;
            }

            if (!catalog.TryGetPackForPart(ThemeCategory.Board, board.ThemeId, out pack) || pack == null)
            {
                WarnOnce($"ThemeResolver: Board theme '{board.ThemeId}' has no owning pack. Falling back to the catalog default pack for UI.");
                pack = catalog.DefaultTheme;
            }

            ui = pack != null ? pack.UiTheme : null;
            if (ui == null)
            {
                WarnOnce($"ThemeResolver: Pack '{pack?.ThemeId ?? "<none>"}' has no UI theme. Falling back to the catalog default UI theme.");
                ui = catalog.DefaultTheme?.UiTheme;
            }

            return true;
        }

        public static ClearEffectSO ResolveClearEffect(ThemeCatalogSO catalog, string requestedId, ThemeDefinitionSO fallbackPack = null)
        {
            if (!string.IsNullOrEmpty(requestedId))
            {
                if (catalog != null && catalog.TryGetClearEffect(requestedId, out var clearEffect) && clearEffect != null)
                {
                    return clearEffect;
                }

                WarnOnce($"ThemeResolver: Requested clear effect '{requestedId}' was not found. Falling back to the default pack.");
            }

            if (fallbackPack != null && fallbackPack.ClearEffect != null)
            {
                return fallbackPack.ClearEffect;
            }

            ClearEffectSO defaultClearEffect = catalog?.DefaultTheme?.ClearEffect;
            if (defaultClearEffect != null)
            {
                return defaultClearEffect;
            }

            WarnOnce("ThemeResolver: Unable to resolve a clear effect because the catalog default pack has no clear effect.");
            return null;
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

using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Core;
using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Pure resolver mapping catalog definitions to UI presentation models and sprites.
    /// Handles preview sprite resolution from BallTheme or UiTheme, swatch slicing, and deduplication.
    /// </summary>
    public static class ThemePartResolver
    {
        public static List<ThemeItemModel> ResolveItems(ThemeCatalogSO catalog, ThemeCategory category)
        {
            var results = new List<ThemeItemModel>();
            if (catalog == null) return results;

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < catalog.Count; i++)
            {
                var bundle = catalog.ThemeAt(i);
                if (bundle == null) continue;

                switch (category)
                {
                    case ThemeCategory.Ball:
                    {
                        var ballTheme = bundle.BallTheme;
                        if (ballTheme == null) continue;

                        string partId = ballTheme.ThemeId;
                        if (!seenIds.Add(partId)) continue;

                        string displayName = !string.IsNullOrEmpty(ballTheme.DisplayName)
                            ? ballTheme.DisplayName
                            : bundle.DisplayName;

                        Sprite[] swatches = ResolveSwatches(catalog, bundle, ballTheme);
                        Sprite effectGlyph = bundle.ClearEffect != null ? bundle.ClearEffect.Thumbnail : bundle.Thumbnail;
                        bool isUnlocked = ballTheme.UnlockedByDefault && bundle.UnlockedByDefault;

                        results.Add(new ThemeItemModel(partId, displayName, bundle, swatches, effectGlyph, isUnlocked));
                        break;
                    }
                    case ThemeCategory.Board:
                    {
                        var boardTheme = bundle.BoardTheme;
                        if (boardTheme == null) continue;

                        string partId = boardTheme.ThemeId;
                        if (!seenIds.Add(partId)) continue;

                        string displayName = !string.IsNullOrEmpty(boardTheme.DisplayName)
                            ? boardTheme.DisplayName
                            : bundle.DisplayName;

                        Sprite effectGlyph = bundle.Thumbnail;
                        bool isUnlocked = boardTheme.UnlockedByDefault && bundle.UnlockedByDefault;

                        results.Add(new ThemeItemModel(partId, displayName, bundle, Array.Empty<Sprite>(), effectGlyph, isUnlocked));
                        break;
                    }
                    case ThemeCategory.ClearEffect:
                    {
                        var clearEffect = bundle.ClearEffect;
                        if (clearEffect == null) continue;

                        string partId = clearEffect.ThemeId;
                        if (!seenIds.Add(partId)) continue;

                        string displayName = !string.IsNullOrEmpty(clearEffect.DisplayName)
                            ? clearEffect.DisplayName
                            : bundle.DisplayName;

                        Sprite effectGlyph = clearEffect.Thumbnail ?? bundle.Thumbnail;
                        bool isUnlocked = clearEffect.UnlockedByDefault && bundle.UnlockedByDefault;

                        results.Add(new ThemeItemModel(partId, displayName, bundle, Array.Empty<Sprite>(), effectGlyph, isUnlocked));
                        break;
                    }
                    case ThemeCategory.Ui:
                    {
                        var uiTheme = bundle.UiTheme;
                        if (uiTheme == null) continue;

                        string partId = uiTheme.ThemeId;
                        if (!seenIds.Add(partId)) continue;

                        string displayName = !string.IsNullOrEmpty(uiTheme.DisplayName)
                            ? uiTheme.DisplayName
                            : bundle.DisplayName;

                        results.Add(new ThemeItemModel(partId, displayName, bundle, Array.Empty<Sprite>(), bundle.Thumbnail, uiTheme.UnlockedByDefault));
                        break;
                    }
                }
            }

            return results;
        }

        public static Sprite[] ResolveSwatches(ThemeCatalogSO catalog, ThemeDefinitionSO bundle, BallThemeSO ballTheme = null)
        {
            var swatches = new Sprite[3];
            var spriteSet = ResolveSpriteSet(catalog, bundle, ballTheme);
            if (spriteSet != null)
            {
                swatches[0] = spriteSet.GetSprite(BallColor.Red);
                swatches[1] = spriteSet.GetSprite(BallColor.Green);
                swatches[2] = spriteSet.GetSprite(BallColor.Blue);
            }
            return swatches;
        }

        public static Sprite[] ResolvePreviewSprites(ThemeCatalogSO catalog, ThemeCategory category, string partId, ThemeDefinitionSO contextBundle = null)
        {
            var sprites = new Sprite[7];
            if (catalog == null) return sprites;

            ThemeDefinitionSO resolvedBundle = contextBundle;
            BallThemeSO resolvedBall = null;

            if (resolvedBundle == null)
            {
                resolvedBundle = ResolveBundle(catalog, category, partId);
            }

            if (resolvedBundle != null)
            {
                resolvedBall = resolvedBundle.BallTheme;
            }

            if (resolvedBall == null && category == ThemeCategory.Ball)
            {
                catalog.TryGetBallTheme(partId, out resolvedBall);
            }

            FillPreviewSprites(sprites, ResolveSpriteSet(catalog, resolvedBundle, resolvedBall));
            return sprites;
        }

        /// <summary>The 7 preview gems for a specific ball theme, independent of any board/UI selection.</summary>
        public static Sprite[] ResolvePreviewSprites(ThemeCatalogSO catalog, BallThemeSO ballTheme)
        {
            var sprites = new Sprite[7];
            ThemeDefinitionSO pack = null;
            if (catalog != null && ballTheme != null)
            {
                catalog.TryGetPackForPart(ThemeCategory.Ball, ballTheme.ThemeId, out pack);
            }
            FillPreviewSprites(sprites, ResolveSpriteSet(catalog, pack, ballTheme));
            return sprites;
        }

        private static void FillPreviewSprites(Sprite[] sprites, UiPreviewSpriteSetSO spriteSet)
        {
            if (spriteSet == null) return;
            sprites[0] = spriteSet.GetSprite(BallColor.Red);
            sprites[1] = spriteSet.GetSprite(BallColor.Orange);
            sprites[2] = spriteSet.GetSprite(BallColor.Yellow);
            sprites[3] = spriteSet.GetSprite(BallColor.Green);
            sprites[4] = spriteSet.GetSprite(BallColor.Cyan);
            sprites[5] = spriteSet.GetSprite(BallColor.Purple);
            sprites[6] = spriteSet.GetSprite(BallColor.Blue);
        }

        public static UiPreviewSpriteSetSO ResolveSpriteSet(ThemeCatalogSO catalog, ThemeDefinitionSO bundle, BallThemeSO ballTheme = null)
        {
            // DA2 priority: ballTheme.PreviewSpriteSet -> bundle.UiTheme.PreviewSpriteSet -> catalog default
            if (ballTheme != null && ballTheme.PreviewSpriteSet != null)
            {
                return ballTheme.PreviewSpriteSet;
            }

            if (bundle != null && bundle.BallTheme != null && bundle.BallTheme.PreviewSpriteSet != null)
            {
                return bundle.BallTheme.PreviewSpriteSet;
            }

            if (bundle != null && bundle.UiTheme != null && bundle.UiTheme.PreviewSpriteSet != null)
            {
                return bundle.UiTheme.PreviewSpriteSet;
            }

            if (catalog != null && catalog.DefaultTheme != null && catalog.DefaultTheme.UiTheme != null)
            {
                return catalog.DefaultTheme.UiTheme.PreviewSpriteSet;
            }

            return null;
        }

        public static ThemeDefinitionSO ResolveBundle(ThemeCatalogSO catalog, ThemeCategory category, string partId)
        {
            if (catalog == null || string.IsNullOrEmpty(partId)) return null;

            for (int i = 0; i < catalog.Count; i++)
            {
                var bundle = catalog.ThemeAt(i);
                if (bundle == null) continue;

                switch (category)
                {
                    case ThemeCategory.Ball:
                        if (bundle.BallTheme != null && string.Equals(bundle.BallTheme.ThemeId, partId, StringComparison.OrdinalIgnoreCase))
                            return bundle;
                        break;
                    case ThemeCategory.Board:
                        if (bundle.BoardTheme != null && string.Equals(bundle.BoardTheme.ThemeId, partId, StringComparison.OrdinalIgnoreCase))
                            return bundle;
                        break;
                    case ThemeCategory.ClearEffect:
                        if (bundle.ClearEffect != null && string.Equals(bundle.ClearEffect.ThemeId, partId, StringComparison.OrdinalIgnoreCase))
                            return bundle;
                        break;
                    case ThemeCategory.Ui:
                        if (bundle.UiTheme != null && string.Equals(bundle.UiTheme.ThemeId, partId, StringComparison.OrdinalIgnoreCase))
                            return bundle;
                        break;
                }
            }

            // Fallback: check bundle id itself
            if (catalog.TryGetTheme(partId, out var directBundle))
            {
                return directBundle;
            }

            return catalog.DefaultTheme;
        }
    }
}

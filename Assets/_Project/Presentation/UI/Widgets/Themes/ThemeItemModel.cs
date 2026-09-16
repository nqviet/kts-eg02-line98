using System;
using UnityEngine;
using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Presentation model representing an item in the theme list.
    /// Holds part identity, display name, owning bundle, preview swatches, and unlock state.
    /// </summary>
    public readonly struct ThemeItemModel
    {
        public readonly string PartId;
        public readonly string DisplayName;
        public readonly ThemeDefinitionSO Bundle;
        public readonly Sprite[] Swatches; // 3 ball sprites (indices 0, 3, 6)
        public readonly Sprite EffectGlyph;
        public readonly bool IsUnlocked;

        public ThemeItemModel(
            string partId,
            string displayName,
            ThemeDefinitionSO bundle,
            Sprite[] swatches,
            Sprite effectGlyph = null,
            bool isUnlocked = true)
        {
            PartId = partId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Bundle = bundle;
            Swatches = swatches ?? Array.Empty<Sprite>();
            EffectGlyph = effectGlyph;
            IsUnlocked = isUnlocked;
        }
    }
}

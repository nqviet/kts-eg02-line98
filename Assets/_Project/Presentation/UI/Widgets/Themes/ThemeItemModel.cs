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
        public readonly Material[] BoardMaterials; // [0] = Frame material, [1] = Cell material

        public ThemeItemModel(
            string partId,
            string displayName,
            ThemeDefinitionSO bundle,
            Sprite[] swatches,
            Sprite effectGlyph = null,
            bool isUnlocked = true,
            Material[] boardMaterials = null)
        {
            PartId = partId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Bundle = bundle;
            Swatches = swatches ?? Array.Empty<Sprite>();
            EffectGlyph = effectGlyph;
            IsUnlocked = isUnlocked;
            BoardMaterials = boardMaterials ?? Array.Empty<Material>();
        }
    }
}

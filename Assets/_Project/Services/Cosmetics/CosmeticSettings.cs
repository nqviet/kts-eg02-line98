using System;

namespace Line98.Services
{
    /// <summary>
    /// Persisted cosmetic selections under isolated storage key 'line98_cosmetics'.
    /// v3: three independent part axes. UI is derived from the board's pack and never persisted.
    /// </summary>
    [Serializable]
    public sealed class CosmeticSettings
    {
        public const int CurrentVersion = 3;

        public int Version = CurrentVersion;

        // Deliberately unset: an empty id resolves to whatever the catalog's default pack provides,
        // so the shipped default theme is owned by ThemeCatalogSO alone and never duplicated here.
        public string BallThemeId = string.Empty;
        public string BoardThemeId = string.Empty;
        public string ClearEffectThemeId = string.Empty;
    }
}

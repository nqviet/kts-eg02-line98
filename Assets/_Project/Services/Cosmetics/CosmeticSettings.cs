using System;
using Line98.Data;

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
        public string BallThemeId = ThemeIds.Classic;
        public string BoardThemeId = ThemeIds.Classic;
        public string ClearEffectThemeId = ThemeIds.Classic;
    }
}

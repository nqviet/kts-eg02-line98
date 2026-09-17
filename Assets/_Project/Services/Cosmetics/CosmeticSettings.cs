using System;
using Line98.Data;

namespace Line98.Services
{
    /// <summary>
    /// Persisted cosmetic selections under isolated storage key 'line98_cosmetics'.
    /// </summary>
    [Serializable]
    public sealed class CosmeticSettings
    {
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public string ThemeId = ThemeIds.Classic;
        public string BallOverrideId;
        public string BoardOverrideId;
        public string UiOverrideId;
        public string ClearEffectOverrideId;
    }
}

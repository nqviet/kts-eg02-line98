using System;

namespace Line98.Services
{
    /// <summary>
    /// Persisted cosmetic selections under isolated storage key 'line98_cosmetics'.
    /// </summary>
    [Serializable]
    public sealed class CosmeticSettings
    {
        public int Version = 1;
        public string ThemeId = "classic";
        public string BallOverrideId;
        public string BoardOverrideId;
        public string UiOverrideId;
        public string ClearEffectOverrideId;
    }
}

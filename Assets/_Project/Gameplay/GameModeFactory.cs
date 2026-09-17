using Line98.Core;
using Line98.Data;

namespace Line98.Gameplay
{
    public readonly struct ModeSpec
    {
        public readonly uint DailySeed;
        public readonly string DailyDate;
        public readonly int DailySeedVersion;
        public readonly ScoreRules? ScoreRules;
        public readonly SpawnRules? SpawnRules;
        public readonly int FreeUndoCount;
        public readonly int MaxRewardedUndos;

        public ModeSpec(
            uint dailySeed = 0,
            string dailyDate = null,
            int dailySeedVersion = 1,
            ScoreRules? scoreRules = null,
            SpawnRules? spawnRules = null,
            int freeUndoCount = 3,
            int maxRewardedUndos = 3)
        {
            DailySeed = dailySeed;
            DailyDate = dailyDate;
            DailySeedVersion = dailySeedVersion;
            ScoreRules = scoreRules;
            SpawnRules = spawnRules;
            FreeUndoCount = freeUndoCount;
            MaxRewardedUndos = maxRewardedUndos;
        }
    }

    public static class GameModeFactory
    {
        public static bool IsKnownMode(string modeId)
        {
            if (string.IsNullOrEmpty(modeId)) return false;
            string id = modeId.ToLowerInvariant();
            return id == "classic" || id == "daily" || id == "zen";
        }

        public static IGameModeStrategy Create(string modeId, ModeSpec spec = default)
        {
            string id = !string.IsNullOrEmpty(modeId) ? modeId.ToLowerInvariant() : "classic";
            switch (id)
            {
                case "daily":
                    return new DailyChallengeMode(
                        spec.DailyDate ?? System.DateTime.UtcNow.ToString("yyyy-MM-dd"),
                        spec.DailySeed != 0 ? spec.DailySeed : (uint?)null,
                        spec.ScoreRules,
                        spec.SpawnRules);

                case "zen":
                    return new ZenMode(spec.SpawnRules);

                case "classic":
                default:
                    return new ClassicMode(
                        spec.ScoreRules,
                        spec.SpawnRules,
                        spec.FreeUndoCount > 0 ? spec.FreeUndoCount : 3,
                        spec.MaxRewardedUndos > 0 ? spec.MaxRewardedUndos : 3);
            }
        }
    }
}

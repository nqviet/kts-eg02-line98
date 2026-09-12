using Line98.Core;
using Line98.Data;

namespace Line98.Gameplay
{
    public interface IGameModeStrategy
    {
        GameModeType ModeType { get; }
        string ModeId { get; }
        string DisplayName { get; }
        bool ScoringEnabled { get; }
        bool AdsEnabled { get; }
        bool UndoAllowed { get; }
        bool HintsAllowed { get; }
        bool RecordsStatistics { get; }

        SpawnRules GetSpawnRules(int totalLinesCleared);
        ScoreRules GetScoreRules();
        uint GenerateSeed();
    }

    public class ClassicMode : IGameModeStrategy
    {
        private readonly ScoreRules m_ScoreRules;
        private readonly SpawnRules m_SpawnRules;

        public GameModeType ModeType => GameModeType.Classic;
        public string ModeId => "classic";
        public string DisplayName => "Classic";
        public bool ScoringEnabled => true;
        public bool AdsEnabled => true;
        public bool UndoAllowed => true;
        public bool HintsAllowed => true;
        public bool RecordsStatistics => true;

        public ClassicMode(ScoreRules? scoreRules = null, SpawnRules? spawnRules = null)
        {
            m_ScoreRules = scoreRules ?? ScoreRules.Default;
            m_SpawnRules = spawnRules ?? SpawnRules.Default;
        }

        public SpawnRules GetSpawnRules(int totalLinesCleared) => m_SpawnRules;
        public ScoreRules GetScoreRules() => m_ScoreRules;

        public uint GenerateSeed()
        {
            return (uint)(System.DateTime.UtcNow.Ticks ^ System.Guid.NewGuid().GetHashCode());
        }
    }

    public class DailyChallengeMode : IGameModeStrategy
    {
        private readonly uint m_DailySeed;
        private readonly ScoreRules m_ScoreRules;
        private readonly SpawnRules m_SpawnRules;

        public GameModeType ModeType => GameModeType.DailyChallenge;
        public string ModeId => "daily";
        public string DisplayName => "Daily Challenge";
        public bool ScoringEnabled => true;
        public bool AdsEnabled => true;
        public bool UndoAllowed => false; // Strictly disabled per ADR D4 / Gap #7
        public bool HintsAllowed => true;
        public bool RecordsStatistics => true;

        public DailyChallengeMode(string dateString, ScoreRules? scoreRules = null, SpawnRules? spawnRules = null)
        {
            m_DailySeed = ComputeFnv1a32($"{dateString}-v1");
            m_ScoreRules = scoreRules ?? ScoreRules.Default;
            m_SpawnRules = spawnRules ?? SpawnRules.Default;
        }

        public SpawnRules GetSpawnRules(int totalLinesCleared) => m_SpawnRules;
        public ScoreRules GetScoreRules() => m_ScoreRules;
        public uint GenerateSeed() => m_DailySeed;

        public static uint ComputeFnv1a32(string text)
        {
            uint hash = 2166136261U;
            if (string.IsNullOrEmpty(text)) return hash;
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619U;
            }
            return hash;
        }
    }

    public class ZenMode : IGameModeStrategy
    {
        private readonly SpawnRules m_SpawnRules;

        public GameModeType ModeType => GameModeType.Zen;
        public string ModeId => "zen";
        public string DisplayName => "Zen";
        public bool ScoringEnabled => false;
        public bool AdsEnabled => false;
        public bool UndoAllowed => true; // Unlimited in Zen
        public bool HintsAllowed => true;
        public bool RecordsStatistics => false;

        public ZenMode(SpawnRules? spawnRules = null)
        {
            m_SpawnRules = spawnRules ?? SpawnRules.Default;
        }

        public SpawnRules GetSpawnRules(int totalLinesCleared) => m_SpawnRules;
        public ScoreRules GetScoreRules() => ScoreRules.Default;
        public uint GenerateSeed() => 0xCAFEF00D;
    }
}

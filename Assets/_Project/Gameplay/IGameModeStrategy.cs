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
        bool UnlimitedUndo { get; }

        string DisplayNameLocKey { get; }
        bool PresentsScore { get; }
        string AmbienceKey { get; }
        string MotionPresetId { get; }
        bool UsesDailySeed { get; }
        int FreeUndoCount { get; }
        int MaxRewardedUndos { get; }

        SpawnRules GetSpawnRules(int totalLinesCleared);
        ScoreRules GetScoreRules();
        uint GenerateSeed();
    }

    public class ClassicMode : IGameModeStrategy
    {
        private readonly ScoreRules m_ScoreRules;
        private readonly SpawnRules m_SpawnRules;
        private readonly int m_FreeUndoCount;
        private readonly int m_MaxRewardedUndos;

        public GameModeType ModeType => GameModeType.Classic;
        public string ModeId => "classic";
        public string DisplayName => "Classic";
        public bool ScoringEnabled => true;
        public bool AdsEnabled => true;
        public bool UndoAllowed => true;
        public bool HintsAllowed => true;
        public bool RecordsStatistics => true;
        public bool UnlimitedUndo => false;

        public string DisplayNameLocKey => "ui.mode.classic";
        public bool PresentsScore => true;
        public string AmbienceKey => "bgm_classic_main";
        public string MotionPresetId => "default";
        public bool UsesDailySeed => false;
        public int FreeUndoCount => m_FreeUndoCount;
        public int MaxRewardedUndos => m_MaxRewardedUndos;

        public ClassicMode(
            ScoreRules? scoreRules = null,
            SpawnRules? spawnRules = null,
            int freeUndoCount = 3,
            int maxRewardedUndos = 3)
        {
            m_ScoreRules = scoreRules ?? ScoreRules.Default;
            m_SpawnRules = spawnRules ?? SpawnRules.Default;
            m_FreeUndoCount = freeUndoCount;
            m_MaxRewardedUndos = maxRewardedUndos;
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
        private readonly string m_DateString;
        private readonly int m_SeedVersion;
        private readonly ScoreRules m_ScoreRules;
        private readonly SpawnRules m_SpawnRules;

        public GameModeType ModeType => GameModeType.DailyChallenge;
        public string ModeId => "daily";
        public string DisplayName => "Daily Challenge";
        public bool ScoringEnabled => true;
        public bool AdsEnabled => true;
        public bool UndoAllowed => false; // Strictly disabled in Daily
        public bool HintsAllowed => true;
        public bool RecordsStatistics => true;
        public bool UnlimitedUndo => false;

        public string DisplayNameLocKey => "ui.mode.daily";
        public bool PresentsScore => true;
        public string AmbienceKey => "bgm_classic_main";
        public string MotionPresetId => "default";
        public bool UsesDailySeed => true;
        public int FreeUndoCount => 0;
        public int MaxRewardedUndos => 0;
        public string DateString => m_DateString;
        public string TargetDate => m_DateString;
        public int SeedVersion => m_SeedVersion;

        public DailyChallengeMode(
            string dateString,
            uint? explicitSeed = null,
            ScoreRules? scoreRules = null,
            SpawnRules? spawnRules = null,
            int seedVersion = 1)
        {
            m_DateString = dateString;
            m_SeedVersion = seedVersion;
            m_DailySeed = explicitSeed ?? ComputeFnv1a32($"{dateString}-v{seedVersion}");
            m_ScoreRules = scoreRules ?? ScoreRules.Default;
            m_SpawnRules = spawnRules ?? SpawnRules.Default;
        }

        public DailyChallengeMode(
            string dateString,
            ScoreRules? scoreRules,
            SpawnRules? spawnRules = null,
            int seedVersion = 1)
            : this(dateString, null, scoreRules, spawnRules, seedVersion)
        {
        }

        public SpawnRules GetSpawnRules(int totalLinesCleared) => m_SpawnRules;
        public ScoreRules GetScoreRules() => m_ScoreRules;
        public uint GenerateSeed() => m_DailySeed;

        public static uint ComputeFnv1a32(string text)
        {
            return Fnv1a32.Compute(text);
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
        public bool RecordsStatistics => false; // Zen does not pollute statistics
        public bool UnlimitedUndo => true;

        public string DisplayNameLocKey => "ui.mode.zen";
        public bool PresentsScore => false;
        public string AmbienceKey => "bgm_zen_ambience";
        public string MotionPresetId => "zen";
        public bool UsesDailySeed => false;
        public int FreeUndoCount => int.MaxValue;
        public int MaxRewardedUndos => 0;

        public ZenMode(SpawnRules? spawnRules = null)
        {
            m_SpawnRules = spawnRules ?? SpawnRules.Default;
        }

        public SpawnRules GetSpawnRules(int totalLinesCleared) => m_SpawnRules;
        public ScoreRules GetScoreRules() => ScoreRules.Default;
        public uint GenerateSeed() => 0xCAFEF00D;
    }
}

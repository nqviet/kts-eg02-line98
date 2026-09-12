using System;
using Line98.Core;
using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Line98/Data/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [SerializeField] private int m_BoardSize = 9;
        [SerializeField] private int m_SpawnCount = 3;
        [SerializeField] private int m_PreviewCount = 3;
        [SerializeField] private int m_FreeUndoCount = 3;
        [SerializeField] private int m_MaxRewardedUndos = 3;

        public int BoardSize => m_BoardSize;
        public int SpawnCount => m_SpawnCount;
        public int PreviewCount => m_PreviewCount;
        public int FreeUndoCount => m_FreeUndoCount;
        public int MaxRewardedUndos => m_MaxRewardedUndos;
    }

    [CreateAssetMenu(fileName = "ScoreTable", menuName = "Line98/Data/Score Table")]
    public class ScoreTableSO : ScriptableObject
    {
        [SerializeField] private int m_Score5 = 100;
        [SerializeField] private int m_Score6 = 180;
        [SerializeField] private int m_Score7 = 300;
        [SerializeField] private int m_Score8 = 500;
        [SerializeField] private int m_Score9 = 800;
        [SerializeField] private float m_ComboStep = 0.25f;
        [SerializeField] private float m_MaxComboMultiplier = 2.0f;

        public ScoreRules ToRules()
        {
            return new ScoreRules(
                m_Score5,
                m_Score6,
                m_Score7,
                m_Score8,
                m_Score9,
                m_ComboStep,
                m_MaxComboMultiplier);
        }
    }

    [CreateAssetMenu(fileName = "SpawnColorPolicy", menuName = "Line98/Data/Spawn Color Policy")]
    public class SpawnColorPolicySO : ScriptableObject
    {
        [SerializeField] private int m_InitialActiveColors = 5;
        [SerializeField] private int m_LinesForSixColors = 10;
        [SerializeField] private int m_LinesForSevenColors = 25;
        [SerializeField] private int m_SpawnCount = 3;

        public SpawnRules ToRules()
        {
            return new SpawnRules(
                m_InitialActiveColors,
                m_LinesForSixColors,
                m_LinesForSevenColors,
                m_SpawnCount);
        }
    }

    public enum GameModeType : byte
    {
        Classic,
        DailyChallenge,
        Zen
    }

    [CreateAssetMenu(fileName = "ModeDefinition", menuName = "Line98/Data/Mode Definition")]
    public class ModeDefinitionSO : ScriptableObject
    {
        [SerializeField] private GameModeType m_ModeType;
        [SerializeField] private string m_ModeId = "classic";
        [SerializeField] private string m_DisplayName = "Classic";
        [SerializeField] private bool m_ScoringEnabled = true;
        [SerializeField] private bool m_AdsEnabled = true;
        [SerializeField] private bool m_UndoAllowed = true;
        [SerializeField] private bool m_HintsAllowed = true;
        [SerializeField] private bool m_RecordsStatistics = true;

        public GameModeType ModeType => m_ModeType;
        public string ModeId => m_ModeId;
        public string DisplayName => m_DisplayName;
        public bool ScoringEnabled => m_ScoringEnabled;
        public bool AdsEnabled => m_AdsEnabled;
        public bool UndoAllowed => m_UndoAllowed;
        public bool HintsAllowed => m_HintsAllowed;
        public bool RecordsStatistics => m_RecordsStatistics;
    }

    [CreateAssetMenu(fileName = "DailyChallengeConfig", menuName = "Line98/Data/Daily Challenge Config")]
    public class DailyChallengeConfigSO : ScriptableObject
    {
        [SerializeField] private string m_SeedFormat = "{0:yyyy-MM-dd}-v1";
        [SerializeField] private int m_StreakGraceHours = 24;

        public string SeedFormat => m_SeedFormat;
        public int StreakGraceHours => m_StreakGraceHours;
    }

    public enum AchievementMetric
    {
        FirstLine,
        FiveLine,
        LongShot7,
        Perfect9,
        Score1000,
        Score10000,
        Score50000,
        Moves100,
        Streak7Days,
        Streak30Days
    }

    [CreateAssetMenu(fileName = "Achievement", menuName = "Line98/Data/Achievement")]
    public class AchievementSO : ScriptableObject
    {
        [SerializeField] private string m_Id;
        [SerializeField] private AchievementMetric m_Metric;
        [SerializeField] private int m_Threshold;
        [SerializeField] private string m_LocKey;

        public string Id => m_Id;
        public AchievementMetric Metric => m_Metric;
        public int Threshold => m_Threshold;
        public string LocKey => m_LocKey;
    }
}

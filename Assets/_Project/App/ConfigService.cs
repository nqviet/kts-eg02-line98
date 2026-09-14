using Line98.Core;
using Line98.Data;
using UnityEngine;

namespace Line98.App
{
    /// <summary>
    /// Single authoritative access point for data-driven configuration per GDD P0.3.
    /// Provides ScoreRules, SpawnRules, and GameConfig with guarded fallback detection.
    /// </summary>
    public sealed class ConfigService
    {
        private readonly ScoreTableSO m_ScoreTable;
        private readonly SpawnColorPolicySO m_SpawnColorPolicy;
        private readonly GameConfigSO m_GameConfig;

        public ScoreTableSO ScoreTable => m_ScoreTable;
        public SpawnColorPolicySO SpawnColorPolicy => m_SpawnColorPolicy;
        public GameConfigSO GameConfig => m_GameConfig;

        public ConfigService(ScoreTableSO scoreTable = null, SpawnColorPolicySO spawnColorPolicy = null, GameConfigSO gameConfig = null)
        {
            m_ScoreTable = scoreTable;
            m_SpawnColorPolicy = spawnColorPolicy;
            m_GameConfig = gameConfig;
        }

        public ScoreRules GetScoreRules()
        {
            if (m_ScoreTable != null)
            {
                return m_ScoreTable.ToRules();
            }

            WarnFallback(nameof(ScoreTableSO));
            return ScoreRules.Default;
        }

        public SpawnRules GetSpawnRules()
        {
            if (m_SpawnColorPolicy != null)
            {
                return m_SpawnColorPolicy.ToRules();
            }

            WarnFallback(nameof(SpawnColorPolicySO));
            return SpawnRules.Default;
        }

        private static bool s_HasWarned;

        private static void WarnFallback(string configName)
        {
            if (!s_HasWarned)
            {
                s_HasWarned = true;
                Debug.LogWarning($"[ConfigService] Fallback to code defaults used for {configName}. Assign config assets to prevent configuration drift.");
            }
        }
    }
}

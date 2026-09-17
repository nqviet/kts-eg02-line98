using System;
using System.Collections.Generic;
using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "AchievementCatalog", menuName = "Line98/Data/Achievement Catalog")]
    public sealed class AchievementCatalogSO : ScriptableObject
    {
        [SerializeField] private AchievementSO[] m_Achievements = Array.Empty<AchievementSO>();

        public IReadOnlyList<AchievementSO> Achievements => m_Achievements;
        public int Count => m_Achievements != null ? m_Achievements.Length : 0;

        public bool TryGet(string id, out AchievementSO achievement)
        {
            achievement = null;
            if (string.IsNullOrEmpty(id) || m_Achievements == null) return false;

            for (int i = 0; i < m_Achievements.Length; i++)
            {
                if (m_Achievements[i] != null && m_Achievements[i].Id == id)
                {
                    achievement = m_Achievements[i];
                    return true;
                }
            }

            return false;
        }

        public void SetAchievementsForRuntime(IEnumerable<AchievementSO> achievements)
        {
            m_Achievements = achievements != null ? new List<AchievementSO>(achievements).ToArray() : Array.Empty<AchievementSO>();
        }

        public void InitializeRuntime(IEnumerable<AchievementSO> achievements) => SetAchievementsForRuntime(achievements);
    }
}

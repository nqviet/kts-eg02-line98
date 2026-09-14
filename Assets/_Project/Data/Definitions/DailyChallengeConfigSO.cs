using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "DailyChallengeConfig", menuName = "Line98/Data/Daily Challenge Config")]
    public class DailyChallengeConfigSO : ScriptableObject
    {
        [SerializeField] private string m_SeedFormat = "{0:yyyy-MM-dd}-v1";
        [SerializeField] private int m_StreakGraceHours = 24;

        public string SeedFormat => m_SeedFormat;
        public int StreakGraceHours => m_StreakGraceHours;
    }
}

using UnityEngine;

namespace Line98.Data
{
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

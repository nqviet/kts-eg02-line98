using UnityEngine;
using Line98.Core;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "Achievement", menuName = "Line98/Data/Achievement")]
    public class AchievementSO : ScriptableObject
    {
        [SerializeField] private string m_Id;
        [SerializeField] private AchievementMetric m_Metric;
        [SerializeField] private AchievementScope m_Scope;
        [SerializeField] private int m_Threshold;
        [SerializeField] private string m_LocKey;
        [SerializeField] private string m_DisplayName;
        [SerializeField] private Sprite m_Icon;

        public string Id => m_Id;
        public AchievementMetric Metric => m_Metric;
        public AchievementScope Scope => m_Scope;
        public int Threshold => m_Threshold;
        public string LocKey => m_LocKey;
        public string DisplayName => m_DisplayName;
        public Sprite Icon => m_Icon;

        public void InitializeRuntime(
            string id,
            AchievementMetric metric,
            AchievementScope scope,
            int threshold,
            string locKey,
            string displayName = null,
            Sprite icon = null)
        {
            m_Id = id;
            m_Metric = metric;
            m_Scope = scope;
            m_Threshold = threshold;
            m_LocKey = locKey;
            m_DisplayName = displayName;
            m_Icon = icon;
        }
    }
}

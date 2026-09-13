using System;
using UnityEngine;

namespace Line98.Data
{
    [Serializable]
    public struct FeedbackTierData
    {
        public int MinLen;
        public int MaxLen;
        public float AnimationScale;
        public float StaggerMs;
        public float ShakeAmp;
        public float GlowIntensity;
        public float HoldMs;
        public float RibbonWidth;
        public bool ShowBanner;
        public bool AllowSlowMo;
        public string VfxKey;
        public string AudioKey;
    }

    [CreateAssetMenu(fileName = "FeedbackProfile_Tiers", menuName = "Line98/Definitions/Feedback Profile")]
    public class FeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private FeedbackTierData[] m_Tiers;
        public FeedbackTierData[] Tiers => m_Tiers;

        public Line98.Core.FeedbackRules ToRules()
        {
            if (m_Tiers == null || m_Tiers.Length == 0)
            {
                return Line98.Core.FeedbackRules.Default;
            }

            var rules = new Line98.Core.FeedbackTierRule[m_Tiers.Length];
            for (int i = 0; i < m_Tiers.Length; i++)
            {
                var t = m_Tiers[i];
                rules[i] = new Line98.Core.FeedbackTierRule(
                    t.MinLen,
                    t.MaxLen,
                    t.AnimationScale,
                    t.StaggerMs,
                    t.ShakeAmp,
                    t.GlowIntensity,
                    t.HoldMs,
                    t.RibbonWidth,
                    t.ShowBanner,
                    t.AllowSlowMo,
                    t.VfxKey,
                    t.AudioKey);
            }
            return new Line98.Core.FeedbackRules(rules);
        }
    }
}

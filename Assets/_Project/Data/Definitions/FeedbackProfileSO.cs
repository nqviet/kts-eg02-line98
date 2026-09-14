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

        public void SetTiers(FeedbackTierData[] tiers)
        {
            m_Tiers = tiers;
        }

        private static FeedbackProfileSO s_Default;
        public static FeedbackProfileSO Default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = CreateInstance<FeedbackProfileSO>();
                    s_Default.name = "FeedbackProfile_CodeDefault";
                    s_Default.m_Tiers = new FeedbackTierData[]
                    {
                        new FeedbackTierData { MinLen = 5, MaxLen = 5, AnimationScale = 1.00f, StaggerMs = 22f, ShakeAmp = 0.00f, GlowIntensity = 1.0f, HoldMs = 0f, RibbonWidth = 0.08f, ShowBanner = false, AllowSlowMo = false, VfxKey = "ClearTier1", AudioKey = "sfx_clear_tier1" },
                        new FeedbackTierData { MinLen = 6, MaxLen = 7, AnimationScale = 1.25f, StaggerMs = 24f, ShakeAmp = 0.07f, GlowIntensity = 1.2f, HoldMs = 60f, RibbonWidth = 0.10f, ShowBanner = false, AllowSlowMo = false, VfxKey = "ClearTier2", AudioKey = "sfx_clear_tier2" },
                        new FeedbackTierData { MinLen = 8, MaxLen = 8, AnimationScale = 1.45f, StaggerMs = 26f, ShakeAmp = 0.14f, GlowIntensity = 1.5f, HoldMs = 180f, RibbonWidth = 0.12f, ShowBanner = false, AllowSlowMo = false, VfxKey = "ClearTier3", AudioKey = "sfx_clear_tier3" },
                        new FeedbackTierData { MinLen = 9, MaxLen = 81, AnimationScale = 2.10f, StaggerMs = 28f, ShakeAmp = 0.18f, GlowIntensity = 2.0f, HoldMs = 400f, RibbonWidth = 0.14f, ShowBanner = true, AllowSlowMo = true, VfxKey = "ClearTier4", AudioKey = "sfx_clear_tier4_perfect" }
                    };
                }
                return s_Default;
            }
        }

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

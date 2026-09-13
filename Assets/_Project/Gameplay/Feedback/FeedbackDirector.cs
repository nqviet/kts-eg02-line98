using Line98.Core;

namespace Line98.Gameplay
{
    /// <summary>
    /// Evaluates move clear results and determines presentation timing, tier, and feedback scalars.
    /// Emits scalar cues only; never accesses presentation assets per Architecture §8.
    /// </summary>
    public static class FeedbackDirector
    {
        public static FeedbackCue Evaluate(in ClearGroup clearGroup, in FeedbackRules rules)
        {
            if (clearGroup.IsEmpty)
            {
                return default;
            }

            int longestRun = clearGroup.LongestRun;
            FeedbackTierRule[] tiers = rules.Tiers ?? FeedbackRules.Default.Tiers;

            int selectedTierIndex = 0;
            for (int i = 0; i < tiers.Length; i++)
            {
                if (longestRun >= tiers[i].MinLength && longestRun <= tiers[i].MaxLength)
                {
                    selectedTierIndex = i;
                    break;
                }
                if (longestRun > tiers[i].MaxLength)
                {
                    selectedTierIndex = i;
                }
            }

            FeedbackTierRule rule = tiers[selectedTierIndex];
            int tierNumber = selectedTierIndex + 1;

            return new FeedbackCue(
                tier: tierNumber,
                animationScale: rule.AnimationScale,
                staggerMs: rule.StaggerMs,
                shakeAmp: rule.ShakeAmp,
                glowIntensity: rule.GlowIntensity,
                holdMs: rule.HoldMs,
                ribbonWidth: rule.RibbonWidth,
                showBanner: rule.ShowBanner,
                allowSlowMo: rule.AllowSlowMo,
                vfxKey: rule.VfxKey,
                audioKey: rule.AudioKey);
        }
    }
}

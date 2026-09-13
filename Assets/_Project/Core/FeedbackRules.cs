namespace Line98.Core
{
    public readonly struct FeedbackTierRule
    {
        public readonly int MinLength;
        public readonly int MaxLength;
        public readonly float AnimationScale;
        public readonly float StaggerMs;
        public readonly float ShakeAmp;
        public readonly float GlowIntensity;
        public readonly float HoldMs;
        public readonly float RibbonWidth;
        public readonly bool ShowBanner;
        public readonly bool AllowSlowMo;
        public readonly string VfxKey;
        public readonly string AudioKey;

        public FeedbackTierRule(
            int minLength,
            int maxLength,
            float animationScale,
            float staggerMs,
            float shakeAmp,
            float glowIntensity,
            float holdMs,
            float ribbonWidth,
            bool showBanner,
            bool allowSlowMo,
            string vfxKey,
            string audioKey)
        {
            MinLength = minLength;
            MaxLength = maxLength;
            AnimationScale = animationScale;
            StaggerMs = staggerMs;
            ShakeAmp = shakeAmp;
            GlowIntensity = glowIntensity;
            HoldMs = holdMs;
            RibbonWidth = ribbonWidth;
            ShowBanner = showBanner;
            AllowSlowMo = allowSlowMo;
            VfxKey = vfxKey;
            AudioKey = audioKey;
        }
    }

    public readonly struct FeedbackRules
    {
        public readonly FeedbackTierRule[] Tiers;

        public FeedbackRules(FeedbackTierRule[] tiers)
        {
            Tiers = tiers;
        }

        public static FeedbackRules Default => new FeedbackRules(new[]
        {
            new FeedbackTierRule(5, 5, 1.00f, 22f, 0.00f, 1.0f, 0f,   0.08f, false, false, "ClearTier1", "sfx_clear_tier1"),
            new FeedbackTierRule(6, 7, 1.25f, 24f, 0.07f, 1.2f, 60f,  0.10f, false, false, "ClearTier2", "sfx_clear_tier2"),
            new FeedbackTierRule(8, 8, 1.45f, 26f, 0.14f, 1.5f, 180f, 0.12f, false, false, "ClearTier3", "sfx_clear_tier3"),
            new FeedbackTierRule(9, 81, 2.10f, 28f, 0.18f, 2.0f, 400f, 0.14f, true,  true,  "ClearTier4", "sfx_clear_tier4_perfect")
        });
    }
}

namespace Line98.Gameplay
{
    /// <summary>
    /// Timeline cue emitted by Gameplay.FeedbackDirector to Presentation.
    /// Strictly contains scalars and catalog keys; never engine assets or prefabs.
    /// Preserves strict architecture separation per Architecture §8 and Animation §1.
    /// </summary>
    public readonly struct FeedbackCue
    {
        public readonly int Tier; // 1 (5 balls), 2 (6-7 balls), 3 (8 balls), 4 (9+ balls / Perfect)
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

        public FeedbackCue(
            int tier,
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
            Tier = tier;
            AnimationScale = animationScale;
            StaggerMs = staggerMs;
            ShakeAmp = shakeAmp;
            GlowIntensity = glowIntensity;
            HoldMs = holdMs;
            RibbonWidth = ribbonWidth;
            ShowBanner = showBanner;
            AllowSlowMo = allowSlowMo;
            VfxKey = vfxKey ?? string.Empty;
            AudioKey = audioKey ?? string.Empty;
        }
    }
}

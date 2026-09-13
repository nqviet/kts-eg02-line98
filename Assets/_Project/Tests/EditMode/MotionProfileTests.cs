using NUnit.Framework;
using Line98.Core;
using Line98.Gameplay;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class MotionProfileTests
    {
        [Test]
        public void FeedbackRules_AllFourTiersPresent()
        {
            var rules = FeedbackRules.Default;
            Assert.IsNotNull(rules.Tiers);
            Assert.AreEqual(4, rules.Tiers.Length);

            Assert.AreEqual(5, rules.Tiers[0].MinLength);
            Assert.AreEqual(5, rules.Tiers[0].MaxLength);

            Assert.AreEqual(6, rules.Tiers[1].MinLength);
            Assert.AreEqual(7, rules.Tiers[1].MaxLength);

            Assert.AreEqual(8, rules.Tiers[2].MinLength);
            Assert.AreEqual(8, rules.Tiers[2].MaxLength);

            Assert.AreEqual(9, rules.Tiers[3].MinLength);
        }

        [Test]
        public void FeedbackRules_MetricsMonotonicAcrossTiers()
        {
            var tiers = FeedbackRules.Default.Tiers;

            for (int i = 1; i < tiers.Length; i++)
            {
                Assert.GreaterOrEqual(tiers[i].AnimationScale, tiers[i - 1].AnimationScale, $"AnimationScale at tier {i + 1} must be >= tier {i}");
                Assert.GreaterOrEqual(tiers[i].HoldMs, tiers[i - 1].HoldMs, $"HoldMs at tier {i + 1} must be >= tier {i}");
                Assert.GreaterOrEqual(tiers[i].ShakeAmp, tiers[i - 1].ShakeAmp, $"ShakeAmp at tier {i + 1} must be >= tier {i}");
            }
        }

        [Test]
        public void FeedbackDirector_EvaluatesCorrectTiers()
        {
            var rules = FeedbackRules.Default;

            // Tier 1: 5 balls
            var group5 = new ClearGroup(new GridPos[5], 5, 1, BallColor.Red);
            var cue1 = FeedbackDirector.Evaluate(group5, rules);
            Assert.AreEqual(1, cue1.Tier);
            Assert.AreEqual("ClearTier1", cue1.VfxKey);
            Assert.IsFalse(cue1.ShowBanner);

            // Tier 2: 7 balls
            var group7 = new ClearGroup(new GridPos[7], 7, 1, BallColor.Red);
            var cue2 = FeedbackDirector.Evaluate(group7, rules);
            Assert.AreEqual(2, cue2.Tier);
            Assert.AreEqual("ClearTier2", cue2.VfxKey);
            Assert.IsFalse(cue2.ShowBanner);

            // Tier 3: 8 balls
            var group8 = new ClearGroup(new GridPos[8], 8, 1, BallColor.Red);
            var cue3 = FeedbackDirector.Evaluate(group8, rules);
            Assert.AreEqual(3, cue3.Tier);
            Assert.AreEqual("ClearTier3", cue3.VfxKey);

            // Tier 4: 9 balls (Perfect)
            var group9 = new ClearGroup(new GridPos[9], 9, 1, BallColor.Red);
            var cue4 = FeedbackDirector.Evaluate(group9, rules);
            Assert.AreEqual(4, cue4.Tier);
            Assert.AreEqual("ClearTier4", cue4.VfxKey);
            Assert.IsTrue(cue4.ShowBanner);
            Assert.IsTrue(cue4.AllowSlowMo);
        }
    }
}

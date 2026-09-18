using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Line98.Core;
using Line98.Data;
using Line98.Presentation;
using Line98.Presentation.Animation;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class MotionProfileAssetTests
    {
        private const string MotionProfileDefaultPath = "Assets/_Project/Content/Definitions/MotionProfile_Default.asset";
        private const string FeedbackProfileTiersPath = "Assets/_Project/Content/Definitions/FeedbackProfile_Tiers.asset";
        private const string MotionPresetZenPath = "Assets/_Project/Content/Definitions/MotionPreset_Zen.asset";
        private const string MotionPresetReducedMotionPath = "Assets/_Project/Content/Definitions/MotionPreset_ReducedMotion.asset";

        private GameObject m_TestRoot;
        private BoardView m_BoardView;
        private BallViewManager m_BallManager;
        private TweenRunner m_TweenRunner;

        [SetUp]
        public void SetUp()
        {
            m_TestRoot = new GameObject("MotionProfileTestRoot");
            m_BoardView = m_TestRoot.AddComponent<BoardView>();
            m_BoardView.Initialize();

            m_TweenRunner = new TweenRunner();
            m_BallManager = new BallViewManager();
            m_BallManager.Initialize(null, null, null, null, null, m_TestRoot.transform, m_TweenRunner);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_TestRoot != null)
            {
                Object.DestroyImmediate(m_TestRoot);
            }
        }

        [Test]
        public void MotionAndFeedbackAssets_LoadAndAreNotScriptless()
        {
            var motionProfile = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionProfileDefaultPath);
            Assert.IsNotNull(motionProfile, $"Failed to load MotionProfileSO at {MotionProfileDefaultPath}");

            var feedbackProfile = AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(FeedbackProfileTiersPath);
            Assert.IsNotNull(feedbackProfile, $"Failed to load FeedbackProfileSO at {FeedbackProfileTiersPath}");

            var zenPreset = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionPresetZenPath);
            Assert.IsNotNull(zenPreset, $"Failed to load Zen preset at {MotionPresetZenPath}");

            var reducedMotionPreset = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionPresetReducedMotionPath);
            Assert.IsNotNull(reducedMotionPreset, $"Failed to load ReducedMotion preset at {MotionPresetReducedMotionPath}");
        }

        [Test]
        public void FeedbackProfileAsset_CarriesFourTiersWithCorrectRangesAndKeys()
        {
            var feedbackProfile = AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(FeedbackProfileTiersPath);
            Assert.IsNotNull(feedbackProfile, $"Missing feedback profile asset at {FeedbackProfileTiersPath}");

            FeedbackRules rules = feedbackProfile.ToRules();
            Assert.IsNotNull(rules.Tiers, "Tiers array must not be null");
            Assert.AreEqual(4, rules.Tiers.Length, "Must have exactly 4 tiers");

            // Tier 1: 5
            Assert.AreEqual(5, rules.Tiers[0].MinLength);
            Assert.AreEqual(5, rules.Tiers[0].MaxLength);
            Assert.IsFalse(string.IsNullOrEmpty(rules.Tiers[0].VfxKey), "Tier 1 VfxKey must not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(rules.Tiers[0].AudioKey), "Tier 1 AudioKey must not be empty");

            // Tier 2: 6-7
            Assert.AreEqual(6, rules.Tiers[1].MinLength);
            Assert.AreEqual(7, rules.Tiers[1].MaxLength);
            Assert.IsFalse(string.IsNullOrEmpty(rules.Tiers[1].VfxKey), "Tier 2 VfxKey must not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(rules.Tiers[1].AudioKey), "Tier 2 AudioKey must not be empty");

            // Tier 3: 8
            Assert.AreEqual(8, rules.Tiers[2].MinLength);
            Assert.AreEqual(8, rules.Tiers[2].MaxLength);
            Assert.IsFalse(string.IsNullOrEmpty(rules.Tiers[2].VfxKey), "Tier 3 VfxKey must not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(rules.Tiers[2].AudioKey), "Tier 3 AudioKey must not be empty");

            // Tier 4: 9+
            Assert.AreEqual(9, rules.Tiers[3].MinLength);
            Assert.GreaterOrEqual(rules.Tiers[3].MaxLength, 9);
            Assert.IsFalse(string.IsNullOrEmpty(rules.Tiers[3].VfxKey), "Tier 4 VfxKey must not be empty");
            Assert.IsFalse(string.IsNullOrEmpty(rules.Tiers[3].AudioKey), "Tier 4 AudioKey must not be empty");
            Assert.IsTrue(rules.Tiers[3].ShowBanner, "Tier 4 must show banner");
        }

        [Test]
        public void FeedbackProfileAsset_MetricsMonotonicAcrossTiers()
        {
            var feedbackProfile = AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(FeedbackProfileTiersPath);
            Assert.IsNotNull(feedbackProfile, $"Missing feedback profile asset at {FeedbackProfileTiersPath}");

            var tiers = feedbackProfile.ToRules().Tiers;
            for (int i = 1; i < tiers.Length; i++)
            {
                Assert.GreaterOrEqual(tiers[i].AnimationScale, tiers[i - 1].AnimationScale,
                    $"AnimationScale at tier {i + 1} must be >= tier {i}");
                Assert.GreaterOrEqual(tiers[i].HoldMs, tiers[i - 1].HoldMs,
                    $"HoldMs at tier {i + 1} must be >= tier {i}");
                Assert.GreaterOrEqual(tiers[i].ShakeAmp, tiers[i - 1].ShakeAmp,
                    $"ShakeAmp at tier {i + 1} must be >= tier {i}");
                Assert.GreaterOrEqual(tiers[i].RibbonWidth, tiers[i - 1].RibbonWidth,
                    $"RibbonWidth at tier {i + 1} must be >= tier {i}");
            }
        }

        [Test]
        public void MotionProfileAsset_DefaultMatchesCodeDefaultValues()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionProfileDefaultPath);
            Assert.IsNotNull(asset, $"Missing motion profile asset at {MotionProfileDefaultPath}");

            Assert.AreEqual(300f, asset.MoveBaseMs);
            Assert.AreEqual(42f, asset.PerStepMinMs);
            Assert.AreEqual(85f, asset.PerStepMaxMs);
            Assert.AreEqual(40f, asset.AnticipationMs);
            Assert.AreEqual(160f, asset.LandingMs);
            Assert.AreEqual(22f, asset.StaggerPerCellMs);
            Assert.AreEqual(70f, asset.SpawnStaggerMs);
            Assert.AreEqual(10, asset.MaxFlightWaypoints);
            Assert.GreaterOrEqual(asset.PathPreviewMs, 0f);

            // Destination indicator timings live in config, never hardcoded (GDD P2.3).
            Assert.Greater(asset.DestRingInMs, 0f, "The destination ring must settle in over time, not pop.");
            Assert.Greater(asset.DestRingConvergeMs, 0f);
            Assert.Greater(asset.DestRingHoldMs, 0f);
            Assert.Greater(asset.DestRingFadeMs, 0f, "The ring must fade rather than vanish under the landing ball.");
            Assert.Greater(asset.DestRingInvalidMs, 0f);
            Assert.Less(asset.DestRingFadeMs, asset.MoveBaseMs, "The fade has to fit inside a flight to finish before landing.");
            Assert.That(asset.GhostAlpha, Is.InRange(0.05f, 0.45f), "The ghost ball must read as a hint, not as a second ball.");

            var reduced = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionPresetReducedMotionPath);
            Assert.IsNotNull(reduced, "Missing reduced motion preset");
            Assert.AreEqual(0f, reduced.PathPreviewMs, "Reduced motion preset must set PathPreviewMs to 0");
        }

        [Test]
        public void Behavioural_MoveAnimator_DifferentProfiles_ProduceDifferentDurations()
        {
            var defaultProfile = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionProfileDefaultPath);
            var zenProfile = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionPresetZenPath);

            Assert.IsNotNull(defaultProfile, "Missing default profile asset");
            Assert.IsNotNull(zenProfile, "Missing zen profile asset");

            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(0, 3);
            var path = new List<GridPos>
            {
                new GridPos(0, 0),
                new GridPos(0, 1),
                new GridPos(0, 2),
                new GridPos(0, 3)
            };

            // Spawn ball for animator 1
            m_BallManager.SpawnBall(from, BallColor.Red, m_BoardView.GridToWorld(from));
            var anim1 = new MoveAnimator(m_BoardView, m_BallManager, m_TweenRunner, defaultProfile);
            anim1.AnimateMove(from, to, path, null, null);

            // Re-spawn ball at 'from' for animator 2
            m_BallManager.SpawnBall(from, BallColor.Blue, m_BoardView.GridToWorld(from));
            var anim2 = new MoveAnimator(m_BoardView, m_BallManager, m_TweenRunner, zenProfile);
            anim2.AnimateMove(from, to, path, null, null);

            // Assert behavioural divergence driven by profiles
            Assert.AreNotEqual(anim1.StepDuration, anim2.StepDuration,
                "Different MotionProfiles must produce different StepDuration.");
            Assert.AreNotEqual(anim1.TotalFlightDuration, anim2.TotalFlightDuration,
                "Different MotionProfiles must produce different TotalFlightDuration.");
            Assert.AreNotEqual(anim1.LandingDuration, anim2.LandingDuration,
                "Different MotionProfiles must produce different LandingDuration.");

            // Verify Zen profile is scaled by 0.7
            Assert.AreEqual(defaultProfile.LandingMs * 0.001f * 1.0f, anim1.LandingDuration, 0.001f);
            Assert.AreEqual(zenProfile.LandingMs * 0.001f * 0.7f, anim2.LandingDuration, 0.001f);
        }

        [Test]
        public void Behavioural_MoveAnimator_DecimatesPathWhenOver12Cells()
        {
            var defaultProfile = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionProfileDefaultPath);
            Assert.IsNotNull(defaultProfile, "Missing default profile asset");

            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(8, 8);

            // Path with 15 steps
            var longPath = new List<GridPos>();
            for (int i = 0; i <= 8; i++) longPath.Add(new GridPos(0, i));
            for (int i = 1; i <= 6; i++) longPath.Add(new GridPos(i, 8));
            Assert.AreEqual(15, longPath.Count);

            m_BallManager.SpawnBall(from, BallColor.Red, m_BoardView.GridToWorld(from));
            var anim = new MoveAnimator(m_BoardView, m_BallManager, m_TweenRunner, defaultProfile);
            anim.AnimateMove(from, to, longPath, null, null);

            // Must be decimated to MaxFlightWaypoints (10) per Animation §6.1 #8
            Assert.AreEqual(defaultProfile.MaxFlightWaypoints, anim.WaypointCount,
                "Waypoints must be decimated to MaxFlightWaypoints when path > 12 cells.");
            Assert.LessOrEqual(anim.WaypointCount, 10);
        }

        [Test]
        public void Behavioural_MoveAnimator_AnticipationPullBack_MovesAwayFromSegment0_AndDisabledWhenZero()
        {
            var defaultProfile = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionProfileDefaultPath);
            Assert.IsNotNull(defaultProfile, "Missing default profile asset");

            GridPos from = new GridPos(1, 1);
            GridPos to = new GridPos(3, 1);
            var path = new List<GridPos>
            {
                new GridPos(1, 1),
                new GridPos(2, 1),
                new GridPos(3, 1)
            };

            var ball = m_BallManager.SpawnBall(from, BallColor.Red, m_BoardView.GridToWorld(from));
            var anim = new MoveAnimator(m_BoardView, m_BallManager, m_TweenRunner, defaultProfile);
            anim.AnimateMove(from, to, path, null, null);

            Assert.IsTrue(anim.IsActive, "Animator must be active after AnimateMove");
            Assert.IsTrue(anim.IsAnticipating, "Animator must start in anticipation phase when AnticipationMs > 0");

            Vector3 originPos = m_BoardView.GridToWorld(from);
            Vector3 seg0Dir = (m_BoardView.GridToWorld(new GridPos(2, 1)) - originPos).normalized;

            // Tick halfway through anticipation
            float halfAnticipation = (defaultProfile.AnticipationMs * 0.001f) * 0.5f;
            anim.Tick(halfAnticipation);

            // Ball position must have moved away from segment 0 (opposite to seg0Dir)
            Vector3 displacement = ball.transform.position - originPos;
            float dot = Vector3.Dot(displacement, seg0Dir);
            Assert.Less(dot, 0f, "Ball must move away from segment 0 during anticipation phase");

            // Complete anticipation phase
            anim.Tick(halfAnticipation + 0.01f);
            Assert.IsFalse(anim.IsAnticipating, "Anticipation phase must end after AnticipationMs");

            // Test AnticipationMs == 0 disables the phase
            var zeroAnticipationProfile = ScriptableObject.CreateInstance<MotionProfileSO>();
            zeroAnticipationProfile.Initialize(300f, 42f, 85f, anticipationMs: 0f, 160f, 22f, 70f);

            m_BallManager.SpawnBall(from, BallColor.Blue, m_BoardView.GridToWorld(from));
            var animZero = new MoveAnimator(m_BoardView, m_BallManager, m_TweenRunner, zeroAnticipationProfile);
            animZero.AnimateMove(from, to, path, null, null);

            Assert.IsFalse(animZero.IsAnticipating, "Anticipation must be immediately skipped when AnticipationMs is 0");
            Object.DestroyImmediate(zeroAnticipationProfile);
        }

        [Test]
        public void Behavioural_BoardAnimator_DifferentTiersAndProfiles_ProduceDifferentClearDurations()
        {
            var feedbackProfile = AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(FeedbackProfileTiersPath);
            Assert.IsNotNull(feedbackProfile, "Missing feedback profile asset");

            FeedbackRules rules = feedbackProfile.ToRules();
            var boardAnimator = new BoardAnimator(m_BoardView, m_BallManager, m_TweenRunner, null, rules);

            // Tier 1 clear: 5 balls
            var group5 = new ClearGroup(
                new[] { new GridPos(0, 0), new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0), new GridPos(4, 0) },
                5, 1, BallColor.Red);

            boardAnimator.AnimateClear(group5, new GridPos(2, 0), null);
            float tier1Duration = boardAnimator.ClearTotalDuration;
            Assert.AreEqual(1, boardAnimator.ActiveTierRule.Tier);

            // Tier 4 clear: 9 balls
            var group9 = new ClearGroup(
                new[]
                {
                    new GridPos(0, 0), new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0), new GridPos(4, 0),
                    new GridPos(5, 0), new GridPos(6, 0), new GridPos(7, 0), new GridPos(8, 0)
                },
                9, 1, BallColor.Red);

            boardAnimator.AnimateClear(group9, new GridPos(4, 0), null);
            float tier4Duration = boardAnimator.ClearTotalDuration;
            Assert.IsTrue(boardAnimator.ActiveTierRule.ShowBanner, "Tier 4 active rule must show banner.");

            // Tier 4 duration must be strictly greater than Tier 1
            Assert.Greater(tier4Duration, tier1Duration,
                "Tier 4 clear duration must be significantly longer than Tier 1 clear duration.");

            // Cancel must release ribbons and halt clear
            boardAnimator.Cancel();
            Assert.IsFalse(boardAnimator.IsActive, "BoardAnimator must not be active after Cancel().");
            Assert.IsFalse(boardAnimator.RibbonRenderers[0].enabled, "Ribbon 0 must be disabled after Cancel().");
            Assert.IsFalse(boardAnimator.RibbonRenderers[1].enabled, "Ribbon 1 must be disabled after Cancel().");
        }

        private sealed class TestSlowMoController : ISlowMoController
        {
            public float RequestedScale { get; private set; } = 1f;
            public float RequestedDuration { get; private set; }
            public int RequestCount { get; private set; }

            public void RequestSlowMo(float scale, float durationSeconds)
            {
                RequestedScale = scale;
                RequestedDuration = durationSeconds;
                RequestCount++;
            }
        }

        [Test]
        public void Behavioural_BoardAnimator_Tier4Clear_RequestsSlowMo_AndReducedMotionDisablesIt()
        {
            var feedbackProfile = AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(FeedbackProfileTiersPath);
            var defaultProfile = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionProfileDefaultPath);
            var reducedProfile = AssetDatabase.LoadAssetAtPath<MotionProfileSO>(MotionPresetReducedMotionPath);

            var slowMo = new TestSlowMoController();
            var boardAnimator = new BoardAnimator(
                m_BoardView, m_BallManager, m_TweenRunner, null,
                feedbackProfile.ToRules(), null, null, null, defaultProfile, slowMo);

            var group9 = new ClearGroup(
                new[]
                {
                    new GridPos(0, 0), new GridPos(1, 0), new GridPos(2, 0), new GridPos(3, 0), new GridPos(4, 0),
                    new GridPos(5, 0), new GridPos(6, 0), new GridPos(7, 0), new GridPos(8, 0)
                },
                9, 1, BallColor.Red);

            boardAnimator.AnimateClear(group9, new GridPos(4, 0), null);
            Assert.AreEqual(0, slowMo.RequestCount, "SlowMo must not trigger before burst phase");

            // Advance past burstStart (420 ms * scale)
            float burstStart = 0.42f * boardAnimator.ActiveTierRule.AnimationScale;
            boardAnimator.Tick(burstStart + 0.05f);
            Assert.AreEqual(1, slowMo.RequestCount, "Tier 4 clear must trigger slow-mo request at burst start");
            Assert.AreEqual(defaultProfile.SlowMoScale, slowMo.RequestedScale, 0.001f);
            Assert.AreEqual(defaultProfile.SlowMoMs * 0.001f, slowMo.RequestedDuration, 0.001f);

            // Test with ReducedMotion profile: SlowMoMs == 0 disables slow-mo data-driven
            var slowMoReduced = new TestSlowMoController();
            var boardAnimatorReduced = new BoardAnimator(
                m_BoardView, m_BallManager, m_TweenRunner, null,
                feedbackProfile.ToRules(), null, null, null, reducedProfile, slowMoReduced);

            boardAnimatorReduced.AnimateClear(group9, new GridPos(4, 0), null);
            float reducedBurstStart = 0.42f * boardAnimatorReduced.ActiveTierRule.AnimationScale;
            boardAnimatorReduced.Tick(reducedBurstStart + 0.05f);
            Assert.AreEqual(0, slowMoReduced.RequestCount, "Reduced motion preset must never request slow-mo");
        }

        [Test]
        public void PresentationRoot_SlowMo_ScalesClockAndRestoresWithoutTouchingTimeScale()
        {
            var rootGo = new GameObject("TestPresentationRoot");
            var presentationRoot = rootGo.AddComponent<Line98.Presentation.PresentationRoot>();

            Assert.AreEqual(1.0f, presentationRoot.ClockScale);
            Assert.AreEqual(1.0f, Time.timeScale, "Time.timeScale must be 1.0f initially");

            presentationRoot.RequestSlowMo(0.35f, 0.40f);
            Assert.AreEqual(0.35f, presentationRoot.ClockScale, 0.001f, "ClockScale must reflect requested slow-mo");
            Assert.AreEqual(1.0f, Time.timeScale, "Time.timeScale must never be modified by presentation slow-mo");

            // Tick midway through slow-mo
            presentationRoot.Tick(0.20f);
            Assert.AreEqual(0.35f, presentationRoot.ClockScale, 0.001f);
            Assert.AreEqual(1.0f, Time.timeScale);

            // Tick past completion
            presentationRoot.Tick(0.25f);
            Assert.AreEqual(1.0f, presentationRoot.ClockScale, 0.001f, "ClockScale must restore to 1.0f after duration completes");
            Assert.AreEqual(1.0f, Time.timeScale, "Time.timeScale must remain 1.0f throughout");

            Object.DestroyImmediate(rootGo);
        }
    }
}

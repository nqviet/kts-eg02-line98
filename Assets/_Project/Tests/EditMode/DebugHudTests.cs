using NUnit.Framework;
using UnityEngine;
using Line98.App;
using Line98.Core;
using Line98.Gameplay;
using Line98.Presentation;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class DebugHudTests
    {
        private GameObject m_HudGo;
        private DebugHud m_Hud;

        [SetUp]
        public void SetUp()
        {
            m_HudGo = new GameObject("TestDebugHud");
            m_Hud = m_HudGo.AddComponent<DebugHud>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_HudGo != null)
            {
                Object.DestroyImmediate(m_HudGo);
            }
        }

        [Test]
        public void DebugHud_IsHiddenBeforeAnyToggle()
        {
            Assert.IsFalse(m_Hud.IsHudVisible, "The debug hub must draw nothing until the user presses F1.");
        }

        [Test]
        public void DebugHud_ToggleHud_FlipsVisibilityBothWays()
        {
            m_Hud.ToggleHud();
            Assert.IsTrue(m_Hud.IsHudVisible, "The first F1 press should reveal the hub.");

            m_Hud.ToggleHud();
            Assert.IsFalse(m_Hud.IsHudVisible, "A second F1 press should hide it again.");
        }

        [Test]
        public void DebugHud_Initialize_DoesNotForceHudVisible()
        {
            var session = new GameSession();
            session.StartNewGame(3333U);

            m_Hud.Initialize(session);

            Assert.IsFalse(m_Hud.IsHudVisible, "Binding a session must not pop the hub open.");
        }

        [Test]
        public void DebugHud_SetHudVisible_AssignsExplicitVisibility()
        {
            m_Hud.SetHudVisible(true);
            Assert.IsTrue(m_Hud.IsHudVisible);

            m_Hud.SetHudVisible(false);
            Assert.IsFalse(m_Hud.IsHudVisible);
        }

        [Test]
        public void DebugHud_SetTab_PresentsTheRequestedSection()
        {
            Assert.AreEqual(DebugHud.DebugTab.All, m_Hud.ActiveTab, "The hub opens on the All tab.");

            m_Hud.SetTab(DebugHud.DebugTab.PopupHub);
            Assert.AreEqual(DebugHud.DebugTab.PopupHub, m_Hud.ActiveTab);

            m_Hud.SetTab(DebugHud.DebugTab.Gameplay);
            Assert.AreEqual(DebugHud.DebugTab.Gameplay, m_Hud.ActiveTab);
        }

        [Test]
        public void DebugHud_StatusMessage_RoundTrips()
        {
            m_Hud.StatusMessage = "Pushed from a tool.";

            Assert.AreEqual("Pushed from a tool.", m_Hud.StatusMessage);
        }

        [Test]
        public void DebugHud_SelectMockFixture_JumpsToAFixtureAndWrapsAround()
        {
            m_Hud.SelectMockFixture(3);
            Assert.AreEqual(3, m_Hud.MockSummaryIndex);
            StringAssert.Contains("Mock fixture 4/5", m_Hud.StatusMessage);

            m_Hud.SelectMockFixture(-1);
            Assert.AreEqual(4, m_Hud.MockSummaryIndex, "Negative indexes wrap to the end of the fixture list.");

            m_Hud.SelectMockFixture(6);
            Assert.AreEqual(1, m_Hud.MockSummaryIndex, "Indexes past the end wrap around.");
        }

        [Test]
        public void DebugHud_StartStandaloneSession_BindsAPlayingSession()
        {
            Assert.IsNull(m_Hud.Session);

            m_Hud.StartStandaloneSession();

            Assert.IsNotNull(m_Hud.Session);
            Assert.AreEqual(GamePhase.Playing, m_Hud.Session.Phase);
            Assert.IsFalse(m_Hud.IsHudVisible, "Starting a session must not reveal the hub.");

            // A second call must not detach the hub from the session it already drives.
            GameSession bound = m_Hud.Session;
            m_Hud.StartStandaloneSession();
            Assert.AreSame(bound, m_Hud.Session);
        }

        [Test]
        public void DebugHud_StartGameCommands_SelectTheRequestedMode()
        {
            m_Hud.StartStandaloneSession();

            m_Hud.StartZenGame();
            Assert.IsInstanceOf<ZenMode>(m_Hud.Session.Mode);
            StringAssert.Contains("Zen", m_Hud.StatusMessage);

            m_Hud.StartDailyChallenge("2030-05-04");
            Assert.IsInstanceOf<DailyChallengeMode>(m_Hud.Session.Mode);
            StringAssert.Contains("2030-05-04", m_Hud.StatusMessage);

            m_Hud.StartClassicGame();
            Assert.IsInstanceOf<ClassicMode>(m_Hud.Session.Mode);
            Assert.AreEqual(GamePhase.Playing, m_Hud.Session.Phase);
        }

        [Test]
        public void DebugHud_RequestHint_ReportsAMoveOnAFreshBoard()
        {
            m_Hud.StartStandaloneSession();

            Assert.IsTrue(m_Hud.RequestHint(), "A freshly started board always has a reachable move.");
            StringAssert.Contains("Hint:", m_Hud.StatusMessage);

            m_Hud.ClearHint();
        }

        [Test]
        public void DebugHud_UndoMove_ReturnsFalseWhenNothingWasCommitted()
        {
            m_Hud.StartStandaloneSession();

            Assert.IsFalse(m_Hud.UndoMove(), "A fresh session has no committed move to undo.");
        }

        [Test]
        public void DebugHud_PopupCommands_AreSafeToCallWithoutAShell()
        {
            // EditMode runs against whatever scene is open, so only the read-only and
            // closing calls are safe here; nothing may throw when no shell can be found.
            Assert.DoesNotThrow(() => m_Hud.IsPopupOpen(UiPopupId.GameOver));
            Assert.IsFalse(m_Hud.CloseMockPopup(UiPopupId.GameOver), "Nothing is open in an edit-mode scene.");
            Assert.DoesNotThrow(() => m_Hud.CloseAllPopups());
        }

        [Test]
        public void DebugHud_Instance_ResolvesTheHubAndForgetsItWhenDestroyed()
        {
            Assert.AreSame(m_Hud, DebugHud.Instance, "Instance must resolve the hub living in the scene.");

            Object.DestroyImmediate(m_HudGo);
            m_HudGo = null;

            Assert.IsNull(DebugHud.Instance, "A destroyed hub must not stay reachable through Instance.");
        }

        [Test]
        public void DebugHud_MockSummaries_AreExposedAndMatchFixtures()
        {
            Assert.IsNotNull(DebugHud.MockSummaries);
            Assert.AreEqual(5, DebugHud.MockSummaries.Count);

            // Verify Fixture 1
            Assert.AreEqual(1250, DebugHud.MockSummaries[0].FinalScore);
            Assert.AreEqual(3480, DebugHud.MockSummaries[0].BestScore);
            Assert.IsTrue(DebugHud.MockSummaries[0].CanContinue);

            // Verify Fixture 2 (New Best)
            Assert.AreEqual(7890, DebugHud.MockSummaries[1].FinalScore);
            Assert.AreEqual(7890, DebugHud.MockSummaries[1].BestScore);

            // Verify Fixture 3 (Cannot continue)
            Assert.IsFalse(DebugHud.MockSummaries[2].CanContinue);

            // Verify Fixture 4 (Large numbers)
            Assert.AreEqual(1234567, DebugHud.MockSummaries[3].FinalScore);
            Assert.AreEqual(9999999, DebugHud.MockSummaries[3].BestScore);

            // Verify Fixture 5 (Empty / zero)
            Assert.AreEqual(0, DebugHud.MockSummaries[4].FinalScore);
        }

        [Test]
        public void DebugHud_CycleMockFixture_CyclesThroughAllFixtures()
        {
            Assert.AreEqual(0, m_Hud.MockSummaryIndex);

            for (int i = 1; i < DebugHud.MockSummaries.Count; i++)
            {
                m_Hud.CycleMockFixture();
                Assert.AreEqual(i, m_Hud.MockSummaryIndex);
            }

            // Wrapping back to 0
            m_Hud.CycleMockFixture();
            Assert.AreEqual(0, m_Hud.MockSummaryIndex);
        }

        [Test]
        public void DebugHud_FormatSummary_IncludesAllStatsAndNewBestBadge()
        {
            // Test standard fixture
            SessionSummary standard = DebugHud.MockSummaries[0];
            string standardText = DebugHud.FormatSummary(standard);
            StringAssert.Contains("score 1,250", standardText);
            StringAssert.Contains("best 3,480", standardText);
            StringAssert.Contains("lines 8", standardText);
            StringAssert.Contains("longest 6", standardText);
            StringAssert.Contains("moves 41", standardText);
            StringAssert.Contains("continue True", standardText);
            StringAssert.DoesNotContain("[NEW BEST]", standardText);

            // Test New Best fixture
            SessionSummary newBest = DebugHud.MockSummaries[1];
            string newBestText = DebugHud.FormatSummary(newBest);
            StringAssert.Contains("score 7,890", newBestText);
            StringAssert.Contains("best 7,890", newBestText);
            StringAssert.Contains("[NEW BEST]", newBestText);
        }

        [Test]
        public void DebugHud_CreateMockPayload_ConstructsValidPayloadsForAllPopupIds()
        {
            // GameOver
            IUiPopupPayload gameOverPayload = m_Hud.CreateMockPayload(UiPopupId.GameOver);
            Assert.IsInstanceOf<GameOverPopupPayload>(gameOverPayload);
            var goPayload = (GameOverPopupPayload)gameOverPayload;
            Assert.AreEqual(DebugHud.MockSummaries[m_Hud.MockSummaryIndex].FinalScore, goPayload.Summary.FinalScore);

            // Confirm
            IUiPopupPayload confirmPayload = m_Hud.CreateMockPayload(UiPopupId.Confirm);
            Assert.IsInstanceOf<ConfirmPopupPayload>(confirmPayload);
            var conf = (ConfirmPopupPayload)confirmPayload;
            Assert.AreEqual("Debug Confirm", conf.Title);

            // Statistics
            IUiPopupPayload statsPayload = m_Hud.CreateMockPayload(UiPopupId.Statistics);
            Assert.IsInstanceOf<StatisticsPopupPayload>(statsPayload);
            var stats = (StatisticsPopupPayload)statsPayload;
            Assert.AreEqual(7, stats.GamesPlayed);
            Assert.AreEqual(3480, stats.BestScore);
        }

        [Test]
        public void DebugHud_Initialize_HandlesNullAndReinitializationCleanly()
        {
            // Null session should not throw
            Assert.DoesNotThrow(() => m_Hud.Initialize(null));

            // Valid session
            var session = new GameSession();
            session.StartNewGame(1111U);
            Assert.DoesNotThrow(() => m_Hud.Initialize(session));

            // Re-initializing with new session
            var session2 = new GameSession();
            session2.StartNewGame(2222U);
            Assert.DoesNotThrow(() => m_Hud.Initialize(session2));
        }
    }
}

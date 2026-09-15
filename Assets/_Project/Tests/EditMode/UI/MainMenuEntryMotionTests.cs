using Line98.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Line98.Tests.EditMode.UI
{
    [TestFixture]
    public sealed class MainMenuEntryMotionTests
    {
        private const string MainMenuPrefabPath = "Assets/_Project/Content/Prefabs/UI/Screens/MainMenu/Screen_MainMenu.prefab";

        private GameObject m_Root;
        private MainMenuEntryMotion m_Motion;
        private RectTransform m_Wordmark;
        private RectTransform[] m_Cards;
        private RectTransform[] m_Buttons;

        [SetUp]
        public void SetUp()
        {
            m_Root = new GameObject("MainMenuEntryMotion_TestRoot", typeof(RectTransform));
            m_Motion = m_Root.AddComponent<MainMenuEntryMotion>();

            m_Wordmark = CreateRect("Brand_WordmarkGroup");
            m_Wordmark.anchoredPosition = new Vector2(0f, -110f);
            m_Cards = new[] { CreateRect("Card_Best"), CreateRect("Card_Streak") };
            m_Buttons = new[]
            {
                CreateRect("Button_Play"), CreateRect("Button_Daily"), CreateRect("Button_Zen"),
                CreateRect("Button_Statistics"), CreateRect("Button_Settings")
            };

            m_Motion.Configure(m_Wordmark, m_Cards, m_Buttons);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Root != null)
            {
                Object.DestroyImmediate(m_Root);
            }
        }

        [Test]
        public void TotalDuration_MatchesSpecTimings()
        {
            // Buttons: start 260 ms + 4 x 60 ms stagger + 240 ms duration.
            Assert.AreEqual(740f, m_Motion.TotalDurationMs, 0.01f);
        }

        [Test]
        public void Play_StartsWithStaggeredElementsHidden()
        {
            m_Motion.Play(false);

            Assert.IsTrue(m_Motion.IsPlaying);
            Assert.AreEqual(1.12f, m_Wordmark.localScale.x, 0.001f, "Wordmark starts enlarged before settling");
            Assert.Greater(m_Wordmark.anchoredPosition.y, -110f, "Wordmark starts above its rest position");
            Assert.AreEqual(0f, m_Cards[0].localScale.x, 0.001f);
            Assert.AreEqual(0f, m_Buttons[4].localScale.x, 0.001f);
        }

        [Test]
        public void Tick_CardsStaggerBy70Ms()
        {
            m_Motion.Play(false);
            m_Motion.Tick(0.170f); // 50 ms into card 0, 20 ms before card 1 starts

            Assert.Greater(m_Cards[0].localScale.x, 0f, "First card has started");
            Assert.AreEqual(0f, m_Cards[1].localScale.x, 0.001f, "Second card waits its 70 ms stagger");
        }

        [Test]
        public void Tick_ButtonsStaggerBy60Ms()
        {
            m_Motion.Play(false);
            m_Motion.Tick(0.350f); // 90 ms into button 0; button 1 at 30 ms; button 2 not started

            Assert.Greater(m_Buttons[0].localScale.x, m_Buttons[1].localScale.x);
            Assert.Greater(m_Buttons[1].localScale.x, 0f);
            Assert.AreEqual(0f, m_Buttons[2].localScale.x, 0.001f);
        }

        [Test]
        public void Tick_PastTotalDuration_SettlesAtRest()
        {
            m_Motion.Play(false);
            m_Motion.Tick(0.4f);
            m_Motion.Tick(0.4f);

            Assert.IsFalse(m_Motion.IsPlaying);
            AssertAtRest();
        }

        [Test]
        public void Play_ReducedMotion_SnapsToRestImmediately()
        {
            m_Motion.Play(true);

            Assert.IsFalse(m_Motion.IsPlaying);
            AssertAtRest();
        }

        [Test]
        public void Snap_MidTimeline_RestoresRest()
        {
            m_Motion.Play(false);
            m_Motion.Tick(0.1f);
            m_Motion.Snap();

            Assert.IsFalse(m_Motion.IsPlaying);
            AssertAtRest();
        }

        [Test]
        public void MainMenuPrefab_WiresEntryMotionAndClickSfx()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MainMenuPrefabPath);
            Assert.IsNotNull(prefab, "Screen_MainMenu prefab missing");

            var motion = prefab.GetComponent<MainMenuEntryMotion>();
            Assert.IsNotNull(motion, "Screen_MainMenu must carry MainMenuEntryMotion");
            Assert.IsNotNull(motion.Wordmark);
            Assert.AreEqual(2, motion.Cards.Length);
            Assert.AreEqual(5, motion.Buttons.Length);

            var buttonEffects = prefab.GetComponentsInChildren<UiButtonFx>(true);
            Assert.AreEqual(5, buttonEffects.Length);
            for (int i = 0; i < buttonEffects.Length; i++)
            {
                var clip = new SerializedObject(buttonEffects[i]).FindProperty("m_ClickSfx").objectReferenceValue as AudioClip;
                Assert.IsNotNull(clip, $"{buttonEffects[i].name} has no click SFX");
                Assert.AreEqual("sfx_ui_button_click", clip.name);
            }
        }

        private void AssertAtRest()
        {
            Assert.AreEqual(Vector3.one, m_Wordmark.localScale);
            Assert.AreEqual(-110f, m_Wordmark.anchoredPosition.y, 0.001f);
            for (int i = 0; i < m_Cards.Length; i++)
            {
                Assert.AreEqual(Vector3.one, m_Cards[i].localScale, $"Card {i}");
            }
            for (int i = 0; i < m_Buttons.Length; i++)
            {
                Assert.AreEqual(Vector3.one, m_Buttons[i].localScale, $"Button {i}");
            }
        }

        private RectTransform CreateRect(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(m_Root.transform, false);
            return go.GetComponent<RectTransform>();
        }
    }
}

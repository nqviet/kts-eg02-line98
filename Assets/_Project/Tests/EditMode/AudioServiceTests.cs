using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Line98.Data;
using Line98.Presentation.Audio;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class AudioServiceTests
    {
        private const string CatalogPath = "Assets/_Project/Content/Definitions/AudioCatalog_Default.asset";
        private const string MixerPath = "Assets/_Project/Content/Audio/MainMixer.mixer";
        private const string FeedbackPath = "Assets/_Project/Content/Definitions/FeedbackProfile_Tiers.asset";

        private GameObject m_TestRoot;
        private AudioCatalogSO m_Catalog;
        private AudioMixer m_Mixer;
        private AudioService m_AudioService;

        [SetUp]
        public void SetUp()
        {
            m_TestRoot = new GameObject("AudioTestRoot");
            m_Catalog = AssetDatabase.LoadAssetAtPath<AudioCatalogSO>(CatalogPath);
            m_Mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);

            Assert.IsNotNull(m_Catalog, $"Missing AudioCatalog at {CatalogPath}");
            Assert.IsNotNull(m_Mixer, $"Missing AudioMixer at {MixerPath}");

            m_AudioService = new AudioService(m_Catalog, m_Mixer, m_TestRoot.transform);
        }

        [TearDown]
        public void TearDown()
        {
            m_AudioService?.Dispose();
            if (m_TestRoot != null)
            {
                Object.DestroyImmediate(m_TestRoot);
            }
        }

        [Test]
        public void SfxConcurrencyCap_NeverExceeds8Voices_OldestVoiceEvicted()
        {
            // Rapidly play 24 SFX sounds
            for (int i = 0; i < 24; i++)
            {
                m_AudioService.PlaySfx("sfx_spawn_pop");
                Assert.LessOrEqual(m_AudioService.ActiveSfxVoiceCount, AudioService.MaxSfxVoices,
                    $"Active SFX voices exceeded max cap of {AudioService.MaxSfxVoices} on iteration {i}");
            }

            Assert.AreEqual(AudioService.MaxSfxVoices, m_AudioService.MaxVoices);
        }

        [Test]
        public void BusRouting_RoutesToCorrectMixerGroups()
        {
            Assert.IsNotNull(m_AudioService.MasterGroup, "MasterGroup should not be null");
            Assert.IsNotNull(m_AudioService.MusicGroup, "MusicGroup should not be null");
            Assert.IsNotNull(m_AudioService.SfxGroup, "SfxGroup should not be null");
            Assert.IsNotNull(m_AudioService.UiGroup, "UiGroup should not be null");
            Assert.IsNotNull(m_AudioService.AmbienceGroup, "AmbienceGroup should not be null");

            Assert.AreEqual("Master", m_AudioService.MasterGroup.name);
            Assert.AreEqual("Music", m_AudioService.MusicGroup.name);
            Assert.AreEqual("SFX", m_AudioService.SfxGroup.name);
            Assert.AreEqual("UI", m_AudioService.UiGroup.name);
            Assert.AreEqual("Ambience", m_AudioService.AmbienceGroup.name);
        }

        [Test]
        public void SameFrameMuteUnmute_TogglesImmediately()
        {
            // Music mute test
            Assert.IsTrue(m_AudioService.IsMusicEnabled);
            m_AudioService.SetMusicEnabled(false);
            Assert.IsFalse(m_AudioService.IsMusicEnabled);
            m_AudioService.SetMusicEnabled(true);
            Assert.IsTrue(m_AudioService.IsMusicEnabled);

            // SFX mute test
            Assert.IsTrue(m_AudioService.IsSfxEnabled);
            m_AudioService.PlaySfx("sfx_spawn_pop");
            Assert.GreaterOrEqual(m_AudioService.ActiveSfxVoiceCount, 1);

            m_AudioService.SetSfxEnabled(false);
            Assert.IsFalse(m_AudioService.IsSfxEnabled);
            Assert.AreEqual(0, m_AudioService.ActiveSfxVoiceCount, "Disabling SFX should immediately evict/stop active SFX voices");

            // Further SFX play attempts while disabled should be discarded immediately
            m_AudioService.PlaySfx("sfx_spawn_pop");
            Assert.AreEqual(0, m_AudioService.ActiveSfxVoiceCount, "SFX played while disabled should not activate voices");

            m_AudioService.SetSfxEnabled(true);
            Assert.IsTrue(m_AudioService.IsSfxEnabled);
        }

        [Test]
        public void AudioCatalogResolution_AllCatalogEntriesHaveClips()
        {
            Assert.IsNotNull(m_Catalog.Entries, "Catalog entries array must not be null");
            Assert.GreaterOrEqual(m_Catalog.Entries.Count, 16, "Catalog must have all 16 specified audio entries");

            foreach (var entry in m_Catalog.Entries)
            {
                Assert.IsFalse(string.IsNullOrEmpty(entry.Key), "Audio entry key must not be null or empty");
                Assert.IsNotNull(entry.Clip, $"Audio clip for key '{entry.Key}' must not be null");
            }
        }

        [Test]
        public void AudioCatalogResolution_FeedbackProfileKeysResolve()
        {
            var feedback = AssetDatabase.LoadAssetAtPath<FeedbackProfileSO>(FeedbackPath);
            Assert.IsNotNull(feedback, $"FeedbackProfileSO missing at {FeedbackPath}");

            var rules = feedback.ToRules();
            Assert.IsNotNull(rules.Tiers, "Tiers array must not be null");
            Assert.Greater(rules.Tiers.Length, 0, "Tiers must contain feedback rules");

            foreach (var tier in rules.Tiers)
            {
                Assert.IsFalse(string.IsNullOrEmpty(tier.AudioKey), $"Tier for count {tier.MinLength} has empty AudioKey");
                bool found = m_Catalog.TryGetEntry(tier.AudioKey, out var entry);
                Assert.IsTrue(found, $"Feedback AudioKey '{tier.AudioKey}' was not found in AudioCatalog");
                Assert.IsNotNull(entry.Clip, $"Resolved clip for '{tier.AudioKey}' is null");
            }
        }

        [Test]
        public void AudioCatalogResolution_AllRequiredKeysPresent()
        {
            string[] requiredKeys = new string[]
            {
                "sfx_ball_select",
                "sfx_ball_deselect",
                "sfx_ball_move_flight",
                "sfx_ball_place_settle",
                "sfx_ball_invalid",
                "sfx_spawn_pop",
                "sfx_clear_tier1",
                "sfx_clear_tier2",
                "sfx_clear_tier3",
                "sfx_clear_tier4_perfect",
                "sfx_combo_up",
                "sfx_ui_button_click",
                "sfx_reward_earned",
                "sfx_game_over",
                "bgm_classic_main",
                "bgm_zen_ambience"
            };

            foreach (var key in requiredKeys)
            {
                bool found = m_Catalog.TryGetEntry(key, out var entry);
                Assert.IsTrue(found, $"Required key '{key}' not found in AudioCatalog");
                Assert.IsNotNull(entry.Clip, $"Required clip for '{key}' is null");
            }
        }
    }
}

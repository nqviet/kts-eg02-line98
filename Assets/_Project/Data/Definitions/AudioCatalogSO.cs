using System;
using System.Collections.Generic;
using UnityEngine;

namespace Line98.Data
{
    public enum AudioBusType : byte
    {
        Master = 0,
        Music = 1,
        SFX = 2,
        UI = 3,
        Ambience = 4
    }

    [Serializable]
    public struct AudioEntry
    {
        [SerializeField] private string m_Key;
        [SerializeField] private AudioClip m_Clip;
        [SerializeField] private AudioBusType m_Bus;
        [Range(0f, 1f)] [SerializeField] private float m_Volume;
        [Range(0f, 0.5f)] [SerializeField] private float m_PitchVariance;

        public string Key => m_Key;
        public AudioClip Clip => m_Clip;
        public AudioBusType Bus => m_Bus;
        public float Volume => m_Volume;
        public float PitchVariance => m_PitchVariance;

        public AudioEntry(string key, AudioClip clip, AudioBusType bus, float volume = 1f, float pitchVariance = 0f)
        {
            m_Key = key;
            m_Clip = clip;
            m_Bus = bus;
            m_Volume = volume;
            m_PitchVariance = pitchVariance;
        }
    }

    [CreateAssetMenu(fileName = "AudioCatalog_Default", menuName = "Line98/Definitions/Audio Catalog")]
    public class AudioCatalogSO : ScriptableObject
    {
        [SerializeField] private AudioEntry[] m_Entries = Array.Empty<AudioEntry>();

        // Legacy fields preserved to support safe migration
        [SerializeField] private AudioClip m_BallSelectClip;
        [SerializeField] private AudioClip m_BallDeselectClip;
        [SerializeField] private AudioClip m_BallMoveFlightClip;
        [SerializeField] private AudioClip m_BallPlaceSettleClip;
        [SerializeField] private AudioClip m_BallInvalidClip;
        [SerializeField] private AudioClip m_SpawnPopClip;
        [SerializeField] private AudioClip m_ClearTier1Clip;
        [SerializeField] private AudioClip m_ClearTier2Clip;
        [SerializeField] private AudioClip m_ClearTier3Clip;
        [SerializeField] private AudioClip m_ClearTier4Clip;
        [SerializeField] private AudioClip m_ComboUpClip;
        [SerializeField] private AudioClip m_ButtonClickClip;
        [SerializeField] private AudioClip m_RewardEarnedClip;
        [SerializeField] private AudioClip m_GameOverClip;

        private Dictionary<string, AudioEntry> m_Lookup;

        public IReadOnlyList<AudioEntry> Entries => m_Entries;

        public void SetEntriesForMigration(AudioEntry[] entries)
        {
            m_Entries = entries;
            m_Lookup = null;
        }

        private void BuildLookupIfNeeded()
        {
            if (m_Lookup != null) return;
            m_Lookup = new Dictionary<string, AudioEntry>(StringComparer.OrdinalIgnoreCase);

            if (m_Entries != null)
            {
                foreach (var entry in m_Entries)
                {
                    if (string.IsNullOrEmpty(entry.Key)) continue;

                    m_Lookup[entry.Key] = entry;

                    // Support alias matching (with or without sfx_ / bgm_ prefix)
                    if (entry.Key.StartsWith("sfx_", StringComparison.OrdinalIgnoreCase))
                    {
                        var stripped = entry.Key.Substring(4);
                        if (!m_Lookup.ContainsKey(stripped))
                        {
                            m_Lookup[stripped] = entry;
                        }

                        // Also support CamelCase alias (e.g. sfx_ball_select -> BallSelect)
                        var camel = ToPascalCase(stripped);
                        if (!m_Lookup.ContainsKey(camel))
                        {
                            m_Lookup[camel] = entry;
                        }
                    }
                    else if (entry.Key.StartsWith("bgm_", StringComparison.OrdinalIgnoreCase))
                    {
                        var stripped = entry.Key.Substring(4);
                        if (!m_Lookup.ContainsKey(stripped))
                        {
                            m_Lookup[stripped] = entry;
                        }
                        var camel = ToPascalCase(stripped);
                        if (!m_Lookup.ContainsKey(camel))
                        {
                            m_Lookup[camel] = entry;
                        }
                    }
                }
            }
        }

        private static string ToPascalCase(string input)
        {
            var parts = input.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + (parts[i].Length > 1 ? parts[i].Substring(1) : string.Empty);
                }
            }
            return string.Concat(parts);
        }

        public bool TryGetEntry(string key, out AudioEntry entry)
        {
            BuildLookupIfNeeded();
            if (string.IsNullOrEmpty(key))
            {
                entry = default;
                return false;
            }
            return m_Lookup.TryGetValue(key, out entry);
        }

        public AudioEntry GetEntry(string key)
        {
            if (TryGetEntry(key, out AudioEntry entry))
            {
                return entry;
            }
            throw new KeyNotFoundException($"[AudioCatalogSO] Audio key '{key}' not found in catalog.");
        }

        public AudioClip GetClip(string key)
        {
            return TryGetEntry(key, out AudioEntry entry) ? entry.Clip : null;
        }

        // Backward compatibility properties
        public AudioClip BallSelectClip => GetClip("sfx_ball_select") ?? m_BallSelectClip;
        public AudioClip BallDeselectClip => GetClip("sfx_ball_deselect") ?? m_BallDeselectClip;
        public AudioClip BallMoveFlightClip => GetClip("sfx_ball_move_flight") ?? m_BallMoveFlightClip;
        public AudioClip BallPlaceSettleClip => GetClip("sfx_ball_place_settle") ?? m_BallPlaceSettleClip;
        public AudioClip BallInvalidClip => GetClip("sfx_ball_invalid") ?? m_BallInvalidClip;
        public AudioClip SpawnPopClip => GetClip("sfx_spawn_pop") ?? m_SpawnPopClip;
        public AudioClip ClearTier1Clip => GetClip("sfx_clear_tier1") ?? m_ClearTier1Clip;
        public AudioClip ClearTier2Clip => GetClip("sfx_clear_tier2") ?? m_ClearTier2Clip;
        public AudioClip ClearTier3Clip => GetClip("sfx_clear_tier3") ?? m_ClearTier3Clip;
        public AudioClip ClearTier4Clip => GetClip("sfx_clear_tier4_perfect") ?? m_ClearTier4Clip;
        public AudioClip ComboUpClip => GetClip("sfx_combo_up") ?? m_ComboUpClip;
        public AudioClip ButtonClickClip => GetClip("sfx_ui_button_click") ?? m_ButtonClickClip;
        public AudioClip RewardEarnedClip => GetClip("sfx_reward_earned") ?? m_RewardEarnedClip;
        public AudioClip GameOverClip => GetClip("sfx_game_over") ?? m_GameOverClip;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Line98.Data;
using Line98.Presentation.Animation;

namespace Line98.Presentation.Audio
{
    /// <summary>
    /// Master audio presentation service.
    /// Strictly limits simultaneous playback to <= 8 active SFX voices.
    /// Manages dual music sources for seamless cross-fading, bus routing through MainMixer,
    /// and immediate frame-accurate mute/unmute capabilities.
    /// </summary>
    public sealed class AudioService : ITickable, IDisposable
    {
        public const int MaxSfxVoices = 8;
        public int MaxVoices => MaxSfxVoices;

        private readonly AudioCatalogSO m_Catalog;
        private readonly AudioMixer m_Mixer;
        private readonly Transform m_AudioRoot;

        private readonly AudioSource[] m_SfxVoices = new AudioSource[MaxSfxVoices];
        private readonly float[] m_SfxVoiceStartTime = new float[MaxSfxVoices];
        private readonly AudioSource m_MusicSourceA;
        private readonly AudioSource m_MusicSourceB;
        private readonly AudioSource m_AmbienceSource;

        private readonly Dictionary<AudioBusType, AudioMixerGroup> m_BusGroups = new Dictionary<AudioBusType, AudioMixerGroup>();

        private bool m_MusicEnabled = true;
        private bool m_SfxEnabled = true;

        private AudioSource m_ActiveMusicSource;
        private AudioSource m_FadingMusicSource;
        private float m_MusicFadeTimer;
        private float m_MusicFadeDuration;
        private float m_TargetMusicVolume = 1f;

        private AudioMixerSnapshot m_DefaultSnapshot;
        private AudioMixerSnapshot m_ZenSnapshot;

        public bool IsMusicEnabled => m_MusicEnabled;
        public bool IsSfxEnabled => m_SfxEnabled;
        public AudioMixer Mixer => m_Mixer;
        public AudioCatalogSO Catalog => m_Catalog;

        public AudioMixerGroup MasterGroup => m_BusGroups.TryGetValue(AudioBusType.Master, out var g) ? g : null;
        public AudioMixerGroup MusicGroup => m_BusGroups.TryGetValue(AudioBusType.Music, out var g) ? g : null;
        public AudioMixerGroup SfxGroup => m_BusGroups.TryGetValue(AudioBusType.SFX, out var g) ? g : null;
        public AudioMixerGroup UiGroup => m_BusGroups.TryGetValue(AudioBusType.UI, out var g) ? g : null;
        public AudioMixerGroup AmbienceGroup => m_BusGroups.TryGetValue(AudioBusType.Ambience, out var g) ? g : null;

        public int ActiveSfxVoiceCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxSfxVoices; i++)
                {
                    if (m_SfxVoices[i] != null && m_SfxVoices[i].isPlaying)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        public AudioService(AudioCatalogSO catalog, AudioMixer mixer, Transform parent)
        {
            m_Catalog = catalog;
            m_Mixer = mixer;

            var rootGo = new GameObject("AudioService_Root");
            rootGo.transform.SetParent(parent, false);
            m_AudioRoot = rootGo.transform;

            CacheMixerGroups();

            // 1. Initialize 8 pooled SFX AudioSources
            for (int i = 0; i < MaxSfxVoices; i++)
            {
                var voiceGo = new GameObject($"SfxVoice_{i}");
                voiceGo.transform.SetParent(m_AudioRoot, false);
                var src = voiceGo.AddComponent<AudioSource>();
                src.playOnAwake = false;
                src.loop = false;
                src.spatialBlend = 0f; // 2D casual stereo
                if (m_BusGroups.TryGetValue(AudioBusType.SFX, out var sfxGroup))
                {
                    src.outputAudioMixerGroup = sfxGroup;
                }
                m_SfxVoices[i] = src;
                m_SfxVoiceStartTime[i] = 0f;
            }

            // 2. Initialize dual music sources
            var musicGoA = new GameObject("MusicSource_A");
            musicGoA.transform.SetParent(m_AudioRoot, false);
            m_MusicSourceA = musicGoA.AddComponent<AudioSource>();
            m_MusicSourceA.playOnAwake = false;
            m_MusicSourceA.loop = true;
            m_MusicSourceA.spatialBlend = 0f;
            if (m_BusGroups.TryGetValue(AudioBusType.Music, out var musicGroup))
            {
                m_MusicSourceA.outputAudioMixerGroup = musicGroup;
            }

            var musicGoB = new GameObject("MusicSource_B");
            musicGoB.transform.SetParent(m_AudioRoot, false);
            m_MusicSourceB = musicGoB.AddComponent<AudioSource>();
            m_MusicSourceB.playOnAwake = false;
            m_MusicSourceB.loop = true;
            m_MusicSourceB.spatialBlend = 0f;
            if (musicGroup != null)
            {
                m_MusicSourceB.outputAudioMixerGroup = musicGroup;
            }

            // 3. Initialize dedicated ambience source
            var ambGo = new GameObject("AmbienceSource");
            ambGo.transform.SetParent(m_AudioRoot, false);
            m_AmbienceSource = ambGo.AddComponent<AudioSource>();
            m_AmbienceSource.playOnAwake = false;
            m_AmbienceSource.loop = true;
            m_AmbienceSource.spatialBlend = 0f;
            if (m_BusGroups.TryGetValue(AudioBusType.Ambience, out var ambGroup))
            {
                m_AmbienceSource.outputAudioMixerGroup = ambGroup;
            }

            // 4. Snapshots
            if (m_Mixer != null)
            {
                m_DefaultSnapshot = m_Mixer.FindSnapshot("Snapshot");
                m_ZenSnapshot = m_Mixer.FindSnapshot("Zen");
            }
        }

        private void CacheMixerGroups()
        {
            if (m_Mixer == null) return;

            var allGroups = m_Mixer.FindMatchingGroups(string.Empty);
            foreach (var group in allGroups)
            {
                if (group.name.Equals("Master", StringComparison.OrdinalIgnoreCase))
                    m_BusGroups[AudioBusType.Master] = group;
                else if (group.name.Equals("Music", StringComparison.OrdinalIgnoreCase))
                    m_BusGroups[AudioBusType.Music] = group;
                else if (group.name.Equals("SFX", StringComparison.OrdinalIgnoreCase))
                    m_BusGroups[AudioBusType.SFX] = group;
                else if (group.name.Equals("UI", StringComparison.OrdinalIgnoreCase))
                    m_BusGroups[AudioBusType.UI] = group;
                else if (group.name.Equals("Ambience", StringComparison.OrdinalIgnoreCase))
                    m_BusGroups[AudioBusType.Ambience] = group;
            }
        }

        public AudioMixerGroup GetBusGroup(AudioBusType bus)
        {
            return m_BusGroups.TryGetValue(bus, out var group) ? group : null;
        }

        public void SetMusicEnabled(bool enabled)
        {
            m_MusicEnabled = enabled;
            if (!m_MusicEnabled)
            {
                if (m_MusicSourceA != null) m_MusicSourceA.mute = true;
                if (m_MusicSourceB != null) m_MusicSourceB.mute = true;
                if (m_AmbienceSource != null) m_AmbienceSource.mute = true;
            }
            else
            {
                if (m_MusicSourceA != null) m_MusicSourceA.mute = false;
                if (m_MusicSourceB != null) m_MusicSourceB.mute = false;
                if (m_AmbienceSource != null) m_AmbienceSource.mute = false;
            }
        }

        public void SetSfxEnabled(bool enabled)
        {
            m_SfxEnabled = enabled;
            for (int i = 0; i < MaxSfxVoices; i++)
            {
                if (m_SfxVoices[i] != null)
                {
                    m_SfxVoices[i].mute = !enabled;
                    if (!enabled && m_SfxVoices[i].isPlaying)
                    {
                        m_SfxVoices[i].Stop();
                    }
                }
            }
        }

        public AudioSource PlaySfx(string key, float volumeScale = 1.0f)
        {
            if (m_Catalog == null || string.IsNullOrEmpty(key)) return null;

            if (m_Catalog.TryGetEntry(key, out AudioEntry entry))
            {
                return PlaySfx(entry.Clip, entry.Bus, entry.Volume * volumeScale, entry.PitchVariance);
            }

            return null;
        }

        private Line98.Core.XorShift128 m_Rng = new Line98.Core.XorShift128(987654321);

        public AudioSource PlaySfx(AudioClip clip, AudioBusType bus = AudioBusType.SFX, float volume = 1.0f, float pitchVariance = 0.0f)
        {
            if (clip == null || !m_SfxEnabled) return null;

            int voiceIndex = GetAvailableVoiceIndex();
            var source = m_SfxVoices[voiceIndex];

            // Set bus routing
            if (m_BusGroups.TryGetValue(bus, out var busGroup))
            {
                source.outputAudioMixerGroup = busGroup;
            }

            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            float randomOffset = (m_Rng.NextUInt() / (float)uint.MaxValue) * 2.0f - 1.0f; // [-1.0f, 1.0f]
            source.pitch = pitchVariance > 0.0001f
                ? 1.0f + (randomOffset * pitchVariance)
                : 1.0f;

            source.mute = !m_SfxEnabled;
            source.Play();
            m_SfxVoiceStartTime[voiceIndex] = Time.unscaledTime;

            return source;
        }

        private int GetAvailableVoiceIndex()
        {
            // 1. Look for a stopped voice
            for (int i = 0; i < MaxSfxVoices; i++)
            {
                if (!m_SfxVoices[i].isPlaying)
                {
                    return i;
                }
            }

            // 2. All 8 voices are active: evict the oldest voice
            int oldestIndex = 0;
            float oldestTime = float.MaxValue;
            for (int i = 0; i < MaxSfxVoices; i++)
            {
                if (m_SfxVoiceStartTime[i] < oldestTime)
                {
                    oldestTime = m_SfxVoiceStartTime[i];
                    oldestIndex = i;
                }
            }

            m_SfxVoices[oldestIndex].Stop();
            return oldestIndex;
        }

        public void PlayMusic(string key, bool loop = true, float fadeDuration = 0.5f)
        {
            if (m_Catalog == null || string.IsNullOrEmpty(key)) return;

            if (m_Catalog.TryGetEntry(key, out AudioEntry entry) && entry.Clip != null)
            {
                PlayMusic(entry.Clip, entry.Volume, loop, fadeDuration);
            }
        }

        public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true, float fadeDuration = 0.5f)
        {
            if (clip == null) return;

            // Pick inactive music source
            var nextSource = (m_ActiveMusicSource == m_MusicSourceA) ? m_MusicSourceB : m_MusicSourceA;
            m_FadingMusicSource = m_ActiveMusicSource;
            m_ActiveMusicSource = nextSource;

            m_ActiveMusicSource.clip = clip;
            m_ActiveMusicSource.loop = loop;
            m_ActiveMusicSource.mute = !m_MusicEnabled;
            m_TargetMusicVolume = volume;

            if (fadeDuration <= 0f || m_FadingMusicSource == null || !m_FadingMusicSource.isPlaying)
            {
                m_ActiveMusicSource.volume = volume;
                m_ActiveMusicSource.Play();
                if (m_FadingMusicSource != null)
                {
                    m_FadingMusicSource.Stop();
                    m_FadingMusicSource = null;
                }
                m_MusicFadeTimer = 0f;
            }
            else
            {
                m_ActiveMusicSource.volume = 0f;
                m_ActiveMusicSource.Play();
                m_MusicFadeDuration = fadeDuration;
                m_MusicFadeTimer = fadeDuration;
            }
        }

        public void StopMusic(float fadeDuration = 0.5f)
        {
            if (m_ActiveMusicSource == null || !m_ActiveMusicSource.isPlaying) return;

            if (fadeDuration <= 0f)
            {
                m_ActiveMusicSource.Stop();
                m_ActiveMusicSource = null;
                if (m_FadingMusicSource != null)
                {
                    m_FadingMusicSource.Stop();
                    m_FadingMusicSource = null;
                }
            }
            else
            {
                m_FadingMusicSource = m_ActiveMusicSource;
                m_ActiveMusicSource = null;
                m_MusicFadeDuration = fadeDuration;
                m_MusicFadeTimer = fadeDuration;
            }
        }

        public void PlayAmbience(string key, bool loop = true, float fadeDuration = 1.0f)
        {
            if (m_Catalog == null || string.IsNullOrEmpty(key) || m_AmbienceSource == null) return;

            if (m_Catalog.TryGetEntry(key, out AudioEntry entry) && entry.Clip != null)
            {
                m_AmbienceSource.clip = entry.Clip;
                m_AmbienceSource.loop = loop;
                m_AmbienceSource.volume = entry.Volume;
                m_AmbienceSource.mute = !m_MusicEnabled;
                m_AmbienceSource.Play();
            }
        }

        public void StopAmbience()
        {
            if (m_AmbienceSource != null && m_AmbienceSource.isPlaying)
            {
                m_AmbienceSource.Stop();
            }
        }

        public void SetZenMode(bool isZen, float transitionTime = 1.0f)
        {
            if (m_Mixer == null) return;

            if (isZen && m_ZenSnapshot != null)
            {
                m_ZenSnapshot.TransitionTo(transitionTime);
            }
            else if (!isZen && m_DefaultSnapshot != null)
            {
                m_DefaultSnapshot.TransitionTo(transitionTime);
            }
        }

        public void Tick(float dt)
        {
            if (m_MusicFadeTimer > 0f)
            {
                m_MusicFadeTimer -= dt;
                float t = Mathf.Clamp01(1f - (m_MusicFadeTimer / m_MusicFadeDuration));

                if (m_ActiveMusicSource != null)
                {
                    m_ActiveMusicSource.volume = Mathf.Lerp(0f, m_TargetMusicVolume, t);
                }

                if (m_FadingMusicSource != null)
                {
                    m_FadingMusicSource.volume = Mathf.Lerp(m_TargetMusicVolume, 0f, t);
                    if (m_MusicFadeTimer <= 0f)
                    {
                        m_FadingMusicSource.Stop();
                        m_FadingMusicSource = null;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (m_AudioRoot != null && m_AudioRoot.gameObject != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(m_AudioRoot.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(m_AudioRoot.gameObject);
                }
            }
        }
    }
}

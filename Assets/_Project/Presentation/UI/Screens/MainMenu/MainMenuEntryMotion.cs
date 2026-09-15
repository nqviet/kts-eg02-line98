using System;
using UnityEngine;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Main menu entry timeline (Animation §6.3 #30): wordmark OutBack settle (240 ms),
    /// mode cards stagger 70 ms, buttons stagger 60 ms. Driven by AppRoot.Tick (no Update loop),
    /// allocation-free, and snaps every element to rest when reduced motion is requested.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenuEntryMotion : MonoBehaviour, ITickable
    {
        [Header("Targets")]
        [SerializeField] private RectTransform m_Wordmark;
        [SerializeField] private RectTransform[] m_Cards = Array.Empty<RectTransform>();
        [SerializeField] private RectTransform[] m_Buttons = Array.Empty<RectTransform>();

        [Header("Wordmark Settle")]
        [SerializeField] private float m_WordmarkDurationMs = 240f;
        [SerializeField] private float m_WordmarkStartScale = 1.12f;
        [SerializeField] private float m_WordmarkDropOffset = 36f;

        [Header("Cards Stagger")]
        [SerializeField] private float m_CardsStartMs = 120f;
        [SerializeField] private float m_CardStaggerMs = 70f;
        [SerializeField] private float m_CardDurationMs = 240f;

        [Header("Buttons Stagger")]
        [SerializeField] private float m_ButtonsStartMs = 260f;
        [SerializeField] private float m_ButtonStaggerMs = 60f;
        [SerializeField] private float m_ButtonDurationMs = 240f;

        private Vector2 m_WordmarkRestPosition;
        private bool m_HasRestPosition;
        private float m_ElapsedMs;
        private bool m_IsPlaying;

        public bool IsPlaying => m_IsPlaying;
        public RectTransform Wordmark => m_Wordmark;
        public RectTransform[] Cards => m_Cards;
        public RectTransform[] Buttons => m_Buttons;

        public float TotalDurationMs
        {
            get
            {
                float total = m_Wordmark != null ? m_WordmarkDurationMs : 0f;
                total = Mathf.Max(total, GroupEndMs(m_Cards, m_CardsStartMs, m_CardStaggerMs, m_CardDurationMs));
                total = Mathf.Max(total, GroupEndMs(m_Buttons, m_ButtonsStartMs, m_ButtonStaggerMs, m_ButtonDurationMs));
                return total;
            }
        }

        public void Configure(RectTransform wordmark, RectTransform[] cards, RectTransform[] buttons)
        {
            m_Wordmark = wordmark;
            m_Cards = cards ?? Array.Empty<RectTransform>();
            m_Buttons = buttons ?? Array.Empty<RectTransform>();
            m_HasRestPosition = false;
        }

        /// <summary>Starts the entry timeline, or snaps to rest when reduced motion is enabled.</summary>
        public void Play(bool reducedMotion)
        {
            CacheRestPosition();

            if (reducedMotion)
            {
                Snap();
                return;
            }

            m_ElapsedMs = 0f;
            m_IsPlaying = true;
            Apply(0f);
        }

        /// <summary>Stops any running timeline and places every element at scale 1.0 and rest position.</summary>
        public void Snap()
        {
            CacheRestPosition();
            m_IsPlaying = false;
            m_ElapsedMs = 0f;

            if (m_Wordmark != null)
            {
                m_Wordmark.localScale = Vector3.one;
                m_Wordmark.anchoredPosition = m_WordmarkRestPosition;
            }

            SetGroupScale(m_Cards, 1f);
            SetGroupScale(m_Buttons, 1f);
        }

        public void Tick(float dt)
        {
            if (!m_IsPlaying)
            {
                return;
            }

            m_ElapsedMs += dt * 1000f;
            if (m_ElapsedMs >= TotalDurationMs)
            {
                Snap();
                return;
            }

            Apply(m_ElapsedMs);
        }

        private void OnDisable()
        {
            if (m_IsPlaying)
            {
                Snap();
            }
        }

        private void Apply(float elapsedMs)
        {
            if (m_Wordmark != null)
            {
                float eased = Easing.OutBack(Normalize(elapsedMs, 0f, m_WordmarkDurationMs));
                float scale = Mathf.LerpUnclamped(m_WordmarkStartScale, 1f, eased);
                m_Wordmark.localScale = new Vector3(scale, scale, 1f);
                m_Wordmark.anchoredPosition = m_WordmarkRestPosition + new Vector2(0f, m_WordmarkDropOffset * (1f - eased));
            }

            ApplyGroup(m_Cards, elapsedMs, m_CardsStartMs, m_CardStaggerMs, m_CardDurationMs);
            ApplyGroup(m_Buttons, elapsedMs, m_ButtonsStartMs, m_ButtonStaggerMs, m_ButtonDurationMs);
        }

        private static void ApplyGroup(RectTransform[] group, float elapsedMs, float startMs, float staggerMs, float durationMs)
        {
            if (group == null) return;

            for (int i = 0; i < group.Length; i++)
            {
                if (group[i] == null) continue;

                float scale = Easing.OutBack(Normalize(elapsedMs, startMs + i * staggerMs, durationMs));
                group[i].localScale = new Vector3(scale, scale, 1f);
            }
        }

        private static void SetGroupScale(RectTransform[] group, float scale)
        {
            if (group == null) return;

            for (int i = 0; i < group.Length; i++)
            {
                if (group[i] != null)
                {
                    group[i].localScale = new Vector3(scale, scale, 1f);
                }
            }
        }

        private static float GroupEndMs(RectTransform[] group, float startMs, float staggerMs, float durationMs)
        {
            if (group == null || group.Length == 0) return 0f;
            return startMs + (group.Length - 1) * staggerMs + durationMs;
        }

        private static float Normalize(float elapsedMs, float delayMs, float durationMs)
        {
            return durationMs > 0.0001f ? Mathf.Clamp01((elapsedMs - delayMs) / durationMs) : 1f;
        }

        private void CacheRestPosition()
        {
            if (m_HasRestPosition || m_Wordmark == null)
            {
                return;
            }

            m_WordmarkRestPosition = m_Wordmark.anchoredPosition;
            m_HasRestPosition = true;
        }
    }
}

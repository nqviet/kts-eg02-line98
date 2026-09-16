using System;
using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "MotionProfile_Default", menuName = "Line98/Definitions/Motion Profile")]
    public sealed class MotionProfileSO : ScriptableObject
    {
        [Header("Procedural Curves")]
        [SerializeField] private AnimationCurve m_OutCubic = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve m_OutBack = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.62f, 1.10f), new Keyframe(1f, 1f));
        [SerializeField] private AnimationCurve m_InExpo = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 0.05f), new Keyframe(1f, 1f));
        [SerializeField] private AnimationCurve m_InOutQuad = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve m_Squash = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.25f, 0.90f), new Keyframe(0.6f, 1.02f), new Keyframe(1f, 1f));
        [SerializeField] private AnimationCurve m_Hop = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.45f, 1f), new Keyframe(1f, 0f));
        [SerializeField] private AnimationCurve m_Pulse = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 1.18f), new Keyframe(1f, 1f));
        [SerializeField] private AnimationCurve m_RimRamp = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(0.7f, 1f), new Keyframe(1f, 0f));
        [SerializeField] private AnimationCurve m_FloatUp = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 0.6f));

        [Header("Durations (ms)")]
        [SerializeField] private float m_MoveBaseMs = 300f;
        [SerializeField] private float m_PerStepMinMs = 42f;
        [SerializeField] private float m_PerStepMaxMs = 85f;
        [SerializeField] private float m_AnticipationMs = 40f;
        [SerializeField] private float m_LandingMs = 160f;
        [SerializeField] private float m_StaggerPerCellMs = 22f;
        [SerializeField] private float m_SpawnStaggerMs = 70f;
        [SerializeField] private float m_PathPreviewMs = 140f;
        [SerializeField] private float m_ScorePopupMs = 480f;
        [SerializeField] private float m_SlowMoMs = 400f;

        [Header("Landing Damped Sine")]
        [SerializeField] private float m_LandingDecay = 9f;
        [SerializeField] private float m_LandingFrequency = 28f;

        [Header("Scalars")]
        [SerializeField] private float m_ClockScale = 1f;
        [SerializeField] private float m_AnimationScale = 1f;
        [SerializeField] private float m_ShakeScale = 1f;
        [SerializeField] private float m_SlowMoScale = 0.35f;
        [SerializeField] private int m_MaxFlightWaypoints = 10;

        public AnimationCurve OutCubic => m_OutCubic;
        public AnimationCurve OutBack => m_OutBack;
        public AnimationCurve InExpo => m_InExpo;
        public AnimationCurve InOutQuad => m_InOutQuad;
        public AnimationCurve Squash => m_Squash;
        public AnimationCurve Hop => m_Hop;
        public AnimationCurve Pulse => m_Pulse;
        public AnimationCurve RimRamp => m_RimRamp;
        public AnimationCurve FloatUp => m_FloatUp;

        public float MoveBaseMs => m_MoveBaseMs;
        public float PerStepMinMs => m_PerStepMinMs;
        public float PerStepMaxMs => m_PerStepMaxMs;
        public float AnticipationMs => m_AnticipationMs;
        public float LandingMs => m_LandingMs;
        public float StaggerPerCellMs => m_StaggerPerCellMs;
        public float SpawnStaggerMs => m_SpawnStaggerMs;
        public float PathPreviewMs => m_PathPreviewMs;
        public float ScorePopupMs => m_ScorePopupMs;
        public float SlowMoMs => m_SlowMoMs;
        public float LandingDecay => m_LandingDecay;
        public float LandingFrequency => m_LandingFrequency;

        public float ClockScale => m_ClockScale;
        public float AnimationScale => m_AnimationScale;
        public float ShakeScale => m_ShakeScale;
        public float SlowMoScale => m_SlowMoScale;
        public int MaxFlightWaypoints => m_MaxFlightWaypoints;

        private static MotionProfileSO s_Default;
        public static MotionProfileSO Default
        {
            get
            {
                if (s_Default == null)
                {
                    s_Default = CreateInstance<MotionProfileSO>();
                    s_Default.name = "MotionProfile_CodeDefault";
                }
                return s_Default;
            }
        }

        public void Initialize(
            float moveBaseMs,
            float perStepMinMs,
            float perStepMaxMs,
            float anticipationMs,
            float landingMs,
            float staggerPerCellMs,
            float spawnStaggerMs,
            float clockScale = 1f,
            float animationScale = 1f,
            float shakeScale = 1f,
            int maxFlightWaypoints = 10,
            float landingDecay = 9f,
            float landingFrequency = 28f,
            float pathPreviewMs = 140f,
            float scorePopupMs = 480f,
            float slowMoScale = 0.35f,
            float slowMoMs = 400f,
            AnimationCurve outCubic = null,
            AnimationCurve outBack = null,
            AnimationCurve inExpo = null,
            AnimationCurve inOutQuad = null,
            AnimationCurve squash = null,
            AnimationCurve hop = null,
            AnimationCurve pulse = null,
            AnimationCurve rimRamp = null,
            AnimationCurve floatUp = null)
        {
            m_MoveBaseMs = moveBaseMs;
            m_PerStepMinMs = perStepMinMs;
            m_PerStepMaxMs = perStepMaxMs;
            m_AnticipationMs = anticipationMs;
            m_LandingMs = landingMs;
            m_StaggerPerCellMs = staggerPerCellMs;
            m_SpawnStaggerMs = spawnStaggerMs;
            m_PathPreviewMs = pathPreviewMs;
            m_ScorePopupMs = scorePopupMs;
            m_SlowMoScale = slowMoScale;
            m_SlowMoMs = slowMoMs;
            m_ClockScale = clockScale;
            m_AnimationScale = animationScale;
            m_ShakeScale = shakeScale;
            m_MaxFlightWaypoints = maxFlightWaypoints;
            m_LandingDecay = landingDecay;
            m_LandingFrequency = landingFrequency;

            if (outCubic != null) m_OutCubic = outCubic;
            if (outBack != null) m_OutBack = outBack;
            if (inExpo != null) m_InExpo = inExpo;
            if (inOutQuad != null) m_InOutQuad = inOutQuad;
            if (squash != null) m_Squash = squash;
            if (hop != null) m_Hop = hop;
            if (pulse != null) m_Pulse = pulse;
            if (rimRamp != null) m_RimRamp = rimRamp;
            if (floatUp != null) m_FloatUp = floatUp;
        }
    }
}

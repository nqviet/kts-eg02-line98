using System;
using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "MotionProfile_Default", menuName = "Line98/Definitions/Motion Profile")]
    public sealed class MotionProfileSO : ScriptableObject
    {
        [Header("Procedural Curves")]
        [SerializeField] private AnimationCurve m_OutCubic = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve m_OutBack = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve m_InExpo = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve m_InOutQuad = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve m_Squash = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField] private AnimationCurve m_Hop = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        [SerializeField] private AnimationCurve m_Pulse = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [SerializeField] private AnimationCurve m_RimRamp = AnimationCurve.Linear(0f, 0f, 1f, 0f);
        [SerializeField] private AnimationCurve m_FloatUp = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Durations (ms)")]
        [SerializeField] private float m_MoveBaseMs = 300f;
        [SerializeField] private float m_PerStepMinMs = 42f;
        [SerializeField] private float m_PerStepMaxMs = 85f;
        [SerializeField] private float m_AnticipationMs = 40f;
        [SerializeField] private float m_LandingMs = 160f;
        [SerializeField] private float m_StaggerPerCellMs = 22f;
        [SerializeField] private float m_SpawnStaggerMs = 70f;

        [Header("Scalars")]
        [SerializeField] private float m_ClockScale = 1f;
        [SerializeField] private float m_AnimationScale = 1f;
        [SerializeField] private float m_ShakeScale = 1f;
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

        public float ClockScale => m_ClockScale;
        public float AnimationScale => m_AnimationScale;
        public float ShakeScale => m_ShakeScale;
        public int MaxFlightWaypoints => m_MaxFlightWaypoints;
    }
}

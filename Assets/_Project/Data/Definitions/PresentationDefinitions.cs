using System;
using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "BallTheme_Crystal", menuName = "Line98/Definitions/Ball Theme")]
    public class BallThemeSO : ScriptableObject
    {
        [SerializeField] private Material[] m_BallMaterials;
        [SerializeField] private Texture2D m_AccessibilityPatterns;
        [SerializeField] private Texture2D m_InnerRefractionMask;
        [SerializeField] private Material m_BlobShadowMaterial;

        public Material[] BallMaterials => m_BallMaterials;
        public Texture2D AccessibilityPatterns => m_AccessibilityPatterns;
        public Texture2D InnerRefractionMask => m_InnerRefractionMask;
        public Material BlobShadowMaterial => m_BlobShadowMaterial;
    }

    [CreateAssetMenu(fileName = "BoardTheme_Classic", menuName = "Line98/Definitions/Board Theme")]
    public class BoardThemeSO : ScriptableObject
    {
        [SerializeField] private GameObject m_BoardFramePrefab;
        [SerializeField] private GameObject m_BoardCellPrefab;
        [SerializeField] private Material m_BoardCellMaterial;
        [SerializeField] private Material m_BoardFrameMaterial;

        public GameObject BoardFramePrefab => m_BoardFramePrefab;
        public GameObject BoardCellPrefab => m_BoardCellPrefab;
        public Material BoardCellMaterial => m_BoardCellMaterial;
        public Material BoardFrameMaterial => m_BoardFrameMaterial;
    }

    [CreateAssetMenu(fileName = "CameraProfile_Default", menuName = "Line98/Definitions/Camera Profile")]
    public class CameraProfileSO : ScriptableObject
    {
        [SerializeField] private float m_FieldOfView = 28.0f;
        [SerializeField] private float m_PitchAngle = 58.0f;
        [SerializeField] private float m_YawAngle = 0.0f;
        [SerializeField] private float m_Distance = 18.5f;
        [SerializeField] private float m_ParallaxFactor = 0.05f;

        public float FieldOfView => m_FieldOfView;
        public float PitchAngle => m_PitchAngle;
        public float YawAngle => m_YawAngle;
        public float Distance => m_Distance;
        public float ParallaxFactor => m_ParallaxFactor;
    }

    [Serializable]
    public struct ClearFeedbackTier
    {
        public int MinimumBalls;
        public int MaximumBalls;
        public GameObject VfxPrefab;
        public AudioClip SfxClip;
        public float CameraShakeIntensity;
        public string TierLabel;
    }

    [CreateAssetMenu(fileName = "FeedbackProfile_Tiers", menuName = "Line98/Definitions/Feedback Profile")]
    public class FeedbackProfileSO : ScriptableObject
    {
        [SerializeField] private ClearFeedbackTier[] m_Tiers;
        public ClearFeedbackTier[] Tiers => m_Tiers;
    }

    [CreateAssetMenu(fileName = "VfxCatalog_Default", menuName = "Line98/Definitions/VFX Catalog")]
    public class VfxCatalogSO : ScriptableObject
    {
        [SerializeField] private GameObject m_SelectPulsePrefab;
        [SerializeField] private GameObject m_PlacementSettlePrefab;
        [SerializeField] private GameObject m_ClearTier1Prefab;
        [SerializeField] private GameObject m_ClearTier2Prefab;
        [SerializeField] private GameObject m_ClearTier3Prefab;
        [SerializeField] private GameObject m_ClearTier4Prefab;
        [SerializeField] private int m_PrewarmPoolCapacity = 4;

        public GameObject SelectPulsePrefab => m_SelectPulsePrefab;
        public GameObject PlacementSettlePrefab => m_PlacementSettlePrefab;
        public GameObject ClearTier1Prefab => m_ClearTier1Prefab;
        public GameObject ClearTier2Prefab => m_ClearTier2Prefab;
        public GameObject ClearTier3Prefab => m_ClearTier3Prefab;
        public GameObject ClearTier4Prefab => m_ClearTier4Prefab;
        public int PrewarmPoolCapacity => m_PrewarmPoolCapacity;
    }

    [CreateAssetMenu(fileName = "AudioCatalog_Default", menuName = "Line98/Definitions/Audio Catalog")]
    public class AudioCatalogSO : ScriptableObject
    {
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

        public AudioClip BallSelectClip => m_BallSelectClip;
        public AudioClip BallDeselectClip => m_BallDeselectClip;
        public AudioClip BallMoveFlightClip => m_BallMoveFlightClip;
        public AudioClip BallPlaceSettleClip => m_BallPlaceSettleClip;
        public AudioClip BallInvalidClip => m_BallInvalidClip;
        public AudioClip SpawnPopClip => m_SpawnPopClip;
        public AudioClip ClearTier1Clip => m_ClearTier1Clip;
        public AudioClip ClearTier2Clip => m_ClearTier2Clip;
        public AudioClip ClearTier3Clip => m_ClearTier3Clip;
        public AudioClip ClearTier4Clip => m_ClearTier4Clip;
        public AudioClip ComboUpClip => m_ComboUpClip;
        public AudioClip ButtonClickClip => m_ButtonClickClip;
        public AudioClip RewardEarnedClip => m_RewardEarnedClip;
        public AudioClip GameOverClip => m_GameOverClip;
    }
}

using UnityEngine;

namespace Line98.Data
{
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

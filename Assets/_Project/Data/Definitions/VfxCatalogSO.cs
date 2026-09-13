using UnityEngine;

namespace Line98.Data
{
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
}

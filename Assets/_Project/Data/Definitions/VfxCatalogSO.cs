using System;
using UnityEngine;

namespace Line98.Data
{
    [Serializable]
    public struct VfxEntry
    {
        [SerializeField] private string m_Key;
        [SerializeField] private GameObject m_Prefab;
        [SerializeField] private int m_PoolSize;
        [SerializeField] private float m_Lifetime;

        public string Key => m_Key;
        public GameObject Prefab => m_Prefab;
        public int PoolSize => m_PoolSize;
        public float Lifetime => m_Lifetime;

        public VfxEntry(string key, GameObject prefab, int poolSize = 4, float lifetime = 1.0f)
        {
            m_Key = key;
            m_Prefab = prefab;
            m_PoolSize = poolSize;
            m_Lifetime = lifetime;
        }
    }

    [CreateAssetMenu(fileName = "VfxCatalog_Default", menuName = "Line98/Definitions/VFX Catalog")]
    public class VfxCatalogSO : ScriptableObject
    {
        [SerializeField] private VfxEntry[] m_Entries;
        [SerializeField] private int m_PrewarmPoolCapacity = 4;

        public VfxEntry[] Entries => m_Entries;
        public int PrewarmPoolCapacity => m_PrewarmPoolCapacity;

        public bool TryGetEntry(string key, out VfxEntry entry)
        {
            if (m_Entries != null && !string.IsNullOrEmpty(key))
            {
                for (int i = 0; i < m_Entries.Length; i++)
                {
                    if (string.Equals(m_Entries[i].Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        entry = m_Entries[i];
                        return true;
                    }
                }
            }
            entry = default;
            return false;
        }

        public GameObject GetPrefab(string key)
        {
            return TryGetEntry(key, out var entry) ? entry.Prefab : null;
        }

        public void SetEntries(VfxEntry[] entries, int prewarmPoolCapacity = 4)
        {
            m_Entries = entries;
            m_PrewarmPoolCapacity = prewarmPoolCapacity;
        }

        // Backward compatibility accessors
        public GameObject SelectPulsePrefab => GetPrefab("BallSelect");
        public GameObject PlacementSettlePrefab => GetPrefab("PlacementSettle");
        public GameObject ClearTier1Prefab => GetPrefab("ClearTier1");
        public GameObject ClearTier2Prefab => GetPrefab("ClearTier2");
        public GameObject ClearTier3Prefab => GetPrefab("ClearTier3");
        public GameObject ClearTier4Prefab => GetPrefab("ClearTier4");
    }
}

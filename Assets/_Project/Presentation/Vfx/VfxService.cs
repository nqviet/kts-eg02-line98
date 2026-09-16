using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Line98.Core;
using Line98.Data;
using Line98.Presentation.Animation;

namespace Line98.Presentation.Vfx
{
    /// <summary>
    /// Service managing visual effects, pre-warmed pooling, burst concurrency throttling (<= 4),
    /// floating score popups (<= 8), and zero-heap runtime playback.
    /// Follows strict CancelByOwner semantics identical to TweenRunner.
    /// </summary>
    public sealed class VfxService : ITickable
    {
        public const int MaxBurstConcurrency = 4;
        public const int MaxPopupConcurrency = 8;
        public const int MaxTotalActive = 32;

        public struct ActiveVfx
        {
            public GameObject GameObject;
            public ParticleSystem ParticleSystem;
            public TrailRenderer TrailRenderer;
            public TMP_Text Text;
            public float RemainingTime;
            public float TotalDuration;
            public object Owner;
            public string Key;
            public bool IsBurst;
            public bool IsPopup;
            public bool IsActive;
        }

        private readonly VfxCatalogSO m_Catalog;
        private readonly Transform m_PoolRoot;
        private readonly Dictionary<string, List<GameObject>> m_Pools = new Dictionary<string, List<GameObject>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<GameObject, ParticleSystem> m_CachedParticleSystems = new Dictionary<GameObject, ParticleSystem>();
        private readonly Dictionary<GameObject, TrailRenderer> m_CachedTrails = new Dictionary<GameObject, TrailRenderer>();
        private readonly Dictionary<GameObject, TMP_Text> m_CachedTexts = new Dictionary<GameObject, TMP_Text>();
        private readonly ActiveVfx[] m_ActiveInstances = new ActiveVfx[MaxTotalActive];
        private int m_ActiveBurstCount;
        private int m_ActivePopupCount;

        public int ActiveBurstCount => m_ActiveBurstCount;
        public int ActivePopupCount => m_ActivePopupCount;
        public VfxCatalogSO Catalog => m_Catalog;

        public VfxService(VfxCatalogSO catalog, Transform poolRoot = null)
        {
            m_Catalog = catalog;
            m_PoolRoot = poolRoot;
            InitializePools();
        }

        public void InitializePools()
        {
            if (m_Catalog == null || m_Catalog.Entries == null)
            {
                return;
            }

            for (int e = 0; e < m_Catalog.Entries.Length; e++)
            {
                VfxEntry entry = m_Catalog.Entries[e];
                if (string.IsNullOrEmpty(entry.Key) || entry.Prefab == null)
                {
                    continue;
                }

                if (!m_Pools.TryGetValue(entry.Key, out List<GameObject> pool))
                {
                    pool = new List<GameObject>(entry.PoolSize);
                    m_Pools[entry.Key] = pool;
                }

                int targetCount = Mathf.Max(1, entry.PoolSize);
                while (pool.Count < targetCount)
                {
                    GameObject instance = UnityEngine.Object.Instantiate(entry.Prefab, m_PoolRoot);
                    instance.name = $"{entry.Key}_{pool.Count}";
                    instance.SetActive(false);
                    pool.Add(instance);

                    var ps = instance.GetComponentInChildren<ParticleSystem>();
                    if (ps != null) m_CachedParticleSystems[instance] = ps;

                    var tr = instance.GetComponentInChildren<TrailRenderer>();
                    if (tr != null) m_CachedTrails[instance] = tr;

                    var tmp = instance.GetComponentInChildren<TMP_Text>();
                    if (tmp != null) m_CachedTexts[instance] = tmp;
                }
            }
        }

        public GameObject PlayBurst(string key, Vector3 worldPosition, object owner = null)
        {
            if (string.IsNullOrEmpty(key)) return null;

            // Enforce burst concurrency cap <= 4
            if (m_ActiveBurstCount >= MaxBurstConcurrency)
            {
                RecycleOldestBurst();
            }

            GameObject instance = GetPooledInstance(key);
            if (instance == null) return null;

            instance.transform.position = worldPosition;
            instance.transform.rotation = Quaternion.identity;
            instance.SetActive(true);

            m_CachedParticleSystems.TryGetValue(instance, out ParticleSystem ps);
            if (ps != null)
            {
                ps.time = 0f;
                ps.Play();
            }

            float lifetime = GetLifetime(key, 1.0f);
            RegisterActive(instance, ps, null, null, lifetime, owner, key, isBurst: true, isPopup: false);
            m_ActiveBurstCount++;
            return instance;
        }

        public bool PlayScorePopup(int score, Vector3 worldPosition, float comboMultiplier = 1.0f, object owner = null)
        {
            const string Key = "ScorePopup";

            if (m_ActivePopupCount >= MaxPopupConcurrency)
            {
                RecycleOldestPopup();
            }

            GameObject instance = GetPooledInstance(Key);
            if (instance == null) return false;

            instance.transform.position = worldPosition;
            instance.transform.rotation = Quaternion.identity;
            instance.SetActive(true);

            m_CachedTexts.TryGetValue(instance, out TMP_Text tmp);
            if (tmp != null)
            {
                if (comboMultiplier > 1.01f)
                {
                    tmp.text = $"+{score}\nCOMBO x{comboMultiplier:F1}";
                }
                else
                {
                    tmp.text = $"+{score}";
                }
            }

            float lifetime = GetLifetime(Key, 0.75f);
            RegisterActive(instance, null, null, tmp, lifetime, owner, Key, isBurst: false, isPopup: true);
            m_ActivePopupCount++;
            return true;
        }

        public GameObject PlayVfx(string key, Vector3 worldPosition, Quaternion rotation = default, object owner = null)
        {
            if (string.IsNullOrEmpty(key)) return null;

            GameObject instance = GetPooledInstance(key);
            if (instance == null) return null;

            instance.transform.position = worldPosition;
            instance.transform.rotation = rotation == default ? Quaternion.identity : rotation;
            instance.SetActive(true);

            m_CachedParticleSystems.TryGetValue(instance, out ParticleSystem ps);
            if (ps != null)
            {
                ps.time = 0f;
                ps.Play();
            }

            float lifetime = GetLifetime(key, 1.0f);
            RegisterActive(instance, ps, null, null, lifetime, owner, key, isBurst: false, isPopup: false);
            return instance;
        }

        public GameObject AttachTrail(Transform target, Color tint, object owner = null)
        {
            const string Key = "BallTrail";
            GameObject instance = GetPooledInstance(Key);
            if (instance == null) return null;

            instance.transform.SetParent(target, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.SetActive(true);

            m_CachedTrails.TryGetValue(instance, out TrailRenderer tr);
            if (tr != null)
            {
                tr.Clear();
                tr.startColor = tint;
                tr.endColor = new Color(tint.r, tint.g, tint.b, 0f);
                tr.emitting = true;
            }

            RegisterActive(instance, null, tr, null, float.MaxValue, owner, Key, isBurst: false, isPopup: false);
            return instance;
        }

        public void DetachTrail(Transform target)
        {
            for (int i = 0; i < m_ActiveInstances.Length; i++)
            {
                if (m_ActiveInstances[i].IsActive && m_ActiveInstances[i].GameObject != null)
                {
                    if (m_ActiveInstances[i].GameObject.transform.parent == target)
                    {
                        if (m_ActiveInstances[i].TrailRenderer != null)
                        {
                            m_ActiveInstances[i].TrailRenderer.emitting = false;
                            m_ActiveInstances[i].TrailRenderer.Clear();
                        }
                        if (m_PoolRoot != null)
                        {
                            m_ActiveInstances[i].GameObject.transform.SetParent(m_PoolRoot, false);
                        }
                        RecycleSlot(i);
                    }
                }
            }
        }

        public void Tick(float dt)
        {
            for (int i = 0; i < m_ActiveInstances.Length; i++)
            {
                if (!m_ActiveInstances[i].IsActive) continue;

                if (m_ActiveInstances[i].IsPopup && m_ActiveInstances[i].GameObject != null)
                {
                    // Gentle vertical rise
                    m_ActiveInstances[i].GameObject.transform.position += Vector3.up * (0.8f * dt);
                }

                m_ActiveInstances[i].RemainingTime -= dt;
                if (m_ActiveInstances[i].RemainingTime <= 0f)
                {
                    RecycleSlot(i);
                }
            }
        }

        public void CancelByOwner(object owner)
        {
            if (owner == null) return;

            for (int i = 0; i < m_ActiveInstances.Length; i++)
            {
                if (m_ActiveInstances[i].IsActive && m_ActiveInstances[i].Owner == owner)
                {
                    RecycleSlot(i);
                }
            }
        }

        public void CancelAll()
        {
            for (int i = 0; i < m_ActiveInstances.Length; i++)
            {
                if (m_ActiveInstances[i].IsActive)
                {
                    RecycleSlot(i);
                }
            }
        }

        public void PrewarmShaders()
        {
            Vector3 offscreen = new Vector3(0f, -100f, 0f);
            foreach (var kvp in m_Pools)
            {
                if (kvp.Value != null && kvp.Value.Count > 0)
                {
                    GameObject go = kvp.Value[0];
                    if (go != null)
                    {
                        go.transform.position = offscreen;
                        go.SetActive(true);
                        ParticleSystem ps = go.GetComponentInChildren<ParticleSystem>();
                        if (ps != null)
                        {
                            ps.Emit(1);
                        }
                        go.SetActive(false);
                    }
                }
            }
        }

        private GameObject GetPooledInstance(string key)
        {
            if (!m_Pools.TryGetValue(key, out List<GameObject> pool) || pool == null || pool.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i] != null && !pool[i].activeSelf)
                {
                    return pool[i];
                }
            }

            // If all are active, recycle the oldest instance of this key
            for (int i = 0; i < m_ActiveInstances.Length; i++)
            {
                if (m_ActiveInstances[i].IsActive && string.Equals(m_ActiveInstances[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    GameObject reused = m_ActiveInstances[i].GameObject;
                    RecycleSlot(i);
                    return reused;
                }
            }

            return pool[0];
        }

        private float GetLifetime(string key, float defaultLifetime)
        {
            if (m_Catalog != null && m_Catalog.TryGetEntry(key, out VfxEntry entry) && entry.Lifetime > 0f)
            {
                return entry.Lifetime;
            }
            return defaultLifetime;
        }

        private void RegisterActive(
            GameObject go,
            ParticleSystem ps,
            TrailRenderer tr,
            TMP_Text txt,
            float duration,
            object owner,
            string key,
            bool isBurst,
            bool isPopup)
        {
            int freeSlot = -1;
            for (int i = 0; i < m_ActiveInstances.Length; i++)
            {
                if (!m_ActiveInstances[i].IsActive)
                {
                    freeSlot = i;
                    break;
                }
            }

            if (freeSlot < 0)
            {
                // Full buffer: recycle slot 0
                RecycleSlot(0);
                freeSlot = 0;
            }

            m_ActiveInstances[freeSlot] = new ActiveVfx
            {
                GameObject = go,
                ParticleSystem = ps,
                TrailRenderer = tr,
                Text = txt,
                RemainingTime = duration,
                TotalDuration = duration,
                Owner = owner,
                Key = key,
                IsBurst = isBurst,
                IsPopup = isPopup,
                IsActive = true
            };
        }

        private void RecycleOldestBurst()
        {
            float lowestTime = float.MaxValue;
            int oldestIndex = -1;

            for (int i = 0; i < m_ActiveInstances.Length; i++)
            {
                if (m_ActiveInstances[i].IsActive && m_ActiveInstances[i].IsBurst)
                {
                    if (m_ActiveInstances[i].RemainingTime < lowestTime)
                    {
                        lowestTime = m_ActiveInstances[i].RemainingTime;
                        oldestIndex = i;
                    }
                }
            }

            if (oldestIndex >= 0)
            {
                RecycleSlot(oldestIndex);
            }
        }

        private void RecycleOldestPopup()
        {
            float lowestTime = float.MaxValue;
            int oldestIndex = -1;

            for (int i = 0; i < m_ActiveInstances.Length; i++)
            {
                if (m_ActiveInstances[i].IsActive && m_ActiveInstances[i].IsPopup)
                {
                    if (m_ActiveInstances[i].RemainingTime < lowestTime)
                    {
                        lowestTime = m_ActiveInstances[i].RemainingTime;
                        oldestIndex = i;
                    }
                }
            }

            if (oldestIndex >= 0)
            {
                RecycleSlot(oldestIndex);
            }
        }

        private void RecycleSlot(int slot)
        {
            if (slot < 0 || slot >= m_ActiveInstances.Length) return;

            if (m_ActiveInstances[slot].IsActive)
            {
                if (m_ActiveInstances[slot].IsBurst) m_ActiveBurstCount = Mathf.Max(0, m_ActiveBurstCount - 1);
                if (m_ActiveInstances[slot].IsPopup) m_ActivePopupCount = Mathf.Max(0, m_ActivePopupCount - 1);

                if (m_ActiveInstances[slot].TrailRenderer != null)
                {
                    m_ActiveInstances[slot].TrailRenderer.emitting = false;
                    m_ActiveInstances[slot].TrailRenderer.Clear();
                }

                if (m_ActiveInstances[slot].GameObject != null)
                {
                    m_ActiveInstances[slot].GameObject.SetActive(false);
                }

                m_ActiveInstances[slot] = default;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Line98.Core;
using Line98.Data;

namespace Line98.Services
{
    public readonly struct AchievementProgress
    {
        public readonly AchievementSO Definition;
        public readonly string Id;
        public readonly bool Unlocked;
        public readonly int Current;
        public readonly int Threshold;
        public readonly float Normalized;
        public readonly int UnlockOrder; // -1 when locked; higher = more recent

        public AchievementProgress(
            AchievementSO definition,
            string id,
            bool unlocked,
            int current,
            int threshold,
            float normalized,
            int unlockOrder)
        {
            Definition = definition;
            Id = id;
            Unlocked = unlocked;
            Current = current;
            Threshold = threshold;
            Normalized = normalized;
            UnlockOrder = unlockOrder;
        }
    }

    public readonly struct AchievementReadModel
    {
        public readonly int Unlocked;
        public readonly int Total;
        public readonly float Ratio;
        public readonly AchievementProgress[] Ordered;
        public readonly int FeaturedCount;

        public AchievementReadModel(
            int unlocked,
            int total,
            float ratio,
            AchievementProgress[] ordered,
            int featuredCount = 3)
        {
            Unlocked = unlocked;
            Total = total;
            Ratio = ratio;
            Ordered = ordered ?? Array.Empty<AchievementProgress>();
            FeaturedCount = featuredCount;
        }
    }

    public readonly struct AchievementAwardReport
    {
        public readonly int Count;
        public readonly IReadOnlyList<string> UnlockedIds;

        public AchievementAwardReport(int count, IReadOnlyList<string> unlockedIds)
        {
            Count = count;
            UnlockedIds = unlockedIds ?? Array.Empty<string>();
        }

        public static AchievementAwardReport Empty => new AchievementAwardReport(0, Array.Empty<string>());
    }

    public interface IAchievementService
    {
        event Action<string> OnAchievementUnlocked;

        bool IsUnlocked(string achievementId);
        IReadOnlyList<string> UnlockedIds { get; }

        AchievementAwardReport Evaluate(in ProgressMetrics metrics);
        AchievementReadModel BuildReadModel(in ProgressMetrics metrics, int featuredCount = 3);
        AchievementProgress GetProgress(string achievementId, in ProgressMetrics metrics);

        AchievementSO Resolve(string achievementId);
        void LoadState(IEnumerable<string> unlockedIds);
        List<string> SaveState();
        bool TryUnlock(string achievementId);
    }

    public sealed class AchievementService : IAchievementService
    {
        private readonly AchievementCatalogSO m_Catalog;
        private readonly List<string> m_UnlockedIds = new List<string>(10);
        private readonly HashSet<string> m_UnlockedSet = new HashSet<string>();

        public event Action<string> OnAchievementUnlocked;

        public IReadOnlyList<string> UnlockedIds => m_UnlockedIds;

        public AchievementService(AchievementCatalogSO catalog = null)
        {
            m_Catalog = catalog;
        }

        public AchievementAwardReport EvaluateAll(in ProgressMetrics metrics) => Evaluate(in metrics);

        public void CheckLine(int length)
        {
        }

        public void CheckScore(int score)
        {
        }

        public bool IsUnlocked(string achievementId)
        {
            return !string.IsNullOrEmpty(achievementId) && m_UnlockedSet.Contains(achievementId);
        }

        public bool TryUnlock(string achievementId)
        {
            if (string.IsNullOrEmpty(achievementId) || m_UnlockedSet.Contains(achievementId))
            {
                return false;
            }

            m_UnlockedSet.Add(achievementId);
            m_UnlockedIds.Add(achievementId);
            OnAchievementUnlocked?.Invoke(achievementId);
            return true;
        }

        public AchievementSO Resolve(string achievementId)
        {
            if (m_Catalog != null && m_Catalog.TryGet(achievementId, out var so))
            {
                return so;
            }
            return null;
        }

        public void LoadState(IEnumerable<string> unlockedIds)
        {
            m_UnlockedSet.Clear();
            m_UnlockedIds.Clear();

            if (unlockedIds != null)
            {
                foreach (string id in unlockedIds)
                {
                    if (!string.IsNullOrEmpty(id) && m_UnlockedSet.Add(id))
                    {
                        m_UnlockedIds.Add(id);
                    }
                }
            }
        }

        public List<string> SaveState()
        {
            return new List<string>(m_UnlockedIds);
        }

        public AchievementAwardReport Evaluate(in ProgressMetrics metrics)
        {
            if (m_Catalog == null || m_Catalog.Achievements == null)
            {
                return AchievementAwardReport.Empty;
            }

            List<string> newlyUnlocked = null;
            var list = m_Catalog.Achievements;

            for (int i = 0; i < list.Count; i++)
            {
                AchievementSO ach = list[i];
                if (ach == null || IsUnlocked(ach.Id)) continue;

                int val = metrics.ValueOf(ach.Metric, ach.Scope);
                if (val >= ach.Threshold)
                {
                    if (TryUnlock(ach.Id))
                    {
                        if (newlyUnlocked == null) newlyUnlocked = new List<string>();
                        newlyUnlocked.Add(ach.Id);
                    }
                }
            }

            return newlyUnlocked != null
                ? new AchievementAwardReport(newlyUnlocked.Count, newlyUnlocked)
                : AchievementAwardReport.Empty;
        }

        public AchievementProgress GetProgress(string achievementId, in ProgressMetrics metrics)
        {
            AchievementSO ach = Resolve(achievementId);
            bool unlocked = IsUnlocked(achievementId);
            int order = unlocked ? m_UnlockedIds.IndexOf(achievementId) : -1;

            if (ach == null)
            {
                return new AchievementProgress(null, achievementId, unlocked, 0, 1, unlocked ? 1f : 0f, order);
            }

            int current = metrics.ValueOf(ach.Metric, ach.Scope);
            int threshold = ach.Threshold > 0 ? ach.Threshold : 1;
            float norm = unlocked ? 1f : Math.Min(1f, Math.Max(0f, (float)current / threshold));

            return new AchievementProgress(ach, ach.Id, unlocked, current, threshold, norm, order);
        }

        public AchievementReadModel BuildReadModel(in ProgressMetrics metrics, int featuredCount = 3)
        {
            int total = m_Catalog != null ? m_Catalog.Count : 10;
            int unlockedCount = m_UnlockedSet.Count;
            float ratio = total > 0 ? (float)unlockedCount / total : 0f;

            var allProgress = new List<AchievementProgress>(total);
            if (m_Catalog != null && m_Catalog.Achievements != null)
            {
                for (int i = 0; i < m_Catalog.Achievements.Count; i++)
                {
                    AchievementSO ach = m_Catalog.Achievements[i];
                    if (ach != null)
                    {
                        allProgress.Add(GetProgress(ach.Id, in metrics));
                    }
                }
            }

            // Pinned featured sort (Plan r2 §8):
            // 1. Unlocked, by UnlockOrder DESC (most recent first)
            // 2. Unlocked (remaining), catalog order
            // 3. Locked, by Normalized DESC (closest first)
            // 4. Locked (remaining), catalog order
            allProgress.Sort((a, b) =>
            {
                if (a.Unlocked && !b.Unlocked) return -1;
                if (!a.Unlocked && b.Unlocked) return 1;

                if (a.Unlocked && b.Unlocked)
                {
                    // Most recently unlocked first
                    return b.UnlockOrder.CompareTo(a.UnlockOrder);
                }

                // Both locked: closest normalized progress first
                int normCompare = b.Normalized.CompareTo(a.Normalized);
                if (normCompare != 0) return normCompare;

                return 0; // Catalog order preserved by stable sort
            });

            return new AchievementReadModel(unlockedCount, total, ratio, allProgress.ToArray(), featuredCount);
        }
    }
}

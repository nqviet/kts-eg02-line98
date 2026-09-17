using System;
using Line98.Core;

namespace Line98.Services
{
    /// <summary>
    /// Frozen interface contract for daily challenge generation and date resolution per GDD P0.1 / P3.5.
    /// In V1, operates locally using LocalDateProvider. Swappable for server authority in future updates.
    /// </summary>
    public interface IDailyChallengeProvider
    {
        string GetCurrentDateString();
        uint GetDailySeed(string dateString);
        int SeedVersion { get; }
        DateTime GetUtcNow();
    }

    public sealed class LocalDateProvider : IDailyChallengeProvider
    {
        public int SeedVersion => 1;

        public DateTime GetUtcNow() => DateTime.UtcNow;

        public string GetCurrentDateString()
        {
            return GetUtcNow().ToString("yyyy-MM-dd");
        }

        public uint GetDailySeed(string dateString)
        {
            return Fnv1a32.Compute($"{dateString}-v{SeedVersion}");
        }
    }

    public sealed class FixedDateProvider : IDailyChallengeProvider
    {
        private DateTime m_CurrentUtc;
        private readonly int m_SeedVersion;

        public int SeedVersion => m_SeedVersion;

        public FixedDateProvider(DateTime fixedUtcDate, int seedVersion = 1)
        {
            m_CurrentUtc = fixedUtcDate;
            m_SeedVersion = seedVersion;
        }

        public void SetDate(DateTime newUtcDate)
        {
            m_CurrentUtc = newUtcDate;
        }

        public void SetUtcNow(DateTime newUtcDate) => SetDate(newUtcDate);

        public void AdvanceDays(int days)
        {
            m_CurrentUtc = m_CurrentUtc.AddDays(days);
        }

        public DateTime GetUtcNow() => m_CurrentUtc;

        public string GetCurrentDateString()
        {
            return m_CurrentUtc.ToString("yyyy-MM-dd");
        }

        public uint GetDailySeed(string dateString)
        {
            return Fnv1a32.Compute($"{dateString}-v{m_SeedVersion}");
        }
    }
}

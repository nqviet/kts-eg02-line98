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
    }

    public sealed class LocalDateProvider : IDailyChallengeProvider
    {
        public string GetCurrentDateString()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public uint GetDailySeed(string dateString)
        {
            return Fnv1a32.Compute($"{dateString}-v1");
        }
    }
}

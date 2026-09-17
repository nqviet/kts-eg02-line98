using System;
using UnityEngine;

namespace Line98.Data
{
    public enum DailyRunPolicy : byte
    {
        OneShot,
        PracticeAllowed
    }

    public enum TimezoneMode : byte
    {
        Utc,
        LocalMidnight
    }

    [CreateAssetMenu(fileName = "DailyChallengeConfig", menuName = "Line98/Data/Daily Challenge Config")]
    public class DailyChallengeConfigSO : ScriptableObject
    {
        [SerializeField] private string m_SeedFormat = "{0:yyyy-MM-dd}-v1";
        [SerializeField] private int m_StreakGraceHours = 24;
        [SerializeField] private DayOfWeek m_WeekStart = DayOfWeek.Monday;
        [SerializeField] private int m_SeedVersion = 1;
        [SerializeField] private DailyRunPolicy m_RunPolicy = DailyRunPolicy.OneShot;
        [SerializeField] private int m_StartLayoutBallCount = 3;
        [SerializeField] private TimezoneMode m_TimezoneMode = TimezoneMode.Utc;

        public string SeedFormat => m_SeedFormat;
        public int StreakGraceHours => m_StreakGraceHours;
        public DayOfWeek WeekStart => m_WeekStart;
        public int SeedVersion => m_SeedVersion;
        public DailyRunPolicy RunPolicy => m_RunPolicy;
        public int StartLayoutBallCount => m_StartLayoutBallCount;
        public TimezoneMode TimezoneMode => m_TimezoneMode;

        public void InitializeRuntime(
            string seedFormat = "{0:yyyy-MM-dd}-v1",
            int streakGraceHours = 24,
            DayOfWeek weekStart = DayOfWeek.Monday,
            int seedVersion = 1,
            DailyRunPolicy runPolicy = DailyRunPolicy.OneShot,
            int startLayoutBallCount = 3,
            TimezoneMode timezoneMode = TimezoneMode.Utc)
        {
            m_SeedFormat = seedFormat;
            m_StreakGraceHours = streakGraceHours;
            m_WeekStart = weekStart;
            m_SeedVersion = seedVersion;
            m_RunPolicy = runPolicy;
            m_StartLayoutBallCount = startLayoutBallCount;
            m_TimezoneMode = timezoneMode;
        }
    }
}

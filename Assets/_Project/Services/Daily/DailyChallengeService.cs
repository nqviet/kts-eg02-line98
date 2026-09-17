using System;
using System.Collections.Generic;
using Line98.Core;
using Line98.Data;

namespace Line98.Services
{
    public enum DailyDayState : byte
    {
        Future,
        Missed,
        Completed,
        TodayPending,
        TodayCompleted
    }

    public static class DailyCellState
    {
        public const DailyDayState Future = DailyDayState.Future;
        public const DailyDayState Missed = DailyDayState.Missed;
        public const DailyDayState Completed = DailyDayState.Completed;
        public const DailyDayState TodayPending = DailyDayState.TodayPending;
        public const DailyDayState TodayUnplayed = DailyDayState.TodayPending;
        public const DailyDayState TodayCompleted = DailyDayState.TodayCompleted;
    }

    public readonly struct DailyDayCell
    {
        public readonly DateTime Date;
        public readonly int IsoDate; // yyyyMMdd key
        public readonly DailyDayState State;
        public readonly DayOfWeek Weekday;
        public readonly bool IsToday;

        public DayOfWeek DayOfWeek => Weekday;

        public DailyDayCell(DateTime date, int isoDate, DailyDayState state, DayOfWeek weekday, bool isToday)
        {
            Date = date;
            IsoDate = isoDate;
            State = state;
            Weekday = weekday;
            IsToday = isToday;
        }
    }

    public readonly struct DailyPreview
    {
        public readonly BoardModel Board;
        public readonly PreviewQueue Queue;
        public readonly uint Seed;
        public readonly bool Valid;

        public byte[] BoardCells => Board != null ? Board.ExportCells() : Array.Empty<byte>();
        public BallColor[] PreviewQueue => Queue != null ? Queue.ToArray() : Array.Empty<BallColor>();

        public DailyPreview(BoardModel board, PreviewQueue queue, uint seed, bool valid)
        {
            Board = board;
            Queue = queue;
            Seed = seed;
            Valid = valid;
        }

        public static DailyPreview Invalid => new DailyPreview(null, null, 0, false);
    }

    public readonly struct DailyStatus
    {
        public readonly string Date;
        public readonly bool Completed;
        public readonly bool StreakAtRisk;
        public readonly int CurrentStreak;
        public readonly int LongestStreak;
        public readonly int LastDailyScore;

        public DailyStatus(string date, bool completed, bool streakAtRisk, int currentStreak, int longestStreak, int lastDailyScore)
        {
            Date = date;
            Completed = completed;
            StreakAtRisk = streakAtRisk;
            CurrentStreak = currentStreak;
            LongestStreak = longestStreak;
            LastDailyScore = lastDailyScore;
        }
    }

    public readonly struct DailyCompletionResult
    {
        public readonly bool FirstCompletionToday;
        public readonly bool StreakExtended;
        public readonly bool StreakReset;
        public readonly int CurrentStreak;
        public readonly int LongestStreak;
        public readonly bool Milestone7;
        public readonly bool Milestone30;

        public DailyCompletionResult(
            bool firstCompletionToday,
            bool streakExtended,
            bool streakReset,
            int currentStreak,
            int longestStreak,
            bool milestone7,
            bool milestone30)
        {
            FirstCompletionToday = firstCompletionToday;
            StreakExtended = streakExtended;
            StreakReset = streakReset;
            CurrentStreak = currentStreak;
            LongestStreak = longestStreak;
            Milestone7 = milestone7;
            Milestone30 = milestone30;
        }
    }

    public readonly struct DailyShareContext
    {
        public readonly int Score;
        public readonly int Lines;
        public readonly int Moves;
        public readonly int CurrentStreak;
        public readonly bool Perfect;

        public DailyShareContext(int score, int lines, int moves, int currentStreak, bool perfect = false)
        {
            Score = score;
            Lines = lines;
            Moves = moves;
            CurrentStreak = currentStreak;
            Perfect = perfect;
        }
    }

    public readonly struct DailyReadModel
    {
        public readonly DateTime Date;
        public readonly string IsoDate;
        public readonly DailyDayCell[] Week;
        public readonly DayOfWeek WeekStart;
        public readonly int CurrentStreak;
        public readonly int LongestStreak;
        public readonly bool CompletedToday;
        public readonly bool PlayedToday;
        public readonly int TodayScore;
        public readonly int BestScore;
        public readonly bool CanPlayToday;
        public readonly bool StreakAtRisk;
        public readonly DailyPreview Preview;

        public DailyReadModel(
            DateTime date,
            string isoDate,
            DailyDayCell[] week,
            DayOfWeek weekStart,
            int currentStreak,
            int longestStreak,
            bool completedToday,
            bool playedToday,
            int todayScore,
            int bestScore,
            bool canPlayToday,
            bool streakAtRisk,
            DailyPreview preview)
        {
            Date = date;
            IsoDate = isoDate;
            Week = week ?? Array.Empty<DailyDayCell>();
            WeekStart = weekStart;
            CurrentStreak = currentStreak;
            LongestStreak = longestStreak;
            CompletedToday = completedToday;
            PlayedToday = playedToday;
            TodayScore = todayScore;
            BestScore = bestScore;
            CanPlayToday = canPlayToday;
            StreakAtRisk = streakAtRisk;
            Preview = preview;
        }
    }

    public sealed class DailyChallengeService
    {
        private readonly IDailyChallengeProvider m_Provider;
        private readonly DailyChallengeConfigSO m_Config;
        private readonly DailySave m_Save;

        public DailySave SaveData => m_Save;
        public string Today => m_Provider.GetCurrentDateString();
        public uint TodaySeed => m_Provider.GetDailySeed(Today);
        public int SeedVersion => m_Provider.SeedVersion;
        public bool IsTodayCompleted => m_Save.LastCompletedDate == Today;
        public bool CompletedToday => IsTodayCompleted;
        public bool PlayedToday => m_Save.LastScoreDate == Today || m_Save.LastCompletedDate == Today;
        public int TodayScore => m_Save.LastScoreDate == Today ? m_Save.LastDailyScore : 0;
        public int CurrentStreak => CalculateCurrentStreak();
        public int LongestStreak => Math.Max(m_Save.LongestStreak, CurrentStreak);

        public DailyChallengeService(
            DailyChallengeConfigSO config = null,
            DailySave save = null)
            : this(null, config, save)
        {
        }

        public DailyChallengeService(
            IDailyChallengeProvider provider,
            DailyChallengeConfigSO config = null,
            DailySave save = null)
        {
            m_Provider = provider ?? new LocalDateProvider();
            m_Config = config;
            m_Save = save ?? new DailySave();
            if (m_Save.RecentCompletionDays == null)
            {
                m_Save.RecentCompletionDays = new List<int>();
            }
        }

        public DailySave SaveState() => m_Save;

        public DailyStatus GetStatus()
        {
            int streak = CurrentStreak;
            bool completed = IsTodayCompleted;
            bool atRisk = !completed && streak > 0;
            return new DailyStatus(Today, completed, atRisk, streak, LongestStreak, m_Save.LastDailyScore);
        }

        public DailyPreview GetPreview() => BuildPreview();

        public DailyPreview BuildPreview()
        {
            var board = new BoardModel();
            var queue = new PreviewQueue(3);
            uint seed = TodaySeed;
            int count = m_Config != null ? m_Config.StartLayoutBallCount : 3;

            InitialLayoutBuilder.Build(seed, SpawnRules.Default, board, queue, count);
            return new DailyPreview(board, queue, seed, true);
        }

        public DailyReadModel BuildReadModel(int careerBestScore = 0)
        {
            DateTime nowUtc = m_Provider.GetUtcNow();
            DateTime today = nowUtc.Date;
            string isoDate = Today;
            DayOfWeek weekStart = m_Config != null ? m_Config.WeekStart : DayOfWeek.Monday;

            DailyDayCell[] week = BuildWeekCells(today, weekStart);
            int streak = CurrentStreak;
            int longest = LongestStreak;
            bool completedToday = IsTodayCompleted;
            bool playedToday = m_Save.LastScoreDate == isoDate;
            int todayScore = m_Save.LastDailyScore;
            bool canPlayToday = true; // Always can play/practice
            bool streakAtRisk = !completedToday && streak > 0;

            DailyPreview preview = BuildPreview();

            return new DailyReadModel(
                today,
                isoDate,
                week,
                weekStart,
                streak,
                longest,
                completedToday,
                playedToday,
                todayScore,
                careerBestScore,
                canPlayToday,
                streakAtRisk,
                preview);
        }

        public DailyCompletionResult CompleteToday(int score, int lines, int moves, bool scoredRun = true)
        {
            DateTime nowUtc = m_Provider.GetUtcNow();
            DateTime todayDate = nowUtc.Date;
            int todayIsoKey = todayDate.Year * 10000 + todayDate.Month * 100 + todayDate.Day;
            string todayStr = Today;

            bool isFirstCompletionToday = m_Save.LastCompletedDate != todayStr;

            if (isFirstCompletionToday)
            {
                int previousStreak = m_Save.CurrentStreak;
                int newStreak = 1;

                if (!string.IsNullOrEmpty(m_Save.LastCompletedDate) &&
                    DateTime.TryParse(m_Save.LastCompletedDate, out DateTime lastDate))
                {
                    int gap = (int)(todayDate - lastDate.Date).TotalDays;
                    bool graceEnabled = m_Config == null || m_Config.StreakGraceHours >= 24;

                    if (gap == 1)
                    {
                        newStreak = previousStreak + 1;
                    }
                    else if (gap == 2 && graceEnabled)
                    {
                        newStreak = previousStreak + 1;
                    }
                    else
                    {
                        newStreak = 1;
                    }
                }

                m_Save.CurrentStreak = newStreak;
                if (newStreak > m_Save.LongestStreak)
                {
                    m_Save.LongestStreak = newStreak;
                }

                m_Save.LastCompletedDate = todayStr;
                if (!m_Save.RecentCompletionDays.Contains(todayIsoKey))
                {
                    m_Save.RecentCompletionDays.Add(todayIsoKey);
                    // Trim old history beyond 70 days
                    if (m_Save.RecentCompletionDays.Count > 70)
                    {
                        m_Save.RecentCompletionDays.RemoveAt(0);
                    }
                }

                if (scoredRun)
                {
                    m_Save.LastScoreDate = todayStr;
                    m_Save.LastDailyScore = score;
                    m_Save.LastDailyLines = lines;
                    m_Save.LastDailyMoves = moves;
                    m_Save.LastDailySeedVersion = SeedVersion;
                }

                bool streakExtended = newStreak > previousStreak;
                bool streakReset = newStreak == 1 && previousStreak > 1;

                return new DailyCompletionResult(
                    firstCompletionToday: true,
                    streakExtended: streakExtended,
                    streakReset: streakReset,
                    currentStreak: newStreak,
                    longestStreak: m_Save.LongestStreak,
                    milestone7: newStreak >= 7,
                    milestone30: newStreak >= 30);
            }
            else
            {
                // Already completed today: idempotent
                return new DailyCompletionResult(
                    firstCompletionToday: false,
                    streakExtended: false,
                    streakReset: false,
                    currentStreak: m_Save.CurrentStreak,
                    longestStreak: m_Save.LongestStreak,
                    milestone7: m_Save.CurrentStreak >= 7,
                    milestone30: m_Save.CurrentStreak >= 30);
            }
        }

        public string FormatShareText(in DailyShareContext ctx)
        {
            string trophy = ctx.Perfect ? " 🏆 PERFECT!" : "";
            return $"LINE 98: Color Lines — Daily {Today}{trophy}\nScore: {ctx.Score:N0} | Lines: {ctx.Lines} | Moves: {ctx.Moves}\n🔥 {ctx.CurrentStreak} Day Streak!";
        }

        private int CalculateCurrentStreak()
        {
            if (string.IsNullOrEmpty(m_Save.LastCompletedDate))
            {
                return 0;
            }

            if (!DateTime.TryParse(m_Save.LastCompletedDate, out DateTime lastDate))
            {
                return 0;
            }

            DateTime todayDate = m_Provider.GetUtcNow().Date;
            int gap = (int)(todayDate - lastDate.Date).TotalDays;
            bool graceEnabled = m_Config == null || m_Config.StreakGraceHours >= 24;

            if (gap == 0)
            {
                // Completed today
                return m_Save.CurrentStreak;
            }
            if (gap == 1 || (gap == 2 && graceEnabled))
            {
                // Not completed yet today, but streak is still intact
                return m_Save.CurrentStreak;
            }

            // Streak expired
            return 0;
        }

        private DailyDayCell[] BuildWeekCells(DateTime todayDate, DayOfWeek weekStart)
        {
            var cells = new DailyDayCell[7];
            int diff = ((int)todayDate.DayOfWeek - (int)weekStart + 7) % 7;
            DateTime weekStartDate = todayDate.AddDays(-diff);

            for (int i = 0; i < 7; i++)
            {
                DateTime day = weekStartDate.AddDays(i);
                int isoKey = day.Year * 10000 + day.Month * 100 + day.Day;
                bool isToday = day == todayDate;
                bool isCompleted = m_Save.RecentCompletionDays.Contains(isoKey);

                DailyDayState state;
                if (isToday)
                {
                    state = isCompleted ? DailyDayState.TodayCompleted : DailyDayState.TodayPending;
                }
                else if (day > todayDate)
                {
                    state = isCompleted ? DailyDayState.Completed : DailyDayState.Future;
                }
                else
                {
                    state = isCompleted ? DailyDayState.Completed : DailyDayState.Missed;
                }

                cells[i] = new DailyDayCell(day, isoKey, state, day.DayOfWeek, isToday);
            }

            return cells;
        }
    }
}

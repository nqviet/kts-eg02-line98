using System;
using Line98.Core;
using UnityEngine;

namespace Line98.Presentation
{
    public enum DailyDayVisualState : byte
    {
        Future,
        Missed,
        Completed,
        TodayPending,
        TodayCompleted
    }

    public readonly struct DailyDayVisual
    {
        public readonly DateTime Date;
        public readonly DailyDayVisualState State;
        public readonly bool IsToday;

        public DailyDayVisual(DateTime date, DailyDayVisualState state, bool isToday)
        {
            Date = date;
            State = state;
            IsToday = isToday;
        }
    }

    public sealed class DailyPopupPayload : IUiPopupPayload
    {
        public DateTime Date;
        public DailyDayVisual[] Week = Array.Empty<DailyDayVisual>();
        public int CurrentStreak;
        public int BestScore;
        public bool PlayedToday;
        public bool CompletedToday;
        public int TodayScore;
        public bool CanPlayToday;
        public BallColor[] Board = Array.Empty<BallColor>();
    }

    public readonly struct AchievementPopupRow
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly Sprite Icon;
        public readonly bool Unlocked;
        public readonly int Current;
        public readonly int Threshold;
        public readonly float Normalized;

        public AchievementPopupRow(
            string id,
            string displayName,
            Sprite icon,
            bool unlocked,
            int current,
            int threshold,
            float normalized)
        {
            Id = id;
            DisplayName = displayName;
            Icon = icon;
            Unlocked = unlocked;
            Current = current;
            Threshold = threshold;
            Normalized = normalized;
        }
    }

    public sealed class ProgressPopupPayload : IUiPopupPayload
    {
        public int BestScore;
        public int GamesPlayed;
        public int TotalScore;
        public int TotalLinesCleared;
        public int LongestLine;
        public int HighestCombo;
        public int CurrentStreak;
        public int AchievementsUnlocked;
        public int AchievementsTotal;
        public float AchievementRatio;
        public AchievementPopupRow[] Achievements = Array.Empty<AchievementPopupRow>();
    }
}

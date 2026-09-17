using System;
using Line98.Services;

namespace Line98.App
{
    public readonly struct MenuSnapshot
    {
        public readonly int BestScore;
        public readonly int CurrentDailyStreak;
        public readonly bool DailyCompletedToday;
        public readonly bool HasResumableGame;
        public readonly string ResumeModeId;
        public readonly int ResumeScore;
        public readonly int ResumeMoveCount;

        public MenuSnapshot(
            int bestScore,
            int currentDailyStreak,
            bool dailyCompletedToday,
            bool hasResumableGame,
            string resumeModeId,
            int resumeScore,
            int resumeMoveCount)
        {
            BestScore = bestScore;
            CurrentDailyStreak = currentDailyStreak;
            DailyCompletedToday = dailyCompletedToday;
            HasResumableGame = hasResumableGame;
            ResumeModeId = resumeModeId ?? string.Empty;
            ResumeScore = resumeScore;
            ResumeMoveCount = resumeMoveCount;
        }
    }

    public sealed class MenuService
    {
        private readonly StatisticsService m_Stats;
        private readonly DailyChallengeService m_Daily;
        private ResumeDecision m_ResumeDecision;

        public event Action OnMenuStateChanged;

        public ResumeDecision ResumeDecision => m_ResumeDecision;

        public MenuService(StatisticsService stats, DailyChallengeService daily, ResumeDecision initialResume = default)
        {
            m_Stats = stats;
            m_Daily = daily;
            m_ResumeDecision = initialResume;
        }

        public void SetResumeDecision(ResumeDecision decision)
        {
            m_ResumeDecision = decision;
            OnMenuStateChanged?.Invoke();
        }

        public void NotifyChanged()
        {
            OnMenuStateChanged?.Invoke();
        }

        public MenuSnapshot GetSnapshot()
        {
            int best = m_Stats != null ? m_Stats.BestScore : 0;
            int streak = m_Daily != null ? m_Daily.CurrentStreak : 0;
            bool completedToday = m_Daily != null && m_Daily.CompletedToday;

            bool hasResume = m_ResumeDecision.Kind == ResumeDecisionKind.Offer;
            string modeId = hasResume ? m_ResumeDecision.ModeId : string.Empty;
            int score = hasResume ? m_ResumeDecision.Score : 0;
            int moves = hasResume ? m_ResumeDecision.MoveCount : 0;

            return new MenuSnapshot(
                best,
                streak,
                completedToday,
                hasResume,
                modeId,
                score,
                moves);
        }
    }
}

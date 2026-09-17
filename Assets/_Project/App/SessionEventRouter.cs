using System;
using System.Collections.Generic;
using Line98.Core;
using Line98.Gameplay;
using Line98.Services;

namespace Line98.App
{
    /// <summary>
    /// Single fan-out router from GameSession events to Services.
    /// Preserves strict assembly decoupling: Services never subscribe to Gameplay directly,
    /// and Gameplay code never references Services.
    /// </summary>
    public sealed class SessionEventRouter : IDisposable
    {
        private readonly GameSession m_Session;
        private readonly StatisticsService m_Stats;
        private readonly AchievementService m_Achievements;
        private readonly DailyChallengeService m_Daily;
        private readonly ISaveService m_Save;
        private readonly MenuService m_Menu;
        private readonly IAnalyticsService m_Analytics;
        private readonly IGameModeStrategy m_Mode;
        private bool m_Disposed;

        public SessionEventRouter(
            GameSession session,
            StatisticsService stats,
            AchievementService achievements,
            DailyChallengeService daily = null,
            ISaveService save = null,
            MenuService menu = null,
            IAnalyticsService analytics = null,
            IGameModeStrategy mode = null)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_Stats = stats;
            m_Achievements = achievements;
            m_Daily = daily;
            m_Save = save;
            m_Menu = menu;
            m_Analytics = analytics;
            m_Mode = mode ?? session.Mode;

            Subscribe();
            OnSessionStarted();
        }

        public SessionEventRouter(GameSession session, ServiceRegistry services, IGameModeStrategy mode = null)
            : this(
                session,
                services?.Resolve<StatisticsService>(),
                services?.Resolve<AchievementService>(),
                services?.Resolve<DailyChallengeService>(),
                services?.Resolve<ISaveService>() ?? services?.Resolve<SaveService>(),
                services?.Resolve<MenuService>(),
                services?.Resolve<IAnalyticsService>(),
                mode)
        {
        }

        private void Subscribe()
        {
            m_Session.OnMoveCommitted += HandleMoveCommitted;
            m_Session.OnMoveRejected += HandleMoveRejected;
            m_Session.OnGameOver += HandleGameOver;
            m_Session.OnStateRestored += HandleStateRestored;
            m_Session.OnPhaseChanged += HandlePhaseChanged;
            m_Session.OnHintRequested += HandleHintRequested;
            m_Session.OnContinueApplied += HandleContinueApplied;
        }

        private void Unsubscribe()
        {
            m_Session.OnMoveCommitted -= HandleMoveCommitted;
            m_Session.OnMoveRejected -= HandleMoveRejected;
            m_Session.OnGameOver -= HandleGameOver;
            m_Session.OnStateRestored -= HandleStateRestored;
            m_Session.OnPhaseChanged -= HandlePhaseChanged;
            m_Session.OnHintRequested -= HandleHintRequested;
            m_Session.OnContinueApplied -= HandleContinueApplied;
        }

        private void OnSessionStarted()
        {
            if (m_Mode != null && m_Mode.RecordsStatistics)
            {
                m_Stats?.RecordGameStart();
            }

            if (m_Mode is DailyChallengeMode)
            {
                m_Analytics?.Track(AnalyticsEvents.DailyStart);
            }
            else if (m_Mode is ZenMode)
            {
                m_Analytics?.Track(AnalyticsEvents.ZenStart);
            }
            else
            {
                m_Analytics?.Track(AnalyticsEvents.GameStart);
            }
        }

        private void HandleMoveCommitted(MoveResult result)
        {
            bool recordStats = m_Mode == null || m_Mode.RecordsStatistics;

            if (recordStats && m_Stats != null)
            {
                m_Stats.RecordMove();
                if (result.Outcome == MoveOutcome.Cleared)
                {
                    m_Stats.RecordLineClear(result.Cleared.LongestRun, result.Cleared.RunCount, result.ScoreDelta);
                    m_Stats.RecordScore(m_Session.Score);
                }
            }

            RecomposeMetricsAndEvaluate();

            m_Save?.TrySaveSession(m_Session.CaptureState());

            m_Analytics?.Track(AnalyticsEvents.GameMove, new Dictionary<string, object>
            {
                ["score"] = m_Session.Score,
                ["moveCount"] = m_Session.MoveCount
            });

            if (result.Outcome == MoveOutcome.Cleared)
            {
                m_Analytics?.Track(AnalyticsEvents.LineClear, new Dictionary<string, object>
                {
                    ["runCount"] = result.Cleared.RunCount,
                    ["longestRun"] = result.Cleared.LongestRun,
                    ["scoreDelta"] = result.ScoreDelta
                });

                if (result.Cleared.LongestRun >= 7)
                {
                    m_Analytics?.Track(AnalyticsEvents.LongLine, new Dictionary<string, object>
                    {
                        ["length"] = result.Cleared.LongestRun
                    });
                }

                if (result.ComboMultiplier > 1f)
                {
                    m_Analytics?.Track(AnalyticsEvents.Combo, new Dictionary<string, object>
                    {
                        ["combo"] = result.ComboMultiplier
                    });
                }
            }
        }

        private void HandleMoveRejected(MoveRejection rejection)
        {
            m_Analytics?.Track(AnalyticsEvents.GameInvalidMove, new Dictionary<string, object>
            {
                ["outcome"] = rejection.Outcome.ToString()
            });
        }

        private void HandleGameOver(SessionSummary summary)
        {
            bool recordStats = m_Mode == null || m_Mode.RecordsStatistics;
            if (recordStats && m_Stats != null)
            {
                m_Stats.RecordGameOver(summary.FinalScore);
            }

            if (m_Mode is DailyChallengeMode && m_Daily != null)
            {
                var completionResult = m_Daily.CompleteToday(summary.FinalScore, summary.LinesCleared, summary.MoveCount);
                m_Analytics?.Track(AnalyticsEvents.DailyComplete, new Dictionary<string, object>
                {
                    ["score"] = summary.FinalScore,
                    ["streak"] = completionResult.CurrentStreak
                });
            }

            RecomposeMetricsAndEvaluate();

            m_Save?.TrySaveSession(m_Session.CaptureState());
            m_Menu?.NotifyChanged();

            m_Analytics?.Track(AnalyticsEvents.GameOver, new Dictionary<string, object>
            {
                ["finalScore"] = summary.FinalScore,
                ["longestLine"] = summary.LongestLine,
                ["linesCleared"] = summary.LinesCleared,
                ["moves"] = summary.MoveCount
            });
        }

        private void HandleStateRestored(GameSnapshot snapshot)
        {
            RecomposeMetricsAndEvaluate();
            m_Save?.TrySaveSession(m_Session.CaptureState());
            m_Analytics?.Track(AnalyticsEvents.UndoUsed);
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            // Pause/resume telemetry or state tracking
        }

        private void HandleHintRequested(HintSuggestion suggestion)
        {
            m_Analytics?.Track(AnalyticsEvents.HintUsed);
        }

        private void HandleContinueApplied(ContinueResult result)
        {
            m_Save?.TrySaveSession(m_Session.CaptureState());
        }

        private void RecomposeMetricsAndEvaluate()
        {
            bool recordStats = m_Mode == null || m_Mode.RecordsStatistics;
            if (!recordStats) return;

            int streak = m_Daily != null ? m_Daily.CurrentStreak : (m_Stats != null ? m_Stats.CurrentStreak : 0);
            var sessionCtx = new SessionContext(
                m_Session.Score,
                m_Session.MoveCount,
                m_Session.LinesCleared,
                m_Session.LongestLine);

            ProgressMetrics metrics = m_Stats != null
                ? m_Stats.BuildMetrics(in sessionCtx, streak)
                : new ProgressMetrics(
                    m_Session.Score,
                    m_Session.Score,
                    1,
                    m_Session.Score,
                    m_Session.LinesCleared,
                    m_Session.LinesCleared,
                    m_Session.LongestLine,
                    m_Session.LongestLine,
                    1f,
                    1f,
                    m_Session.MoveCount,
                    m_Session.MoveCount,
                    streak,
                    streak);

            m_Achievements?.EvaluateAll(in metrics);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            Unsubscribe();
        }
    }
}

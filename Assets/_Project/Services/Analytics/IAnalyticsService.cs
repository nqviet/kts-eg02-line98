using System.Collections.Generic;
using UnityEngine;

namespace Line98.Services
{
    public static class AnalyticsEvents
    {
        public const string GameStart = "game_start";
        public const string GameResume = "game_resume";
        public const string GameMove = "game_move";
        public const string GameInvalidMove = "game_invalid_move";
        public const string LineClear = "line_clear";
        public const string LongLine = "long_line";
        public const string Combo = "combo";
        public const string GameOver = "game_over";
        public const string ContinueOffer = "continue_offer";
        public const string ContinueAdStarted = "continue_ad_started";
        public const string ContinueAdCompleted = "continue_ad_completed";
        public const string UndoUsed = "undo_used";
        public const string HintUsed = "hint_used";
        public const string DailyStart = "daily_start";
        public const string DailyComplete = "daily_complete";
        public const string ZenStart = "zen_start";
        public const string ThemeSelected = "theme_selected";
        public const string RemoveAdsPurchase = "remove_ads_purchase";
    }

    public interface IAnalyticsService
    {
        void Track(string eventName, Dictionary<string, object> parameters = null);
    }

    public sealed class DebugAnalyticsService : IAnalyticsService
    {
        public void Track(string eventName, Dictionary<string, object> parameters = null)
        {
            Debug.Log($"[Analytics] {eventName}");
        }
    }

    public sealed class NoOpAnalyticsService : IAnalyticsService
    {
        public void Track(string eventName, Dictionary<string, object> parameters = null)
        {
        }
    }
}

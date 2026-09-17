using System;
using Line98.Services;

namespace Line98.App
{
    public enum ResumeDecisionKind : byte
    {
        None,
        Offer,
        Expired
    }

    public readonly struct ResumeDecision
    {
        public readonly ResumeDecisionKind Kind;
        public readonly string ModeId;
        public readonly int Score;
        public readonly int MoveCount;
        public readonly string DailySeedDate;

        public ResumeDecision(ResumeDecisionKind kind, string modeId = null, int score = 0, int moveCount = 0, string dailySeedDate = null)
        {
            Kind = kind;
            ModeId = modeId ?? string.Empty;
            Score = score;
            MoveCount = moveCount;
            DailySeedDate = dailySeedDate;
        }

        public static ResumeDecision None => new ResumeDecision(ResumeDecisionKind.None);
        public static ResumeDecision Expired(string modeId, string dailySeedDate) =>
            new ResumeDecision(ResumeDecisionKind.Expired, modeId, 0, 0, dailySeedDate);
        public static ResumeDecision Offer(string modeId, int score, int moveCount, string dailySeedDate = null) =>
            new ResumeDecision(ResumeDecisionKind.Offer, modeId, score, moveCount, dailySeedDate);
    }

    public static class ResumeEvaluator
    {
        public static ResumeDecision Evaluate(SaveData data, string todayIsoDate)
        {
            if (data == null || data.Session == null)
            {
                return ResumeDecision.None;
            }

            if (!data.Session.IsResumable)
            {
                return ResumeDecision.None;
            }

            string modeId = data.Session.ModeId;
            if (string.IsNullOrEmpty(modeId))
            {
                return ResumeDecision.None;
            }

            if (modeId.Equals("daily", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(data.Session.DailySeedDate) || data.Session.DailySeedDate != todayIsoDate)
                {
                    return ResumeDecision.Expired(modeId, data.Session.DailySeedDate);
                }
            }

            return ResumeDecision.Offer(modeId, data.Session.Score, data.Session.MoveCount, data.Session.DailySeedDate);
        }
    }
}

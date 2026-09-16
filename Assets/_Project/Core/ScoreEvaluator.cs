using System;

namespace Line98.Core
{
    public readonly struct ScoreRules
    {
        public readonly int Score5;
        public readonly int Score6;
        public readonly int Score7;
        public readonly int Score8;
        public readonly int Score9;
        public readonly float ComboStep;
        public readonly float MaxComboMultiplier;

        public ScoreRules(
            int score5 = 100,
            int score6 = 180,
            int score7 = 300,
            int score8 = 500,
            int score9 = 800,
            float comboStep = 0.25f,
            float maxComboMultiplier = 2.0f)
        {
            Score5 = score5;
            Score6 = score6;
            Score7 = score7;
            Score8 = score8;
            Score9 = score9;
            ComboStep = comboStep;
            MaxComboMultiplier = maxComboMultiplier;
        }

        public static ScoreRules Default => new ScoreRules(100, 180, 300, 500, 800, 0.25f, 2.0f);

        public int GetBaseScoreForLength(int length)
        {
            int s5 = Score5 > 0 ? Score5 : 100;
            int s6 = Score6 > 0 ? Score6 : 180;
            int s7 = Score7 > 0 ? Score7 : 300;
            int s8 = Score8 > 0 ? Score8 : 500;
            int s9 = Score9 > 0 ? Score9 : 800;

            switch (length)
            {
                case 5: return s5;
                case 6: return s6;
                case 7: return s7;
                case 8: return s8;
                case 9: return s9;
                default:
                    if (length < 5) return 0;
                    return s9 + (length - 9) * (s9 - s8);
            }
        }
    }

    /// <summary>
    /// Pure score calculation utility for cleared lines and combos.
    /// </summary>
    public static class ScoreEvaluator
    {
        public static float GetComboMultiplier(int runCount, in ScoreRules rules)
        {
            int safeRunCount = Math.Max(1, runCount);
            float comboMultiplier = 1.0f + rules.ComboStep * (safeRunCount - 1);
            if (comboMultiplier > rules.MaxComboMultiplier)
            {
                comboMultiplier = rules.MaxComboMultiplier;
            }
            return comboMultiplier;
        }

        public static int Evaluate(in ClearGroup group, in ScoreRules rules)
        {
            if (group.IsEmpty)
            {
                return 0;
            }

            int baseScore = rules.GetBaseScoreForLength(group.LongestRun);
            float comboMultiplier = GetComboMultiplier(group.RunCount, in rules);

            return (int)Math.Round(baseScore * comboMultiplier);
        }
    }
}

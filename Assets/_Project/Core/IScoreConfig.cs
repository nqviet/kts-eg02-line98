namespace Line98.Core
{
    /// <summary>
    /// Frozen interface contract for score configuration per GDD P0.1 / P0.3.
    /// Bridges data-driven assets (ScoreTableSO) to pure Core evaluation (ScoreRules).
    /// </summary>
    public interface IScoreConfig
    {
        ScoreRules ToRules();
    }
}

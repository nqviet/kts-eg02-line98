namespace Line98.Core
{
    /// <summary>
    /// Deterministic, serializable random number source contract per GDD P0.1 / P0.3.
    /// Used across Core and Gameplay to guarantee platform determinism,
    /// seed reproducibility, and state rewinding for true Undo.
    /// </summary>
    public interface IRandomSource
    {
        uint NextUInt();
        int Range(int minInclusive, int maxExclusive);
        void SeedFrom(uint seed);
        (ulong s0, ulong s1, ulong s2, ulong s3) GetState();
        void RestoreState(ulong s0, ulong s1, ulong s2, ulong s3);
    }
}

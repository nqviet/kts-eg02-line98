using System;

namespace Line98.Core
{
    /// <summary>
    /// Deterministic start layout builder for a seed.
    /// Provides the single authoritative initialization path for both the live game and the daily board preview.
    /// </summary>
    public static class InitialLayoutBuilder
    {
        public static void Build(
            uint seed,
            in SpawnRules spawnRules,
            BoardModel boardOut,
            PreviewQueue queueOut,
            int placementCount = 3)
        {
            var rng = new XorShift128(seed);
            Build(ref rng, in spawnRules, boardOut, queueOut, placementCount);
        }

        public static void Build(
            ref XorShift128 rng,
            in SpawnRules spawnRules,
            BoardModel boardOut,
            PreviewQueue queueOut,
            int placementCount = 3)
        {
            if (boardOut == null || queueOut == null) return;

            boardOut.Reset();
            int activeColors = spawnRules.GetActiveColorCount(0);

            // 1. Initial preview queue
            queueOut.Populate(ref rng, activeColors);

            // 2. Place placementCount balls from queue into empty cells deterministically
            int placed = 0;
            int maxAttempts = 1000;
            while (placed < placementCount && maxAttempts-- > 0)
            {
                int randomCell = rng.Range(0, BoardModel.CellCount);
                if (boardOut.IsEmpty(randomCell))
                {
                    boardOut.Set(randomCell, queueOut[placed]);
                    placed++;
                }
            }

            // 3. Repopulate preview queue for the upcoming move
            queueOut.Populate(ref rng, activeColors);
        }
    }
}

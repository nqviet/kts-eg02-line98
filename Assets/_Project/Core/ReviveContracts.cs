using System;

namespace Line98.Core
{
    public readonly struct ReviveRules
    {
        public readonly int RemoveCount;
        public readonly int MinFreedCells;
        public readonly int RecoveryMoves;
        public readonly int MaxContinuesPerRun;

        public ReviveRules(int removeCount, int minFreedCells, int recoveryMoves, int maxContinuesPerRun)
        {
            RemoveCount = removeCount;
            MinFreedCells = minFreedCells;
            RecoveryMoves = recoveryMoves;
            MaxContinuesPerRun = maxContinuesPerRun;
        }

        public static ReviveRules Default => new ReviveRules(
            removeCount: 3,
            minFreedCells: 3,
            recoveryMoves: 1,
            maxContinuesPerRun: 1);
    }

    public readonly struct ContinuePayload
    {
        public readonly GridPos[] ClearedCells;
        public readonly int Count;
        public readonly int FreedCells;
        public readonly int GrantedMoves;
        public readonly bool IsEmpty;

        public ContinuePayload(GridPos[] clearedCells, int count, int freedCells, int grantedMoves)
        {
            ClearedCells = clearedCells ?? Array.Empty<GridPos>();
            Count = count;
            FreedCells = freedCells;
            GrantedMoves = grantedMoves;
            IsEmpty = count == 0;
        }

        public static ContinuePayload Empty => new ContinuePayload(Array.Empty<GridPos>(), 0, 0, 0);
    }
}

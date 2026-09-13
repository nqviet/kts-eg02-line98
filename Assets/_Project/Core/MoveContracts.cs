using System;
using System.Collections.Generic;

namespace Line98.Core
{
    public enum GamePhase : byte
    {
        Boot,
        Menu,
        Playing,
        Resolving,
        Paused,
        GameOver
    }

    public enum MoveOutcome : byte
    {
        Invalid,
        NoPath,
        Moved,
        Cleared,
        GameOver
    }

    public readonly struct MoveRequest
    {
        public readonly GridPos From;
        public readonly GridPos To;
        public readonly uint BallId;

        public MoveRequest(GridPos from, GridPos to, uint ballId = 0)
        {
            From = from;
            To = to;
            BallId = ballId;
        }
    }

    /// <summary>
    /// Represents the pre-computed plan for a move.
    /// Produced by MoveResolver by simulating on a scratch board.
    /// Committed atomically by GameSession once presentation completes.
    /// </summary>
    public sealed class MovePlan
    {
        public MoveOutcome Outcome { get; set; }
        public GridPos From { get; set; }
        public GridPos To { get; set; }
        public List<GridPos> Path { get; } = new List<GridPos>(81);
        public ClearGroup Cleared { get; set; }
        public SpawnBatch Spawned { get; set; }
        public int ScoreDelta { get; set; }
        public XorShift128 PostMoveRng { get; set; }
        public BallColor[] NextPreviewQueue { get; } = new BallColor[PreviewQueue.DefaultCapacity];
        public bool IsGameOver { get; set; }

        public readonly SpawnItem[] SpawnItemsBuffer = new SpawnItem[PreviewQueue.DefaultCapacity];

        public void Reset()
        {
            Outcome = MoveOutcome.Invalid;
            From = default;
            To = default;
            Path.Clear();
            Cleared = ClearGroup.Empty;
            Spawned = SpawnBatch.Empty;
            ScoreDelta = 0;
            PostMoveRng = default;
            IsGameOver = false;
        }
    }

    public readonly struct MoveResult
    {
        public readonly MoveOutcome Outcome;
        public readonly ClearGroup Cleared;
        public readonly SpawnBatch Spawned;
        public readonly int ScoreDelta;

        public MoveResult(MoveOutcome outcome, ClearGroup cleared, SpawnBatch spawned, int scoreDelta)
        {
            Outcome = outcome;
            Cleared = cleared;
            Spawned = spawned;
            ScoreDelta = scoreDelta;
        }
    }
}

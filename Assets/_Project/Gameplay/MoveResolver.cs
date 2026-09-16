using System;
using System.Collections.Generic;
using Line98.Core;

namespace Line98.Gameplay
{
    /// <summary>
    /// Evaluates move requests by simulating them completely on a scratch board.
    /// Produces an immutable/reusable MovePlan for presentation and atomic commit.
    /// Never mutates the authoritative BoardModel.
    /// </summary>
    public sealed class MoveResolver
    {
        private readonly BoardModel m_ScratchBoard = new BoardModel();
        private readonly List<int> m_EmptyIndexPool = new List<int>(BoardModel.CellCount);
        private readonly MovePlan m_FallbackPlan = new MovePlan();

        public MovePlan Resolve(
            BoardModel board,
            PreviewQueue previewQueue,
            in XorShift128 currentRng,
            in MoveRequest request,
            in ScoreRules scoreRules,
            in SpawnRules spawnRules,
            int totalLinesCleared,
            MovePlan targetPlan = null)
        {
            var plan = targetPlan ?? m_FallbackPlan;
            plan.Reset();
            plan.From = request.From;
            plan.To = request.To;

            if (board == null || !request.From.IsValid || !request.To.IsValid)
            {
                plan.Outcome = MoveOutcome.Invalid;
                return plan;
            }

            if (board.IsEmpty(request.From) || !board.IsEmpty(request.To))
            {
                plan.Outcome = MoveOutcome.Invalid;
                return plan;
            }

            // Path check
            if (!Pathfinder.TryFindPath(board, request.From, request.To, plan.Path))
            {
                plan.Outcome = MoveOutcome.NoPath;
                return plan;
            }

            // Copy to scratch board for simulation
            m_ScratchBoard.CopyFrom(board);

            BallColor movingColor = m_ScratchBoard.ColorAt(request.From);
            m_ScratchBoard.Clear(request.From);
            m_ScratchBoard.Set(request.To, movingColor);

            XorShift128 simulatedRng = currentRng;

            // Check for line clears at destination
            if (LineDetector.TryBuildClearGroup(m_ScratchBoard, request.To, out ClearGroup clearedGroup))
            {
                plan.Cleared = clearedGroup;
                plan.ComboMultiplier = ScoreEvaluator.GetComboMultiplier(clearedGroup.RunCount, in scoreRules);
                plan.ScoreDelta = ScoreEvaluator.Evaluate(clearedGroup, scoreRules);

                // Clear balls on scratch board
                for (int i = 0; i < clearedGroup.Positions.Length; i++)
                {
                    m_ScratchBoard.Clear(clearedGroup.Positions[i]);
                }

                // Successful clear suppresses spawn per GDD §2
                plan.Spawned = SpawnBatch.Empty;
                plan.PostMoveRng = simulatedRng;
                previewQueue.CopyTo(plan.NextPreviewQueue);
            }
            else
            {
                plan.Cleared = ClearGroup.Empty;
                plan.ScoreDelta = 0;

                // Non-clearing move: spawn upcoming balls from preview queue
                CollectEmptyCells(m_ScratchBoard, m_EmptyIndexPool);

                int spawnLimit = Math.Min(previewQueue.Capacity, m_EmptyIndexPool.Count);
                if (spawnLimit > 0)
                {
                    for (int i = 0; i < spawnLimit; i++)
                    {
                        int pickedIndex = simulatedRng.Range(0, m_EmptyIndexPool.Count);
                        int cellIndex = m_EmptyIndexPool[pickedIndex];
                        m_EmptyIndexPool.RemoveAt(pickedIndex);

                        GridPos spawnPos = GridPos.FromIndex(cellIndex);
                        BallColor color = previewQueue[i];

                        m_ScratchBoard.Set(spawnPos, color);
                        plan.SpawnItemsBuffer[i] = new SpawnItem(spawnPos, color);
                    }
                    plan.Spawned = new SpawnBatch(plan.SpawnItemsBuffer, spawnLimit);
                }
                else
                {
                    plan.Spawned = SpawnBatch.Empty;
                }

                // Populate next preview queue
                int activeColors = spawnRules.GetActiveColorCount(totalLinesCleared);
                for (int i = 0; i < plan.NextPreviewQueue.Length; i++)
                {
                    plan.NextPreviewQueue[i] = (BallColor)(simulatedRng.Range(0, activeColors) + 1);
                }
                plan.PostMoveRng = simulatedRng;
            }

            // Check game-over condition (O(81) adjacency per Architecture §0 Decision #2)
            bool hasEmptyCells = m_ScratchBoard.EmptyCount > 0;
            bool hasLegalMove = CheckAnyLegalMove(m_ScratchBoard);

            if (!hasEmptyCells || !hasLegalMove)
            {
                plan.IsGameOver = true;
                plan.Outcome = MoveOutcome.GameOver;
            }
            else
            {
                plan.IsGameOver = false;
                plan.Outcome = plan.Cleared.Count > 0 ? MoveOutcome.Cleared : MoveOutcome.Moved;
            }

            return plan;
        }

        private static void CollectEmptyCells(BoardModel board, List<int> pool)
        {
            pool.Clear();
            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                if (board.IsEmpty(i))
                {
                    pool.Add(i);
                }
            }
        }

        public static bool CheckAnyLegalMove(BoardModel board)
        {
            if (board == null || board.IsFull)
            {
                return false;
            }

            // A legal move exists iff some occupied ball has at least one empty 4-neighbor
            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    int index = y * BoardModel.Size + x;
                    if (board.IsEmpty(index))
                    {
                        continue;
                    }

                    // Check 4 neighbors
                    if (x > 0 && board.IsEmpty(index - 1)) return true;
                    if (x < BoardModel.Size - 1 && board.IsEmpty(index + 1)) return true;
                    if (y > 0 && board.IsEmpty(index - BoardModel.Size)) return true;
                    if (y < BoardModel.Size - 1 && board.IsEmpty(index + BoardModel.Size)) return true;
                }
            }

            return false;
        }
    }
}

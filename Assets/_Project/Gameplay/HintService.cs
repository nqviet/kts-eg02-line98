using System;
using System.Collections.Generic;
using Line98.Core;

namespace Line98.Gameplay
{
    public static class HintService
    {
        private static readonly BoardModel s_TestBoard = new BoardModel();
        private static readonly List<GridPos> s_PathCache = new List<GridPos>(BoardModel.CellCount);
        private static readonly List<GridPos> s_BestPathCache = new List<GridPos>(BoardModel.CellCount);
        private static readonly List<GridPos> s_FallbackPathCache = new List<GridPos>(BoardModel.CellCount);

        public static bool TryFindBestMove(
            BoardModel board,
            PreviewQueue previewQueue,
            ScoreRules scoreRules,
            out GridPos bestFrom,
            out GridPos bestTo)
        {
            return TryFindBestMove(board, previewQueue, scoreRules, out bestFrom, out bestTo, null);
        }

        public static bool TryFindBestMove(
            BoardModel board,
            PreviewQueue previewQueue,
            ScoreRules scoreRules,
            out GridPos bestFrom,
            out GridPos bestTo,
            List<GridPos> pathOut)
        {
            bestFrom = default;
            bestTo = default;
            pathOut?.Clear();

            if (board == null || board.IsEmptyBoard || board.IsFull)
            {
                return false;
            }

            int bestScore = -1;
            bool foundAnyMove = false;
            GridPos fallbackFrom = default;
            GridPos fallbackTo = default;
            s_BestPathCache.Clear();
            s_FallbackPathCache.Clear();

            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos from = new GridPos(x, y);
                    if (board.IsEmpty(from))
                    {
                        continue;
                    }

                    BallColor movingColor = board.ColorAt(from);

                    // Check reachable destinations
                    for (int dy = 0; dy < BoardModel.Size; dy++)
                    {
                        for (int dx = 0; dx < BoardModel.Size; dx++)
                        {
                            GridPos to = new GridPos(dx, dy);
                            if (!board.IsEmpty(to))
                            {
                                continue;
                            }

                            if (!Pathfinder.TryFindPath(board, from, to, s_PathCache))
                            {
                                continue;
                            }

                            if (!foundAnyMove)
                            {
                                fallbackFrom = from;
                                fallbackTo = to;
                                foundAnyMove = true;
                                s_FallbackPathCache.Clear();
                                s_FallbackPathCache.AddRange(s_PathCache);
                            }

                            // Simulate on test board
                            s_TestBoard.CopyFrom(board);
                            s_TestBoard.Clear(from);
                            s_TestBoard.Set(to, movingColor);

                            if (LineDetector.TryBuildClearGroup(s_TestBoard, to, out ClearGroup group))
                            {
                                int score = ScoreEvaluator.Evaluate(group, scoreRules);
                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestFrom = from;
                                    bestTo = to;
                                    s_BestPathCache.Clear();
                                    s_BestPathCache.AddRange(s_PathCache);
                                }
                            }
                        }
                    }
                }
            }

            if (bestScore > 0)
            {
                if (pathOut != null)
                {
                    pathOut.AddRange(s_BestPathCache);
                }
                return true;
            }

            if (foundAnyMove)
            {
                bestFrom = fallbackFrom;
                bestTo = fallbackTo;
                if (pathOut != null)
                {
                    pathOut.AddRange(s_FallbackPathCache);
                }
                return true;
            }

            return false;
        }
    }
}

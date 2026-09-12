using System;
using System.Collections.Generic;

namespace Line98.Core
{
    /// <summary>
    /// Allocation-free 4-directional BFS pathfinder for the 9x9 board.
    /// Operates using preallocated static scratch buffers.
    /// Thread-safe via internal synchronization lock.
    /// </summary>
    public static class Pathfinder
    {
        private static readonly object s_Lock = new object();
        private static readonly int[] s_Queue = new int[BoardModel.CellCount];
        private static readonly int[] s_Parent = new int[BoardModel.CellCount];
        private static readonly int[] s_ReversePath = new int[BoardModel.CellCount];

        public static bool TryFindPath(BoardModel board, GridPos from, GridPos to, List<GridPos> pathOut)
        {
            if (pathOut != null)
            {
                pathOut.Clear();
            }

            if (board == null || !from.IsValid || !to.IsValid)
            {
                return false;
            }

            // Destination must be empty
            if (!board.IsEmpty(to))
            {
                return false;
            }

            // Source must contain a ball
            if (board.IsEmpty(from))
            {
                return false;
            }

            if (from == to)
            {
                pathOut?.Add(from);
                return true;
            }

            lock (s_Lock)
            {
                int startIndex = from.Index;
                int targetIndex = to.Index;

                ulong visitedLow = 0UL;
                ulong visitedHigh = 0UL;

                for (int i = 0; i < BoardModel.CellCount; i++)
                {
                    s_Parent[i] = -1;
                }

                int head = 0;
                int tail = 0;

                s_Queue[tail++] = startIndex;
                SetVisited(startIndex, ref visitedLow, ref visitedHigh);

                bool found = false;

                while (head < tail)
                {
                    int current = s_Queue[head++];
                    if (current == targetIndex)
                    {
                        found = true;
                        break;
                    }

                    byte cx = (byte)(current % BoardModel.Size);
                    byte cy = (byte)(current / BoardModel.Size);

                    // 4-directional neighbors: Right, Left, Down, Up
                    CheckNeighbor(cx + 1, cy, cx < BoardModel.Size - 1, current, board, ref visitedLow, ref visitedHigh, ref tail);
                    CheckNeighbor(cx - 1, cy, cx > 0, current, board, ref visitedLow, ref visitedHigh, ref tail);
                    CheckNeighbor(cx, cy + 1, cy < BoardModel.Size - 1, current, board, ref visitedLow, ref visitedHigh, ref tail);
                    CheckNeighbor(cx, cy - 1, cy > 0, current, board, ref visitedLow, ref visitedHigh, ref tail);
                }

                if (!found)
                {
                    return false;
                }

                if (pathOut != null)
                {
                    int step = targetIndex;
                    int count = 0;
                    while (step != -1)
                    {
                        s_ReversePath[count++] = step;
                        step = s_Parent[step];
                    }

                    // Reverse into pathOut: from start to target
                    for (int i = count - 1; i >= 0; i--)
                    {
                        pathOut.Add(GridPos.FromIndex(s_ReversePath[i]));
                    }
                }

                return true;
            }
        }

        private static void CheckNeighbor(
            int nx,
            int ny,
            bool inBounds,
            int current,
            BoardModel board,
            ref ulong visitedLow,
            ref ulong visitedHigh,
            ref int tail)
        {
            if (!inBounds)
            {
                return;
            }

            int nextIndex = ny * BoardModel.Size + nx;
            if (IsVisited(nextIndex, visitedLow, visitedHigh))
            {
                return;
            }

            if (!board.IsEmpty(nextIndex))
            {
                return;
            }

            SetVisited(nextIndex, ref visitedLow, ref visitedHigh);
            s_Parent[nextIndex] = current;
            s_Queue[tail++] = nextIndex;
        }

        private static bool IsVisited(int index, ulong low, ulong high)
        {
            if (index < 64)
            {
                return (low & (1UL << index)) != 0UL;
            }
            return (high & (1UL << (index - 64))) != 0UL;
        }

        private static void SetVisited(int index, ref ulong low, ref ulong high)
        {
            if (index < 64)
            {
                low |= (1UL << index);
            }
            else
            {
                high |= (1UL << (index - 64));
            }
        }
    }
}

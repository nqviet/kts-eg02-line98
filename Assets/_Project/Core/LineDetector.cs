using System;
using System.Collections.Generic;

namespace Line98.Core
{
    public enum LineAxis : byte
    {
        Horizontal,
        Vertical,
        DiagonalAscending,   // bottom-left to top-right
        DiagonalDescending   // top-left to bottom-right
    }

    public readonly struct LineRun
    {
        public readonly LineAxis Axis;
        public readonly BallColor Color;
        public readonly GridPos Start;
        public readonly byte Length;

        public LineRun(LineAxis axis, BallColor color, GridPos start, byte length)
        {
            Axis = axis;
            Color = color;
            Start = start;
            Length = length;
        }
    }

    public readonly struct ClearGroup
    {
        private static readonly GridPos[] s_EmptyPositions = Array.Empty<GridPos>();

        public readonly GridPos[] Positions;
        public readonly int LongestRun;
        public readonly int RunCount;
        public readonly BallColor ClearedColor;

        public int Count => Positions != null ? Positions.Length : 0;
        public bool IsEmpty => Count == 0;

        public ClearGroup(GridPos[] positions, int longestRun, int runCount, BallColor clearedColor)
        {
            Positions = positions ?? s_EmptyPositions;
            LongestRun = longestRun;
            RunCount = runCount;
            ClearedColor = clearedColor;
        }

        public static ClearGroup Empty => new ClearGroup(s_EmptyPositions, 0, 0, BallColor.None);
    }

    /// <summary>
    /// Scans the 4 axes around a target cell and detects completed lines of >= 5 balls.
    /// Unifies multiple simultaneous runs into a deduped ClearGroup without allocating.
    /// </summary>
    public static class LineDetector
    {
        public const int MinimumLineLength = 5;

        // Direction vectors for the 4 axes
        private static readonly int[] s_Dx = { 1, 0, 1, 1 };
        private static readonly int[] s_Dy = { 0, 1, 1, -1 };

        public static bool TryBuildClearGroup(BoardModel board, GridPos origin, out ClearGroup group)
        {
            group = ClearGroup.Empty;
            if (board == null || !origin.IsValid)
            {
                return false;
            }

            BallColor targetColor = board.ColorAt(origin);
            if (targetColor == BallColor.None)
            {
                return false;
            }

            ulong maskLow = 0UL;
            ulong maskHigh = 0UL;
            int runCount = 0;
            int longestRun = 0;

            for (int axisIndex = 0; axisIndex < 4; axisIndex++)
            {
                int dx = s_Dx[axisIndex];
                int dy = s_Dy[axisIndex];

                // Count positive direction
                int posCount = CountDirection(board, origin.X, origin.Y, dx, dy, targetColor);
                // Count negative direction
                int negCount = CountDirection(board, origin.X, origin.Y, -dx, -dy, targetColor);

                int totalLength = 1 + posCount + negCount;
                if (totalLength >= MinimumLineLength)
                {
                    runCount++;
                    if (totalLength > longestRun)
                    {
                        longestRun = totalLength;
                    }

                    // Include origin
                    SetBit(origin.Index, ref maskLow, ref maskHigh);

                    // Include positive side
                    for (int step = 1; step <= posCount; step++)
                    {
                        int px = origin.X + dx * step;
                        int py = origin.Y + dy * step;
                        SetBit(py * BoardModel.Size + px, ref maskLow, ref maskHigh);
                    }

                    // Include negative side
                    for (int step = 1; step <= negCount; step++)
                    {
                        int nx = origin.X - dx * step;
                        int ny = origin.Y - dy * step;
                        SetBit(ny * BoardModel.Size + nx, ref maskLow, ref maskHigh);
                    }
                }
            }

            if (runCount == 0)
            {
                return false;
            }

            // Collect distinct cells from bitmask
            int totalUniqueCells = CountBits(maskLow) + CountBits(maskHigh);
            GridPos[] cells = new GridPos[totalUniqueCells];
            int writeIdx = 0;

            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                if (IsBitSet(i, maskLow, maskHigh))
                {
                    cells[writeIdx++] = GridPos.FromIndex(i);
                }
            }

            group = new ClearGroup(cells, longestRun, runCount, targetColor);
            return true;
        }

        private static int CountDirection(BoardModel board, int startX, int startY, int dx, int dy, BallColor targetColor)
        {
            int count = 0;
            int cx = startX + dx;
            int cy = startY + dy;

            while (cx >= 0 && cx < BoardModel.Size && cy >= 0 && cy < BoardModel.Size)
            {
                int idx = cy * BoardModel.Size + cx;
                if (board.ColorAt(idx) != targetColor)
                {
                    break;
                }
                count++;
                cx += dx;
                cy += dy;
            }

            return count;
        }

        private static void SetBit(int index, ref ulong low, ref ulong high)
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

        private static bool IsBitSet(int index, ulong low, ulong high)
        {
            if (index < 64)
            {
                return (low & (1UL << index)) != 0UL;
            }
            return (high & (1UL << (index - 64))) != 0UL;
        }

        private static int CountBits(ulong v)
        {
            v -= (v >> 1) & 0x5555555555555555UL;
            v = (v & 0x3333333333333333UL) + ((v >> 2) & 0x3333333333333333UL);
            v = (v + (v >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
            return (int)((v * 0x0101010101010101UL) >> 56);
        }
    }
}

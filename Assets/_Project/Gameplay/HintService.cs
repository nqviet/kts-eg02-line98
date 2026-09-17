using System;
using System.Collections.Generic;
using Line98.Core;

namespace Line98.Gameplay
{
    public enum HintTier : byte
    {
        None,
        Safety,
        Setup,
        Clear
    }

    public readonly struct HintSuggestion
    {
        public readonly GridPos From;
        public readonly GridPos To;
        public readonly HintTier Tier;
        public readonly int ExpectedScore;
        public readonly int PotentialGain;

        public HintSuggestion(
            GridPos from,
            GridPos to,
            HintTier tier,
            int expectedScore,
            int potentialGain)
        {
            From = from;
            To = to;
            Tier = tier;
            ExpectedScore = expectedScore;
            PotentialGain = potentialGain;
        }
    }

    /// <summary>
    /// Deterministically ranks every legal move without mutating the authoritative board.
    /// Shared scratch buffers make this service allocation-free after static initialization;
    /// like the rest of the gameplay session, it must only be called on the main thread.
    /// </summary>
    public static class HintService
    {
        private const int WindowLength = LineDetector.MinimumLineLength;
        private const int WindowCount = 140;
        private const int MaxWindowsPerCell = 20;

        private static readonly int[] s_AxisDx = { 1, 0, 1, 1 };
        private static readonly int[] s_AxisDy = { 0, 1, 1, -1 };
        private static readonly BoardModel s_TestBoard = new BoardModel();
        private static readonly int[] s_FloodQueue = new int[BoardModel.CellCount];
        private static readonly ulong[] s_ReachableLow = new ulong[BoardModel.CellCount];
        private static readonly ulong[] s_ReachableHigh = new ulong[BoardModel.CellCount];
        private static readonly int[,] s_WindowCells = new int[WindowCount, WindowLength];
        private static readonly int[,] s_CellWindowIndices = new int[BoardModel.CellCount, MaxWindowsPerCell];
        private static readonly byte[] s_CellWindowCounts = new byte[BoardModel.CellCount];
        private static readonly int[] s_WindowVisitStamps = new int[WindowCount];
        private static readonly GridPos[] s_ScoreProbePositions = { default };

        private static int s_WindowVisitStamp;

        static HintService()
        {
            BuildPotentialWindows();
        }

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
            HintSuggestion suggestion = FindBestMove(board, previewQueue, scoreRules, pathOut);
            bestFrom = suggestion.From;
            bestTo = suggestion.To;
            return suggestion.Tier != HintTier.None;
        }

        public static HintSuggestion FindBestMove(
            BoardModel board,
            PreviewQueue previewQueue,
            ScoreRules scoreRules,
            List<GridPos> pathOut = null)
        {
            pathOut?.Clear();

            if (board == null || board.IsEmptyBoard || !MoveResolver.CheckAnyLegalMove(board))
            {
                return default;
            }

            BuildReachableMasks(board);

            HintSuggestion suggestion = FindBestClear(board, in scoreRules);
            if (suggestion.Tier == HintTier.None)
            {
                suggestion = FindBestNonClear(board, previewQueue);
            }

            if (suggestion.Tier != HintTier.None && pathOut != null &&
                !Pathfinder.TryFindPath(board, suggestion.From, suggestion.To, pathOut))
            {
                pathOut.Clear();
                return default;
            }

            return suggestion;
        }

        private static HintSuggestion FindBestClear(BoardModel board, in ScoreRules scoreRules)
        {
            bool found = false;
            int bestScore = -1;
            int bestFreedCells = -1;
            GridPos bestFrom = default;
            GridPos bestTo = default;

            for (int fromIndex = 0; fromIndex < BoardModel.CellCount; fromIndex++)
            {
                if (board.IsEmpty(fromIndex))
                {
                    continue;
                }

                BallColor movingColor = board.ColorAt(fromIndex);
                ulong reachableLow = s_ReachableLow[fromIndex];
                ulong reachableHigh = s_ReachableHigh[fromIndex];

                for (int toIndex = 0; toIndex < BoardModel.CellCount; toIndex++)
                {
                    if (!IsBitSet(toIndex, reachableLow, reachableHigh))
                    {
                        continue;
                    }

                    s_TestBoard.CopyFrom(board);
                    s_TestBoard.Clear(fromIndex);
                    s_TestBoard.Set(toIndex, movingColor);

                    GridPos to = GridPos.FromIndex(toIndex);
                    if (!TryEvaluateClear(s_TestBoard, to, in scoreRules, out int score, out int freedCells))
                    {
                        continue;
                    }

                    if (!found || score > bestScore ||
                        (score == bestScore && freedCells > bestFreedCells))
                    {
                        found = true;
                        bestScore = score;
                        bestFreedCells = freedCells;
                        bestFrom = GridPos.FromIndex(fromIndex);
                        bestTo = to;
                    }
                }
            }

            return found
                ? new HintSuggestion(bestFrom, bestTo, HintTier.Clear, bestScore, 0)
                : default;
        }

        private static HintSuggestion FindBestNonClear(BoardModel board, PreviewQueue previewQueue)
        {
            int basePotential = CalculatePotential(board, previewQueue);

            bool foundSetup = false;
            int bestSetupWindowOccupancy = -1;
            int bestSetupGain = 0;
            int bestSetupMobility = -1;
            int bestSetupFreeArea = -1;
            GridPos bestSetupFrom = default;
            GridPos bestSetupTo = default;

            bool foundSafety = false;
            int bestSafetyMobility = -1;
            int bestSafetyFreeArea = -1;
            GridPos bestSafetyFrom = default;
            GridPos bestSafetyTo = default;

            for (int fromIndex = 0; fromIndex < BoardModel.CellCount; fromIndex++)
            {
                if (board.IsEmpty(fromIndex))
                {
                    continue;
                }

                BallColor movingColor = board.ColorAt(fromIndex);
                ulong reachableLow = s_ReachableLow[fromIndex];
                ulong reachableHigh = s_ReachableHigh[fromIndex];

                for (int toIndex = 0; toIndex < BoardModel.CellCount; toIndex++)
                {
                    if (!IsBitSet(toIndex, reachableLow, reachableHigh))
                    {
                        continue;
                    }

                    int potentialGain = 0;
                    if (DestinationSharesWindowWithColor(board, fromIndex, toIndex, movingColor))
                    {
                        int potentialAfter = basePotential + CalculatePotentialDelta(
                            board,
                            previewQueue,
                            fromIndex,
                            toIndex,
                            movingColor);
                        potentialGain = potentialAfter - basePotential;
                    }

                    int mobility = CalculateMobility(board, fromIndex, toIndex);
                    int freeArea = CalculateLargestEmptyRegion(board, fromIndex, toIndex);

                    if (potentialGain > 0)
                    {
                        int windowOccupancy = CalculateBestDestinationWindowOccupancy(
                            board,
                            fromIndex,
                            toIndex,
                            movingColor);

                        // A direct 4-of-5 setup is closer to a clear than several distributed
                        // two-ball windows. Potential then ranks moves at the same readiness.
                        if (!foundSetup || windowOccupancy > bestSetupWindowOccupancy ||
                            (windowOccupancy == bestSetupWindowOccupancy && potentialGain > bestSetupGain) ||
                            (windowOccupancy == bestSetupWindowOccupancy && potentialGain == bestSetupGain &&
                             mobility > bestSetupMobility) ||
                            (windowOccupancy == bestSetupWindowOccupancy && potentialGain == bestSetupGain &&
                             mobility == bestSetupMobility &&
                             freeArea > bestSetupFreeArea))
                        {
                            foundSetup = true;
                            bestSetupWindowOccupancy = windowOccupancy;
                            bestSetupGain = potentialGain;
                            bestSetupMobility = mobility;
                            bestSetupFreeArea = freeArea;
                            bestSetupFrom = GridPos.FromIndex(fromIndex);
                            bestSetupTo = GridPos.FromIndex(toIndex);
                        }
                    }
                    else if (!foundSafety || mobility > bestSafetyMobility ||
                             (mobility == bestSafetyMobility && freeArea > bestSafetyFreeArea))
                    {
                        foundSafety = true;
                        bestSafetyMobility = mobility;
                        bestSafetyFreeArea = freeArea;
                        bestSafetyFrom = GridPos.FromIndex(fromIndex);
                        bestSafetyTo = GridPos.FromIndex(toIndex);
                    }
                }
            }

            if (foundSetup)
            {
                return new HintSuggestion(
                    bestSetupFrom,
                    bestSetupTo,
                    HintTier.Setup,
                    0,
                    bestSetupGain);
            }

            return foundSafety
                ? new HintSuggestion(bestSafetyFrom, bestSafetyTo, HintTier.Safety, 0, 0)
                : default;
        }

        private static void BuildReachableMasks(BoardModel board)
        {
            Array.Clear(s_ReachableLow, 0, s_ReachableLow.Length);
            Array.Clear(s_ReachableHigh, 0, s_ReachableHigh.Length);

            for (int sourceIndex = 0; sourceIndex < BoardModel.CellCount; sourceIndex++)
            {
                if (board.IsEmpty(sourceIndex))
                {
                    continue;
                }

                ulong visitedLow = 0UL;
                ulong visitedHigh = 0UL;
                ulong reachableLow = 0UL;
                ulong reachableHigh = 0UL;
                int head = 0;
                int tail = 0;

                s_FloodQueue[tail++] = sourceIndex;
                SetBit(sourceIndex, ref visitedLow, ref visitedHigh);

                while (head < tail)
                {
                    int current = s_FloodQueue[head++];
                    int x = current % BoardModel.Size;
                    int y = current / BoardModel.Size;

                    TryFloodNeighbor(current - 1, x > 0, board, ref visitedLow, ref visitedHigh,
                        ref reachableLow, ref reachableHigh, ref tail);
                    TryFloodNeighbor(current + 1, x < BoardModel.Size - 1, board, ref visitedLow, ref visitedHigh,
                        ref reachableLow, ref reachableHigh, ref tail);
                    TryFloodNeighbor(current - BoardModel.Size, y > 0, board, ref visitedLow, ref visitedHigh,
                        ref reachableLow, ref reachableHigh, ref tail);
                    TryFloodNeighbor(current + BoardModel.Size, y < BoardModel.Size - 1, board, ref visitedLow, ref visitedHigh,
                        ref reachableLow, ref reachableHigh, ref tail);
                }

                s_ReachableLow[sourceIndex] = reachableLow;
                s_ReachableHigh[sourceIndex] = reachableHigh;
            }
        }

        private static void TryFloodNeighbor(
            int index,
            bool inBounds,
            BoardModel board,
            ref ulong visitedLow,
            ref ulong visitedHigh,
            ref ulong reachableLow,
            ref ulong reachableHigh,
            ref int tail)
        {
            if (!inBounds || IsBitSet(index, visitedLow, visitedHigh) || !board.IsEmpty(index))
            {
                return;
            }

            SetBit(index, ref visitedLow, ref visitedHigh);
            SetBit(index, ref reachableLow, ref reachableHigh);
            s_FloodQueue[tail++] = index;
        }

        private static bool TryEvaluateClear(
            BoardModel board,
            GridPos origin,
            in ScoreRules scoreRules,
            out int score,
            out int freedCells)
        {
            BallColor color = board.ColorAt(origin);
            int runCount = 0;
            int longestRun = 0;
            ulong clearedLow = 0UL;
            ulong clearedHigh = 0UL;

            for (int axis = 0; axis < s_AxisDx.Length; axis++)
            {
                int dx = s_AxisDx[axis];
                int dy = s_AxisDy[axis];
                int positive = CountDirection(board, origin, dx, dy, color);
                int negative = CountDirection(board, origin, -dx, -dy, color);
                int length = 1 + positive + negative;

                if (length < LineDetector.MinimumLineLength)
                {
                    continue;
                }

                runCount++;
                if (length > longestRun)
                {
                    longestRun = length;
                }

                for (int step = -negative; step <= positive; step++)
                {
                    int x = origin.X + dx * step;
                    int y = origin.Y + dy * step;
                    SetBit(y * BoardModel.Size + x, ref clearedLow, ref clearedHigh);
                }
            }

            if (runCount == 0)
            {
                score = 0;
                freedCells = 0;
                return false;
            }

            // ScoreEvaluator only needs a non-empty group plus longest-run/run-count metadata.
            // Building that metadata here avoids LineDetector's result-array allocation per candidate.
            var scoreGroup = new ClearGroup(s_ScoreProbePositions, longestRun, runCount, color);
            score = ScoreEvaluator.Evaluate(in scoreGroup, in scoreRules);
            freedCells = CountBits(clearedLow) + CountBits(clearedHigh);
            return true;
        }

        private static int CountDirection(
            BoardModel board,
            GridPos origin,
            int dx,
            int dy,
            BallColor color)
        {
            int count = 0;
            int x = origin.X + dx;
            int y = origin.Y + dy;

            while (x >= 0 && x < BoardModel.Size && y >= 0 && y < BoardModel.Size &&
                   board.ColorAt(y * BoardModel.Size + x) == color)
            {
                count++;
                x += dx;
                y += dy;
            }

            return count;
        }

        private static int CalculatePotential(BoardModel board, PreviewQueue previewQueue)
        {
            int potential = 0;
            for (int windowIndex = 0; windowIndex < WindowCount; windowIndex++)
            {
                potential += CalculateWindowPotential(board, previewQueue, windowIndex, -1, -1, BallColor.None);
            }
            return potential;
        }

        private static int CalculatePotentialDelta(
            BoardModel board,
            PreviewQueue previewQueue,
            int fromIndex,
            int toIndex,
            BallColor movingColor)
        {
            int stamp = NextWindowVisitStamp();
            int delta = 0;

            for (int i = 0; i < s_CellWindowCounts[fromIndex]; i++)
            {
                int windowIndex = s_CellWindowIndices[fromIndex, i];
                s_WindowVisitStamps[windowIndex] = stamp;
                delta += CalculateWindowPotential(
                    board, previewQueue, windowIndex, fromIndex, toIndex, movingColor) -
                    CalculateWindowPotential(board, previewQueue, windowIndex, -1, -1, BallColor.None);
            }

            for (int i = 0; i < s_CellWindowCounts[toIndex]; i++)
            {
                int windowIndex = s_CellWindowIndices[toIndex, i];
                if (s_WindowVisitStamps[windowIndex] == stamp)
                {
                    continue;
                }

                s_WindowVisitStamps[windowIndex] = stamp;
                delta += CalculateWindowPotential(
                    board, previewQueue, windowIndex, fromIndex, toIndex, movingColor) -
                    CalculateWindowPotential(board, previewQueue, windowIndex, -1, -1, BallColor.None);
            }

            return delta;
        }

        private static int CalculateWindowPotential(
            BoardModel board,
            PreviewQueue previewQueue,
            int windowIndex,
            int fromIndex,
            int toIndex,
            BallColor movingColor)
        {
            BallColor windowColor = BallColor.None;
            int colorCount = 0;

            for (int i = 0; i < WindowLength; i++)
            {
                int cellIndex = s_WindowCells[windowIndex, i];
                BallColor color;

                if (cellIndex == fromIndex)
                {
                    color = BallColor.None;
                }
                else if (cellIndex == toIndex)
                {
                    color = movingColor;
                }
                else
                {
                    color = board.ColorAt(cellIndex);
                }

                if (color == BallColor.None)
                {
                    continue;
                }

                if (windowColor != BallColor.None && windowColor != color)
                {
                    return 0;
                }

                windowColor = color;
                colorCount++;
            }

            if (colorCount == 0)
            {
                return 0;
            }

            int weight = IsPreviewColor(previewQueue, windowColor) ? 2 : 1;
            return weight * colorCount * colorCount;
        }

        private static bool DestinationSharesWindowWithColor(
            BoardModel board,
            int fromIndex,
            int toIndex,
            BallColor movingColor)
        {
            for (int i = 0; i < s_CellWindowCounts[toIndex]; i++)
            {
                int windowIndex = s_CellWindowIndices[toIndex, i];
                for (int cell = 0; cell < WindowLength; cell++)
                {
                    int cellIndex = s_WindowCells[windowIndex, cell];
                    if (cellIndex != fromIndex && board.ColorAt(cellIndex) == movingColor)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int CalculateBestDestinationWindowOccupancy(
            BoardModel board,
            int fromIndex,
            int toIndex,
            BallColor movingColor)
        {
            int bestOccupancy = 0;

            for (int i = 0; i < s_CellWindowCounts[toIndex]; i++)
            {
                int windowIndex = s_CellWindowIndices[toIndex, i];
                int occupancy = 0;
                bool blocked = false;

                for (int cell = 0; cell < WindowLength; cell++)
                {
                    int cellIndex = s_WindowCells[windowIndex, cell];
                    BallColor color;

                    if (cellIndex == fromIndex)
                    {
                        color = BallColor.None;
                    }
                    else if (cellIndex == toIndex)
                    {
                        color = movingColor;
                    }
                    else
                    {
                        color = board.ColorAt(cellIndex);
                    }

                    if (color != BallColor.None && color != movingColor)
                    {
                        blocked = true;
                        break;
                    }

                    if (color == movingColor)
                    {
                        occupancy++;
                    }
                }

                if (!blocked && occupancy > bestOccupancy)
                {
                    bestOccupancy = occupancy;
                }
            }

            return bestOccupancy;
        }

        private static bool IsPreviewColor(PreviewQueue previewQueue, BallColor color)
        {
            if (previewQueue == null || color == BallColor.None)
            {
                return false;
            }

            for (int i = 0; i < previewQueue.Count; i++)
            {
                if (previewQueue[i] == color)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CalculateMobility(BoardModel board, int fromIndex, int toIndex)
        {
            int mobility = 0;

            for (int index = 0; index < BoardModel.CellCount; index++)
            {
                if (IsEmptyAfterMove(board, index, fromIndex, toIndex))
                {
                    continue;
                }

                int x = index % BoardModel.Size;
                int y = index / BoardModel.Size;
                if (x > 0 && IsEmptyAfterMove(board, index - 1, fromIndex, toIndex)) mobility++;
                if (x < BoardModel.Size - 1 && IsEmptyAfterMove(board, index + 1, fromIndex, toIndex)) mobility++;
                if (y > 0 && IsEmptyAfterMove(board, index - BoardModel.Size, fromIndex, toIndex)) mobility++;
                if (y < BoardModel.Size - 1 && IsEmptyAfterMove(board, index + BoardModel.Size, fromIndex, toIndex)) mobility++;
            }

            return mobility;
        }

        private static int CalculateLargestEmptyRegion(BoardModel board, int fromIndex, int toIndex)
        {
            ulong visitedLow = 0UL;
            ulong visitedHigh = 0UL;
            int largest = 0;

            for (int startIndex = 0; startIndex < BoardModel.CellCount; startIndex++)
            {
                if (!IsEmptyAfterMove(board, startIndex, fromIndex, toIndex) ||
                    IsBitSet(startIndex, visitedLow, visitedHigh))
                {
                    continue;
                }

                int head = 0;
                int tail = 0;
                int regionSize = 0;
                s_FloodQueue[tail++] = startIndex;
                SetBit(startIndex, ref visitedLow, ref visitedHigh);

                while (head < tail)
                {
                    int current = s_FloodQueue[head++];
                    regionSize++;
                    int x = current % BoardModel.Size;
                    int y = current / BoardModel.Size;

                    TryRegionNeighbor(current - 1, x > 0, board, fromIndex, toIndex,
                        ref visitedLow, ref visitedHigh, ref tail);
                    TryRegionNeighbor(current + 1, x < BoardModel.Size - 1, board, fromIndex, toIndex,
                        ref visitedLow, ref visitedHigh, ref tail);
                    TryRegionNeighbor(current - BoardModel.Size, y > 0, board, fromIndex, toIndex,
                        ref visitedLow, ref visitedHigh, ref tail);
                    TryRegionNeighbor(current + BoardModel.Size, y < BoardModel.Size - 1, board, fromIndex, toIndex,
                        ref visitedLow, ref visitedHigh, ref tail);
                }

                if (regionSize > largest)
                {
                    largest = regionSize;
                }
            }

            return largest;
        }

        private static void TryRegionNeighbor(
            int index,
            bool inBounds,
            BoardModel board,
            int fromIndex,
            int toIndex,
            ref ulong visitedLow,
            ref ulong visitedHigh,
            ref int tail)
        {
            if (!inBounds || IsBitSet(index, visitedLow, visitedHigh) ||
                !IsEmptyAfterMove(board, index, fromIndex, toIndex))
            {
                return;
            }

            SetBit(index, ref visitedLow, ref visitedHigh);
            s_FloodQueue[tail++] = index;
        }

        private static bool IsEmptyAfterMove(BoardModel board, int index, int fromIndex, int toIndex)
        {
            if (index == fromIndex)
            {
                return true;
            }
            if (index == toIndex)
            {
                return false;
            }
            return board.IsEmpty(index);
        }

        private static void BuildPotentialWindows()
        {
            int windowIndex = 0;

            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x <= BoardModel.Size - WindowLength; x++)
                {
                    AddWindow(ref windowIndex, x, y, 1, 0);
                }
            }

            for (int x = 0; x < BoardModel.Size; x++)
            {
                for (int y = 0; y <= BoardModel.Size - WindowLength; y++)
                {
                    AddWindow(ref windowIndex, x, y, 0, 1);
                }
            }

            for (int y = 0; y <= BoardModel.Size - WindowLength; y++)
            {
                for (int x = 0; x <= BoardModel.Size - WindowLength; x++)
                {
                    AddWindow(ref windowIndex, x, y, 1, 1);
                }
            }

            for (int y = WindowLength - 1; y < BoardModel.Size; y++)
            {
                for (int x = 0; x <= BoardModel.Size - WindowLength; x++)
                {
                    AddWindow(ref windowIndex, x, y, 1, -1);
                }
            }
        }

        private static void AddWindow(ref int windowIndex, int startX, int startY, int dx, int dy)
        {
            for (int offset = 0; offset < WindowLength; offset++)
            {
                int x = startX + dx * offset;
                int y = startY + dy * offset;
                int cellIndex = y * BoardModel.Size + x;
                s_WindowCells[windowIndex, offset] = cellIndex;

                int cellWindowCount = s_CellWindowCounts[cellIndex];
                s_CellWindowIndices[cellIndex, cellWindowCount] = windowIndex;
                s_CellWindowCounts[cellIndex] = (byte)(cellWindowCount + 1);
            }

            windowIndex++;
        }

        private static int NextWindowVisitStamp()
        {
            if (s_WindowVisitStamp == int.MaxValue)
            {
                Array.Clear(s_WindowVisitStamps, 0, s_WindowVisitStamps.Length);
                s_WindowVisitStamp = 0;
            }

            return ++s_WindowVisitStamp;
        }

        private static bool IsBitSet(int index, ulong low, ulong high)
        {
            return index < 64
                ? (low & (1UL << index)) != 0UL
                : (high & (1UL << (index - 64))) != 0UL;
        }

        private static void SetBit(int index, ref ulong low, ref ulong high)
        {
            if (index < 64)
            {
                low |= 1UL << index;
            }
            else
            {
                high |= 1UL << (index - 64);
            }
        }

        private static int CountBits(ulong value)
        {
            int count = 0;
            while (value != 0UL)
            {
                value &= value - 1UL;
                count++;
            }
            return count;
        }
    }
}

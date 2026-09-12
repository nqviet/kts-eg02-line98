using System;
using System.Collections.Generic;
using UnityEngine;

namespace Line98.Model
{
    /// <summary>
    /// Pure game domain model for Line 98: manages the grid state, BFS pathfinding,
    /// 5-in-a-row line matching algorithms, turn snapshots, and score calculations.
    /// Completely decoupled from UI views and rendering.
    /// </summary>
    public sealed class Line98Board
    {
        public const int Empty = -1;

        private readonly int m_Size;
        private readonly int[,] m_Grid;
        private readonly Stack<Line98Snapshot> m_History = new Stack<Line98Snapshot>();

        private int[] m_NextColors = { 0, 1, 2 };
        private int m_Score = 1234;
        private int m_BestScore = 2356;
        private int m_Moves;

        public event Action OnBoardChanged;
        public event Action<int, int> OnScoreChanged;
        public event Action<int> OnMovesChanged;
        public event Action<int[]> OnNextColorsChanged;

        public Line98Board(int size = 9)
        {
            m_Size = size;
            m_Grid = new int[size, size];
        }

        public int Size => m_Size;
        public int Score => m_Score;
        public int BestScore => m_BestScore;
        public int Moves => m_Moves;
        public int[] NextColors => (int[])m_NextColors.Clone();
        public int HistoryCount => m_History.Count;

        public int GetCell(int x, int y)
        {
            if (!IsInside(new Vector2Int(x, y)))
            {
                return Empty;
            }

            return m_Grid[x, y];
        }

        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.x < m_Size && position.y >= 0 && position.y < m_Size;
        }

        public bool IsEmpty(int x, int y)
        {
            return GetCell(x, y) == Empty;
        }

        public List<Vector2Int> GetEmptyCells()
        {
            List<Vector2Int> emptyCells = new List<Vector2Int>();
            for (int y = 0; y < m_Size; y++)
            {
                for (int x = 0; x < m_Size; x++)
                {
                    if (m_Grid[x, y] == Empty)
                    {
                        emptyCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return emptyCells;
        }

        public void LoadReferenceBoard()
        {
            int[,] arrangement =
            {
                { 0, 1, Empty, 3, 4, Empty, 6, 0, 2 },
                { 3, 4, 6, 5, Empty, 2, 1, 3, Empty },
                { 2, Empty, 0, 1, 6, 4, Empty, 5, Empty },
                { 4, 3, 2, Empty, 0, 3, 1, 6, Empty },
                { 5, 0, Empty, 2, 4, Empty, 3, 0, 1 },
                { 6, 1, 3, 0, 5, 2, Empty, 4, Empty },
                { 0, Empty, 4, 3, 6, 1, 2, 3, Empty },
                { 1, 2, 6, Empty, 5, 0, 4, Empty, 6 },
                { 3, 4, 5, 0, Empty, 3, 1, 2, Empty },
            };

            for (int y = 0; y < m_Size; y++)
            {
                for (int x = 0; x < m_Size; x++)
                {
                    m_Grid[x, y] = arrangement[y, x];
                }
            }

            m_NextColors = new[] { 0, 1, 2 };
            m_History.Clear();
            m_Score = 1234;
            m_Moves = 0;

            NotifyStateChanged();
        }

        public void StartNewGame(System.Random random, int totalBallColors, int initialBallsCount = 31)
        {
            for (int y = 0; y < m_Size; y++)
            {
                for (int x = 0; x < m_Size; x++)
                {
                    m_Grid[x, y] = Empty;
                }
            }

            for (int ball = 0; ball < initialBallsCount; ball++)
            {
                List<Vector2Int> emptyCells = GetEmptyCells();
                if (emptyCells.Count == 0)
                {
                    break;
                }

                Vector2Int location = emptyCells[random.Next(emptyCells.Count)];
                m_Grid[location.x, location.y] = random.Next(totalBallColors);
            }

            m_NextColors = CreateNextColors(random, totalBallColors);
            m_History.Clear();
            m_Score = 0;
            m_Moves = 0;

            NotifyStateChanged();
        }

        public bool HasPath(Vector2Int from, Vector2Int to)
        {
            if (!IsInside(from) || !IsInside(to))
            {
                return false;
            }

            bool[,] visited = new bool[m_Size, m_Size];
            Queue<Vector2Int> pending = new Queue<Vector2Int>();
            pending.Enqueue(from);
            visited[from.x, from.y] = true;

            Vector2Int[] steps =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right,
            };

            while (pending.Count > 0)
            {
                Vector2Int current = pending.Dequeue();
                if (current == to)
                {
                    return true;
                }

                foreach (Vector2Int step in steps)
                {
                    Vector2Int next = current + step;
                    if (!IsInside(next) || visited[next.x, next.y])
                    {
                        continue;
                    }

                    if (next != to && m_Grid[next.x, next.y] != Empty)
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    pending.Enqueue(next);
                }
            }

            return false;
        }

        public bool TryMove(Vector2Int from, Vector2Int to, System.Random random, int totalBallColors, out int clearedCount, out bool spawnedBalls, out bool isBoardFull)
        {
            clearedCount = 0;
            spawnedBalls = false;
            isBoardFull = false;

            if (!IsInside(to) || m_Grid[to.x, to.y] != Empty)
            {
                return false;
            }

            if (!HasPath(from, to))
            {
                return false;
            }

            m_History.Push(CreateSnapshot());

            int color = m_Grid[from.x, from.y];
            m_Grid[from.x, from.y] = Empty;
            m_Grid[to.x, to.y] = color;
            m_Moves++;

            clearedCount = ClearLinesAt(to);
            if (clearedCount > 0)
            {
                m_Score += clearedCount * 10;
            }
            else
            {
                SpawnNextBalls(random, totalBallColors);
                spawnedBalls = true;
            }

            if (m_Score > m_BestScore)
            {
                m_BestScore = m_Score;
            }

            isBoardFull = GetEmptyCells().Count == 0;

            NotifyStateChanged();
            return true;
        }

        public bool Undo()
        {
            if (m_History.Count == 0)
            {
                return false;
            }

            RestoreSnapshot(m_History.Pop());
            NotifyStateChanged();
            return true;
        }

        public bool FindNearestBall(out Vector2Int position)
        {
            for (int y = 0; y < m_Size; y++)
            {
                for (int x = 0; x < m_Size; x++)
                {
                    if (m_Grid[x, y] != Empty)
                    {
                        position = new Vector2Int(x, y);
                        return true;
                    }
                }
            }

            position = new Vector2Int(-1, -1);
            return false;
        }

        private int ClearLinesAt(Vector2Int origin)
        {
            int color = m_Grid[origin.x, origin.y];
            HashSet<Vector2Int> cellsToClear = new HashSet<Vector2Int>();
            Vector2Int[] directions =
            {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
                new Vector2Int(1, -1),
            };

            foreach (Vector2Int direction in directions)
            {
                List<Vector2Int> line = new List<Vector2Int> { origin };
                CollectLine(line, origin, direction, color);
                CollectLine(line, origin, -direction, color);
                if (line.Count >= 5)
                {
                    foreach (Vector2Int position in line)
                    {
                        cellsToClear.Add(position);
                    }
                }
            }

            foreach (Vector2Int position in cellsToClear)
            {
                m_Grid[position.x, position.y] = Empty;
            }

            return cellsToClear.Count;
        }

        private void CollectLine(List<Vector2Int> line, Vector2Int origin, Vector2Int direction, int color)
        {
            Vector2Int position = origin + direction;
            while (IsInside(position) && m_Grid[position.x, position.y] == color)
            {
                line.Add(position);
                position += direction;
            }
        }

        private void SpawnNextBalls(System.Random random, int totalBallColors)
        {
            foreach (int color in m_NextColors)
            {
                List<Vector2Int> emptyCells = GetEmptyCells();
                if (emptyCells.Count == 0)
                {
                    return;
                }

                Vector2Int location = emptyCells[random.Next(emptyCells.Count)];
                m_Grid[location.x, location.y] = color;
            }

            m_NextColors = CreateNextColors(random, totalBallColors);
        }

        private int[] CreateNextColors(System.Random random, int totalBallColors)
        {
            return new[]
            {
                random.Next(totalBallColors),
                random.Next(totalBallColors),
                random.Next(totalBallColors),
            };
        }

        private Line98Snapshot CreateSnapshot()
        {
            Line98Snapshot snapshot = new Line98Snapshot(m_Size)
            {
                Score = m_Score,
                Moves = m_Moves,
                NextColors = (int[])m_NextColors.Clone(),
            };

            for (int y = 0; y < m_Size; y++)
            {
                for (int x = 0; x < m_Size; x++)
                {
                    snapshot.Board[x, y] = m_Grid[x, y];
                }
            }

            return snapshot;
        }

        private void RestoreSnapshot(Line98Snapshot snapshot)
        {
            for (int y = 0; y < m_Size; y++)
            {
                for (int x = 0; x < m_Size; x++)
                {
                    m_Grid[x, y] = snapshot.Board[x, y];
                }
            }

            m_Score = snapshot.Score;
            m_Moves = snapshot.Moves;
            m_NextColors = snapshot.NextColors;
        }

        private void NotifyStateChanged()
        {
            OnBoardChanged?.Invoke();
            OnScoreChanged?.Invoke(m_Score, m_BestScore);
            OnMovesChanged?.Invoke(m_Moves);
            OnNextColorsChanged?.Invoke(m_NextColors);
        }
    }
}

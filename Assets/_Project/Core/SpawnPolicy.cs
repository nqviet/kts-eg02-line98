using System;
using System.Collections.Generic;

namespace Line98.Core
{
    public readonly struct SpawnItem
    {
        public readonly GridPos Position;
        public readonly BallColor Color;

        public SpawnItem(GridPos position, BallColor color)
        {
            Position = position;
            Color = color;
        }
    }

    public readonly struct SpawnBatch
    {
        private static readonly SpawnItem[] s_Empty = Array.Empty<SpawnItem>();

        public readonly SpawnItem[] Items;
        public int Count => Items != null ? Items.Length : 0;
        public bool IsEmpty => Count == 0;

        public SpawnBatch(SpawnItem[] items)
        {
            Items = items ?? s_Empty;
        }

        public static SpawnBatch Empty => new SpawnBatch(s_Empty);
    }

    /// <summary>
    /// Preview queue containing the upcoming balls (default 3).
    /// </summary>
    [Serializable]
    public sealed class PreviewQueue
    {
        public const int DefaultCapacity = 3;

        private readonly BallColor[] m_Queue;

        public int Capacity => m_Queue.Length;

        public PreviewQueue(int capacity = DefaultCapacity)
        {
            m_Queue = new BallColor[capacity];
        }

        public BallColor this[int index]
        {
            get => m_Queue[index];
            set => m_Queue[index] = value;
        }

        public BallColor[] ToArray()
        {
            BallColor[] copy = new BallColor[m_Queue.Length];
            Array.Copy(m_Queue, copy, m_Queue.Length);
            return copy;
        }

        public void CopyFrom(BallColor[] colors)
        {
            if (colors == null) return;
            int count = Math.Min(m_Queue.Length, colors.Length);
            Array.Copy(colors, m_Queue, count);
        }

        public void Populate(ref XorShift128 rng, int activeColorCount)
        {
            int safeColors = Math.Clamp(activeColorCount, 2, 7);
            for (int i = 0; i < m_Queue.Length; i++)
            {
                m_Queue[i] = (BallColor)(rng.Range(0, safeColors) + 1);
            }
        }
    }

    public readonly struct SpawnRules
    {
        public readonly int InitialActiveColors;
        public readonly int LinesForSixColors;
        public readonly int LinesForSevenColors;
        public readonly int SpawnCount;

        public SpawnRules(
            int initialActiveColors = 5,
            int linesForSixColors = 10,
            int linesForSevenColors = 25,
            int spawnCount = 3)
        {
            InitialActiveColors = initialActiveColors;
            LinesForSixColors = linesForSixColors;
            LinesForSevenColors = linesForSevenColors;
            SpawnCount = spawnCount;
        }

        public static SpawnRules Default => new SpawnRules(5, 10, 25, 3);

        public int GetActiveColorCount(int totalLinesCleared)
        {
            int req7 = LinesForSevenColors > 0 ? LinesForSevenColors : 25;
            int req6 = LinesForSixColors > 0 ? LinesForSixColors : 10;
            int init = InitialActiveColors > 0 ? InitialActiveColors : 5;

            if (totalLinesCleared >= req7)
            {
                return 7;
            }
            if (totalLinesCleared >= req6)
            {
                return 6;
            }
            return init;
        }
    }
}

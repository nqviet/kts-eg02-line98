using System;

namespace Line98.Core
{
    /// <summary>
    /// Pure C# domain model for the 9x9 Color Lines board.
    /// Completely isolated from transforms, views, and UnityEngine.
    /// Uses 81-byte storage plus 2-ulong bitmask for instant occupancy queries.
    /// </summary>
    public sealed class BoardModel
    {
        public const int Size = 9;
        public const int CellCount = 81;

        private readonly byte[] m_Cells;
        private ulong m_OccupancyMaskLow;  // Bits 0..63
        private ulong m_OccupancyMaskHigh; // Bits 64..80
        private int m_OccupiedCount;

        public BoardModel()
        {
            m_Cells = new byte[CellCount];
            Reset();
        }

        public int EmptyCount => CellCount - m_OccupiedCount;
        public int OccupiedCount => m_OccupiedCount;
        public bool IsFull => m_OccupiedCount == CellCount;
        public bool IsEmptyBoard => m_OccupiedCount == 0;

        public void Reset()
        {
            Array.Clear(m_Cells, 0, CellCount);
            m_OccupancyMaskLow = 0UL;
            m_OccupancyMaskHigh = 0UL;
            m_OccupiedCount = 0;
        }

        public BallColor ColorAt(GridPos pos)
        {
            int index = pos.Index;
            return (BallColor)m_Cells[index];
        }

        public BallColor ColorAt(int index)
        {
            return (BallColor)m_Cells[index];
        }

        public bool IsEmpty(GridPos pos)
        {
            int index = pos.Index;
            if (index < 64)
            {
                return (m_OccupancyMaskLow & (1UL << index)) == 0UL;
            }
            return (m_OccupancyMaskHigh & (1UL << (index - 64))) == 0UL;
        }

        public bool IsEmpty(int index)
        {
            if (index < 64)
            {
                return (m_OccupancyMaskLow & (1UL << index)) == 0UL;
            }
            return (m_OccupancyMaskHigh & (1UL << (index - 64))) == 0UL;
        }

        public void Set(GridPos pos, BallColor color)
        {
            int index = pos.Index;
            Set(index, color);
        }

        public void Set(int index, BallColor color)
        {
            if (color == BallColor.None)
            {
                Clear(index);
                return;
            }

            bool wasEmpty = IsEmpty(index);
            m_Cells[index] = (byte)color;

            if (index < 64)
            {
                m_OccupancyMaskLow |= (1UL << index);
            }
            else
            {
                m_OccupancyMaskHigh |= (1UL << (index - 64));
            }

            if (wasEmpty)
            {
                m_OccupiedCount++;
            }
        }

        public void Clear(GridPos pos)
        {
            Clear(pos.Index);
        }

        public void Clear(int index)
        {
            if (IsEmpty(index))
            {
                return;
            }

            m_Cells[index] = (byte)BallColor.None;

            if (index < 64)
            {
                m_OccupancyMaskLow &= ~(1UL << index);
            }
            else
            {
                m_OccupancyMaskHigh &= ~(1UL << (index - 64));
            }

            m_OccupiedCount--;
        }

        public void CopyFrom(BoardModel source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Array.Copy(source.m_Cells, m_Cells, CellCount);
            m_OccupancyMaskLow = source.m_OccupancyMaskLow;
            m_OccupancyMaskHigh = source.m_OccupancyMaskHigh;
            m_OccupiedCount = source.m_OccupiedCount;
        }

        public byte[] ExportCells()
        {
            byte[] copy = new byte[CellCount];
            Array.Copy(m_Cells, copy, CellCount);
            return copy;
        }

        public void ImportCells(byte[] cells)
        {
            if (cells == null || cells.Length != CellCount)
            {
                throw new ArgumentException($"Cell array must have length {CellCount}", nameof(cells));
            }

            Reset();
            for (int i = 0; i < CellCount; i++)
            {
                if (cells[i] != 0)
                {
                    Set(i, (BallColor)cells[i]);
                }
            }
        }

        public (ulong Low, ulong High) GetOccupancyMask() => (m_OccupancyMaskLow, m_OccupancyMaskHigh);
    }
}

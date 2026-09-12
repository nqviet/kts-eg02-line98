using System;

namespace Line98.Core
{
    /// <summary>
    /// Immutable 2D grid position on the 9x9 board.
    /// Hot-path index representation is Index = Y * 9 + X (0..80).
    /// </summary>
    public readonly struct GridPos : IEquatable<GridPos>
    {
        public const int BoardSize = 9;
        public const int CellCount = 81;

        public readonly byte X;
        public readonly byte Y;

        public int Index => Y * BoardSize + X;

        public GridPos(byte x, byte y)
        {
            X = x;
            Y = y;
        }

        public GridPos(int x, int y)
        {
            X = (byte)x;
            Y = (byte)y;
        }

        public static GridPos FromIndex(int index)
        {
            if (index < 0 || index >= CellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index must be within 0..{CellCount - 1}");
            }
            return new GridPos((byte)(index % BoardSize), (byte)(index / BoardSize));
        }

        public bool IsValid => X < BoardSize && Y < BoardSize;

        public bool Equals(GridPos other) => X == other.X && Y == other.Y;

        public override bool Equals(object obj) => obj is GridPos other && Equals(other);

        public override int GetHashCode() => Index;

        public static bool operator ==(GridPos left, GridPos right) => left.Equals(right);

        public static bool operator !=(GridPos left, GridPos right) => !left.Equals(right);

        public override string ToString() => $"({X},{Y})";
    }
}

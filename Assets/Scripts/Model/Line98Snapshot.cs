namespace Line98.Model
{
    /// <summary>
    /// Represents a point-in-time snapshot of the game state for undo functionality.
    /// </summary>
    public sealed class Line98Snapshot
    {
        private readonly int[,] m_Board;
        private int[] m_NextColors;
        private int m_Score;
        private int m_Moves;

        public Line98Snapshot(int size)
        {
            m_Board = new int[size, size];
        }

        public int[,] Board => m_Board;

        public int[] NextColors
        {
            get => m_NextColors;
            set => m_NextColors = value;
        }

        public int Score
        {
            get => m_Score;
            set => m_Score = value;
        }

        public int Moves
        {
            get => m_Moves;
            set => m_Moves = value;
        }
    }
}

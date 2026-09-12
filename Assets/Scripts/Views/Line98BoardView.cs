using System;
using Line98.Design;
using Line98.Model;
using UnityEngine;

namespace Line98.Views
{
    /// <summary>
    /// Presentation component managing the grid of cell views and selection pulse animation.
    /// </summary>
    public sealed class Line98BoardView : MonoBehaviour
    {
        private Line98ThemeData m_Theme;
        private Line98CellView[,] m_Cells;
        private Vector2Int m_Selected = new Vector2Int(-1, -1);
        private int m_Size;

        public event Action<int, int> OnCellPressed;

        public Vector2Int SelectedCell => m_Selected;

        public void Initialize(int size, Line98CellView[,] cells, Line98ThemeData theme)
        {
            m_Size = size;
            m_Cells = cells;
            m_Theme = theme;
            m_Selected = new Vector2Int(-1, -1);
        }

        public void SetSelected(Vector2Int position)
        {
            m_Selected = position;
        }

        public void ClearSelection()
        {
            m_Selected = new Vector2Int(-1, -1);
        }

        public void Refresh(Line98Board board)
        {
            if (m_Cells == null || board == null)
            {
                return;
            }

            for (int y = 0; y < m_Size; y++)
            {
                for (int x = 0; x < m_Size; x++)
                {
                    Line98CellView cell = m_Cells[x, y];
                    if (cell != null)
                    {
                        int ballColor = board.GetCell(x, y);
                        bool isSelected = m_Selected.x == x && m_Selected.y == y;
                        cell.Render(ballColor, isSelected, m_Theme);
                    }
                }
            }
        }

        public void OnCellClicked(int x, int y)
        {
            OnCellPressed?.Invoke(x, y);
        }

        private void Update()
        {
            UpdateSelectionPulse();
        }

        private void UpdateSelectionPulse()
        {
            if (m_Selected.x < 0 || m_Cells == null || m_Theme == null)
            {
                return;
            }

            if (m_Selected.x >= m_Size || m_Selected.y >= m_Size)
            {
                return;
            }

            Line98CellView selectedCell = m_Cells[m_Selected.x, m_Selected.y];
            if (selectedCell != null)
            {
                float speed = m_Theme.PulseSpeed;
                float amplitude = m_Theme.PulseAmplitude;
                float scale = 1.02f + Mathf.Sin(Time.unscaledTime * speed) * amplitude;
                selectedCell.SetBallScale(scale);
            }
        }
    }
}

#if DEBUG || UNITY_EDITOR
using System;
using Line98.Core;
using Line98.Gameplay;
using UnityEngine;

namespace Line98.App
{
    /// <summary>
    /// Interactive IMGUI debug interface for Milestone 1.
    /// Provides immediate playable verification of all core mechanics:
    /// selection, BFS path traversal, line clears, preview queue, score, and game over.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        private GameSession m_Session;
        private GridPos? m_HintFrom;
        private GridPos? m_HintTo;
        private string m_StatusMessage = "Ready";

        public void Initialize(GameSession session)
        {
            m_Session = session;
            if (m_Session != null)
            {
                m_Session.OnMoveCommitted += OnMoveCommitted;
                m_Session.OnGameOver += OnGameOver;
            }
        }

        private void OnDestroy()
        {
            if (m_Session != null)
            {
                m_Session.OnMoveCommitted -= OnMoveCommitted;
                m_Session.OnGameOver -= OnGameOver;
            }
        }

        private void OnMoveCommitted(MoveResult result)
        {
            m_HintFrom = null;
            m_HintTo = null;

            if (result.Outcome == MoveOutcome.Cleared)
            {
                m_StatusMessage = $"Cleared {result.Cleared.Count} balls (+{result.ScoreDelta} pts)!";
            }
            else if (result.Outcome == MoveOutcome.Moved)
            {
                m_StatusMessage = $"Moved. Spawned {result.Spawned.Count} balls.";
            }
            else if (result.Outcome == MoveOutcome.GameOver)
            {
                m_StatusMessage = "GAME OVER! No legal moves or board full.";
            }
        }

        private void OnGameOver()
        {
            m_StatusMessage = "GAME OVER!";
        }

        private void OnGUI()
        {
            if (m_Session == null)
            {
                return;
            }

            GUI.skin.label.fontSize = 14;
            GUI.skin.button.fontSize = 13;

            GUILayout.BeginArea(new Rect(20, 20, 420, Screen.height - 40), GUI.skin.box);
            GUILayout.Label("<b>LINE 98: Color Lines — M1 Debug HUD</b>", GUI.skin.label);
            GUILayout.Space(5);

            // Session Stats
            GUILayout.Label($"Mode: {m_Session.Mode.DisplayName} | Phase: {m_Session.Phase}");
            GUILayout.Label($"Score: <b>{m_Session.Score}</b> | Moves: {m_Session.MoveCount} | Cleared: {m_Session.LinesCleared}");
            GUILayout.Label($"Free Undos: {m_Session.FreeUndosRemaining} | Empty Cells: {m_Session.Board.EmptyCount}/81");

            // Preview Queue Display
            GUILayout.BeginHorizontal();
            GUILayout.Label("Next 3: ", GUILayout.Width(60));
            for (int i = 0; i < m_Session.PreviewQueue.Capacity; i++)
            {
                BallColor color = m_Session.PreviewQueue[i];
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = GetBallUiColor(color);
                GUILayout.Button(color.ToString(), GUILayout.Width(70), GUILayout.Height(24));
                GUI.backgroundColor = oldColor;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label($"Status: {m_StatusMessage}");
            GUILayout.Space(8);

            // 9x9 Board Grid
            float cellSize = 38f;
            for (int y = 0; y < BoardModel.Size; y++)
            {
                GUILayout.BeginHorizontal();
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos pos = new GridPos(x, y);
                    BallColor color = m_Session.Board.ColorAt(pos);

                    Color prevBg = GUI.backgroundColor;

                    bool isSelected = m_Session.HasSelection && m_Session.SelectedPos == pos;
                    bool isHint = (m_HintFrom.HasValue && m_HintFrom.Value == pos) ||
                                  (m_HintTo.HasValue && m_HintTo.Value == pos);

                    if (isSelected)
                    {
                        GUI.backgroundColor = Color.cyan;
                    }
                    else if (isHint)
                    {
                        GUI.backgroundColor = Color.yellow;
                    }
                    else if (color != BallColor.None)
                    {
                        GUI.backgroundColor = GetBallUiColor(color);
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                    }

                    string cellLabel = color != BallColor.None ? GetColorShortCode(color) : "";
                    if (GUILayout.Button(cellLabel, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        OnCellClicked(pos);
                    }

                    GUI.backgroundColor = prevBg;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // Control Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("New Game (Classic)"))
            {
                m_Session.SetMode(new ClassicMode());
                m_Session.StartNewGame();
                m_StatusMessage = "Started new Classic game.";
            }

            if (GUILayout.Button("Daily Challenge"))
            {
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                m_Session.SetMode(new DailyChallengeMode(today));
                m_Session.StartNewGame();
                m_StatusMessage = $"Started Daily Challenge ({today}).";
            }

            if (GUILayout.Button("Zen Mode"))
            {
                m_Session.SetMode(new ZenMode());
                m_Session.StartNewGame();
                m_StatusMessage = "Started Zen Mode.";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.enabled = m_Session.FreeUndosRemaining > 0 && m_Session.Mode.UndoAllowed && m_Session.Phase == GamePhase.Playing;
            if (GUILayout.Button("Undo Move"))
            {
                if (m_Session.TryUndo())
                {
                    m_StatusMessage = "Move undone.";
                }
            }
            GUI.enabled = true;

            if (GUILayout.Button("Request Hint"))
            {
                if (m_Session.RequestHint(out GridPos from, out GridPos to))
                {
                    m_HintFrom = from;
                    m_HintTo = to;
                    m_StatusMessage = $"Hint: Move {m_Session.Board.ColorAt(from)} from {from} to {to}";
                }
                else
                {
                    m_StatusMessage = "No helpful move found.";
                }
            }

            if (m_Session.Phase == GamePhase.GameOver)
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Revive (+3 Free Cells)"))
                {
                    m_Session.Revive(3);
                    m_StatusMessage = "Revived! Cleared 3 balls.";
                }
                GUI.backgroundColor = Color.white;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void OnCellClicked(GridPos pos)
        {
            if (m_Session.Phase != GamePhase.Playing)
            {
                return;
            }

            if (!m_Session.Board.IsEmpty(pos))
            {
                // Select or switch selection
                m_Session.TrySelect(pos);
            }
            else
            {
                // If a ball is selected, move it to this empty cell
                if (m_Session.HasSelection)
                {
                    if (!m_Session.ExecuteMove(pos))
                    {
                        m_StatusMessage = $"Cannot move to {pos}: Path blocked or unreachable!";
                    }
                }
            }
        }

        private static Color GetBallUiColor(BallColor color)
        {
            switch (color)
            {
                case BallColor.Red: return new Color(0.95f, 0.2f, 0.2f);
                case BallColor.Orange: return new Color(1f, 0.55f, 0.1f);
                case BallColor.Yellow: return new Color(0.98f, 0.88f, 0.2f);
                case BallColor.Green: return new Color(0.2f, 0.8f, 0.25f);
                case BallColor.Cyan: return new Color(0.15f, 0.8f, 0.95f);
                case BallColor.Purple: return new Color(0.65f, 0.25f, 0.9f);
                case BallColor.Pink: return new Color(0.95f, 0.4f, 0.75f);
                default: return Color.gray;
            }
        }

        private static string GetColorShortCode(BallColor color)
        {
            switch (color)
            {
                case BallColor.Red: return "R";
                case BallColor.Orange: return "O";
                case BallColor.Yellow: return "Y";
                case BallColor.Green: return "G";
                case BallColor.Cyan: return "C";
                case BallColor.Purple: return "P";
                case BallColor.Pink: return "Pk";
                default: return "";
            }
        }
    }
}
#endif

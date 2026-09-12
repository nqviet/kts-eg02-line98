using System;
using Line98.Design;
using Line98.Model;
using Line98.Views;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Main game coordinator behaviour for Line 98.
/// Coordinates the visual Design Asset (Line98ThemeData), game domain logic (Line98Board),
/// and UI Presentation views.
/// </summary>
public sealed class Line98Game : MonoBehaviour
{
    [Header("Design Asset")]
    [Tooltip("Visual design token asset specifying palettes, metrics, typography, and assets.")]
    [SerializeField] private Line98ThemeData m_Theme;

    private Line98Board m_Board;
    private Line98HeaderView m_HeaderView;
    private Line98ScoreView m_ScoreView;
    private Line98BoardView m_BoardView;
    private Line98FooterView m_FooterView;
    private Line98ToastView m_ToastView;
    private Line98ModalView m_ModalView;
    private Line98SafeAreaFitter m_SafeAreaFitter;

    private readonly System.Random m_Random = new System.Random(98);
    private bool m_SoundEnabled = true;

    public Line98ThemeData Theme => m_Theme;
    public Line98Board Board => m_Board;

    private void Awake()
    {
        EnsureTheme();
        ConfigureCamera();
        EnsureEventSystem();
        InitializeUserInterface();
        InitializeGameModel();
    }

    private void EnsureTheme()
    {
        if (m_Theme != null)
        {
            return;
        }

        m_Theme = Resources.Load<Line98ThemeData>("Line98Theme");
        if (m_Theme == null)
        {
            m_Theme = Line98ThemeData.CreateDefault();
        }
    }

    private void ConfigureCamera()
    {
        Camera sceneCamera = Camera.main;
        if (sceneCamera == null)
        {
            return;
        }

        sceneCamera.clearFlags = CameraClearFlags.SolidColor;
        sceneCamera.backgroundColor = m_Theme.CameraBackgroundColor;
        sceneCamera.orthographic = true;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject systemsObject = new GameObject("Systems", typeof(RectTransform));
        systemsObject.transform.SetParent(transform, false);
        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
        eventSystemObject.transform.SetParent(systemsObject.transform, false);

        Type inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputModuleType != null && typeof(BaseInputModule).IsAssignableFrom(inputModuleType))
        {
            eventSystemObject.AddComponent(inputModuleType);
        }
        else
        {
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }

    private void InitializeUserInterface()
    {
        Line98UIViews views = Line98UIBuilder.Build(transform, m_Theme);
        m_HeaderView = views.HeaderView;
        m_ScoreView = views.ScoreView;
        m_BoardView = views.BoardView;
        m_FooterView = views.FooterView;
        m_ToastView = views.ToastView;
        m_ModalView = views.ModalView;
        m_SafeAreaFitter = views.SafeAreaFitter;

        // Bind presentation events to controller callbacks
        m_BoardView.OnCellPressed += OnCellPressed;
        m_HeaderView.OnSettingsClicked += OnSettingsClicked;
        m_HeaderView.OnStatsClicked += OnStatsClicked;
        m_FooterView.OnUndoClicked += OnUndoClicked;
        m_FooterView.OnNewGameClicked += OnNewGameClicked;
        m_FooterView.OnSelectNearestClicked += OnSelectNearestClicked;
        m_FooterView.OnDirectionClicked += OnDirectionClicked;
    }

    private void InitializeGameModel()
    {
        m_Board = new Line98Board(m_Theme.BoardSize);

        // Bind domain model events to presentation updates
        m_Board.OnBoardChanged += OnBoardChanged;
        m_Board.OnScoreChanged += OnScoreChanged;
        m_Board.OnNextColorsChanged += OnNextColorsChanged;

        m_Board.LoadReferenceBoard();
        RefreshPresentation();
    }

    public void OnCellPressed(int x, int y)
    {
        if (m_ModalView != null && m_ModalView.IsOpen)
        {
            return;
        }

        if (!m_Board.IsEmpty(x, y))
        {
            Vector2Int currentSelected = m_BoardView.SelectedCell;
            bool isDeselecting = currentSelected.x == x && currentSelected.y == y;
            m_BoardView.SetSelected(isDeselecting ? new Vector2Int(-1, -1) : new Vector2Int(x, y));
            m_BoardView.Refresh(m_Board);
            ShowToast(isDeselecting ? "Selection cleared" : "Ball selected");
            return;
        }

        if (m_BoardView.SelectedCell.x < 0)
        {
            ShowToast("Select a ball first");
            return;
        }

        TryMove(m_BoardView.SelectedCell, new Vector2Int(x, y));
    }

    private void TryMove(Vector2Int from, Vector2Int to)
    {
        if (!m_Board.IsInside(to) || !m_Board.IsEmpty(to.x, to.y))
        {
            ShowToast("That space is occupied");
            return;
        }

        if (!m_Board.HasPath(from, to))
        {
            ShowToast("No clear path there");
            return;
        }

        bool moved = m_Board.TryMove(
            from,
            to,
            m_Random,
            m_Theme.TotalBallColors,
            out int clearedCount,
            out bool spawnedBalls,
            out bool isBoardFull);

        if (moved)
        {
            m_BoardView.ClearSelection();

            if (clearedCount > 0)
            {
                ShowToast(clearedCount + " balls cleared");
            }
            else if (spawnedBalls)
            {
                ShowToast("Next balls added");
            }

            if (isBoardFull)
            {
                ShowToast("Board full - start a new game");
            }

            RefreshPresentation();
        }
    }

    public void OnDirectionClicked(Vector2Int direction)
    {
        Vector2Int selected = m_BoardView.SelectedCell;
        if (selected.x < 0)
        {
            OnSelectNearestClicked();
            return;
        }

        Vector2Int target = selected + direction;
        if (!m_Board.IsInside(target))
        {
            ShowToast("Board edge");
            return;
        }

        TryMove(selected, target);
    }

    public void OnSelectNearestClicked()
    {
        if (m_BoardView.SelectedCell.x >= 0)
        {
            m_BoardView.ClearSelection();
            m_BoardView.Refresh(m_Board);
            ShowToast("Selection cleared");
            return;
        }

        if (m_Board.FindNearestBall(out Vector2Int found))
        {
            m_BoardView.SetSelected(found);
            m_BoardView.Refresh(m_Board);
            ShowToast("Ball selected");
        }
    }

    public void OnUndoClicked()
    {
        if (!m_Board.Undo())
        {
            ShowToast("Nothing to undo");
            return;
        }

        m_BoardView.ClearSelection();
        RefreshPresentation();
        ShowToast("Move undone");
    }

    public void OnNewGameClicked()
    {
        m_Board.StartNewGame(m_Random, m_Theme.TotalBallColors);
        m_BoardView.ClearSelection();
        RefreshPresentation();
        ShowToast("New game started");
    }

    public void OnSettingsClicked()
    {
        m_ModalView.ShowSettings(m_SoundEnabled, OnToggleSound, OnCloseModal);
    }

    public void OnStatsClicked()
    {
        m_ModalView.ShowStats(m_Board.Score, m_Board.Moves, m_Board.BestScore, OnCloseModal);
    }

    public void OnToggleSound()
    {
        m_SoundEnabled = !m_SoundEnabled;
        m_ModalView.UpdateSoundLabel(m_SoundEnabled);
    }

    public void OnCloseModal()
    {
        m_ModalView.Close();
    }

    private void OnBoardChanged()
    {
        if (m_BoardView != null)
        {
            m_BoardView.Refresh(m_Board);
        }
    }

    private void OnScoreChanged(int score, int bestScore)
    {
        if (m_ScoreView != null)
        {
            m_ScoreView.UpdateScore(score, bestScore);
        }
    }

    private void OnNextColorsChanged(int[] nextColors)
    {
        if (m_ScoreView != null)
        {
            m_ScoreView.UpdateNextBalls(nextColors);
        }
    }

    private void RefreshPresentation()
    {
        if (m_BoardView != null)
        {
            m_BoardView.Refresh(m_Board);
        }

        if (m_ScoreView != null)
        {
            m_ScoreView.UpdateScore(m_Board.Score, m_Board.BestScore);
            m_ScoreView.UpdateNextBalls(m_Board.NextColors);
        }
    }

    private void ShowToast(string message)
    {
        if (m_ToastView != null)
        {
            m_ToastView.ShowToast(message);
        }
    }
}

using System;
using System.Collections.Generic;
using Line98.Core;
using Line98.Data;

namespace Line98.Gameplay
{
    /// <summary>
    /// Authoritative game session controller.
    /// Drives simulation, undo snapshots, and communicates with presentation via events.
    /// </summary>
    public sealed class GameSession
    {
        private readonly BoardModel m_Board;
        private readonly PreviewQueue m_PreviewQueue;
        private readonly MoveResolver m_Resolver;
        private readonly Stack<GameSnapshot> m_UndoStack;
        private readonly List<int> m_OccupiedIndicesCache;

        private XorShift128 m_Rng;
        private IGameModeStrategy m_Mode;
        private IMovePacer m_Pacer;

        private int m_Score;
        private int m_MoveCount;
        private int m_LinesCleared;
        private int m_FreeUndosRemaining;
        private GridPos m_SelectedPos;
        private bool m_HasSelection;
        private GamePhase m_Phase;

        // Public event emitters (prefix On per coding conventions)
        public event Action<GridPos> OnBallSelected;
        public event Action OnBallDeselected;
        public event Action<MovePlan> OnMovePlanned;
        public event Action<MoveResult> OnMoveCommitted;
        public event Action<int> OnScoreChanged;
        public event Action<GameSnapshot> OnStateRestored;
        public event Action OnGameOver;
        public event Action<GamePhase> OnPhaseChanged;

        public BoardModel Board => m_Board;
        public PreviewQueue PreviewQueue => m_PreviewQueue;
        public XorShift128 Rng => m_Rng;
        public int Score => m_Score;
        public int MoveCount => m_MoveCount;
        public int LinesCleared => m_LinesCleared;
        public int FreeUndosRemaining => m_FreeUndosRemaining;
        public bool HasSelection => m_HasSelection;
        public GridPos SelectedPos => m_SelectedPos;
        public GamePhase Phase => m_Phase;
        public IGameModeStrategy Mode => m_Mode;

        public IMovePacer Pacer
        {
            get => m_Pacer;
            set => m_Pacer = value ?? ImmediatePacer.Instance;
        }

        public GameSession(IGameModeStrategy mode = null, IMovePacer pacer = null)
        {
            m_Board = new BoardModel();
            m_PreviewQueue = new PreviewQueue(3);
            m_Resolver = new MoveResolver();
            m_UndoStack = new Stack<GameSnapshot>();
            m_OccupiedIndicesCache = new List<int>(BoardModel.CellCount);

            m_Mode = mode ?? new ClassicMode();
            m_Pacer = pacer ?? ImmediatePacer.Instance;
            m_Phase = GamePhase.Boot;
        }

        public void SetMode(IGameModeStrategy mode)
        {
            m_Mode = mode ?? new ClassicMode();
        }

        public void StartNewGame(uint? customSeed = null)
        {
            uint seed = customSeed ?? m_Mode.GenerateSeed();
            m_Rng = new XorShift128(seed);

            m_Board.Reset();
            m_Score = 0;
            m_MoveCount = 0;
            m_LinesCleared = 0;
            m_FreeUndosRemaining = 3;
            m_HasSelection = false;
            m_UndoStack.Clear();

            // Initial spawn of 3 balls on empty board
            SpawnRules spawnRules = m_Mode.GetSpawnRules(0);
            int activeColors = spawnRules.GetActiveColorCount(0);

            // Populate initial preview queue
            m_PreviewQueue.Populate(ref m_Rng, activeColors);

            // Place first batch of 3 balls directly
            for (int i = 0; i < 3; i++)
            {
                int randomCell = m_Rng.Range(0, BoardModel.CellCount);
                while (!m_Board.IsEmpty(randomCell))
                {
                    randomCell = m_Rng.Range(0, BoardModel.CellCount);
                }
                m_Board.Set(randomCell, m_PreviewQueue[i]);
            }

            // Repopulate preview queue for next move
            m_PreviewQueue.Populate(ref m_Rng, activeColors);

            SetPhase(GamePhase.Playing);
            OnScoreChanged?.Invoke(m_Score);
        }

        public bool TrySelect(GridPos pos)
        {
            if (m_Phase != GamePhase.Playing)
            {
                return false;
            }

            if (!pos.IsValid || m_Board.IsEmpty(pos))
            {
                return false;
            }

            // Tapping already selected ball deselects it
            if (m_HasSelection && m_SelectedPos == pos)
            {
                Deselect();
                return false;
            }

            m_SelectedPos = pos;
            m_HasSelection = true;
            OnBallSelected?.Invoke(pos);
            return true;
        }

        public void Deselect()
        {
            if (!m_HasSelection)
            {
                return;
            }

            m_HasSelection = false;
            OnBallDeselected?.Invoke();
        }

        public bool ExecuteMove(GridPos to)
        {
            if (m_Phase != GamePhase.Playing || !m_HasSelection)
            {
                return false;
            }

            if (!to.IsValid || !m_Board.IsEmpty(to))
            {
                return false;
            }

            GridPos from = m_SelectedPos;
            MoveRequest request = new MoveRequest(from, to);

            ScoreRules scoreRules = m_Mode.GetScoreRules();
            SpawnRules spawnRules = m_Mode.GetSpawnRules(m_LinesCleared);

            MovePlan plan = m_Resolver.Resolve(
                m_Board,
                m_PreviewQueue,
                in m_Rng,
                in request,
                in scoreRules,
                in spawnRules,
                m_LinesCleared);

            if (plan.Outcome == MoveOutcome.Invalid || plan.Outcome == MoveOutcome.NoPath)
            {
                return false;
            }

            // Capture snapshot BEFORE committing mutation for real undo per GDD §12
            if (m_Mode.UndoAllowed)
            {
                GameSnapshot snapshot = GameSnapshot.Capture(
                    m_Board,
                    m_PreviewQueue,
                    in m_Rng,
                    m_Score,
                    m_MoveCount,
                    m_LinesCleared,
                    from.Index);
                m_UndoStack.Push(snapshot);
            }

            Deselect();
            SetPhase(GamePhase.Resolving);
            OnMovePlanned?.Invoke(plan);

            // Delegate presentation pacing to IMovePacer
            m_Pacer.Play(plan, () => Commit(plan));
            return true;
        }

        public void Commit(MovePlan plan)
        {
            if (plan == null)
            {
                return;
            }

            // Apply ball movement
            BallColor movingColor = m_Board.ColorAt(plan.From);
            m_Board.Clear(plan.From);
            m_Board.Set(plan.To, movingColor);

            // Apply clears if any
            if (!plan.Cleared.IsEmpty)
            {
                for (int i = 0; i < plan.Cleared.Positions.Length; i++)
                {
                    m_Board.Clear(plan.Cleared.Positions[i]);
                }
                m_LinesCleared += plan.Cleared.RunCount;

                if (m_Mode.ScoringEnabled)
                {
                    m_Score += plan.ScoreDelta;
                    OnScoreChanged?.Invoke(m_Score);
                }
            }
            else
            {
                // Apply spawned batch
                if (!plan.Spawned.IsEmpty)
                {
                    for (int i = 0; i < plan.Spawned.Items.Length; i++)
                    {
                        SpawnItem item = plan.Spawned.Items[i];
                        m_Board.Set(item.Position, item.Color);
                    }
                }

                if (plan.NextPreviewQueue != null)
                {
                    m_PreviewQueue.CopyFrom(plan.NextPreviewQueue);
                }
            }

            m_Rng = plan.PostMoveRng;
            m_MoveCount++;

            MoveResult result = new MoveResult(
                plan.Outcome,
                plan.Cleared,
                plan.Spawned,
                plan.ScoreDelta);

            OnMoveCommitted?.Invoke(result);

            if (plan.IsGameOver)
            {
                SetPhase(GamePhase.GameOver);
                OnGameOver?.Invoke();
            }
            else
            {
                SetPhase(GamePhase.Playing);
            }
        }

        public bool TryUndo()
        {
            if (!m_Mode.UndoAllowed || m_UndoStack.Count == 0 || m_Phase != GamePhase.Playing)
            {
                return false;
            }

            if (m_FreeUndosRemaining <= 0)
            {
                return false;
            }

            GameSnapshot snapshot = m_UndoStack.Pop();
            snapshot.RestoreTo(
                m_Board,
                m_PreviewQueue,
                out m_Rng,
                out m_Score,
                out m_MoveCount,
                out m_LinesCleared,
                out int selectedIndex);

            m_FreeUndosRemaining--;
            m_HasSelection = false;

            OnScoreChanged?.Invoke(m_Score);
            OnStateRestored?.Invoke(snapshot);
            return true;
        }

        public bool RequestHint(out GridPos from, out GridPos to)
        {
            from = default;
            to = default;

            if (!m_Mode.HintsAllowed || m_Phase != GamePhase.Playing)
            {
                return false;
            }

            return HintService.TryFindBestMove(
                m_Board,
                m_PreviewQueue,
                m_Mode.GetScoreRules(),
                out from,
                out to);
        }

        public void Revive(int ballsToRemove = 3)
        {
            if (m_Phase != GamePhase.GameOver)
            {
                return;
            }

            m_OccupiedIndicesCache.Clear();
            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                if (!m_Board.IsEmpty(i))
                {
                    m_OccupiedIndicesCache.Add(i);
                }
            }

            int removeCount = Math.Min(ballsToRemove, m_OccupiedIndicesCache.Count);
            for (int i = 0; i < removeCount; i++)
            {
                int removeIdx = m_Rng.Range(0, m_OccupiedIndicesCache.Count);
                int cell = m_OccupiedIndicesCache[removeIdx];
                m_OccupiedIndicesCache.RemoveAt(removeIdx);
                m_Board.Clear(cell);
            }

            SetPhase(GamePhase.Playing);
        }

        private void SetPhase(GamePhase phase)
        {
            if (m_Phase != phase)
            {
                m_Phase = phase;
                OnPhaseChanged?.Invoke(phase);
            }
        }
    }
}

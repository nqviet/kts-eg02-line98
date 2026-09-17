using System;
using System.Collections.Generic;
using Line98.Core;
using Line98.Data;

namespace Line98.Gameplay
{
    public enum SessionReplaceReason : byte
    {
        NewGame,
        Undo,
        Restore
    }

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
        private readonly UndoService m_UndoService;

        private XorShift128 m_Rng;
        private IGameModeStrategy m_Mode;
        private IMovePacer m_Pacer;

        private int m_Score;
        private int m_MoveCount;
        private int m_LinesCleared;
        private int m_LongestLine;
        private int m_BestScore;
        private GridPos m_SelectedPos;
        private bool m_HasSelection;
        private GamePhase m_Phase;
        private readonly MovePlan[] m_PlanBuffer = new[] { new MovePlan(), new MovePlan() };
        private int m_PlanBufferIndex = 0;
        private MovePlan m_PendingPlan;
        private readonly Action m_CommitPendingAction;

        // Public event emitters (prefix On per coding conventions)
        public event Action<GridPos> OnBallSelected;
        public event Action OnBallDeselected;
        public event Action<MovePlan> OnMovePlanned;
        public event Action<MoveResult> OnMoveCommitted;
        public event Action<int> OnScoreChanged;
        public event Action<GameSnapshot> OnStateRestored;
        public event Action<SessionSummary> OnGameOver;
        public event Action<GamePhase> OnPhaseChanged;

        // Phase 3 events
        public event Action<MoveRejection> OnMoveRejected;
        public event Action<HintSuggestion> OnHintRequested;
        public event Action<ContinueResult> OnContinueApplied;
        public event Action<SessionState> OnSessionRestored;

        /// <summary>
        /// Raised when the authoritative session state is replaced wholesale.
        /// Presentation must discard in-flight work and re-sync from the model.
        /// </summary>
        public event Action<SessionReplaceReason> OnSessionReplaced;

        public BoardModel Board => m_Board;
        public PreviewQueue PreviewQueue => m_PreviewQueue;
        public XorShift128 Rng => m_Rng;
        public int Score => m_Score;
        public int MoveCount => m_MoveCount;
        public int LinesCleared => m_LinesCleared;
        public int LongestLine => m_LongestLine;
        public int BestScore => Math.Max(m_BestScore, m_Score);
        public int FreeUndosRemaining => m_UndoService.FreeUndosRemaining;
        public bool HasSelection => m_HasSelection;
        public GridPos SelectedPos => m_SelectedPos;
        public GamePhase Phase => m_Phase;
        public IGameModeStrategy Mode => m_Mode;
        public UndoService UndoService => m_UndoService;
        public bool HasSnapshotStack => m_UndoStack.Count > 0;

        public IMovePacer Pacer
        {
            get => m_Pacer;
            set => m_Pacer = value ?? ImmediatePacer.Instance;
        }

        public GameSession(IGameModeStrategy mode = null, IMovePacer pacer = null, UndoService undoService = null)
        {
            m_Board = new BoardModel();
            m_PreviewQueue = new PreviewQueue(3);
            m_Resolver = new MoveResolver();
            m_UndoStack = new Stack<GameSnapshot>();
            m_OccupiedIndicesCache = new List<int>(BoardModel.CellCount);
            m_UndoService = undoService ?? new UndoService();

            m_Mode = mode ?? new ClassicMode();
            m_Pacer = pacer ?? ImmediatePacer.Instance;
            m_Phase = GamePhase.Boot;
            m_CommitPendingAction = OnCommitPending;
        }

        public void SetMode(IGameModeStrategy mode)
        {
            m_Mode = mode ?? new ClassicMode();
        }

        public void SetBestScore(int bestScore)
        {
            if (bestScore > m_BestScore)
            {
                m_BestScore = bestScore;
            }
        }

        public SessionSummary GetSummary(int? bestScoreOverride = null, bool canContinue = true)
        {
            int best = bestScoreOverride ?? Math.Max(m_BestScore, m_Score);
            return new SessionSummary(
                m_Score,
                best,
                m_LongestLine,
                m_LinesCleared,
                m_MoveCount,
                canContinue);
        }

        public void StartNewGame(uint? customSeed = null)
        {
            uint seed = customSeed ?? m_Mode.GenerateSeed();
            m_Rng = new XorShift128(seed);

            m_Board.Reset();
            m_Score = 0;
            m_MoveCount = 0;
            m_LinesCleared = 0;
            m_LongestLine = 0;
            m_HasSelection = false;
            m_UndoStack.Clear();
            m_UndoService.ResetForNewRun();

            SpawnRules spawnRules = m_Mode.GetSpawnRules(0);
            InitialLayoutBuilder.Build(ref m_Rng, in spawnRules, m_Board, m_PreviewQueue, 3);

            m_PendingPlan = null;
            SetPhase(GamePhase.Playing);
            OnScoreChanged?.Invoke(m_Score);
            OnSessionReplaced?.Invoke(SessionReplaceReason.NewGame);
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

            GridPos from = m_SelectedPos;
            if (!to.IsValid || !m_Board.IsEmpty(to))
            {
                OnMoveRejected?.Invoke(new MoveRejection(from, to, MoveOutcome.Invalid));
                return false;
            }

            MoveRequest request = new MoveRequest(from, to);

            ScoreRules scoreRules = m_Mode.GetScoreRules();
            SpawnRules spawnRules = m_Mode.GetSpawnRules(m_LinesCleared);

            MovePlan plan = m_PlanBuffer[m_PlanBufferIndex];
            m_PlanBufferIndex = (m_PlanBufferIndex + 1) % m_PlanBuffer.Length;

            m_Resolver.Resolve(
                m_Board,
                m_PreviewQueue,
                in m_Rng,
                in request,
                in scoreRules,
                in spawnRules,
                m_LinesCleared,
                plan);

            if (plan.Outcome == MoveOutcome.Invalid || plan.Outcome == MoveOutcome.NoPath)
            {
                OnMoveRejected?.Invoke(new MoveRejection(from, to, plan.Outcome));
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
                    m_LongestLine,
                    from.Index);
                m_UndoStack.Push(snapshot);
            }

            Deselect();
            SetPhase(GamePhase.Resolving);
            OnMovePlanned?.Invoke(plan);

            // Delegate presentation pacing to IMovePacer without closure allocation
            m_PendingPlan = plan;
            m_Pacer.Play(plan, m_CommitPendingAction);
            return true;
        }

        private void OnCommitPending()
        {
            if (m_PendingPlan != null)
            {
                Commit(m_PendingPlan);
            }
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

                if (plan.Cleared.LongestRun > m_LongestLine)
                {
                    m_LongestLine = plan.Cleared.LongestRun;
                }

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
                    for (int i = 0; i < plan.Spawned.Count; i++)
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
                plan.ScoreDelta,
                plan.ComboMultiplier);

            OnMoveCommitted?.Invoke(result);

            if (plan.IsGameOver)
            {
                SetPhase(GamePhase.GameOver);
                SessionSummary summary = GetSummary();
                OnGameOver?.Invoke(summary);
            }
            else
            {
                SetPhase(GamePhase.Playing);
            }
        }

        internal bool TryUndoCore()
        {
            return TryUndoCore(out _);
        }

        internal bool TryUndoCore(out GameSnapshot snapshot)
        {
            snapshot = null;
            if (m_UndoStack.Count == 0)
            {
                return false;
            }

            snapshot = m_UndoStack.Pop();
            snapshot.RestoreTo(
                m_Board,
                m_PreviewQueue,
                out m_Rng,
                out m_Score,
                out m_MoveCount,
                out m_LinesCleared,
                out m_LongestLine,
                out int selectedIndex);

            m_HasSelection = false;

            OnScoreChanged?.Invoke(m_Score);
            OnStateRestored?.Invoke(snapshot);
            OnSessionReplaced?.Invoke(SessionReplaceReason.Undo);
            return true;
        }

        public bool TryUndo()
        {
            var ctx = new UndoAvailability(m_Phase, m_Mode.UndoAllowed, m_UndoStack.Count > 0, m_Mode.UnlimitedUndo);
            var result = m_UndoService.Request(ctx, () =>
            {
                if (TryUndoCore(out GameSnapshot snapshot))
                {
                    return snapshot;
                }
                return null;
            });
            return result.Success;
        }

        public bool RequestHint(out GridPos from, out GridPos to)
        {
            return RequestHint(out from, out to, null);
        }

        public bool RequestHint(out GridPos from, out GridPos to, List<GridPos> pathOut)
        {
            from = default;
            to = default;

            if (!m_Mode.HintsAllowed || m_Phase != GamePhase.Playing)
            {
                return false;
            }

            HintSuggestion suggestion = HintService.FindBestMove(
                m_Board,
                m_PreviewQueue,
                m_Mode.GetScoreRules(),
                pathOut);

            if (suggestion.Tier != HintTier.None)
            {
                from = suggestion.From;
                to = suggestion.To;
                OnHintRequested?.Invoke(suggestion);
                return true;
            }

            return false;
        }

        public void ApplyContinue(in ContinuePayload payload)
        {
            if (payload.IsEmpty || payload.FreedCells < 3)
            {
                return;
            }

            for (int i = 0; i < payload.Count; i++)
            {
                m_Board.Clear(payload.ClearedCells[i]);
            }

            SetPhase(GamePhase.Playing);
            OnContinueApplied?.Invoke(new ContinueResult(true, payload.Count, payload.FreedCells, payload.GrantedMoves, false));
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
            var cleared = new GridPos[removeCount];
            for (int i = 0; i < removeCount; i++)
            {
                cleared[i] = GridPos.FromIndex(m_OccupiedIndicesCache[i]);
            }

            ApplyContinue(new ContinuePayload(cleared, removeCount, removeCount, 1));
        }

        public SessionState CaptureState()
        {
            byte[] cells = m_Board.ExportCells();
            BallColor[] preview = new BallColor[m_PreviewQueue.Capacity];
            m_PreviewQueue.CopyTo(preview);

            int selectedIdx = m_HasSelection ? m_SelectedPos.Index : -1;
            int rewardedUsed = m_UndoService.MaxRewarded - m_UndoService.RewardedUndosRemaining;

            string dailySeedDate = null;
            int dailySeedVersion = 1;
            if (m_Mode is DailyChallengeMode dailyMode)
            {
                dailySeedDate = dailyMode.TargetDate;
                dailySeedVersion = dailyMode.SeedVersion;
            }

            return new SessionState(
                cells,
                preview,
                in m_Rng,
                m_Score,
                m_MoveCount,
                m_LinesCleared,
                m_LongestLine,
                m_UndoService.FreeUndosRemaining,
                rewardedUsed,
                selectedIdx,
                m_Phase,
                m_Mode.ModeId,
                dailySeedDate,
                dailySeedVersion);
        }

        public bool TryRestore(in SessionState state)
        {
            if (state.BoardCells == null || state.BoardCells.Length != BoardModel.CellCount)
            {
                return false;
            }
            if (state.Preview == null || state.Preview.Length != m_PreviewQueue.Capacity)
            {
                return false;
            }
            if (state.Phase == GamePhase.Boot)
            {
                return false;
            }

            m_Board.ImportCells(state.BoardCells);
            m_PreviewQueue.CopyFrom(state.Preview);
            m_Rng = state.Rng;
            m_Score = state.Score;
            m_MoveCount = state.MoveCount;
            m_LinesCleared = state.LinesCleared;
            m_LongestLine = state.LongestLine;

            m_UndoService.LoadState(state.FreeUndosRemaining, state.RewardedUndosUsed);
            m_UndoStack.Clear();

            if (state.SelectedIndex >= 0 && state.SelectedIndex < BoardModel.CellCount && !m_Board.IsEmpty(state.SelectedIndex))
            {
                m_SelectedPos = GridPos.FromIndex(state.SelectedIndex);
                m_HasSelection = true;
            }
            else
            {
                m_HasSelection = false;
            }

            SetPhase(state.Phase);
            OnScoreChanged?.Invoke(m_Score);
            OnSessionRestored?.Invoke(state);
            OnSessionReplaced?.Invoke(SessionReplaceReason.Restore);
            return true;
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

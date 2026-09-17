using System;
using System.Collections.Generic;
using Line98.Core;

namespace Line98.Gameplay
{
    public enum ContinueDenialReason : byte
    {
        None,
        NotGameOver,
        AlreadyUsedThisRun,
        NoRecoverableBoard,
        AdUnavailable,
        AdCancelled
    }

    public readonly struct ContinueOffer
    {
        public readonly bool Available;
        public readonly ContinueDenialReason Denial;
        public readonly bool RequiresReward;

        public ContinueOffer(bool available, ContinueDenialReason denial, bool requiresReward)
        {
            Available = available;
            Denial = denial;
            RequiresReward = requiresReward;
        }

        public static ContinueOffer Denied(ContinueDenialReason reason) => new ContinueOffer(false, reason, false);
    }

    public readonly struct ContinueResult
    {
        public readonly bool Applied;
        public readonly int RemovedBalls;
        public readonly int FreedCells;
        public readonly int GrantedMoves;
        public readonly bool StillGameOver;

        public ContinueResult(bool applied, int removedBalls, int freedCells, int grantedMoves, bool stillGameOver)
        {
            Applied = applied;
            RemovedBalls = removedBalls;
            FreedCells = freedCells;
            GrantedMoves = grantedMoves;
            StillGameOver = stillGameOver;
        }

        public static ContinueResult Failed => new ContinueResult(false, 0, 0, 0, true);
    }

    public sealed class ReviveService
    {
        private readonly ReviveRules m_Rules;
        private readonly IAdGate m_Gate;
        private int m_ContinuesUsedThisRun;

        public event Action<ContinueResult> OnContinueApplied;

        public int ContinuesUsedThisRun => m_ContinuesUsedThisRun;

        public ReviveService(ReviveRules? rules = null, IAdGate gate = null)
        {
            m_Rules = rules ?? ReviveRules.Default;
            m_Gate = gate ?? new AlwaysGrantAdGate();
            m_ContinuesUsedThisRun = 0;
        }

        public void ResetForNewRun()
        {
            m_ContinuesUsedThisRun = 0;
        }

        public void LoadState(int continuesUsedThisRun)
        {
            m_ContinuesUsedThisRun = Math.Max(0, continuesUsedThisRun);
        }

        public bool CanOffer(in SessionSummary summary, GamePhase phase, IGameModeStrategy mode)
        {
            if (phase != GamePhase.GameOver) return false;
            if (m_ContinuesUsedThisRun >= m_Rules.MaxContinuesPerRun) return false;
            if (mode != null && !mode.AdsEnabled && mode is ZenMode) return false;

            return true;
        }

        public void Offer(
            in SessionSummary summary,
            GamePhase phase,
            IGameModeStrategy mode,
            Action<ContinueOffer> onReady)
        {
            if (!CanOffer(in summary, phase, mode))
            {
                ContinueDenialReason reason = phase != GamePhase.GameOver
                    ? ContinueDenialReason.NotGameOver
                    : ContinueDenialReason.AlreadyUsedThisRun;
                onReady?.Invoke(ContinueOffer.Denied(reason));
                return;
            }

            bool requiresReward = true; // In Phase 3, gated by IAdGate
            bool available = m_Gate.IsRewardAvailable(AdRewardKind.Continue);

            if (!available)
            {
                onReady?.Invoke(ContinueOffer.Denied(ContinueDenialReason.AdUnavailable));
                return;
            }

            onReady?.Invoke(new ContinueOffer(true, ContinueDenialReason.None, requiresReward));
        }

        public ContinueResult Apply(GameSession session, IGameModeStrategy mode)
        {
            if (session == null || session.Board == null || session.Phase != GamePhase.GameOver)
            {
                return ContinueResult.Failed;
            }

            if (m_ContinuesUsedThisRun >= m_Rules.MaxContinuesPerRun)
            {
                return ContinueResult.Failed;
            }

            // Gated by IAdGate
            if (!m_Gate.IsRewardAvailable(AdRewardKind.Continue))
            {
                return ContinueResult.Failed;
            }

            bool granted = false;
            m_Gate.RequestReward(AdRewardKind.Continue, success => granted = success);
            if (!granted)
            {
                return ContinueResult.Failed;
            }

            // Deterministic lowest-value ranking without touching RNG
            BoardModel board = session.Board;
            var occupied = new List<CellRanking>(BoardModel.CellCount);

            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos pos = new GridPos(x, y);
                    BallColor color = board.ColorAt(pos);
                    if (color != BallColor.None)
                    {
                        int sameColorNeighbors = CountSameColorNeighbors(board, pos, color);
                        occupied.Add(new CellRanking(pos, sameColorNeighbors, pos.Index));
                    }
                }
            }

            if (occupied.Count < m_Rules.MinFreedCells)
            {
                return ContinueResult.Failed;
            }

            // Sort: fewest same-color neighbors, then lowest CellIndex
            occupied.Sort((a, b) =>
            {
                int c = a.SameColorNeighbors.CompareTo(b.SameColorNeighbors);
                if (c != 0) return c;
                return a.CellIndex.CompareTo(b.CellIndex);
            });

            int removeCount = Math.Min(m_Rules.RemoveCount, occupied.Count);
            if (removeCount < m_Rules.MinFreedCells)
            {
                return ContinueResult.Failed;
            }

            var clearedPositions = new GridPos[removeCount];
            for (int i = 0; i < removeCount; i++)
            {
                clearedPositions[i] = occupied[i].Pos;
            }

            var payload = new ContinuePayload(
                clearedPositions,
                removeCount,
                removeCount,
                m_Rules.RecoveryMoves);

            session.ApplyContinue(payload);
            m_ContinuesUsedThisRun++;

            bool stillGameOver = !MoveResolver.CheckAnyLegalMove(session.Board);
            var result = new ContinueResult(true, removeCount, removeCount, m_Rules.RecoveryMoves, stillGameOver);

            OnContinueApplied?.Invoke(result);
            return result;
        }

        private static int CountSameColorNeighbors(BoardModel board, GridPos pos, BallColor color)
        {
            int count = 0;
            int x = pos.X;
            int y = pos.Y;

            if (x > 0 && board.ColorAt(new GridPos(x - 1, y)) == color) count++;
            if (x < BoardModel.Size - 1 && board.ColorAt(new GridPos(x + 1, y)) == color) count++;
            if (y > 0 && board.ColorAt(new GridPos(x, y - 1)) == color) count++;
            if (y < BoardModel.Size - 1 && board.ColorAt(new GridPos(x, y + 1)) == color) count++;

            return count;
        }

        private readonly struct CellRanking
        {
            public readonly GridPos Pos;
            public readonly int SameColorNeighbors;
            public readonly int CellIndex;

            public CellRanking(GridPos pos, int sameColorNeighbors, int cellIndex)
            {
                Pos = pos;
                SameColorNeighbors = sameColorNeighbors;
                CellIndex = cellIndex;
            }
        }
    }
}

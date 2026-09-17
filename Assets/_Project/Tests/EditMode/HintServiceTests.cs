using System.Diagnostics;
using System.Collections.Generic;
using Line98.Core;
using Line98.Data;
using Line98.Gameplay;
using Line98.Presentation;
using Line98.Presentation.Animation;
using NUnit.Framework;
using UnityEngine;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class HintServiceTests
    {
        private BoardModel m_Board;
        private PreviewQueue m_Queue;
        private ScoreRules m_Rules;
        private List<GridPos> m_PathOut;

        [SetUp]
        public void SetUp()
        {
            m_Board = new BoardModel();
            m_Queue = new PreviewQueue();
            m_Rules = ScoreRules.Default;
            m_PathOut = new List<GridPos>(BoardModel.CellCount);
        }

        [Test]
        public void NonClearingBoard_ExtendsSameColorRun()
        {
            m_Board.Set(new GridPos(2, 4), BallColor.Red);
            m_Board.Set(new GridPos(3, 4), BallColor.Red);
            m_Board.Set(new GridPos(4, 4), BallColor.Red);
            m_Board.Set(new GridPos(0, 0), BallColor.Red);
            SetQueue(BallColor.Red);

            HintSuggestion suggestion = HintService.FindBestMove(m_Board, m_Queue, m_Rules, m_PathOut);

            Assert.AreEqual(HintTier.Setup, suggestion.Tier);
            Assert.Greater(suggestion.PotentialGain, 0);
            Assert.AreEqual(new GridPos(0, 0), suggestion.From);
            Assert.IsTrue(
                suggestion.To == new GridPos(1, 4) || suggestion.To == new GridPos(5, 4),
                $"Expected the loose Red ball to extend the run, got {suggestion.From}->{suggestion.To}.");
        }

        [Test]
        public void NonClearingBoard_DoesNotBreakFourRun()
        {
            for (int x = 2; x <= 5; x++)
            {
                m_Board.Set(new GridPos(x, 4), BallColor.Red);
            }

            m_Board.Set(new GridPos(2, 7), BallColor.Blue);
            m_Board.Set(new GridPos(3, 7), BallColor.Blue);
            m_Board.Set(new GridPos(4, 7), BallColor.Blue);
            m_Board.Set(new GridPos(0, 0), BallColor.Blue);
            SetQueue(BallColor.Blue);

            HintSuggestion suggestion = HintService.FindBestMove(m_Board, m_Queue, m_Rules);

            Assert.AreEqual(HintTier.Setup, suggestion.Tier);
            Assert.AreEqual(new GridPos(0, 0), suggestion.From,
                "The hint should build the Blue line instead of dismantling the Red four-run.");
        }

        [Test]
        public void ClearCase_Wins_AndReportsTier()
        {
            for (int y = 0; y < 4; y++)
            {
                m_Board.Set(new GridPos(0, y), BallColor.Red);
            }
            m_Board.Set(new GridPos(2, 4), BallColor.Red);

            HintSuggestion suggestion = HintService.FindBestMove(m_Board, m_Queue, m_Rules, m_PathOut);

            Assert.AreEqual(HintTier.Clear, suggestion.Tier);
            Assert.AreEqual(100, suggestion.ExpectedScore);
            Assert.AreEqual(new GridPos(2, 4), suggestion.From);
            Assert.AreEqual(new GridPos(0, 4), suggestion.To);
        }

        [Test]
        public void PrefersHigherScoringClear()
        {
            m_Board.Set(new GridPos(0, 0), BallColor.Red);
            m_Board.Set(new GridPos(1, 0), BallColor.Red);
            m_Board.Set(new GridPos(2, 0), BallColor.Red);
            m_Board.Set(new GridPos(3, 0), BallColor.Red);
            m_Board.Set(new GridPos(5, 0), BallColor.Red);
            m_Board.Set(new GridPos(8, 8), BallColor.Red);

            m_Board.Set(new GridPos(0, 2), BallColor.Blue);
            m_Board.Set(new GridPos(1, 2), BallColor.Blue);
            m_Board.Set(new GridPos(2, 2), BallColor.Blue);
            m_Board.Set(new GridPos(3, 2), BallColor.Blue);
            m_Board.Set(new GridPos(7, 8), BallColor.Blue);

            HintSuggestion suggestion = HintService.FindBestMove(m_Board, m_Queue, m_Rules);

            Assert.AreEqual(HintTier.Clear, suggestion.Tier);
            Assert.AreEqual(180, suggestion.ExpectedScore);
            Assert.AreEqual(new GridPos(8, 8), suggestion.From);
            Assert.AreEqual(new GridPos(4, 0), suggestion.To);
        }

        [Test]
        public void PreviewQueueChangesSuggestion()
        {
            m_Board.Set(new GridPos(2, 2), BallColor.Red);
            m_Board.Set(new GridPos(3, 2), BallColor.Red);
            m_Board.Set(new GridPos(4, 2), BallColor.Red);
            m_Board.Set(new GridPos(0, 0), BallColor.Red);

            m_Board.Set(new GridPos(2, 6), BallColor.Blue);
            m_Board.Set(new GridPos(3, 6), BallColor.Blue);
            m_Board.Set(new GridPos(4, 6), BallColor.Blue);
            m_Board.Set(new GridPos(8, 8), BallColor.Blue);

            SetQueue(BallColor.Red);
            HintSuggestion redSuggestion = HintService.FindBestMove(m_Board, m_Queue, m_Rules);

            SetQueue(BallColor.Blue);
            HintSuggestion blueSuggestion = HintService.FindBestMove(m_Board, m_Queue, m_Rules);

            Assert.AreEqual(HintTier.Setup, redSuggestion.Tier);
            Assert.AreEqual(HintTier.Setup, blueSuggestion.Tier);
            Assert.AreEqual(BallColor.Red, m_Board.ColorAt(redSuggestion.From));
            Assert.AreEqual(BallColor.Blue, m_Board.ColorAt(blueSuggestion.From));
            Assert.AreNotEqual(redSuggestion.From, blueSuggestion.From);
        }

        [Test]
        public void SeededFuzz_AlwaysLegal_OrReportsNoMove()
        {
            var random = new System.Random(0x5EED);
            var freshPath = new List<GridPos>(BoardModel.CellCount);

            for (int sample = 0; sample < 64; sample++)
            {
                m_Board.Reset();

                for (int index = 0; index < BoardModel.CellCount; index++)
                {
                    bool occupied = sample == 1 ||
                        (sample > 1 && random.Next(100) < 10 + sample);
                    if (occupied)
                    {
                        m_Board.Set(index, (BallColor)(random.Next(7) + 1));
                    }
                }

                for (int i = 0; i < m_Queue.Count; i++)
                {
                    m_Queue[i] = (BallColor)(random.Next(7) + 1);
                }

                bool hasLegalMove = MoveResolver.CheckAnyLegalMove(m_Board);
                HintSuggestion suggestion = HintService.FindBestMove(m_Board, m_Queue, m_Rules, m_PathOut);
                bool found = suggestion.Tier != HintTier.None;

                Assert.AreEqual(hasLegalMove, found, $"Mismatch on seeded board {sample}.");
                if (!found)
                {
                    Assert.AreEqual(0, m_PathOut.Count);
                    continue;
                }

                Assert.IsFalse(m_Board.IsEmpty(suggestion.From), $"Source was empty on board {sample}.");
                Assert.IsTrue(m_Board.IsEmpty(suggestion.To), $"Destination was occupied on board {sample}.");
                Assert.IsTrue(Pathfinder.TryFindPath(m_Board, suggestion.From, suggestion.To, freshPath));
                CollectionAssert.AreEqual(freshPath, m_PathOut, $"Path mismatch on board {sample}.");
            }
        }

        [Test]
        public void Hint_IsRepeatableAndRngFree()
        {
            m_Board.Set(new GridPos(2, 4), BallColor.Green);
            m_Board.Set(new GridPos(3, 4), BallColor.Green);
            m_Board.Set(new GridPos(4, 4), BallColor.Green);
            m_Board.Set(new GridPos(0, 0), BallColor.Green);
            SetQueue(BallColor.Green);

            HintSuggestion expected = HintService.FindBestMove(m_Board, m_Queue, m_Rules);

            for (int i = 0; i < 10; i++)
            {
                HintSuggestion actual = HintService.FindBestMove(m_Board, m_Queue, m_Rules);
                Assert.AreEqual(expected.From, actual.From);
                Assert.AreEqual(expected.To, actual.To);
                Assert.AreEqual(expected.Tier, actual.Tier);
                Assert.AreEqual(expected.ExpectedScore, actual.ExpectedScore);
                Assert.AreEqual(expected.PotentialGain, actual.PotentialGain);
            }
        }

        [Test]
        public void Hint_StaysWithinBudget()
        {
            for (int i = 0; i < 25; i++)
            {
                int index = i * 17 % BoardModel.CellCount;
                m_Board.Set(index, (BallColor)(i % 7 + 1));
            }
            SetQueue(BallColor.Cyan);

            for (int i = 0; i < 3; i++)
            {
                HintService.FindBestMove(m_Board, m_Queue, m_Rules);
            }

            var stopwatch = new Stopwatch();
            double worstMilliseconds = 0d;
            for (int i = 0; i < 5; i++)
            {
                stopwatch.Restart();
                HintService.FindBestMove(m_Board, m_Queue, m_Rules);
                stopwatch.Stop();
                worstMilliseconds = System.Math.Max(worstMilliseconds, stopwatch.Elapsed.TotalMilliseconds);
            }

            Assert.Less(worstMilliseconds, 15d,
                $"Hint evaluation took {worstMilliseconds:F2} ms on the 25-ball smoke board.");
        }

        [Test]
        public void Hint_AfterWarmup_AllocatesZeroBytes()
        {
            m_Board.Set(new GridPos(2, 4), BallColor.Red);
            m_Board.Set(new GridPos(3, 4), BallColor.Red);
            m_Board.Set(new GridPos(4, 4), BallColor.Red);
            m_Board.Set(new GridPos(0, 0), BallColor.Red);
            SetQueue(BallColor.Red);

            HintService.FindBestMove(m_Board, m_Queue, m_Rules);

            long beforeBytes = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 20; i++)
            {
                HintService.FindBestMove(m_Board, m_Queue, m_Rules);
            }
            long allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - beforeBytes;

            Assert.AreEqual(0, allocatedBytes,
                $"Hint evaluation allocated {allocatedBytes} bytes after warmup.");
        }

        [Test]
        public void HintPathReturned_OrderedSourceToDestination_MatchesPathfinder()
        {
            // Set up a board where moving (2,4) to (0,4) completes a 5-line of Red
            m_Board.Set(new GridPos(0, 0), BallColor.Red);
            m_Board.Set(new GridPos(0, 1), BallColor.Red);
            m_Board.Set(new GridPos(0, 2), BallColor.Red);
            m_Board.Set(new GridPos(0, 3), BallColor.Red);
            // 5th Red ball is at (2, 4)
            m_Board.Set(new GridPos(2, 4), BallColor.Red);

            bool found = HintService.TryFindBestMove(m_Board, m_Queue, m_Rules, out GridPos from, out GridPos to, m_PathOut);
            Assert.IsTrue(found, "Hint should find a legal move");
            Assert.AreEqual(new GridPos(2, 4), from);
            Assert.AreEqual(new GridPos(0, 4), to);

            // Path must be returned, start at 'from', end at 'to'
            Assert.IsNotNull(m_PathOut);
            Assert.IsTrue(m_PathOut.Count >= 2);
            Assert.AreEqual(from, m_PathOut[0]);
            Assert.AreEqual(to, m_PathOut[m_PathOut.Count - 1]);

            // Verify path matches a fresh Pathfinder query
            var freshPath = new List<GridPos>(BoardModel.CellCount);
            Assert.IsTrue(Pathfinder.TryFindPath(m_Board, from, to, freshPath));
            Assert.AreEqual(freshPath.Count, m_PathOut.Count);
            for (int i = 0; i < freshPath.Count; i++)
            {
                Assert.AreEqual(freshPath[i], m_PathOut[i]);
            }
        }

        [Test]
        public void Hint_NeverMutatesBoard()
        {
            m_Board.Set(new GridPos(2, 2), BallColor.Yellow);
            m_Board.Set(new GridPos(3, 3), BallColor.Blue);

            // Take board snapshot
            var colorsBefore = new BallColor[BoardModel.CellCount];
            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                colorsBefore[i] = m_Board.ColorAt(i);
            }

            bool found = HintService.TryFindBestMove(m_Board, m_Queue, m_Rules, out GridPos from, out GridPos to, m_PathOut);
            Assert.IsTrue(found);

            // GDD §13 requirement: "Never automatically execute" — board must remain completely unchanged
            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                Assert.AreEqual(colorsBefore[i], m_Board.ColorAt(i), $"Cell {i} was mutated by HintService!");
            }
        }

        [Test]
        public void GameSession_RequestHint_ReturnsPath()
        {
            var session = new GameSession();
            session.StartNewGame();

            var path = new List<GridPos>();
            bool requested = session.RequestHint(out GridPos from, out GridPos to, path);

            if (requested)
            {
                Assert.IsTrue(from.IsValid);
                Assert.IsTrue(to.IsValid);
                Assert.IsFalse(session.Board.IsEmpty(from));
                Assert.IsTrue(session.Board.IsEmpty(to));
                Assert.IsTrue(path.Count >= 2);
                Assert.AreEqual(from, path[0]);
                Assert.AreEqual(to, path[path.Count - 1]);
            }
        }

        [Test]
        public void PathPreviewView_ShowAndHide_TogglesVisibilityCleanly()
        {
            var rootGo = new GameObject("TestRoot");
            var boardGo = new GameObject("BoardView");
            boardGo.transform.SetParent(rootGo.transform);
            var boardView = boardGo.AddComponent<BoardView>();
            boardView.Initialize();

            var preview = new PathPreviewView(boardView, null, null, rootGo.transform);
            Assert.IsFalse(preview.IsVisible);

            var path = new List<GridPos>
            {
                new GridPos(0, 0),
                new GridPos(0, 1),
                new GridPos(0, 2)
            };

            object owner = new object();
            preview.ShowPath(path, 1.0f, owner);

            Assert.IsTrue(preview.IsVisible);
            Assert.IsTrue(preview.LineRenderer.enabled);
            Assert.AreEqual(3, preview.LineRenderer.positionCount);
            Assert.IsTrue(preview.DestRingObject.activeSelf);

            // Cancel by wrong owner should do nothing
            preview.CancelByOwner(new object());
            Assert.IsTrue(preview.IsVisible);

            // Cancel by actual owner should hide
            preview.CancelByOwner(owner);
            Assert.IsFalse(preview.IsVisible);
            Assert.IsFalse(preview.LineRenderer.enabled);
            Assert.IsFalse(preview.DestRingObject.activeSelf);

            Object.DestroyImmediate(rootGo);
        }

        private void SetQueue(BallColor color)
        {
            for (int i = 0; i < m_Queue.Count; i++)
            {
                m_Queue[i] = color;
            }
        }
    }
}

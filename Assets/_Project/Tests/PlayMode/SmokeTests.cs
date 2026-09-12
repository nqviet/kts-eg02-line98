using System.Collections;
using Line98.Core;
using Line98.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Line98.Tests.PlayMode
{
    [TestFixture]
    public class SmokeTests
    {
        [UnityTest]
        public IEnumerator FullGameLoop_SmokeTest()
        {
            var session = new GameSession();
            session.StartNewGame(4242U);

            yield return null;

            Assert.AreEqual(GamePhase.Playing, session.Phase);
            Assert.AreEqual(3, session.Board.OccupiedCount);
            Assert.AreEqual(78, session.Board.EmptyCount);
            Assert.AreEqual(0, session.Score);

            // Find a valid adjacent move
            GridPos from = default;
            GridPos to = default;
            bool moveFound = false;

            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos p = new GridPos(x, y);
                    if (!session.Board.IsEmpty(p))
                    {
                        if (x < BoardModel.Size - 1 && session.Board.IsEmpty(new GridPos(x + 1, y)))
                        {
                            from = p;
                            to = new GridPos(x + 1, y);
                            moveFound = true;
                            break;
                        }
                    }
                }
                if (moveFound) break;
            }

            Assert.IsTrue(moveFound, "Should find a legal adjacent move");

            session.TrySelect(from);
            Assert.IsTrue(session.HasSelection);

            bool executed = session.ExecuteMove(to);
            Assert.IsTrue(executed);

            yield return null;

            Assert.AreEqual(1, session.MoveCount);
            // Non-clearing move spawned 3 balls: 3 initial + 3 spawned = 6 occupied
            Assert.AreEqual(6, session.Board.OccupiedCount);
            Assert.AreEqual(75, session.Board.EmptyCount);
        }
    }
}

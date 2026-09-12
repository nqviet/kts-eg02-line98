using Line98.Core;
using Line98.Gameplay;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class SpawnServiceTests
    {
        [Test]
        public void SeedDeterminism_ProducesIdenticalSequence()
        {
            uint seed = 12345U;
            var rng1 = new XorShift128(seed);
            var rng2 = new XorShift128(seed);

            var queue1 = new PreviewQueue(3);
            var queue2 = new PreviewQueue(3);

            queue1.Populate(ref rng1, 5);
            queue2.Populate(ref rng2, 5);

            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(queue1[i], queue2[i]);
            }
        }

        [Test]
        public void ResolveMove_SpawnsOnlyIntoEmptyCells()
        {
            var board = new BoardModel();
            var queue = new PreviewQueue(3);
            var rng = new XorShift128(42U);
            queue.Populate(ref rng, 5);

            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(0, 1);
            board.Set(from, BallColor.Red);

            var resolver = new MoveResolver();
            MovePlan plan = resolver.Resolve(
                board,
                queue,
                in rng,
                new MoveRequest(from, to),
                ScoreRules.Default,
                SpawnRules.Default,
                0);

            Assert.AreEqual(MoveOutcome.Moved, plan.Outcome);
            Assert.AreEqual(3, plan.Spawned.Count);

            for (int i = 0; i < plan.Spawned.Count; i++)
            {
                GridPos spawnPos = plan.Spawned.Items[i].Position;
                Assert.IsTrue(spawnPos != from && spawnPos != to);
            }
        }
    }
}

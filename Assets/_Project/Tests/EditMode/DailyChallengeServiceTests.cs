using Line98.Gameplay;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class DailyChallengeServiceTests
    {
        [Test]
        public void SameDate_ProducesIdenticalSeed()
        {
            var mode1 = new DailyChallengeMode("2026-09-13");
            var mode2 = new DailyChallengeMode("2026-09-13");

            Assert.AreEqual(mode1.GenerateSeed(), mode2.GenerateSeed());
        }

        [Test]
        public void DifferentDate_ProducesDifferentSeed()
        {
            var mode1 = new DailyChallengeMode("2026-09-13");
            var mode2 = new DailyChallengeMode("2026-09-14");

            Assert.AreNotEqual(mode1.GenerateSeed(), mode2.GenerateSeed());
        }
    }
}

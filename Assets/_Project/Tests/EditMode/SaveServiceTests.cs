using Line98.Services;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class SaveServiceTests
    {
        [Test]
        public void RoundTripSave_PreservesData()
        {
            var backend = new InMemorySaveBackend();
            var service = new SaveService(backend);

            var originalData = new SaveData
            {
                Score = 1500,
                MoveCount = 42,
                LinesCleared = 7,
                FreeUndos = 2,
                BestScore = 3200
            };

            bool saved = service.SaveGame(originalData);
            Assert.IsTrue(saved);

            SaveData loaded = service.LoadGame();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1500, loaded.Score);
            Assert.AreEqual(42, loaded.MoveCount);
            Assert.AreEqual(7, loaded.LinesCleared);
            Assert.AreEqual(2, loaded.FreeUndos);
            Assert.AreEqual(3200, loaded.BestScore);
        }
    }
}

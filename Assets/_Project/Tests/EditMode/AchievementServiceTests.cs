using Line98.Services;
using NUnit.Framework;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class AchievementServiceTests
    {
        [Test]
        public void UnlockAchievement_EmitsEventAndPersists()
        {
            var service = new AchievementService();
            string unlockedId = null;
            service.OnAchievementUnlocked += id => unlockedId = id;

            bool result = service.TryUnlock("score_1000");

            Assert.IsTrue(result);
            Assert.AreEqual("score_1000", unlockedId);
            Assert.IsTrue(service.IsUnlocked("score_1000"));

            // Duplicate unlock should return false and not fire event again
            unlockedId = null;
            bool duplicate = service.TryUnlock("score_1000");
            Assert.IsFalse(duplicate);
            Assert.IsNull(unlockedId);
        }
    }
}

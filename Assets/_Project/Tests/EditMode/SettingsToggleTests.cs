using Line98.Data;
using Line98.Services;
using NUnit.Framework;
using UnityEngine;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class SettingsToggleTests
    {
        [Test]
        public void FourToggles_RoundTripThroughService()
        {
            var service = new SettingsService();

            service.SetMusic(false);
            service.SetSfx(false);
            service.SetVibration(false);
            service.SetReduceEffects(true);

            Assert.IsFalse(service.Current.Music);
            Assert.IsFalse(service.Current.Sfx);
            Assert.IsFalse(service.Current.Vibration);
            Assert.IsTrue(service.Current.ReduceEffects);

            SettingsSave snapshot = service.Snapshot();
            var restored = new SettingsService();
            restored.Load(snapshot);

            Assert.IsFalse(restored.Current.Music);
            Assert.IsFalse(restored.Current.Sfx);
            Assert.IsFalse(restored.Current.Vibration);
            Assert.IsTrue(restored.Current.ReduceEffects);
        }

        [Test]
        public void PatternHints_PersistsInSettings()
        {
            var service = new SettingsService();
            service.SetPatternHints(true);
            Assert.IsTrue(service.Current.PatternHints);

            SettingsSave snapshot = service.Snapshot();
            var restored = new SettingsService();
            restored.Load(snapshot);
            Assert.IsTrue(restored.Current.PatternHints);
        }

        [Test]
        public void AvailableLocales_ExposesEnglishAndVietnameseWithNativeNames()
        {
            var service = new SettingsService();
            var readModel = service.BuildReadModel(null, null);

            Assert.IsNotNull(readModel.AvailableLocales);
            Assert.AreEqual(2, readModel.AvailableLocales.Length);

            Assert.AreEqual("en", readModel.AvailableLocales[0].Code);
            Assert.AreEqual("ENGLISH", readModel.AvailableLocales[0].DisplayName);

            Assert.AreEqual("vi", readModel.AvailableLocales[1].Code);
            Assert.AreEqual("TIẾNG VIỆT", readModel.AvailableLocales[1].DisplayName);
            Assert.AreEqual("Tiếng Việt", readModel.AvailableLocales[1].NativeName);
        }

        [Test]
        public void BrandConfig_SuppliesUrlsAndVersion()
        {
            var brand = ScriptableObject.CreateInstance<BrandConfigSO>();
            brand.InitializeRuntime("https://custom.com/privacy", "https://custom.com/support", "mailto:test@custom.com", "2.0.0");

            var service = new SettingsService();
            var readModel = service.BuildReadModel(null, brand);

            Assert.AreEqual("https://custom.com/privacy", readModel.PrivacyUrl);
            Assert.AreEqual("https://custom.com/support", readModel.SupportUrl);
            Assert.AreEqual("2.0.0", readModel.VersionLabel);
        }

        [Test]
        public void PurchaseState_ReflectsInReadModel()
        {
            var service = new SettingsService();
            var iap = new EditorStubIapService();

            var readModelBefore = service.BuildReadModel(iap, null);
            Assert.AreEqual(PurchaseState.NotOwned, readModelBefore.RemoveAdsState);

            iap.BuyRemoveAds(success => { });

            var readModelAfter = service.BuildReadModel(iap, null);
            Assert.AreEqual(PurchaseState.Owned, readModelAfter.RemoveAdsState);
        }
    }
}

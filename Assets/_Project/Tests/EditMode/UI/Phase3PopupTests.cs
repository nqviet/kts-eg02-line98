using Line98.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Line98.Tests.EditMode.UI
{
    [TestFixture]
    public sealed class Phase3PopupTests
    {
        private const string DailyPath = "Assets/_Project/Content/Prefabs/UI/Popups/Popup_DailyChallenge.prefab";
        private const string ProgressPath = "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Statistics.prefab";
        private const string UiRootPath = "Assets/_Project/Content/Prefabs/UI/Shell/UI_Root.prefab";

        [Test]
        public void DailyChallengePrefab_ContainsCompleteReferenceLayout()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DailyPath);
            Assert.IsNotNull(prefab);
            DailyChallengePopup popup = prefab.GetComponent<DailyChallengePopup>();
            Assert.IsNotNull(popup);

            var serialized = new SerializedObject(popup);
            Assert.AreEqual(81, serialized.FindProperty("m_BoardBalls").arraySize);
            Assert.AreEqual(7, serialized.FindProperty("m_WeekdayTexts").arraySize);
            Assert.AreEqual(7, serialized.FindProperty("m_DayBadges").arraySize);
            Assert.AreEqual(7, serialized.FindProperty("m_DayChecks").arraySize);
            Assert.AreEqual(3, serialized.FindProperty("m_MissionBalls").arraySize);
            Assert.IsNotNull(serialized.FindProperty("m_PlayButton").objectReferenceValue);
            Assert.IsNotNull(serialized.FindProperty("m_HowToPlayButton").objectReferenceValue);
            AssertReferenceCanvas(popup.ModalContainer);
        }

        [Test]
        public void ProgressPrefab_ContainsSevenMetricsAndBothTabs()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProgressPath);
            Assert.IsNotNull(prefab);
            StatisticsPopup popup = prefab.GetComponent<StatisticsPopup>();
            Assert.IsNotNull(popup);

            var serialized = new SerializedObject(popup);
            string[] metricFields =
            {
                "m_GamesPlayedText", "m_BestScoreText", "m_TotalScoreText", "m_TotalLinesText",
                "m_LongestLineText", "m_HighestComboText", "m_CurrentStreakText"
            };
            for (int i = 0; i < metricFields.Length; i++)
            {
                Assert.IsNotNull(serialized.FindProperty(metricFields[i]).objectReferenceValue, metricFields[i]);
            }

            Assert.IsNotNull(serialized.FindProperty("m_StatisticsTabButton").objectReferenceValue);
            Assert.IsNotNull(serialized.FindProperty("m_AchievementsTabButton").objectReferenceValue);
            Assert.AreEqual(6, serialized.FindProperty("m_AchievementRows").arraySize);
            Assert.AreEqual(7, serialized.FindProperty("m_HeroBalls").arraySize);
            AssertReferenceCanvas(popup.ModalContainer);
        }

        [Test]
        public void UiRoot_InstallsExactlyOneDailyAndProgressPopup()
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(UiRootPath);
            Assert.IsNotNull(root);
            Assert.AreEqual(1, root.GetComponentsInChildren<DailyChallengePopup>(true).Length);
            Assert.AreEqual(1, root.GetComponentsInChildren<StatisticsPopup>(true).Length);
        }

        private static void AssertReferenceCanvas(RectTransform container)
        {
            Assert.IsNotNull(container);
            Assert.That(container.sizeDelta.x, Is.EqualTo(940f).Within(0.1f));
            Assert.That(container.sizeDelta.y, Is.EqualTo(1670f).Within(0.1f));
        }
    }
}

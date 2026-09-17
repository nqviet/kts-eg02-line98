using System.Collections.Generic;
using Line98.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public sealed class UiPrefabAuditTests
    {
        private static readonly string[] s_ExpectedPrefabPaths =
        {
            "Assets/_Project/Content/Prefabs/UI/Shell/UI_Root.prefab",
            "Assets/_Project/Content/Prefabs/UI/Widgets/Buttons/Button_Icon_Base.prefab",
            "Assets/_Project/Content/Prefabs/UI/Widgets/Buttons/Button_Action_Base.prefab",
            "Assets/_Project/Content/Prefabs/UI/Widgets/Cards/Card_Value.prefab",
            "Assets/_Project/Content/Prefabs/UI/Widgets/Cards/Card_Preview.prefab",
            "Assets/_Project/Content/Prefabs/UI/Widgets/Cards/Tray_Preview.prefab",
            "Assets/_Project/Content/Prefabs/UI/Widgets/Buttons/Badge_Count.prefab",
            "Assets/_Project/Content/Prefabs/UI/Widgets/Rows/Row_Stat.prefab",
            "Assets/_Project/Content/Prefabs/UI/Screens/Game/Panel_BrandBar.prefab",
            "Assets/_Project/Content/Prefabs/UI/Screens/Game/Panel_CardRow.prefab",
            "Assets/_Project/Content/Prefabs/UI/Screens/Game/Panel_ActionBar.prefab",
            "Assets/_Project/Content/Prefabs/UI/Screens/Game/Hud_DynamicLayer.prefab",
            "Assets/_Project/Content/Prefabs/UI/Popups/Popup_GameOver.prefab",
            "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Confirm.prefab",
            "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Settings.prefab",
            "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Statistics.prefab",
            "Assets/_Project/Content/Prefabs/UI/Popups/Popup_DailyChallenge.prefab",
            "Assets/_Project/Content/Prefabs/UI/Popups/Popup_Cosmetics.prefab",
            "Assets/_Project/Content/Prefabs/UI/Screens/MainMenu/Screen_MainMenu.prefab"
        };

        [Test]
        public void RequiredUiPrefabs_ExistWithoutMissingScripts()
        {
            for (int i = 0; i < s_ExpectedPrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_ExpectedPrefabPaths[i]);
                Assert.IsNotNull(prefab, s_ExpectedPrefabPaths[i]);

                var missingScripts = new List<string>();
                Component[] components = prefab.GetComponentsInChildren<Component>(true);
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    if (components[componentIndex] == null)
                    {
                        missingScripts.Add(s_ExpectedPrefabPaths[i]);
                    }
                }

                Assert.IsEmpty(missingScripts, $"Missing script in {s_ExpectedPrefabPaths[i]}");
            }
        }

        [Test]
        public void UiRootPrefab_ContainsLegacyBridgesAndSemanticBehaviours()
        {
            GameObject uiRoot = AssetDatabase.LoadAssetAtPath<GameObject>(s_ExpectedPrefabPaths[0]);

            Assert.IsNotNull(uiRoot.GetComponent<SafeAreaFitter>());
            Assert.IsNotNull(uiRoot.GetComponent<UiShell>());
            Assert.IsNotNull(uiRoot.GetComponent<HudPresenter>());
            Assert.IsNotNull(uiRoot.GetComponent<UiCanvasStack>());
            Assert.IsNotNull(uiRoot.GetComponent<UiPopupRegistry>());
            Assert.IsNotNull(uiRoot.GetComponent<UiScreenRegistry>());
            Assert.IsNotEmpty(uiRoot.GetComponentsInChildren<UiActionButton>(true));
            Assert.IsNotEmpty(uiRoot.GetComponentsInChildren<UiResponsiveModal>(true));
        }

        [TestCase("Assets/_Project/Content/Prefabs/UI/Popups/Popup_GameOver.prefab")]
        [TestCase("Assets/_Project/Content/Prefabs/UI/Popups/Popup_Confirm.prefab")]
        [TestCase("Assets/_Project/Content/Prefabs/UI/Popups/Popup_Settings.prefab")]
        [TestCase("Assets/_Project/Content/Prefabs/UI/Popups/Popup_Statistics.prefab")]
        [TestCase("Assets/_Project/Content/Prefabs/UI/Popups/Popup_DailyChallenge.prefab")]
        [TestCase("Assets/_Project/Content/Prefabs/UI/Popups/Popup_Cosmetics.prefab")]
        public void PopupPrefabs_HaveLifecycleAndResponsiveBehaviours(string prefabPath)
        {
            GameObject popup = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            Assert.IsNotNull(popup.GetComponent<PopupView>());
            Assert.IsNotNull(popup.GetComponent<CanvasGroup>());
            Assert.IsNotNull(popup.GetComponent<UiResponsiveModal>());
        }
    }
}

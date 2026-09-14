using System.IO;
using Line98.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Line98.Tests.EditMode.UI
{
    [TestFixture]
    public sealed class UiPathAuditTests
    {
        private const string s_ScriptRoot = "Assets/_Project/Presentation/UI";
        private const string s_PrefabRoot = "Assets/_Project/Content/Prefabs/UI";

        [Test]
        public void UiScripts_AreOwnedByTaxonomyFolders()
        {
            string[] flatScripts = Directory.Exists(s_ScriptRoot)
                ? Directory.GetFiles(s_ScriptRoot, "*.cs", SearchOption.TopDirectoryOnly)
                : new string[0];
            Assert.IsEmpty(flatScripts, "UI scripts must live below Shell, Screens, Popups, Widgets, or Theme.");

            Assert.IsTrue(Directory.Exists(Path.Combine(s_ScriptRoot, "Shell")));
            Assert.IsTrue(Directory.Exists(Path.Combine(s_ScriptRoot, "Screens", "Game")));
            Assert.IsTrue(Directory.Exists(Path.Combine(s_ScriptRoot, "Popups")));
            Assert.IsTrue(Directory.Exists(Path.Combine(s_ScriptRoot, "Widgets")));
            Assert.IsTrue(Directory.Exists(Path.Combine(s_ScriptRoot, "Theme")));
        }

        [Test]
        public void UiPrefabs_DoNotUseLegacyRoleFolders()
        {
            Assert.IsFalse(AssetDatabase.IsValidFolder(s_PrefabRoot + "/Components"));
            Assert.IsFalse(AssetDatabase.IsValidFolder(s_PrefabRoot + "/Hud"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(s_PrefabRoot + "/Shell"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(s_PrefabRoot + "/Screens/Game"));
            Assert.IsTrue(AssetDatabase.IsValidFolder(s_PrefabRoot + "/Widgets/Buttons"));
        }

        [Test]
        public void MainMenuScreenPrefab_UsesScreenAndNavigationContracts()
        {
            const string path = s_PrefabRoot + "/Screens/MainMenu/Screen_MainMenu.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, path);
            Assert.IsNotNull(prefab.GetComponent<MainMenuScreen>());
            Assert.IsNotNull(prefab.GetComponent<UiSceneNavigator>());
            Assert.IsNotEmpty(prefab.GetComponentsInChildren<UiNavigationButton>(true));
            Assert.AreEqual(SafeAreaFitter.FitMode.Overlay, prefab.GetComponent<SafeAreaFitter>().Mode);
        }

        [Test]
        public void ThemeAndSceneHomes_AreCanonical()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Content/Themes/UI/UiTheme_Default.asset"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Content/Themes/UI/PreviewSpriteSet_Default.asset"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Object>("Assets/_Project/Content/Scenes/MainMenu.unity"));
            Assert.IsFalse(File.Exists("Assets/_Project/Content/Scenes/Menu.unity"));
            Assert.IsFalse(Directory.Exists("Assets/Art/Prefabs/VFX"));
        }

        [Test]
        public void BuildSettings_ExposeBootMenuAndGameInOrder()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.GreaterOrEqual(scenes.Length, 3);
            Assert.AreEqual("Assets/_Project/Content/Scenes/Boot.unity", scenes[0].path);
            Assert.IsTrue(scenes[0].enabled);
            Assert.AreEqual("Assets/_Project/Content/Scenes/MainMenu.unity", scenes[1].path);
            Assert.IsTrue(scenes[1].enabled);
            Assert.AreEqual("Assets/_Project/Content/Scenes/Game.unity", scenes[2].path);
            Assert.IsTrue(scenes[2].enabled);
        }
    }
}

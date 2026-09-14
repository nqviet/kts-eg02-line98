using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Line98.Tests.PlayMode.UI
{
    [TestFixture]
    public sealed class UiNavigationFlowTests
    {
        [UnityTest]
        public IEnumerator BootMenuGameBackFlow_UsesCanonicalSceneNames()
        {
            yield return SceneManager.LoadSceneAsync("Boot");
            yield return WaitForScene("MainMenu");

            GameObject playButtonObject = GameObject.Find("UI_Root/Screen_MainMenu/Content/Button_Play");
            Assert.IsNotNull(playButtonObject, "MainMenu must expose a Play navigation button.");
            playButtonObject.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return WaitForScene("Game");

            GameObject menuButtonObject = GameObject.Find("UI_Root/Canvas_StaticHUD/Button_Menu");
            Assert.IsNotNull(menuButtonObject, "Game shell must expose a Menu navigation button.");
            menuButtonObject.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return WaitForScene("MainMenu");

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
        }

        private static IEnumerator WaitForScene(string expectedName)
        {
            for (int i = 0; i < 180 && SceneManager.GetActiveScene().name != expectedName; i++)
            {
                yield return null;
            }

            Assert.AreEqual(expectedName, SceneManager.GetActiveScene().name);
        }
    }
}

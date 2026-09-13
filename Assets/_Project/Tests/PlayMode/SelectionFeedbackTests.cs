using System.Collections;
using Line98.Core;
using Line98.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Line98.Tests.PlayMode
{
    [TestFixture]
    public class SelectionFeedbackTests
    {
        [UnityTest]
        public IEnumerator Selection_KeepsBallAtRestAndAnimatesGlow()
        {
            if (SceneManager.GetActiveScene().name != "Game")
            {
                yield return SceneManager.LoadSceneAsync("Game");
            }

            yield return null;

            var presentationRoot = Object.FindAnyObjectByType<PresentationRoot>();
            Assert.IsNotNull(presentationRoot, "Game scene must include a PresentationRoot");
            Assert.IsTrue(presentationRoot.IsInitialized, "PresentationRoot must be initialized before selecting a ball");

            GridPos selectedPos = default;
            bool foundBall = false;
            for (int y = 0; y < BoardModel.Size && !foundBall; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos pos = new GridPos(x, y);
                    if (!presentationRoot.Session.Board.IsEmpty(pos))
                    {
                        selectedPos = pos;
                        foundBall = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(foundBall, "A new game must contain a selectable ball");

            var ball = presentationRoot.BallManager.GetBallAt(selectedPos);
            Assert.IsNotNull(ball, "The selected model ball must have a visual");
            Transform shadow = ball.transform.Find("BlobShadow");
            Transform glow = ball.transform.Find("GlowShell");
            Assert.IsNotNull(shadow);
            Assert.IsNotNull(glow);

            Vector3 visualPosition = ball.Visual.localPosition;
            Vector3 rootPosition = ball.transform.position;
            Vector3 shadowScale = shadow.localScale;

            Assert.IsTrue(presentationRoot.Session.TrySelect(selectedPos));
            yield return null;
            yield return null;

            Assert.AreEqual(visualPosition, ball.Visual.localPosition, "Selecting a ball must not lift its visual");
            Assert.AreEqual(rootPosition, ball.transform.position, "Selecting a ball must not move its root");
            Assert.AreEqual(shadowScale, shadow.localScale, "Selecting a ball must not change its shadow scale");
            Assert.IsTrue(glow.gameObject.activeSelf, "Selecting a ball must show its glow shell");

            float minScale = glow.localScale.x;
            float maxScale = minScale;
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                minScale = Mathf.Min(minScale, glow.localScale.x);
                maxScale = Mathf.Max(maxScale, glow.localScale.x);
                Assert.IsTrue(glow.gameObject.activeSelf, "Glow shell must remain visible while selected");
            }

            Assert.Greater(maxScale - minScale, 0.005f, "Selected glow shell must animate over time");
        }
    }
}

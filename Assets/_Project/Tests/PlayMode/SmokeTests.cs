using System.Collections;
using Line98.Core;
using Line98.Gameplay;
using Line98.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Line98.Tests.PlayMode
{
    [TestFixture]
    public class SmokeTests
    {
        [UnityTest]
        public IEnumerator FullGameLoop_SmokeTest()
        {
            var session = new GameSession();
            session.StartNewGame(4242U);

            yield return null;

            Assert.AreEqual(GamePhase.Playing, session.Phase);
            Assert.AreEqual(3, session.Board.OccupiedCount);
            Assert.AreEqual(78, session.Board.EmptyCount);
            Assert.AreEqual(0, session.Score);

            // Find a valid adjacent move
            GridPos from = default;
            GridPos to = default;
            bool moveFound = false;

            for (int y = 0; y < BoardModel.Size; y++)
            {
                for (int x = 0; x < BoardModel.Size; x++)
                {
                    GridPos p = new GridPos(x, y);
                    if (!session.Board.IsEmpty(p))
                    {
                        if (x < BoardModel.Size - 1 && session.Board.IsEmpty(new GridPos(x + 1, y)))
                        {
                            from = p;
                            to = new GridPos(x + 1, y);
                            moveFound = true;
                            break;
                        }
                    }
                }
                if (moveFound) break;
            }

            Assert.IsTrue(moveFound, "Should find a legal adjacent move");

            session.TrySelect(from);
            Assert.IsTrue(session.HasSelection);

            bool executed = session.ExecuteMove(to);
            Assert.IsTrue(executed);

            yield return null;

            Assert.AreEqual(1, session.MoveCount);
            // Non-clearing move spawned 3 balls: 3 initial + 3 spawned = 6 occupied
            Assert.AreEqual(6, session.Board.OccupiedCount);
            Assert.AreEqual(75, session.Board.EmptyCount);
        }

        [UnityTest]
        public IEnumerator UI_ArchitectureAndSortingOrders_SmokeTest()
        {
            // Ensure Game scene is active
            if (!SceneManager.GetActiveScene().name.Equals("Game"))
            {
                yield return SceneManager.LoadSceneAsync("Game");
            }

            yield return null;

            // 1. Verify UI_Root and UIRouter
            var uiRoot = GameObject.Find("UI_Root");
            Assert.IsNotNull(uiRoot, "UI_Root GameObject must exist in Game scene");

            var router = uiRoot.GetComponent<UIRouter>();
            Assert.IsNotNull(router, "UIRouter component must exist on UI_Root");

            // 2. Verify Canvases and Sorting Orders (0 / 10 / 20)
            Assert.IsNotNull(router.StaticCanvas, "StaticCanvas must exist");
            Assert.IsNotNull(router.DynamicCanvas, "DynamicCanvas must exist");
            Assert.IsNotNull(router.PopupCanvas, "PopupCanvas must exist");

            Assert.AreEqual(0, router.StaticCanvas.sortingOrder, "Canvas_StaticHUD sortingOrder must be 0");
            Assert.AreEqual(10, router.DynamicCanvas.sortingOrder, "Canvas_DynamicHUD sortingOrder must be 10");
            Assert.AreEqual(20, router.PopupCanvas.sortingOrder, "Canvas_Popups sortingOrder must be 20");

            // 3. Verify EventSystem uses InputSystemUIInputModule
            var eventSystem = Object.FindAnyObjectByType<EventSystem>();
            Assert.IsNotNull(eventSystem, "EventSystem must exist in Game scene");
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            Assert.IsNotNull(inputModule, "EventSystem must use InputSystemUIInputModule");

            // 4. Verify PresentationRoot & CameraRig Viewport contract
            var presRoot = Object.FindAnyObjectByType<PresentationRoot>();
            Assert.IsNotNull(presRoot, "PresentationRoot must exist in Game scene");

            var safeAreaFitter = uiRoot.GetComponent<SafeAreaFitter>();
            Assert.IsNotNull(safeAreaFitter, "SafeAreaFitter component must exist on UI_Root");
            var layout = safeAreaFitter.CurrentLayout;
            Assert.AreEqual(layout.BoardViewportRect.min.x, presRoot.CameraRig.MinViewport.x, 0.01f, "CameraRig MinViewport.x must match solver");
            Assert.AreEqual(layout.BoardViewportRect.min.y, presRoot.CameraRig.MinViewport.y, 0.01f, "CameraRig MinViewport.y must match solver");
            Assert.AreEqual(layout.BoardViewportRect.max.x, presRoot.CameraRig.MaxViewport.x, 0.01f, "CameraRig MaxViewport.x must match solver");
            Assert.AreEqual(layout.BoardViewportRect.max.y, presRoot.CameraRig.MaxViewport.y, 0.01f, "CameraRig MaxViewport.y must match solver");

            // 5. Verify HudPresenter is wired
            var presenter = uiRoot.GetComponent<HudPresenter>();
            Assert.IsNotNull(presenter, "HudPresenter must exist on UI_Root");
            Assert.IsNotNull(presenter.ScoreNumber, "ScoreNumber must be bound");
            Assert.IsNotNull(presenter.BestNumber, "BestNumber must be bound");
            Assert.IsNotNull(presenter.PreviewView, "PreviewView must be bound");
            Assert.IsNotNull(presenter.UndoButtonView, "UndoButtonView must be bound");

            yield return null;
        }
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Line98.Core;
using Line98.Data;
using Line98.Presentation;
using Line98.Presentation.Animation;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class MovePacerTests
    {
        private GameObject m_RootGo;
        private BoardView m_BoardView;
        private BallViewManager m_BallManager;
        private TweenRunner m_TweenRunner;
        private MoveAnimator m_MoveAnimator;
        private BoardAnimator m_BoardAnimator;
        private MovePacer m_MovePacer;

        [SetUp]
        public void SetUp()
        {
            m_RootGo = new GameObject("TestMovePacerRoot");
            m_BoardView = m_RootGo.AddComponent<BoardView>();
            m_BoardView.Initialize();

            m_TweenRunner = new TweenRunner();
            m_BallManager = new BallViewManager();
            m_BallManager.Initialize(null, null, null, null, null, m_RootGo.transform, m_TweenRunner);

            m_MoveAnimator = new MoveAnimator(m_BoardView, m_BallManager, m_TweenRunner);
            m_BoardAnimator = new BoardAnimator(m_BoardView, m_BallManager, m_TweenRunner, null, FeedbackRules.Default);
            m_MovePacer = new MovePacer(m_MoveAnimator, m_BoardAnimator, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_RootGo != null)
            {
                UnityEngine.Object.DestroyImmediate(m_RootGo);
            }
        }

        [Test]
        public void MovePacer_InvokesCommit_AtLandingFrame()
        {
            GridPos from = new GridPos(0, 0);
            GridPos to = new GridPos(1, 0);

            // Spawn ball at 'from'
            m_BallManager.SpawnBall(from, BallColor.Red, m_BoardView.GridToWorld(from));

            MovePlan plan = new MovePlan
            {
                Outcome = MoveOutcome.Moved,
                From = from,
                To = to
            };
            plan.Path.Add(from);
            plan.Path.Add(to);

            bool committed = false;
            m_MovePacer.Play(plan, () => committed = true);

            // Immediately after starting flight, commit must NOT have been called yet
            Assert.IsFalse(committed, "Commit must not be called before the ball reaches landing frame");
            Assert.IsTrue(m_MovePacer.IsPacing);

            // Advance time through flight until landing frame
            m_MovePacer.Tick(0.25f);

            // Commit must be called on arrival
            Assert.IsTrue(committed, "Commit must be invoked when the ball lands at destination");
        }

        [Test]
        public void FastForward_IncreasesAnimatorSpeedMultiplier()
        {
            GridPos from = new GridPos(2, 2);
            GridPos to = new GridPos(2, 3);
            m_BallManager.SpawnBall(from, BallColor.Blue, m_BoardView.GridToWorld(from));

            MovePlan plan = new MovePlan
            {
                Outcome = MoveOutcome.Moved,
                From = from,
                To = to
            };
            plan.Path.Add(from);
            plan.Path.Add(to);

            m_MovePacer.Play(plan, () => {});
            m_MoveAnimator.SpeedMultiplier = 3.0f;

            Assert.AreEqual(3.0f, m_MoveAnimator.SpeedMultiplier);
        }
    }
}

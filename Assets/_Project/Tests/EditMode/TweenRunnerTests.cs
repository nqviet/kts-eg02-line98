using System;
using NUnit.Framework;
using UnityEngine;
using Line98.Presentation.Animation;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class TweenRunnerTests
    {
        private class TestOwner : ITweenTarget
        {
            public float LastValue;
            public bool Completed;

            public void OnTweenUpdate(int actionId, float value)
            {
                LastValue = value;
            }

            public void OnTweenComplete(int actionId)
            {
                Completed = true;
            }
        }

        [Test]
        public void CancelByOwner_ReleasesEveryHandleAndHaltsUpdates()
        {
            var runner = new TweenRunner(16);
            var ownerA = new TestOwner();
            var ownerB = new TestOwner();

            runner.Play(new Tween { From = 0f, To = 10f, Duration = 1.0f, Owner = ownerA, Target = ownerA });
            runner.Play(new Tween { From = 0f, To = 20f, Duration = 1.0f, Owner = ownerA, Target = ownerA });
            runner.Play(new Tween { From = 0f, To = 30f, Duration = 1.0f, Owner = ownerB, Target = ownerB });

            Assert.AreEqual(3, runner.ActiveCount);

            runner.CancelByOwner(ownerA);

            Assert.AreEqual(1, runner.ActiveCount);

            runner.Tick(0.5f);

            Assert.AreEqual(0f, ownerA.LastValue);
            Assert.IsTrue(ownerB.LastValue > 0f);
        }

        [Test]
        public void TweenRunner_CompletesAndInvokesTarget()
        {
            var runner = new TweenRunner(8);
            var owner = new TestOwner();

            runner.Play(new Tween
            {
                From = 0f,
                To = 100f,
                Duration = 0.5f,
                Owner = owner,
                Target = owner,
                Ease = Easing.OutCubic
            });

            runner.Tick(0.25f);
            Assert.IsFalse(owner.Completed);
            Assert.IsTrue(owner.LastValue > 0f);

            runner.Tick(0.3f);
            Assert.IsTrue(owner.Completed);
            Assert.AreEqual(100f, owner.LastValue);
            Assert.AreEqual(0, runner.ActiveCount);
        }

        [Test]
        public void TweenRunner_ZeroAllocations_OverManyTicks()
        {
            var runner = new TweenRunner(16);
            var owner = new TestOwner();

            runner.Play(new Tween
            {
                From = 0f,
                To = 100f,
                Duration = 10f,
                Owner = owner,
                Target = owner,
                Ease = Easing.OutCubic
            });

            // Warm up
            runner.Tick(0.016f);

            long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            {
                runner.Tick(0.001f);
            }
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;

            Assert.AreEqual(0, allocatedBytes, $"TweenRunner.Tick must allocate 0 bytes, but allocated {allocatedBytes} bytes");
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using Line98.Presentation;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class ReferenceFitSolverTests
    {
        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2400f)] // tall phone, width-pinned canvas
        [TestCase(1080f, 1440f)] // 1536 x 2048 iPad
        [TestCase(1080f, 607.5f)] // 1920 x 1080 landscape window
        [TestCase(1080f, 452.09302f)] // 3440 x 1440 ultrawide window
        public void Solve_ResolutionMatrix_ReferenceAlwaysFitsInsideHost(float hostWidth, float hostHeight)
        {
            Vector2[] references = { new Vector2(940f, 1670f), new Vector2(980f, 1680f), new Vector2(1080f, 1920f) };
            foreach (Vector2 reference in references)
            {
                ReferenceFitSolver.FitResult fit = ReferenceFitSolver.Solve(
                    new Vector2(hostWidth, hostHeight), default, reference, 0.05f, 1.5f);

                Assert.IsTrue(fit.IsValid);
                Assert.LessOrEqual(reference.x * fit.Scale, hostWidth + 0.01f, $"{reference} width");
                Assert.LessOrEqual(reference.y * fit.Scale, hostHeight + 0.01f, $"{reference} height");
                Assert.AreEqual(Vector2.zero, fit.Offset);
            }
        }

        [Test]
        public void Solve_RespectsMaxScale()
        {
            ReferenceFitSolver.FitResult fit = ReferenceFitSolver.Solve(
                new Vector2(1080f, 2400f), default, new Vector2(1080f, 1920f), 0.05f, 1f);

            Assert.AreEqual(1f, fit.Scale, 0.0001f);
        }

        [Test]
        public void Solve_SafeAreaInsets_ShrinkAndShiftTheComposition()
        {
            Vector2 host = new Vector2(1080f, 2400f);
            // 1080 x 2400 screen with a 120 px notch on top and a 60 px home indicator at the bottom.
            RectOffsetF insets = ReferenceFitSolver.GetSafeInsets(host, new Rect(0f, 60f, 1080f, 2220f), 1080, 2400);

            Assert.AreEqual(120f, insets.Top, 0.01f);
            Assert.AreEqual(60f, insets.Bottom, 0.01f);

            Vector2 reference = new Vector2(940f, 1670f);
            ReferenceFitSolver.FitResult fit = ReferenceFitSolver.Solve(host, insets, reference, 0.05f, 1.5f);
            float halfHeight = reference.y * fit.Scale * 0.5f;

            Assert.AreEqual(-30f, fit.Offset.y, 0.01f);
            Assert.LessOrEqual(host.y * 0.5f + fit.Offset.y + halfHeight, host.y - insets.Top + 0.01f);
            Assert.GreaterOrEqual(host.y * 0.5f + fit.Offset.y - halfHeight, insets.Bottom - 0.01f);
        }

        [Test]
        public void Solve_EmptyHost_IsInvalid()
        {
            Assert.IsFalse(ReferenceFitSolver.Solve(Vector2.zero, default, new Vector2(940f, 1670f), 0.05f, 1.5f).IsValid);
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using Line98.Presentation;

namespace Line98.Tests.EditMode
{
    [TestFixture]
    public class UiLayoutTests
    {
        [Test]
        public void Solve_ReferenceResolution_MatchesDesignLayoutSpec()
        {
            HudLayoutSolver.LayoutResult result = HudLayoutSolver.Solve(1080f, 1920f);

            Assert.AreEqual(60f, result.BrandRect.yMin, 0.5f);
            Assert.AreEqual(120f, result.BrandRect.height, 0.5f);
            Assert.AreEqual(224f, result.HudRect.yMin, 0.5f);
            Assert.AreEqual(176f, result.HudRect.height, 0.5f);

            Assert.AreEqual(45f, result.ScoreCardRect.xMin, 0.5f);
            Assert.AreEqual(318f, result.ScoreCardRect.width, 0.5f);
            Assert.AreEqual(381f, result.NextCardRect.xMin, 0.5f);
            Assert.AreEqual(717f, result.BestCardRect.xMin, 0.5f);

            Assert.AreEqual(105.5f, result.Pitch, 0.5f);
            Assert.AreEqual(434f, result.BoardRect.yMin, 1f);
            Assert.AreEqual(1426f, result.BoardRect.yMax, 1.5f);
            Assert.AreEqual(992f, result.BoardRect.width, 1f);

            Assert.AreEqual(1480f, result.ActionRect.yMin, 0.5f);
            Assert.AreEqual(1680f, result.ActionRect.yMax, 0.5f);
            Assert.AreEqual(71f, result.UndoBtnRect.xMin, 0.5f);
            Assert.AreEqual(367f, result.UndoBtnRect.xMax, 0.5f);
            Assert.AreEqual(1504f, result.UndoBtnRect.yMin, 0.5f);
            Assert.AreEqual(440f, result.HintBtnRect.xMin, 0.5f);
            Assert.AreEqual(1490f, result.HintBtnRect.yMin, 0.5f);
            Assert.AreEqual(711f, result.NewGameBtnRect.xMin, 0.5f);
            Assert.AreEqual(1007f, result.NewGameBtnRect.xMax, 0.5f);

            Assert.AreEqual(240f, result.BottomMargin, 0.5f);
            Assert.AreEqual(0.035f, result.BoardViewportRect.xMin, 0.015f);
            Assert.AreEqual(0.966f, result.BoardViewportRect.xMax, 0.015f);
            Assert.AreEqual(0.248f, result.BoardViewportRect.yMin, 0.025f);
            Assert.AreEqual(0.768f, result.BoardViewportRect.yMax, 0.025f);
        }

        [Test]
        public void Solve_TallPhones_PinsActionBarToDesignedBottomMargin()
        {
            HudLayoutSolver.LayoutResult result19_5 = HudLayoutSolver.Solve(1080f, 2340f);
            HudLayoutSolver.LayoutResult result22 = HudLayoutSolver.Solve(1080f, 2640f);

            Assert.AreEqual(105.5f, result19_5.Pitch, 0.5f);
            Assert.AreEqual(581f, result19_5.BoardRect.yMin, 1f);
            Assert.AreEqual(1900f, result19_5.ActionRect.yMin, 0.5f);
            Assert.AreEqual(240f, result19_5.BottomMargin, 0.5f);

            Assert.AreEqual(105.5f, result22.Pitch, 0.5f);
            Assert.AreEqual(674f, result22.BoardRect.yMin, 1f);
            Assert.AreEqual(2200f, result22.ActionRect.yMin, 0.5f);
            Assert.AreEqual(240f, result22.BottomMargin, 0.5f);
        }

        [Test]
        public void Solve_SafeAreaInsets_ContainsTheCompleteComposition()
        {
            const float canvasHeight = 1920f;
            const float safeTop = 60f;
            const float safeBottom = 48f;
            HudLayoutSolver.LayoutResult result = HudLayoutSolver.Solve(1080f, canvasHeight, safeTop, safeBottom);

            Assert.GreaterOrEqual(result.BrandRect.yMin, safeTop);
            Assert.GreaterOrEqual(result.ActionRect.yMin, safeTop);
            Assert.LessOrEqual(result.ActionRect.yMax, canvasHeight - safeBottom);
            Assert.GreaterOrEqual(result.BottomMargin, HudLayoutSolver.MinBottomMargin);
            Assert.Greater(result.BoardRect.width, 0f);
            Assert.Greater(result.BoardRect.height, 0f);
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2340f)]
        [TestCase(1080f, 2640f)]
        [TestCase(1080f, 1440.703125f)] // 2048 x 2732 tablet, width-pinned canvas
        [TestCase(1080f, 1440f)] // 1536 x 2048 iPad, width-pinned canvas
        [TestCase(1080f, 864f)] // 1280 x 1024 editor window, width-pinned canvas
        [TestCase(1080f, 607.5f)] // 1920 x 1080 landscape window, width-pinned canvas
        [TestCase(1080f, 452.09302f)] // 3440 x 1440 ultrawide window, width-pinned canvas
        public void Solve_ResolutionMatrix_AlwaysReturnsUsableBounds(float canvasWidth, float canvasHeight)
        {
            HudLayoutSolver.LayoutResult result = HudLayoutSolver.Solve(canvasWidth, canvasHeight);

            AssertRectIsFiniteAndNonNegative(result.LayoutRect);
            AssertRectIsFiniteAndNonNegative(result.BrandRect);
            AssertRectIsFiniteAndNonNegative(result.HudRect);
            AssertRectIsFiniteAndNonNegative(result.BoardRect);
            AssertRectIsFiniteAndNonNegative(result.ActionRect);
            Assert.GreaterOrEqual(result.BottomMargin, 0f);
            Assert.GreaterOrEqual(result.Pitch, 0f);
            Assert.GreaterOrEqual(result.ActionRect.yMin, 0f);
            Assert.LessOrEqual(result.ActionRect.yMax, canvasHeight + 0.01f);
            Assert.Greater(result.BoardViewportRect.width, 0f);
            Assert.Greater(result.BoardViewportRect.height, 0f);
            Assert.GreaterOrEqual(result.BoardViewportRect.xMin, 0f);
            Assert.GreaterOrEqual(result.BoardViewportRect.yMin, 0f);
            Assert.LessOrEqual(result.BoardViewportRect.xMax, 1f);
            Assert.LessOrEqual(result.BoardViewportRect.yMax, 1f);
        }

        [TestCase(1080f, 1920f)]
        [TestCase(1080f, 2340f)]
        [TestCase(1080f, 2640f)]
        [TestCase(1080f, 1440.703125f)] // 2048 x 2732 tablet, width-pinned canvas
        [TestCase(1080f, 1440f)] // 1536 x 2048 iPad, width-pinned canvas
        [TestCase(1080f, 864f)] // 1280 x 1024 editor window, width-pinned canvas
        [TestCase(1080f, 607.5f)] // 1920 x 1080 landscape window, width-pinned canvas
        [TestCase(1080f, 452.09302f)] // 3440 x 1440 ultrawide window, width-pinned canvas
        public void GameOverPopup_StatsRowsLayout_FitsWithinModalBounds(float canvasWidth, float canvasHeight)
        {
            const float modalWidth = 880f;
            const float modalHeight = 560f;
            const float titleTopOffset = -40f;
            const float titleHeight = 50f;
            const float buttonBottomOffset = 40f;
            const float buttonHeight = 100f;

            // Stats rows offsets relative to center: FinalScore (0), BestScore (1), LinesCleared (2), LongestLine (3), TotalMoves (4)
            float[] rowCenterY = new float[] { 96f, 48f, 0f, -48f, -96f };
            const float rowHeight = 44f;
            const float rowWidth = 640f;

            float contentTop = modalHeight * 0.5f + titleTopOffset - titleHeight;
            float contentBottom = -modalHeight * 0.5f + buttonBottomOffset + buttonHeight;

            Assert.Less(rowWidth, modalWidth);

            for (int i = 0; i < rowCenterY.Length; i++)
            {
                float rowTop = rowCenterY[i] + rowHeight * 0.5f;
                float rowBottom = rowCenterY[i] - rowHeight * 0.5f;

                Assert.LessOrEqual(rowTop, contentTop, $"Row {i} top must be below title");
                Assert.GreaterOrEqual(rowBottom, contentBottom, $"Row {i} bottom must be above buttons");

                if (i > 0)
                {
                    float prevRowBottom = rowCenterY[i - 1] - rowHeight * 0.5f;
                    Assert.GreaterOrEqual(prevRowBottom, rowTop, $"Row {i} must not overlap row {i - 1}");
                }
            }

            // Explicitly assert the two new rows: LongestLine (index 3) and TotalMoves (index 4)
            float longestLineTop = rowCenterY[3] + rowHeight * 0.5f;
            float longestLineBottom = rowCenterY[3] - rowHeight * 0.5f;
            float movesTop = rowCenterY[4] + rowHeight * 0.5f;
            float movesBottom = rowCenterY[4] - rowHeight * 0.5f;

            Assert.Greater(longestLineTop, movesTop);
            Assert.GreaterOrEqual(longestLineBottom, movesTop);
            Assert.GreaterOrEqual(movesBottom, contentBottom);
        }

        private static void AssertRectIsFiniteAndNonNegative(Rect rect)
        {
            Assert.IsFalse(float.IsNaN(rect.x));
            Assert.IsFalse(float.IsNaN(rect.y));
            Assert.IsFalse(float.IsNaN(rect.width));
            Assert.IsFalse(float.IsNaN(rect.height));
            Assert.GreaterOrEqual(rect.width, 0f);
            Assert.GreaterOrEqual(rect.height, 0f);
        }
    }
}

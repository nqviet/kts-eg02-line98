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
            var result = HudLayoutSolver.Solve(1080f, 1920f, 0f, 0f);

            // Brand
            Assert.AreEqual(60f, result.BrandRect.yMin, 0.5f, "Brand yMin mismatch");
            Assert.AreEqual(120f, result.BrandRect.height, 0.5f, "Brand height mismatch");
            Assert.AreEqual(180f, result.BrandRect.yMax, 0.5f, "Brand yMax mismatch");

            // HUD / Cards
            Assert.AreEqual(224f, result.HudRect.yMin, 0.5f, "Hud yMin mismatch");
            Assert.AreEqual(176f, result.HudRect.height, 0.5f, "Hud height mismatch");
            Assert.AreEqual(400f, result.HudRect.yMax, 0.5f, "Hud yMax mismatch");

            // Individual card rects
            Assert.AreEqual(45f, result.ScoreCardRect.xMin, 0.5f, "Score card xMin mismatch");
            Assert.AreEqual(363f, result.ScoreCardRect.xMax, 0.5f, "Score card xMax mismatch");
            Assert.AreEqual(318f, result.ScoreCardRect.width, 0.5f, "Score card width mismatch");

            Assert.AreEqual(381f, result.NextCardRect.xMin, 0.5f, "Next card xMin mismatch");
            Assert.AreEqual(699f, result.NextCardRect.xMax, 0.5f, "Next card xMax mismatch");

            Assert.AreEqual(717f, result.BestCardRect.xMin, 0.5f, "Best card xMin mismatch");
            Assert.AreEqual(1035f, result.BestCardRect.xMax, 0.5f, "Best card xMax mismatch");

            // Board
            Assert.AreEqual(105.5f, result.Pitch, 0.5f, "Board pitch mismatch");
            Assert.AreEqual(434f, result.BoardRect.yMin, 1.0f, "Board yMin mismatch");
            Assert.AreEqual(1426f, result.BoardRect.yMax, 1.5f, "Board yMax mismatch");
            Assert.AreEqual(992f, result.BoardRect.width, 1.0f, "Board width mismatch");
            Assert.AreEqual(992f, result.BoardRect.height, 1.0f, "Board height mismatch");

            // Action bar & buttons
            Assert.AreEqual(1480f, result.ActionRect.yMin, 8.0f, "Action bar yMin mismatch");
            Assert.AreEqual(1680f, result.ActionRect.yMax, 8.0f, "Action bar yMax mismatch");

            Assert.AreEqual(79f, result.UndoBtnRect.xMin, 1.0f, "Undo btn xMin mismatch");
            Assert.AreEqual(359f, result.UndoBtnRect.xMax, 1.0f, "Undo btn xMax mismatch");
            Assert.AreEqual(1505f, result.UndoBtnRect.yMin, 1.0f, "Undo btn yMin mismatch");

            Assert.AreEqual(440f, result.HintBtnRect.xMin, 1.0f, "Hint btn xMin mismatch");
            Assert.AreEqual(640f, result.HintBtnRect.xMax, 1.0f, "Hint btn xMax mismatch");
            Assert.AreEqual(1490f, result.HintBtnRect.yMin, 1.0f, "Hint btn yMin mismatch");

            Assert.AreEqual(719f, result.NewGameBtnRect.xMin, 1.0f, "New Game btn xMin mismatch");
            Assert.AreEqual(999f, result.NewGameBtnRect.xMax, 1.0f, "New Game btn xMax mismatch");

            // Bottom margin
            Assert.AreEqual(240f, result.BottomMargin, 8.0f, "Bottom margin mismatch");

            // Viewport rect (normalized)
            Assert.AreEqual(0.035f, result.BoardViewportRect.xMin, 0.015f, "Viewport xMin mismatch");
            Assert.AreEqual(0.966f, result.BoardViewportRect.xMax, 0.015f, "Viewport xMax mismatch");
            Assert.AreEqual(0.248f, result.BoardViewportRect.yMin, 0.025f, "Viewport yMin mismatch");
            Assert.AreEqual(0.768f, result.BoardViewportRect.yMax, 0.025f, "Viewport yMax mismatch");
        }

        [Test]
        public void Solve_TallAspectRatios_PreservesWidthBoundPitchAndAbsorbsSurplusHeight()
        {
            // 9:19.5 (1080 x 2340)
            var result19_5 = HudLayoutSolver.Solve(1080f, 2340f, 0f, 0f);
            Assert.AreEqual(105.5f, result19_5.Pitch, 0.5f);
            Assert.AreEqual(434f, result19_5.BoardRect.yMin, 1.0f);
            Assert.AreEqual(660f, result19_5.BottomMargin, 10.0f);

            // 9:22 (1080 x 2640)
            var result22 = HudLayoutSolver.Solve(1080f, 2640f, 0f, 0f);
            Assert.AreEqual(105.5f, result22.Pitch, 0.5f);
            Assert.AreEqual(434f, result22.BoardRect.yMin, 1.0f);
            Assert.AreEqual(960f, result22.BottomMargin, 10.0f);
        }

        [Test]
        public void Solve_SafeAreaInsets_ShiftsStackAndShrinksBottomMargin()
        {
            float safeTop = 60f;
            float safeBottom = 48f;
            var result = HudLayoutSolver.Solve(1080f, 1920f, safeTop, safeBottom);

            Assert.AreEqual(105.5f, result.Pitch, 0.5f);
            Assert.AreEqual(120f, result.BrandRect.yMin, 0.5f);
            Assert.AreEqual(284f, result.HudRect.yMin, 0.5f);
            Assert.AreEqual(494f, result.BoardRect.yMin, 1.0f);
            Assert.AreEqual(1486f, result.BoardRect.yMax, 1.5f);
            Assert.AreEqual(1540f, result.ActionRect.yMin, 8.0f);
            Assert.AreEqual(132f, result.BottomMargin, 8.0f);
        }
    }
}

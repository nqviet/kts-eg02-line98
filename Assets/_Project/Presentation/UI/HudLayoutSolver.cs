using System;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Pure, deterministic static layout solver for LINE 98 UI.
    /// Solves pixel-perfect metrics matching main_scene_mockup.png on a 1080x1920 reference frame.
    /// Handles safe-area insets and tall aspect ratios (9:16 -> 9:22) by absorbing surplus height into the bottom margin.
    /// </summary>
    public static class HudLayoutSolver
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

        // Baseline rhythm metrics (at 1080x1920 reference)
        public const float TopMargin = 60f;
        public const float BrandHeight = 120f;
        public const float GapBrandToHud = 44f;
        public const float HudHeight = 176f;
        public const float GapHudToBoard = 34f;
        public const float BoardUnits = 9.4f;
        public const float GapBoardToActions = 54f;
        public const float ActionsHeight = 200f;
        public const float MinBottomMargin = 240f;

        // Card & button horizontal specifications
        public const float CardWidth = 318f;
        public const float CardGap = 18f;
        public const float ActionCardWidth = 280f;
        public const float ActionCardHeight = 160f;
        public const float CenterButtonSize = 200f;

        public readonly struct LayoutResult
        {
            public readonly Rect BrandRect;
            public readonly Rect HudRect;
            public readonly Rect BoardRect;
            public readonly Rect ActionRect;

            public readonly Rect ScoreCardRect;
            public readonly Rect NextCardRect;
            public readonly Rect BestCardRect;

            public readonly Rect UndoBtnRect;
            public readonly Rect HintBtnRect;
            public readonly Rect NewGameBtnRect;

            public readonly float Pitch;
            public readonly float BottomMargin;
            public readonly Rect BoardViewportRect; // Normalized min/max: xMin, yMin, xMax, yMax

            public LayoutResult(
                Rect brandRect,
                Rect hudRect,
                Rect boardRect,
                Rect actionRect,
                Rect scoreCardRect,
                Rect nextCardRect,
                Rect bestCardRect,
                Rect undoBtnRect,
                Rect hintBtnRect,
                Rect newGameBtnRect,
                float pitch,
                float bottomMargin,
                Rect boardViewportRect)
            {
                BrandRect = brandRect;
                HudRect = hudRect;
                BoardRect = boardRect;
                ActionRect = actionRect;
                ScoreCardRect = scoreCardRect;
                NextCardRect = nextCardRect;
                BestCardRect = bestCardRect;
                UndoBtnRect = undoBtnRect;
                HintBtnRect = hintBtnRect;
                NewGameBtnRect = newGameBtnRect;
                Pitch = pitch;
                BottomMargin = bottomMargin;
                BoardViewportRect = boardViewportRect;
            }
        }

        /// <summary>
        /// Solves layout rects and board pitch given canvas dimensions and safe area insets.
        /// Coordinates returned are origin top-left, Y down (matching mockup specifications).
        /// </summary>
        public static LayoutResult Solve(float canvasWidth, float canvasHeight, float safeTop = 0f, float safeBottom = 0f)
        {
            float safeW = canvasWidth;
            float safeH = canvasHeight - safeTop - safeBottom;

            // Pitch calculation: min(safeW - 88, safeH - 624) / 9.4 -> 105.5f at 1080x1920
            float pitch = Mathf.Min(safeW - 88f, safeH - 624f) / BoardUnits;
            pitch = (float)Math.Round(pitch, 1);

            float boardSize = (float)Math.Round(BoardUnits * pitch, 0); // ~992 at pitch 105.5

            // Stack top-down starting from safeTop + TopMargin
            float curY = safeTop + TopMargin;
            Rect brandRect = new Rect(0f, curY, canvasWidth, BrandHeight);

            curY += BrandHeight + GapBrandToHud;
            Rect hudRect = new Rect(0f, curY, canvasWidth, HudHeight);

            // Three HUD cards (centered horizontally)
            float totalCardsWidth = (3f * CardWidth) + (2f * CardGap); // 318*3 + 18*2 = 990
            float cardsStartX = (canvasWidth - totalCardsWidth) * 0.5f; // 45 at 1080

            Rect scoreCardRect = new Rect(cardsStartX, curY, CardWidth, HudHeight);
            Rect nextCardRect = new Rect(cardsStartX + CardWidth + CardGap, curY, CardWidth, HudHeight);
            Rect bestCardRect = new Rect(cardsStartX + (CardWidth + CardGap) * 2f, curY, CardWidth, HudHeight);

            curY += HudHeight + GapHudToBoard;
            float boardX = (canvasWidth - boardSize) * 0.5f;
            Rect boardRect = new Rect(boardX, curY, boardSize, boardSize);

            curY += boardSize + GapBoardToActions;
            Rect actionRect = new Rect(0f, curY, canvasWidth, ActionsHeight);

            // Action bar items
            float undoX = 79f;
            float undoY = curY + 25f; // 1480 + 25 = 1505
            Rect undoBtnRect = new Rect(undoX, undoY, ActionCardWidth, ActionCardHeight);

            float hintX = (canvasWidth - CenterButtonSize) * 0.5f; // 440 at 1080
            float hintY = curY + 10f; // 1480 + 10 = 1490
            Rect hintBtnRect = new Rect(hintX, hintY, CenterButtonSize, CenterButtonSize);

            float newGameX = canvasWidth - 81f - ActionCardWidth; // 719 at 1080 (matching xMax 999)
            float newGameY = undoY;
            Rect newGameBtnRect = new Rect(newGameX, newGameY, ActionCardWidth, ActionCardHeight);

            float bottomMargin = (canvasHeight - safeBottom) - (curY + ActionsHeight);

            // Normalized viewport rect for CameraRig / BoardFitSolver:
            // In Unity viewport coords: (0,0) is bottom-left, (1,1) is top-right.
            // The supplied board extent includes its rim; extra padding would oversize it.
            float frameRimPadding = 0f;
            float vpMinX = Mathf.Clamp01((boardRect.xMin - frameRimPadding) / canvasWidth);
            float vpMaxX = Mathf.Clamp01((boardRect.xMax + frameRimPadding) / canvasWidth);
            float vpMinY = Mathf.Clamp01((canvasHeight - boardRect.yMax - frameRimPadding) / canvasHeight);
            float vpMaxY = Mathf.Clamp01((canvasHeight - boardRect.yMin + frameRimPadding) / canvasHeight);

            Rect boardViewportRect = Rect.MinMaxRect(vpMinX, vpMinY, vpMaxX, vpMaxY);

            return new LayoutResult(
                brandRect,
                hudRect,
                boardRect,
                actionRect,
                scoreCardRect,
                nextCardRect,
                bestCardRect,
                undoBtnRect,
                hintBtnRect,
                newGameBtnRect,
                pitch,
                bottomMargin,
                boardViewportRect);
        }
    }
}

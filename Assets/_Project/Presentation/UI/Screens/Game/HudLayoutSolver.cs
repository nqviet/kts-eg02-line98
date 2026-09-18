using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Pure, deterministic layout solver for the LINE 98 HUD.
    /// Coordinates use a top-left origin with Y increasing downwards.
    /// </summary>
    public static class HudLayoutSolver
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

        // Baseline rhythm metrics at 1080 x 1920.
        public const float TopMargin = 60f;
        public const float BrandHeight = 120f;
        public const float GapBrandToHud = 44f;
        public const float HudHeight = 176f;
        public const float GapHudToBoard = 34f;
        public const float BoardUnits = 9.4f;
        public const float GapBoardToActions = 54f;
        public const float ActionsHeight = 200f;
        public const float MinBottomMargin = 96f;
        public const float DesignedBottomMargin = 240f;

        // Horizontal specifications at the reference width.
        public const float BoardHorizontalMargin = 88f;
        public const float CardWidth = 318f;
        public const float CardGap = 18f;
        public const float ActionCardWidth = 296f;
        public const float ActionCardHeight = 162f;
        public const float CenterButtonSize = 200f;
        public const float UndoLeftMargin = 71f;
        public const float NewGameRightMargin = 73f;

        // Undo counter badge. Sized for legibility rather than for the 48px source sprite: at 3x
        // density 1080 canvas units span 360dp, so 72 units is ~24dp of badge carrying a ~12dp
        // digit -- clear of the 10sp mobile floor the old 48/26 pair fell well below.
        // The digit sits at half the badge diameter: at 44 its line box overran the badge's
        // circle, and since the font floors at MinCaption while the badge keeps scaling down,
        // any headroom given up at reference scale is lost again on shorter windows.
        public const float BadgeSize = 72f;
        public const float BadgeFontSize = 40f;

        // Horizontal distance from the Undo card's right edge to the badge centre. The badge
        // deliberately overhangs the corner, and its centre stays put as the badge grows.
        public const float BadgeCenterInsetX = 14f;

        private const float PortraitAspect = 9f / 16f;
        private const float BoardSurplusShare = 0.35f;
        private const float MaxBoardSurplusOffset = 240f;

        public readonly struct LayoutResult
        {
            public readonly Rect LayoutRect;
            public readonly Rect TopGroupRect;
            public readonly Rect MiddleRect;
            public readonly Rect BottomGroupRect;

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
            public readonly Rect UndoBadgeRect;

            public readonly float Scale;
            public readonly float Pitch;
            public readonly float BottomMargin;
            public readonly Rect BoardViewportRect;

            /// <summary>Resolved badge digit size in canvas units, floored for readability.</summary>
            public readonly float UndoBadgeFontSize;

            public LayoutResult(
                Rect layoutRect,
                Rect topGroupRect,
                Rect middleRect,
                Rect bottomGroupRect,
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
                Rect undoBadgeRect,
                float scale,
                float pitch,
                float bottomMargin,
                Rect boardViewportRect,
                float undoBadgeFontSize)
            {
                LayoutRect = layoutRect;
                TopGroupRect = topGroupRect;
                MiddleRect = middleRect;
                BottomGroupRect = bottomGroupRect;
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
                UndoBadgeRect = undoBadgeRect;
                Scale = scale;
                Pitch = pitch;
                BottomMargin = bottomMargin;
                BoardViewportRect = boardViewportRect;
                UndoBadgeFontSize = undoBadgeFontSize;
            }
        }

        /// <summary>
        /// Solves the HUD inside the supplied canvas and safe-area insets.
        /// Portrait layouts use the full safe width. Squarer and landscape windows use a
        /// centred 9:16 column, so the board can never receive a negative extent.
        /// </summary>
        public static LayoutResult Solve(
            float canvasWidth,
            float canvasHeight,
            float safeTop = 0f,
            float safeBottom = 0f,
            float safeLeft = 0f,
            float safeRight = 0f)
        {
            canvasWidth = Mathf.Max(0f, canvasWidth);
            canvasHeight = Mathf.Max(0f, canvasHeight);
            safeTop = Mathf.Clamp(safeTop, 0f, canvasHeight);
            safeBottom = Mathf.Clamp(safeBottom, 0f, canvasHeight - safeTop);
            safeLeft = Mathf.Clamp(safeLeft, 0f, canvasWidth);
            safeRight = Mathf.Clamp(safeRight, 0f, canvasWidth - safeLeft);

            float safeWidth = Mathf.Max(0f, canvasWidth - safeLeft - safeRight);
            float safeHeight = Mathf.Max(0f, canvasHeight - safeTop - safeBottom);
            float layoutWidth = Mathf.Min(safeWidth, safeHeight * PortraitAspect);
            float layoutX = safeLeft + (safeWidth - layoutWidth) * 0.5f;
            float scale = ReferenceWidth > 0f ? layoutWidth / ReferenceWidth : 0f;

            float topMargin = TopMargin * scale;
            float brandHeight = BrandHeight * scale;
            float gapBrandToHud = GapBrandToHud * scale;
            float hudHeight = HudHeight * scale;
            float gapHudToBoard = GapHudToBoard * scale;
            float gapBoardToActions = GapBoardToActions * scale;
            float actionsHeight = ActionsHeight * scale;
            float bottomMargin = Mathf.Min(
                Mathf.Max(MinBottomMargin, DesignedBottomMargin * scale),
                Mathf.Max(0f, safeHeight - actionsHeight));

            float fixedTop = topMargin + brandHeight + gapBrandToHud + hudHeight + gapHudToBoard;
            float fixedBottom = gapBoardToActions + actionsHeight + bottomMargin;
            float boardWidthLimit = Mathf.Max(0f, layoutWidth - BoardHorizontalMargin * scale);
            float boardHeightLimit = Mathf.Max(0f, safeHeight - fixedTop - fixedBottom);
            float boardSize = Mathf.Round(Mathf.Min(boardWidthLimit, boardHeightLimit));
            float usedHeight = fixedTop + boardSize + fixedBottom;
            float surplus = Mathf.Max(0f, safeHeight - usedHeight);
            float boardSurplusOffset = Mathf.Min(surplus * BoardSurplusShare, MaxBoardSurplusOffset * scale);

            float brandY = safeTop + topMargin;
            Rect brandRect = new Rect(layoutX, brandY, layoutWidth, brandHeight);

            float hudY = brandRect.yMax + gapBrandToHud;
            Rect hudRect = new Rect(layoutX, hudY, layoutWidth, hudHeight);

            float boardY = hudRect.yMax + gapHudToBoard + boardSurplusOffset;
            float boardX = layoutX + (layoutWidth - boardSize) * 0.5f;
            Rect boardRect = new Rect(boardX, boardY, boardSize, boardSize);

            float actionY = safeTop + safeHeight - bottomMargin - actionsHeight;
            Rect actionRect = new Rect(layoutX, actionY, layoutWidth, actionsHeight);

            float cardWidth = CardWidth * scale;
            float cardGap = CardGap * scale;
            float totalCardsWidth = cardWidth * 3f + cardGap * 2f;
            float cardsStartX = layoutX + (layoutWidth - totalCardsWidth) * 0.5f;
            Rect scoreCardRect = new Rect(cardsStartX, hudY, cardWidth, hudHeight);
            Rect nextCardRect = new Rect(cardsStartX + cardWidth + cardGap, hudY, cardWidth, hudHeight);
            Rect bestCardRect = new Rect(cardsStartX + (cardWidth + cardGap) * 2f, hudY, cardWidth, hudHeight);

            float actionCardWidth = ActionCardWidth * scale;
            float actionCardHeight = ActionCardHeight * scale;
            float undoY = actionY + 24f * scale;
            Rect undoBtnRect = new Rect(layoutX + UndoLeftMargin * scale, undoY, actionCardWidth, actionCardHeight);
            float hintSize = CenterButtonSize * scale;
            Rect hintBtnRect = new Rect(
                layoutX + (layoutWidth - hintSize) * 0.5f,
                actionY + Mathf.Min(10f * scale, bottomMargin),
                hintSize,
                hintSize);
            Rect newGameBtnRect = new Rect(
                layoutX + layoutWidth - NewGameRightMargin * scale - actionCardWidth,
                undoY,
                actionCardWidth,
                actionCardHeight);

            float badgeSize = BadgeSize * scale;
            Rect undoBadgeRect = new Rect(
                undoBtnRect.xMax - BadgeCenterInsetX * scale - badgeSize * 0.5f,
                undoBtnRect.yMin - badgeSize * 0.5f,
                badgeSize,
                badgeSize);
            float undoBadgeFontSize = BadgeFontSize * scale;

            Rect topGroupRect = Rect.MinMaxRect(layoutX, safeTop, layoutX + layoutWidth, hudRect.yMax);
            Rect middleRect = Rect.MinMaxRect(layoutX, hudRect.yMax, layoutX + layoutWidth, Mathf.Max(hudRect.yMax, actionRect.yMin));
            Rect bottomGroupRect = Rect.MinMaxRect(layoutX, actionRect.yMin, layoutX + layoutWidth, safeTop + safeHeight);

            float pitch = boardSize > 0f ? Mathf.Round(boardSize / BoardUnits * 10f) * 0.1f : 0f;
            Rect boardViewportRect = BuildViewportRect(boardRect, canvasWidth, canvasHeight);

            return new LayoutResult(
                new Rect(layoutX, safeTop, layoutWidth, safeHeight),
                topGroupRect,
                middleRect,
                bottomGroupRect,
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
                undoBadgeRect,
                scale,
                pitch,
                bottomMargin,
                boardViewportRect,
                undoBadgeFontSize);
        }

        private static Rect BuildViewportRect(Rect boardRect, float canvasWidth, float canvasHeight)
        {
            if (canvasWidth <= 0f || canvasHeight <= 0f || boardRect.width <= 0f || boardRect.height <= 0f)
            {
                return Rect.MinMaxRect(
                    BoardFitSolver.DefaultMinViewport.x,
                    BoardFitSolver.DefaultMinViewport.y,
                    BoardFitSolver.DefaultMaxViewport.x,
                    BoardFitSolver.DefaultMaxViewport.y);
            }

            float minX = Mathf.Clamp01(boardRect.xMin / canvasWidth);
            float maxX = Mathf.Clamp01(boardRect.xMax / canvasWidth);
            float minY = Mathf.Clamp01((canvasHeight - boardRect.yMax) / canvasHeight);
            float maxY = Mathf.Clamp01((canvasHeight - boardRect.yMin) / canvasHeight);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}

using System;
using Line98.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Views
{
    /// <summary>
    /// Bundles view references constructed by Line98UIBuilder.
    /// </summary>
    public sealed class Line98UIViews
    {
        public Line98HeaderView HeaderView { get; set; }
        public Line98ScoreView ScoreView { get; set; }
        public Line98BoardView BoardView { get; set; }
        public Line98FooterView FooterView { get; set; }
        public Line98ToastView ToastView { get; set; }
        public Line98ModalView ModalView { get; set; }
        public Line98SafeAreaFitter SafeAreaFitter { get; set; }
    }

    /// <summary>
    /// Builder class responsible for procedurally constructing the uGUI hierarchy
    /// styled entirely by a Line98ThemeData design asset.
    /// Decouples visual hierarchy construction from game behaviour.
    /// </summary>
    public static class Line98UIBuilder
    {
        public static Line98UIViews Build(Transform root, Line98ThemeData theme)
        {
            float designWidth = theme.DesignWidth;
            float designHeight = theme.DesignHeight;
            int boardSize = theme.BoardSize;

            GameObject canvasObject = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 1f;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(designWidth, designHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            CreateBackground(canvasRect, theme);

            RectTransform safeArea = CreateStretchRect(canvasRect, "SafeArea");
            RectTransform designRoot = CreateStretchRect(safeArea, "DesignRoot");
            designRoot.anchorMin = new Vector2(0.5f, 0.5f);
            designRoot.anchorMax = new Vector2(0.5f, 0.5f);
            designRoot.pivot = new Vector2(0.5f, 0.5f);
            designRoot.sizeDelta = new Vector2(designWidth, designHeight);

            RectTransform headerLayer = CreateStretchRect(designRoot, "Header");
            RectTransform scoreLayer = CreateStretchRect(designRoot, "ScoreStrip");
            RectTransform gameplayLayer = CreateStretchRect(designRoot, "Gameplay");
            RectTransform footerLayer = CreateStretchRect(designRoot, "Footer");
            RectTransform effectsLayer = CreateStretchRect(designRoot, "Effects");
            RectTransform overlayLayer = CreateStretchRect(designRoot, "Overlays");

            Line98HeaderView headerView = CreateHeader(headerLayer, theme);
            Line98ScoreView scoreView = CreateScoreStrip(scoreLayer, theme);
            Line98BoardView boardView = CreateBoard(gameplayLayer, theme, boardSize);
            Line98FooterView footerView = CreateFooter(footerLayer, theme);
            Line98ToastView toastView = CreateToast(effectsLayer, theme);
            Line98ModalView modalView = CreateOverlay(overlayLayer, theme);

            Line98SafeAreaFitter safeAreaFitter = canvasObject.AddComponent<Line98SafeAreaFitter>();
            safeAreaFitter.Initialize(safeArea, designRoot, designWidth, designHeight);

            return new Line98UIViews
            {
                HeaderView = headerView,
                ScoreView = scoreView,
                BoardView = boardView,
                FooterView = footerView,
                ToastView = toastView,
                ModalView = modalView,
                SafeAreaFitter = safeAreaFitter,
            };
        }

        private static void CreateBackground(RectTransform canvasRect, Line98ThemeData theme)
        {
            RawImage background = CreateRawImage(canvasRect, "Environment/Landscape", theme.GetLandscapeTexture());
            Stretch(background.rectTransform);
            AspectRatioFitter backgroundAspect = background.gameObject.AddComponent<AspectRatioFitter>();
            backgroundAspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundAspect.aspectRatio = (float)background.texture.width / background.texture.height;
            background.raycastTarget = false;

            Image skyWash = CreateImage(canvasRect, "Environment/SkyWash", theme.GetVerticalFadeSprite(), theme.SkyWashColor);
            Stretch(skyWash.rectTransform);
            skyWash.raycastTarget = false;
        }

        private static Line98HeaderView CreateHeader(RectTransform layer, Line98ThemeData theme)
        {
            float designWidth = theme.DesignWidth;
            float designHeight = theme.DesignHeight;

            CreateLogoBall(layer, "LogoRed", 45f, 40f, 40f, 0, theme);
            CreateLogoBall(layer, "LogoYellow", 93f, 40f, 40f, 2, theme);
            CreateLogoBall(layer, "LogoBlue", 45f, 86f, 40f, 1, theme);
            CreateLogoBall(layer, "LogoGreen", 93f, 86f, 40f, 3, theme);

            TextMeshProUGUI lineTitle = CreateText(layer, "LineTitle", "Line", 158f, 26f, 188f, 80f, 68f, theme.TitleColor, TextAlignmentOptions.Left, FontStyles.Bold, theme);
            AddTextShadow(lineTitle, new Color(0.92f, 0.98f, 1f, 0.55f), new Vector2(1f, -2f));

            TextMeshProUGUI numberTitle = CreateText(layer, "NumberTitle", "98", 331f, 19f, 150f, 91f, 76f, theme.NumberTitleColor, TextAlignmentOptions.Left, FontStyles.Bold, theme);
            AddTextShadow(numberTitle, new Color(0.70f, 0.91f, 1f, 0.7f), new Vector2(1f, -2f));

            CreateText(layer, "Subtitle", "Color Lines", 160f, 102f, 250f, 42f, 30f, theme.SubtitleColor, TextAlignmentOptions.Left, FontStyles.Bold, theme);

            GameObject settingsButtonObj = CreateIconButton(layer, "SettingsButton", 563f, 48f, 78f, 78f, Line98IconType.Gear, theme.ButtonNormalTint, theme, theme.GetButtonSquareSprite());
            GameObject statsButtonObj = CreateIconButton(layer, "StatsButton", 660f, 48f, 78f, 78f, Line98IconType.Bars, theme.ButtonNormalTint, theme, theme.GetButtonSquareSprite());

            Line98HeaderView headerView = layer.gameObject.AddComponent<Line98HeaderView>();
            headerView.Initialize(settingsButtonObj.GetComponent<Button>(), statsButtonObj.GetComponent<Button>());
            return headerView;
        }

        private static Line98ScoreView CreateScoreStrip(RectTransform layer, Line98ThemeData theme)
        {
            float designWidth = theme.DesignWidth;
            float designHeight = theme.DesignHeight;

            CreatePanel(layer, "ScorePanel", 32f, 160f, 220f, 124f, theme.PanelBackgroundColor, theme);
            CreateText(layer, "ScoreLabel", "SCORE", 32f, 173f, 220f, 30f, 22f, theme.TextSecondaryColor, TextAlignmentOptions.Center, FontStyles.Bold, theme);
            TextMeshProUGUI scoreValue = CreateText(layer, "ScoreValue", "01234", 32f, 203f, 220f, 63f, 48f, theme.TextPrimaryColor, TextAlignmentOptions.Center, FontStyles.Bold, theme);
            AddTextShadow(scoreValue, theme.TextShadowColor, new Vector2(1f, -2f));

            CreatePanel(layer, "NextPanel", 266f, 160f, 240f, 124f, theme.PanelBackgroundColor, theme);
            CreateText(layer, "NextLabel", "NEXT", 266f, 173f, 240f, 30f, 22f, theme.TextSecondaryColor, TextAlignmentOptions.Center, FontStyles.Bold, theme);

            Image[] nextBallImages = new Image[3];
            for (int index = 0; index < 3; index++)
            {
                Image tile = CreateImage(layer, "NextTile" + index, theme.GetTrayNextSprite(), Color.white);
                SetDesignRect(tile.rectTransform, 292f + index * 72f, 207f, 64f, 64f, designWidth, designHeight);
                tile.type = Image.Type.Sliced;
                tile.raycastTarget = false;

                Image ball = CreateImage(layer, "NextBall" + index, theme.GetBallSprite(index), Color.white);
                SetDesignRect(ball.rectTransform, 296f + index * 72f, 211f, 56f, 56f, designWidth, designHeight);
                ball.raycastTarget = false;
                nextBallImages[index] = ball;
            }

            CreatePanel(layer, "BestPanel", 518f, 160f, 218f, 124f, theme.PanelBackgroundColor, theme);
            Image crown = CreateImage(layer, "BestCrown", theme.GetIconSprite(Line98IconType.Crown), theme.CrownTint);
            SetDesignRect(crown.rectTransform, 607f, 164f, 40f, 40f, designWidth, designHeight);
            crown.raycastTarget = false;

            CreateText(layer, "BestLabel", "BEST", 518f, 200f, 218f, 28f, 21f, theme.TextSecondaryColor, TextAlignmentOptions.Center, FontStyles.Bold, theme);
            TextMeshProUGUI bestValue = CreateText(layer, "BestValue", "02356", 518f, 223f, 218f, 48f, 43f, theme.TextPrimaryColor, TextAlignmentOptions.Center, FontStyles.Bold, theme);
            AddTextShadow(bestValue, theme.TextShadowColor, new Vector2(1f, -2f));

            Line98ScoreView scoreView = layer.gameObject.AddComponent<Line98ScoreView>();
            scoreView.Initialize(scoreValue, bestValue, nextBallImages, theme);
            return scoreView;
        }

        private static Line98BoardView CreateBoard(RectTransform layer, Line98ThemeData theme, int boardSize)
        {
            float designWidth = theme.DesignWidth;
            float designHeight = theme.DesignHeight;

            CreatePanel(layer, "BoardFrame", 32f, 308f, 704f, 704f, theme.BoardFrameColor, theme, 12f, theme.BoardFrameOutlineColor, customSprite: theme.GetBoardFrameSprite());
            RectTransform gridRect = CreateDesignRect(layer, "BallGrid", 48f, 324f, 672f, 672f, designWidth, designHeight);

            GridLayoutGroup grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(theme.CellSize, theme.CellSize);
            grid.spacing = new Vector2(theme.CellSpacing, theme.CellSpacing);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = boardSize;

            Line98BoardView boardView = gridRect.gameObject.AddComponent<Line98BoardView>();
            Line98CellView[,] cells = new Line98CellView[boardSize, boardSize];

            for (int y = 0; y < boardSize; y++)
            {
                for (int x = 0; x < boardSize; x++)
                {
                    cells[x, y] = CreateCell(gridRect, x, y, theme, boardView);
                }
            }

            boardView.Initialize(boardSize, cells, theme);
            return boardView;
        }

        private static Line98CellView CreateCell(RectTransform parent, int x, int y, Line98ThemeData theme, Line98BoardView boardView)
        {
            GameObject cellObject = new GameObject("Cell_" + (x + 1) + "_" + (y + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(Line98CellView));
            cellObject.transform.SetParent(parent, false);

            Image background = cellObject.GetComponent<Image>();
            background.sprite = theme.GetCellSprite();
            background.type = Image.Type.Sliced;
            background.color = theme.CellNormalColor;

            Button button = cellObject.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock buttonColors = button.colors;
            buttonColors.normalColor = Color.white;
            buttonColors.highlightedColor = new Color(0.95f, 0.99f, 1f, 1f);
            buttonColors.pressedColor = new Color(0.70f, 0.86f, 1f, 1f);
            buttonColors.selectedColor = buttonColors.highlightedColor;
            buttonColors.fadeDuration = 0.08f;
            button.colors = buttonColors;

            Image glow = CreateImage(cellObject.transform, "Glow", theme.GetGlowSprite(), new Color(0f, 0.62f, 1f, 0f));
            Stretch(glow.rectTransform, 1f);
            glow.raycastTarget = false;
            glow.enabled = false;

            Image ball = CreateImage(cellObject.transform, "Ball", theme.GetBallSprite(0), Color.white);
            Stretch(ball.rectTransform, 4f);
            ball.raycastTarget = false;
            ball.enabled = false;

            Image selection = CreateImage(cellObject.transform, "Selection", theme.GetOutlineSprite(), theme.SelectionOutlineColor);
            Stretch(selection.rectTransform, 1.5f);
            selection.type = Image.Type.Sliced;
            selection.raycastTarget = false;
            selection.enabled = false;

            Line98CellView cellView = cellObject.GetComponent<Line98CellView>();
            cellView.Initialize(x, y, background, glow, ball, selection, button, (cx, cy) => boardView.OnCellClicked(cx, cy));
            return cellView;
        }

        private static Line98FooterView CreateFooter(RectTransform layer, Line98ThemeData theme)
        {
            float designWidth = theme.DesignWidth;
            float designHeight = theme.DesignHeight;

            GameObject undo = CreateIconButton(layer, "UndoButton", 52f, 1073f, 210f, 112f, Line98IconType.Undo, theme.ButtonNormalTint, theme, theme.GetUndoButtonSprite());
            TextMeshProUGUI undoLabel = CreateText(undo.transform, "UndoLabel", "Undo", 0f, 76f, 210f, 31f, 23f, new Color(0.16f, 0.25f, 0.47f), TextAlignmentOptions.Center, FontStyles.Bold, theme, true, 210f, 112f);
            SetRelativeDesignRect(undoLabel.rectTransform, 0f, 76f, 210f, 31f, 210f, 112f);

            const float dpadSize = 136f;
            GameObject dpad = CreatePanel(layer, "DirectionPad", 316f, 1065f, dpadSize, dpadSize, Color.white, theme, customSprite: theme.GetDpadSprite());
            Button dpadCenterButton = dpad.AddComponent<Button>();
            dpadCenterButton.targetGraphic = dpad.GetComponent<Image>();

            Button upBtn = CreateDirectionButton(dpad.transform, "Up", 49f, 12f, 50f, 42f, 0f, dpadSize, theme);
            Button leftBtn = CreateDirectionButton(dpad.transform, "Left", 8f, 53f, 42f, 50f, 90f, dpadSize, theme);
            Button rightBtn = CreateDirectionButton(dpad.transform, "Right", 98f, 53f, 42f, 50f, -90f, dpadSize, theme);
            Button downBtn = CreateDirectionButton(dpad.transform, "Down", 49f, 94f, 50f, 42f, 180f, dpadSize, theme);

            GameObject restart = CreateIconButton(layer, "NewGameButton", 508f, 1073f, 210f, 112f, Line98IconType.Restart, theme.ButtonNormalTint, theme, theme.GetNewGameButtonSprite());
            TextMeshProUGUI restartLabel = CreateText(restart.transform, "NewGameLabel", "New Game", 0f, 76f, 210f, 31f, 23f, new Color(0.16f, 0.25f, 0.47f), TextAlignmentOptions.Center, FontStyles.Bold, theme, true, 210f, 112f);
            SetRelativeDesignRect(restartLabel.rectTransform, 0f, 76f, 210f, 31f, 210f, 112f);

            Line98FooterView footerView = layer.gameObject.AddComponent<Line98FooterView>();
            footerView.Initialize(
                undo.GetComponent<Button>(),
                restart.GetComponent<Button>(),
                dpadCenterButton,
                upBtn,
                leftBtn,
                rightBtn,
                downBtn);

            return footerView;
        }

        private static Button CreateDirectionButton(Transform parent, string name, float x, float y, float width, float height, float rotation, float parentSize, Line98ThemeData theme)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            float scale = parentSize / 148f;
            SetRelativeDesignRect(rect, x * scale, y * scale, width * scale, height * scale, parentSize, parentSize);

            Image image = buttonObject.GetComponent<Image>();
            image.sprite = theme.GetIconSprite(Line98IconType.Arrow);
            image.color = theme.ArrowTint;
            image.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);

            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.65f, 0.90f, 1f, 1f);
            colors.pressedColor = new Color(0.3f, 0.7f, 1f, 1f);
            button.colors = colors;

            return button;
        }

        private static Line98ToastView CreateToast(RectTransform layer, Line98ThemeData theme)
        {
            GameObject toast = new GameObject("MoveFeedback", typeof(RectTransform), typeof(CanvasGroup), typeof(Line98ToastView));
            toast.transform.SetParent(layer, false);
            RectTransform rect = toast.GetComponent<RectTransform>();
            SetDesignRect(rect, 211f, 1019f, 346f, 32f, theme.DesignWidth, theme.DesignHeight);

            CanvasGroup toastGroup = toast.GetComponent<CanvasGroup>();
            toastGroup.alpha = 0f;

            TextMeshProUGUI toastText = CreateText(toast.transform, "Message", string.Empty, 0f, 0f, 346f, 32f, 17f, new Color(0.10f, 0.26f, 0.53f), TextAlignmentOptions.Center, FontStyles.Bold, theme, true, 346f, 32f);
            SetRelativeDesignRect(toastText.rectTransform, 0f, 0f, 346f, 32f, 346f, 32f);

            Line98ToastView toastView = toast.GetComponent<Line98ToastView>();
            toastView.Initialize(toastGroup, toastText, theme.ToastDuration, theme.ToastFadeDuration);
            return toastView;
        }

        private static Line98ModalView CreateOverlay(RectTransform layer, Line98ThemeData theme)
        {
            float designWidth = theme.DesignWidth;
            float designHeight = theme.DesignHeight;

            GameObject overlay = new GameObject("Modal", typeof(RectTransform), typeof(Line98ModalView));
            overlay.transform.SetParent(layer, false);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            Stretch(overlayRect);

            Image dimmer = CreateImage(overlay.transform, "Dim", theme.GetRoundedSprite(), theme.ModalDimmerColor);
            Stretch(dimmer.rectTransform);
            dimmer.raycastTarget = true;

            CreatePanel(overlay.transform, "Dialog", 136f, 455f, 496f, 338f, theme.ModalDialogColor, theme, 14f, new Color(1f, 1f, 1f, 0.9f), true, designWidth, designHeight);
            TextMeshProUGUI dialogTitle = CreateText(overlay.transform, "DialogTitle", "Settings", 166f, 492f, 436f, 46f, 34f, new Color(0.03f, 0.11f, 0.31f), TextAlignmentOptions.Center, FontStyles.Bold, theme, false, designWidth, designHeight);
            TextMeshProUGUI dialogBody = CreateText(overlay.transform, "DialogBody", string.Empty, 176f, 549f, 416f, 92f, 22f, new Color(0.19f, 0.30f, 0.54f), TextAlignmentOptions.Center, FontStyles.Normal, theme, false, designWidth, designHeight);

            GameObject primaryButton = CreateTextButton(overlay.transform, "DialogPrimary", "Close", 240f, 669f, 288f, 56f, theme.ModalPrimaryButtonColor, designWidth, designHeight, theme);
            TextMeshProUGUI primaryLabel = primaryButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();

            GameObject secondaryButton = CreateTextButton(overlay.transform, "DialogSecondary", "Close", 240f, 733f, 288f, 40f, theme.ModalSecondaryButtonColor, designWidth, designHeight, theme);
            TextMeshProUGUI secondaryLabel = secondaryButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();

            Line98ModalView modalView = overlay.GetComponent<Line98ModalView>();
            modalView.Initialize(overlay, dialogTitle, dialogBody, primaryLabel, secondaryLabel, primaryButton, secondaryButton);
            return modalView;
        }

        private static GameObject CreateTextButton(Transform parent, string name, string label, float x, float y, float width, float height, Color color, float parentWidth, float parentHeight, Line98ThemeData theme)
        {
            GameObject buttonObject = CreatePanel(parent, name, x, y, width, height, color, theme, 5f, Color.clear, true, parentWidth, parentHeight);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();

            TextMeshProUGUI text = CreateText(buttonObject.transform, "Label", label, 0f, 0f, width, height, 20f, Color.white, TextAlignmentOptions.Center, FontStyles.Bold, theme, true, width, height);
            SetRelativeDesignRect(text.rectTransform, 0f, 0f, width, height, width, height);
            return buttonObject;
        }

        private static GameObject CreateIconButton(RectTransform parent, string name, float x, float y, float width, float height, Line98IconType icon, Color color, Line98ThemeData theme, Sprite customSprite = null)
        {
            GameObject buttonObject = CreatePanel(parent, name, x, y, width, height, color, theme, customSprite: customSprite);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.99f, 1f, 1f);
            colors.pressedColor = new Color(0.67f, 0.84f, 1f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            Image iconImage = CreateImage(buttonObject.transform, "Icon", theme.GetIconSprite(icon), theme.IconTint);
            float iconSize = Mathf.Min(width, height) * 0.48f;
            SetRelativeDesignRect(iconImage.rectTransform, (width - iconSize) * 0.5f, height <= 100f ? (height - iconSize) * 0.5f : 22f, iconSize, iconSize, width, height);
            iconImage.raycastTarget = false;
            return buttonObject;
        }

        private static void CreateLogoBall(RectTransform parent, string name, float x, float y, float size, int colorIndex, Line98ThemeData theme)
        {
            Image ball = CreateImage(parent, name, theme.GetBallSprite(colorIndex), Color.white);
            SetDesignRect(ball.rectTransform, x, y, size, size, theme.DesignWidth, theme.DesignHeight);
            ball.raycastTarget = false;
        }

        private static GameObject CreatePanel(Transform parent, string name, float x, float y, float width, float height, Color color, Line98ThemeData theme, float shadowOffset = 7f, Color? outlineColor = null, bool localCoordinates = false, float localWidth = 0f, float localHeight = 0f, Sprite customSprite = null)
        {
            Sprite targetSprite = customSprite != null ? customSprite : theme.GetRoundedSprite();
            Color outline = outlineColor ?? theme.PanelOutlineColor;
            RectTransform shadowRect = localCoordinates
                ? CreateRelativeRect(parent, name + " Shadow", x + 1f, y + shadowOffset, width, height, localWidth, localHeight)
                : CreateDesignRect(parent, name + " Shadow", x + 1f, y + shadowOffset, width, height, theme.DesignWidth, theme.DesignHeight);

            Image shadow = shadowRect.gameObject.AddComponent<Image>();
            shadow.sprite = targetSprite;
            shadow.type = Image.Type.Sliced;
            shadow.color = theme.PanelShadowColor;
            shadow.raycastTarget = false;

            RectTransform panelRect = localCoordinates
                ? CreateRelativeRect(parent, name, x, y, width, height, localWidth, localHeight)
                : CreateDesignRect(parent, name, x, y, width, height, theme.DesignWidth, theme.DesignHeight);

            Image panel = panelRect.gameObject.AddComponent<Image>();
            panel.sprite = targetSprite;
            panel.type = Image.Type.Sliced;
            panel.color = color;
            AddOutline(panel, outline, 1f);
            return panel.gameObject;
        }

        private static void AddOutline(Image image, Color color, float distance)
        {
            Outline outline = image.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
        }

        private static void AddTextShadow(TextMeshProUGUI text, Color color, Vector2 distance)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static RawImage CreateRawImage(Transform parent, string name, Texture texture)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            imageObject.transform.SetParent(parent, false);
            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float x, float y, float width, float height, float fontSize, Color color, TextAlignmentOptions alignment, FontStyles style, Line98ThemeData theme, bool localCoordinates = false, float localWidth = 0f, float localHeight = 0f)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = theme.GetFont();
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;

            if (localCoordinates)
            {
                SetRelativeDesignRect(label.rectTransform, x, y, width, height, localWidth, localHeight);
            }
            else
            {
                SetDesignRect(label.rectTransform, x, y, width, height, theme.DesignWidth, theme.DesignHeight);
            }

            return label;
        }

        private static RectTransform CreateDesignRect(Transform parent, string name, float x, float y, float width, float height, float designWidth, float designHeight)
        {
            GameObject rectangleObject = new GameObject(name, typeof(RectTransform));
            rectangleObject.transform.SetParent(parent, false);
            RectTransform rect = rectangleObject.GetComponent<RectTransform>();
            SetDesignRect(rect, x, y, width, height, designWidth, designHeight);
            return rect;
        }

        private static RectTransform CreateRelativeRect(Transform parent, string name, float x, float y, float width, float height, float parentWidth, float parentHeight)
        {
            GameObject rectangleObject = new GameObject(name, typeof(RectTransform));
            rectangleObject.transform.SetParent(parent, false);
            RectTransform rect = rectangleObject.GetComponent<RectTransform>();
            SetRelativeDesignRect(rect, x, y, width, height, parentWidth, parentHeight);
            return rect;
        }

        private static RectTransform CreateStretchRect(Transform parent, string name)
        {
            GameObject rectangleObject = new GameObject(name, typeof(RectTransform));
            rectangleObject.transform.SetParent(parent, false);
            RectTransform rect = rectangleObject.GetComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        private static void SetDesignRect(RectTransform rect, float x, float y, float width, float height, float designWidth, float designHeight)
        {
            SetRelativeDesignRect(rect, x, y, width, height, designWidth, designHeight);
        }

        private static void SetRelativeDesignRect(RectTransform rect, float x, float y, float width, float height, float parentWidth, float parentHeight)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x + width * 0.5f - parentWidth * 0.5f, parentHeight * 0.5f - y - height * 0.5f);
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }
    }
}

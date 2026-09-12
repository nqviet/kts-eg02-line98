using System;
using Line98.Design;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Views
{
    /// <summary>
    /// Presentation component for a single grid cell on the board.
    /// Controls the background, selection border, glow, and ball rendering.
    /// </summary>
    public sealed class Line98CellView : MonoBehaviour
    {
        [SerializeField] private RectTransform m_Root;
        [SerializeField] private Image m_Background;
        [SerializeField] private Image m_Glow;
        [SerializeField] private Image m_Ball;
        [SerializeField] private Image m_Selection;
        [SerializeField] private Button m_Button;

        private int m_X;
        private int m_Y;
        private Action<int, int> m_OnCellClicked;

        public RectTransform Root => m_Root != null ? m_Root : (m_Root = GetComponent<RectTransform>());
        public Image Background => m_Background;
        public Image Glow => m_Glow;
        public Image Ball => m_Ball;
        public Image Selection => m_Selection;
        public Button Button => m_Button;
        public int X => m_X;
        public int Y => m_Y;

        public void Initialize(int x, int y, Image background, Image glow, Image ball, Image selection, Button button, Action<int, int> onCellClicked)
        {
            m_X = x;
            m_Y = y;
            m_Background = background;
            m_Glow = glow;
            m_Ball = ball;
            m_Selection = selection;
            m_Button = button;
            m_OnCellClicked = onCellClicked;

            if (m_Button != null)
            {
                m_Button.onClick.RemoveAllListeners();
                m_Button.onClick.AddListener(OnCellPressed);
            }
        }

        public void Render(int ballColorIndex, bool isSelected, Line98ThemeData theme)
        {
            bool hasBall = ballColorIndex >= 0;

            if (m_Ball != null)
            {
                m_Ball.enabled = hasBall;
                if (hasBall)
                {
                    m_Ball.sprite = theme.GetBallSprite(ballColorIndex);
                    m_Ball.rectTransform.localScale = Vector3.one;
                }
            }

            if (m_Glow != null)
            {
                m_Glow.enabled = isSelected;
                if (isSelected && hasBall)
                {
                    Color ballColor = theme.GetBallColor(ballColorIndex);
                    m_Glow.color = new Color(ballColor.r, ballColor.g, ballColor.b, theme.SelectionGlowAlpha);
                }
            }

            if (m_Selection != null)
            {
                m_Selection.enabled = isSelected;
            }

            if (m_Background != null)
            {
                m_Background.color = hasBall
                    ? Color.Lerp(new Color(0.78f, 0.86f, 0.95f, 0.90f), theme.GetBallColor(ballColorIndex), 0.13f)
                    : theme.CellNormalColor;
            }
        }

        public void SetBallScale(float scale)
        {
            if (m_Ball != null && m_Ball.enabled)
            {
                m_Ball.rectTransform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void OnCellPressed()
        {
            m_OnCellClicked?.Invoke(m_X, m_Y);
        }
    }
}

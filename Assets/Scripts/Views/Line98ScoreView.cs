using Line98.Design;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Views
{
    /// <summary>
    /// Presentation component for Score, Best Score, and Next Balls preview.
    /// </summary>
    public sealed class Line98ScoreView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_ScoreText;
        [SerializeField] private TextMeshProUGUI m_BestText;
        [SerializeField] private Image[] m_NextBallImages;

        private Line98ThemeData m_Theme;

        public void Initialize(TextMeshProUGUI scoreText, TextMeshProUGUI bestText, Image[] nextBallImages, Line98ThemeData theme)
        {
            m_ScoreText = scoreText;
            m_BestText = bestText;
            m_NextBallImages = nextBallImages;
            m_Theme = theme;
        }

        public void UpdateScore(int score, int bestScore)
        {
            if (m_ScoreText != null)
            {
                m_ScoreText.SetText(score.ToString("00000"));
            }

            if (m_BestText != null)
            {
                m_BestText.SetText(bestScore.ToString("00000"));
            }
        }

        public void UpdateNextBalls(int[] nextColors)
        {
            if (m_NextBallImages == null || m_Theme == null || nextColors == null)
            {
                return;
            }

            for (int index = 0; index < m_NextBallImages.Length && index < nextColors.Length; index++)
            {
                int colorIndex = nextColors[index];
                if (m_NextBallImages[index] != null)
                {
                    m_NextBallImages[index].sprite = m_Theme.GetBallSprite(colorIndex);
                }
            }
        }
    }
}

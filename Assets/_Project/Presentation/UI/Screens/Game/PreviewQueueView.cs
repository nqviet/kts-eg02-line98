using System;
using UnityEngine;
using UnityEngine.UI;
using Line98.Core;
using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Presents the 3-ball preview queue inside the recessed NEXT tray.
    /// Handles ball color sprite binding, slot positioning, and shortfall ghosting (alpha 0.35).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PreviewQueueView : MonoBehaviour
    {
        [SerializeField] private Image[] m_SlotImages = new Image[3];
        [SerializeField] private CanvasGroup[] m_SlotCanvasGroups = new CanvasGroup[3];

        [SerializeField] private UiPreviewSpriteSetSO m_SpriteSet;

        [Header("Ball Sprites (7 Colors)")]
        [SerializeField] private Sprite m_SpriteRed;
        [SerializeField] private Sprite m_SpriteOrange;
        [SerializeField] private Sprite m_SpriteYellow;
        [SerializeField] private Sprite m_SpriteGreen;
        [SerializeField] private Sprite m_SpriteCyan;
        [SerializeField] private Sprite m_SpritePurple;
        [SerializeField] private Sprite m_SpriteBlue;

        public const float BallDiameter = 79.6f;
        public const float SlotPitch = 92.0f;
        public const float ShortfallAlpha = 0.35f;

        public Image[] SlotImages => m_SlotImages;

        public void SetSpriteSet(UiPreviewSpriteSetSO spriteSet)
        {
            m_SpriteSet = spriteSet;
        }

        private void Awake()
        {
            EnsureCanvasGroups();
        }

        private void EnsureCanvasGroups()
        {
            for (int i = 0; i < m_SlotImages.Length; i++)
            {
                if (m_SlotImages[i] != null && m_SlotCanvasGroups[i] == null)
                {
                    m_SlotCanvasGroups[i] = m_SlotImages[i].GetComponent<CanvasGroup>();
                    if (m_SlotCanvasGroups[i] == null)
                    {
                        m_SlotCanvasGroups[i] = m_SlotImages[i].gameObject.AddComponent<CanvasGroup>();
                    }
                }
            }
        }

        public void SetSprites(Sprite red, Sprite orange, Sprite yellow, Sprite green, Sprite cyan, Sprite purple, Sprite blue)
        {
            m_SpriteRed = red;
            m_SpriteOrange = orange;
            m_SpriteYellow = yellow;
            m_SpriteGreen = green;
            m_SpriteCyan = cyan;
            m_SpritePurple = purple;
            m_SpriteBlue = blue;
        }

        public void SetSlots(Image slot0, Image slot1, Image slot2)
        {
            m_SlotImages = new[] { slot0, slot1, slot2 };
            m_SlotCanvasGroups = new CanvasGroup[3];
            EnsureCanvasGroups();
        }

        /// <summary>
        /// Updates the preview queue display with up to 3 upcoming balls.
        /// </summary>
        public void SetQueue(PreviewQueue queue, int emptyCellCount = 3)
        {
            EnsureCanvasGroups();

            for (int i = 0; i < 3; i++)
            {
                if (i >= m_SlotImages.Length || m_SlotImages[i] == null) continue;

                if (queue == null || i >= queue.Count)
                {
                    m_SlotImages[i].enabled = false;
                    continue;
                }

                BallColor color = queue[i];
                Sprite sprite = GetSpriteForColor(color);

                if (sprite != null && color != BallColor.None)
                {
                    m_SlotImages[i].sprite = sprite;
                    m_SlotImages[i].enabled = true;
                    m_SlotImages[i].preserveAspect = true;

                    // Shortfall state: if fewer cells remain than slot index
                    float targetAlpha = (i < emptyCellCount) ? 1.0f : ShortfallAlpha;
                    if (m_SlotCanvasGroups[i] != null)
                    {
                        m_SlotCanvasGroups[i].alpha = targetAlpha;
                    }
                }
                else
                {
                    m_SlotImages[i].enabled = false;
                }
            }
        }

        public Sprite GetSpriteForColor(BallColor color)
        {
            if (m_SpriteSet != null)
            {
                return m_SpriteSet.GetSprite(color);
            }

            switch (color)
            {
                case BallColor.Red: return m_SpriteRed;
                case BallColor.Orange: return m_SpriteOrange;
                case BallColor.Yellow: return m_SpriteYellow;
                case BallColor.Green: return m_SpriteGreen;
                case BallColor.Cyan: return m_SpriteCyan;
                case BallColor.Purple: return m_SpritePurple;
                case BallColor.Blue: return m_SpriteBlue;
                default:
                    return null;
            }
        }
    }
}

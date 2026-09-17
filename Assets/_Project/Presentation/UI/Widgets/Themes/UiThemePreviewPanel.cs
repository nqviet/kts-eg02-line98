using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Line98.Core;
using Line98.Data;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Preview card displaying the selected/active theme bundle:
    /// - Uppercased theme name
    /// - 5x5 recessed cell board mock (material-driven from BoardThemeSO or UiThemeSO)
    /// - 7 ball slots with two layouts: diamond flower (BALLS) and board-fitted (BOARD)
    /// - SELECTED badge
    /// - Diagonal wipe on board preview, staggered pop on ball preview
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiThemePreviewPanel : MonoBehaviour
    {
        private static readonly Vector2[] s_DefaultFlowerPositions = new Vector2[7]
        {
            new Vector2(0f, 219f),     // 0: Red
            new Vector2(-117f, 122f),  // 1: Orange
            new Vector2(117f, 122f),   // 2: Yellow
            new Vector2(0f, 30f),      // 3: Green (centre)
            new Vector2(-127f, -50f),  // 4: Cyan
            new Vector2(127f, -50f),   // 5: Purple
            new Vector2(0f, -139f)     // 6: Blue
        };

        private static readonly Vector2[] s_DefaultBoardPositions = new Vector2[7]
        {
            new Vector2(-100f, 100f),  // row 1, col 1
            new Vector2(100f, 100f),   // row 1, col 3
            new Vector2(-200f, 0f),    // row 2, col 0
            new Vector2(0f, 0f),       // row 2, col 2 (center)
            new Vector2(200f, 0f),     // row 2, col 4
            new Vector2(-100f, -100f), // row 3, col 1
            new Vector2(100f, -100f)   // row 3, col 3
        };

        [Header("Header")]
        [SerializeField] private TMP_Text m_ThemeNameLabel;

        [Header("Board Mock & Slots")]
        [SerializeField] private GameObject m_BoardMockRoot;
        [SerializeField] private Image m_BoardMockPlate;
        [SerializeField] private Image[] m_CellImages = Array.Empty<Image>();
        [SerializeField] private UiPreviewSlot[] m_Slots = new UiPreviewSlot[7];
        [SerializeField] private Graphic m_PreviewGlow;
        [SerializeField] private GameObject m_SelectedBadge;

        [Header("Slot Layout Positions")]
        [SerializeField] private Vector2[] m_FlowerPositions = s_DefaultFlowerPositions;
        [SerializeField] private Vector2[] m_BoardPositions = s_DefaultBoardPositions;

        [Header("Empty State")]
        [SerializeField] private GameObject m_EmptyStateRoot;
        [SerializeField] private TMP_Text m_EmptyStateLabel;

        private TweenRunner m_TweenRunner;
        private UiThemeSO m_CurrentTheme;

        public TMP_Text ThemeNameLabel => m_ThemeNameLabel;
        public string ThemeName => m_ThemeNameLabel != null ? m_ThemeNameLabel.text : string.Empty;
        public GameObject SelectedBadge => m_SelectedBadge;
        public UiPreviewSlot[] Slots => m_Slots;
        public GameObject BoardMockRoot => m_BoardMockRoot;
        public Image BoardMockPlate => m_BoardMockPlate;
        public Image[] CellImages => m_CellImages;
        public Vector2[] FlowerPositions => m_FlowerPositions;
        public Vector2[] BoardPositions => m_BoardPositions;
        public GameObject EmptyStateRoot => m_EmptyStateRoot;

        public void Initialize(TweenRunner tweenRunner)
        {
            m_TweenRunner = tweenRunner;
        }

        private void EnsureCellImages()
        {
            if (m_BoardMockPlate == null && m_BoardMockRoot != null)
            {
                m_BoardMockPlate = m_BoardMockRoot.GetComponent<Image>();
            }

            if ((m_CellImages == null || m_CellImages.Length == 0) && m_BoardMockRoot != null)
            {
                var grid = m_BoardMockRoot.transform.Find("Grid");
                if (grid != null)
                {
                    m_CellImages = grid.GetComponentsInChildren<Image>(true);
                }
                else
                {
                    m_CellImages = m_BoardMockRoot.GetComponentsInChildren<Image>(true);
                }
            }

            if (m_FlowerPositions == null || m_FlowerPositions.Length != 7)
            {
                m_FlowerPositions = s_DefaultFlowerPositions;
            }

            if (m_BoardPositions == null || m_BoardPositions.Length != 7)
            {
                m_BoardPositions = s_DefaultBoardPositions;
            }
        }

        public void ApplyBoardTheme(BoardThemeSO board)
        {
            if (board == null) return;
            EnsureCellImages();

            if (m_BoardMockPlate != null)
            {
                m_BoardMockPlate.material = board.BoardFrameMaterial;
                m_BoardMockPlate.color = Color.white;
            }

            if (m_CellImages != null)
            {
                for (int i = 0; i < m_CellImages.Length; i++)
                {
                    var cell = m_CellImages[i];
                    if (cell != null)
                    {
                        cell.material = board.BoardCellMaterial;
                        cell.color = Color.white;
                    }
                }
            }
        }

        public void ApplyUiTheme(UiThemeSO theme)
        {
            if (theme == null) return;
            m_CurrentTheme = theme;
            EnsureCellImages();

            if (m_BoardMockPlate != null)
            {
                m_BoardMockPlate.material = theme.SurfaceMaterialCard;
                m_BoardMockPlate.color = theme.PanelTray;
            }

            if (m_CellImages != null)
            {
                for (int i = 0; i < m_CellImages.Length; i++)
                {
                    var cell = m_CellImages[i];
                    if (cell != null)
                    {
                        cell.material = theme.SurfaceMaterialCard;
                        cell.color = theme.PanelCell;
                    }
                }
            }
        }

        public void Show(in ThemePreviewRequest request)
        {
            if (m_BoardMockRoot != null) m_BoardMockRoot.SetActive(true);
            if (m_EmptyStateRoot != null) m_EmptyStateRoot.SetActive(false);

            if (m_ThemeNameLabel != null)
            {
                m_ThemeNameLabel.text = !string.IsNullOrEmpty(request.DisplayName)
                    ? request.DisplayName.ToUpperInvariant()
                    : string.Empty;
            }

            if (m_SelectedBadge != null)
            {
                m_SelectedBadge.SetActive(request.IsApplied);
            }

            EnsureCellImages();

            var gemSprites = request.GemSprites;
            if ((gemSprites == null || gemSprites.Length == 0) && request.BallTheme != null && request.BallTheme.PreviewSpriteSet != null)
            {
                gemSprites = new Sprite[7];
                for (int c = 1; c <= 7; c++)
                {
                    gemSprites[c - 1] = request.BallTheme.PreviewSpriteSet.GetSprite((BallColor)c);
                }
            }

            bool isBoard = request.Emphasis == PreviewEmphasis.Board;
            if (isBoard && request.Board != null)
            {
                ApplyBoardTheme(request.Board);

                if (m_PreviewGlow != null)
                {
                    m_PreviewGlow.enabled = false;
                }

                LayoutSlots(m_BoardPositions);

                if (request.Animate && m_TweenRunner != null)
                {
                    AnimateBoardDiagonalWipe();
                    AnimateBoardGems(gemSprites);
                }
                else
                {
                    SetGemSpritesImmediate(gemSprites);
                }
            }
            else
            {
                var uiToApply = request.UiTheme ?? m_CurrentTheme;
                if (uiToApply != null)
                {
                    ApplyUiTheme(uiToApply);
                }
                else if (request.Board != null)
                {
                    ApplyBoardTheme(request.Board);
                }

                if (m_PreviewGlow != null)
                {
                    m_PreviewGlow.enabled = true;
                }

                LayoutSlots(m_FlowerPositions);

                for (int i = 0; i < m_Slots.Length; i++)
                {
                    var slot = m_Slots[i];
                    if (slot == null) continue;

                    Sprite sp = (gemSprites != null && i < gemSprites.Length) ? gemSprites[i] : null;
                    slot.SetSprite(sp, 1f);

                    if (request.Animate && m_TweenRunner != null && sp != null)
                    {
                        AnimateSlot(slot, i);
                    }
                }
            }
        }

        public void Show(
            string themeName,
            Sprite[] sprites,
            bool isApplied,
            bool animate = true)
        {
            Show(new ThemePreviewRequest(
                displayName: themeName,
                board: null,
                ball: null,
                gemSprites: sprites,
                emphasis: PreviewEmphasis.Gems,
                isApplied: isApplied,
                animate: animate));
        }

        public void ShowEmpty(ThemeCategory category)
        {
            if (m_BoardMockRoot != null) m_BoardMockRoot.SetActive(false);
            if (m_EmptyStateRoot != null) m_EmptyStateRoot.SetActive(true);

            if (m_ThemeNameLabel != null)
            {
                m_ThemeNameLabel.text = category switch
                {
                    ThemeCategory.Board => "BOARD THEMES",
                    ThemeCategory.ClearEffect => "EFFECT THEMES",
                    _ => "THEMES"
                };
            }

            if (m_EmptyStateLabel != null)
            {
                m_EmptyStateLabel.text = "Coming soon";
            }

            if (m_SelectedBadge != null)
            {
                m_SelectedBadge.SetActive(false);
            }
        }

        private void LayoutSlots(Vector2[] positions)
        {
            if (positions == null) return;
            for (int i = 0; i < m_Slots.Length && i < positions.Length; i++)
            {
                var slot = m_Slots[i];
                if (slot == null) continue;

                var rt = slot.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchoredPosition = positions[i];
                }
            }
        }

        private void SetGemSpritesImmediate(Sprite[] gemSprites)
        {
            for (int i = 0; i < m_Slots.Length; i++)
            {
                var slot = m_Slots[i];
                if (slot == null) continue;

                Sprite sp = (gemSprites != null && i < gemSprites.Length) ? gemSprites[i] : null;
                slot.SetSprite(sp, 1f);

                var rt = slot.transform as RectTransform;
                if (rt != null)
                {
                    rt.localScale = Vector3.one;
                }
            }
        }

        private void AnimateBoardDiagonalWipe()
        {
            if (m_CellImages == null || m_TweenRunner == null) return;

            int count = m_CellImages.Length;
            int cols = 5;

            for (int i = 0; i < count; i++)
            {
                var cell = m_CellImages[i];
                if (cell == null) continue;

                var rt = cell.transform as RectTransform;
                if (rt == null) continue;

                int x = i % cols;
                int y = i / cols;
                int diag = x + y;
                float delay = diag * 0.008f; // 8 ms per diagonal

                m_TweenRunner.CancelByOwner(rt);
                rt.localScale = new Vector3(0.88f, 0.88f, 1f);

                Tween wipeTween = new Tween
                {
                    From = 0.88f,
                    To = 1.0f,
                    Duration = 0.38f,
                    Ease = Easing.OutCubic,
                    Owner = rt,
                    OnUpdate = val =>
                    {
                        if (rt != null) rt.localScale = new Vector3(val, val, 1f);
                    }
                };

                if (delay > 0.001f)
                {
                    Tween delayTween = new Tween
                    {
                        From = 0f,
                        To = 1f,
                        Duration = delay,
                        Owner = rt,
                        OnComplete = () =>
                        {
                            if (rt != null) m_TweenRunner.Play(in wipeTween);
                        }
                    };
                    m_TweenRunner.Play(in delayTween);
                }
                else
                {
                    m_TweenRunner.Play(in wipeTween);
                }
            }
        }

        private void AnimateBoardGems(Sprite[] gemSprites)
        {
            for (int i = 0; i < m_Slots.Length; i++)
            {
                var slot = m_Slots[i];
                if (slot == null) continue;

                Sprite sp = (gemSprites != null && i < gemSprites.Length) ? gemSprites[i] : null;
                slot.SetSprite(sp, 1f);

                var rt = slot.transform as RectTransform;
                if (rt == null) continue;

                m_TweenRunner.CancelByOwner(rt);
                rt.localScale = new Vector3(0.7f, 0.7f, 1f);

                Tween scaleTween = new Tween
                {
                    From = 0.7f,
                    To = 1.0f,
                    Duration = 0.22f,
                    Ease = Easing.OutQuad,
                    Owner = rt,
                    OnUpdate = val =>
                    {
                        if (rt != null) rt.localScale = new Vector3(val, val, 1f);
                    }
                };

                // Delay 120 ms after board wipe start
                Tween delayTween = new Tween
                {
                    From = 0f,
                    To = 1f,
                    Duration = 0.12f,
                    Owner = rt,
                    OnComplete = () =>
                    {
                        if (rt != null) m_TweenRunner.Play(in scaleTween);
                    }
                };
                m_TweenRunner.Play(in delayTween);
            }
        }

        private void AnimateSlot(UiPreviewSlot slot, int slotIndex)
        {
            // Center is slot 3 -> ring 0 (0 ms delay)
            // Slots 1, 2, 4, 5 -> ring 1 (22 ms delay)
            // Slots 0, 6 -> ring 2 (44 ms delay)
            float delaySeconds = slotIndex switch
            {
                3 => 0.0f,
                1 or 2 or 4 or 5 => 0.022f,
                _ => 0.044f
            };

            var rt = slot.transform as RectTransform;
            if (rt == null) return;

            m_TweenRunner.CancelByOwner(rt);
            rt.localScale = new Vector3(0.75f, 0.75f, 1f);

            Tween tween = new Tween
            {
                From = 0.75f,
                To = 1.0f,
                Duration = 0.22f,
                Ease = Easing.OutBack,
                Owner = rt,
                OnUpdate = val =>
                {
                    if (rt != null) rt.localScale = new Vector3(val, val, 1f);
                }
            };

            if (delaySeconds > 0.001f)
            {
                Tween delayTween = new Tween
                {
                    From = 0f,
                    To = 1f,
                    Duration = delaySeconds,
                    Owner = rt,
                    OnComplete = () =>
                    {
                        if (rt != null) m_TweenRunner.Play(in tween);
                    }
                };
                m_TweenRunner.Play(in delayTween);
            }
            else
            {
                m_TweenRunner.Play(in tween);
            }
        }
    }
}

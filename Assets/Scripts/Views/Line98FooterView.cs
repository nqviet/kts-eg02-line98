using System;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Views
{
    /// <summary>
    /// Presentation component for footer controls: Undo, D-Pad directional navigation, and New Game.
    /// </summary>
    public sealed class Line98FooterView : MonoBehaviour
    {
        [SerializeField] private Button m_UndoButton;
        [SerializeField] private Button m_NewGameButton;
        [SerializeField] private Button m_DpadCenterButton;
        [SerializeField] private Button m_UpButton;
        [SerializeField] private Button m_LeftButton;
        [SerializeField] private Button m_RightButton;
        [SerializeField] private Button m_DownButton;

        public event Action OnUndoClicked;
        public event Action OnNewGameClicked;
        public event Action OnSelectNearestClicked;
        public event Action<Vector2Int> OnDirectionClicked;

        public void Initialize(
            Button undoButton,
            Button newGameButton,
            Button dpadCenterButton,
            Button upButton,
            Button leftButton,
            Button rightButton,
            Button downButton)
        {
            m_UndoButton = undoButton;
            m_NewGameButton = newGameButton;
            m_DpadCenterButton = dpadCenterButton;
            m_UpButton = upButton;
            m_LeftButton = leftButton;
            m_RightButton = rightButton;
            m_DownButton = downButton;

            if (m_UndoButton != null)
            {
                m_UndoButton.onClick.RemoveAllListeners();
                m_UndoButton.onClick.AddListener(OnUndoPressed);
            }

            if (m_NewGameButton != null)
            {
                m_NewGameButton.onClick.RemoveAllListeners();
                m_NewGameButton.onClick.AddListener(OnNewGamePressed);
            }

            if (m_DpadCenterButton != null)
            {
                m_DpadCenterButton.onClick.RemoveAllListeners();
                m_DpadCenterButton.onClick.AddListener(OnSelectNearestPressed);
            }

            if (m_UpButton != null)
            {
                m_UpButton.onClick.RemoveAllListeners();
                m_UpButton.onClick.AddListener(() => OnDirectionPressed(new Vector2Int(0, -1)));
            }

            if (m_LeftButton != null)
            {
                m_LeftButton.onClick.RemoveAllListeners();
                m_LeftButton.onClick.AddListener(() => OnDirectionPressed(new Vector2Int(-1, 0)));
            }

            if (m_RightButton != null)
            {
                m_RightButton.onClick.RemoveAllListeners();
                m_RightButton.onClick.AddListener(() => OnDirectionPressed(new Vector2Int(1, 0)));
            }

            if (m_DownButton != null)
            {
                m_DownButton.onClick.RemoveAllListeners();
                m_DownButton.onClick.AddListener(() => OnDirectionPressed(new Vector2Int(0, 1)));
            }
        }

        public void OnUndoPressed()
        {
            OnUndoClicked?.Invoke();
        }

        public void OnNewGamePressed()
        {
            OnNewGameClicked?.Invoke();
        }

        public void OnSelectNearestPressed()
        {
            OnSelectNearestClicked?.Invoke();
        }

        public void OnDirectionPressed(Vector2Int direction)
        {
            OnDirectionClicked?.Invoke(direction);
        }
    }
}

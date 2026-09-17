using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Line98.Core;
using Line98.Gameplay;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    public enum InputState : byte
    {
        Idle,
        BallSelected,
        Locked
    }

    /// <summary>
    /// Translates screen-space pointer taps into board grid actions via mathematical Plane.Raycast.
    /// Completely bypasses PhysX: zero colliders, zero raycast overhead.
    /// Manages the selection FSM, fast-forward during animation tails, and timed locks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InputRouter : MonoBehaviour, ITickable
    {
        [SerializeField] private Camera m_Camera;
        [SerializeField] private BoardView m_BoardView;

        private GameSession m_Session;
        private InputState m_State = InputState.Idle;
        private GridPos m_SelectedPos;
        private float m_LockUntilTime;
        private Vector3 m_PlaneOrigin = Vector3.zero;

        public event Action<GridPos> OnBallSelected;
        public event Action<GridPos> OnBallDeselected;
        public event Action<GridPos> OnInvalidMoveAttempted;
        public event Action OnFastForwardRequested;

        public InputState State => m_State;
        public bool HasSelection => m_State == InputState.BallSelected;
        public GridPos SelectedPos => m_SelectedPos;
        public bool IsLocked => m_State == InputState.Locked || Time.time < m_LockUntilTime;
        public bool IsPointerOverUi => UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        public void Initialize(Camera cam, BoardView boardView, GameSession session)
        {
            m_Camera = cam;
            m_BoardView = boardView;
            m_Session = session;
            m_State = InputState.Idle;
            m_LockUntilTime = 0f;
            if (m_BoardView != null)
            {
                m_PlaneOrigin = m_BoardView.transform.position;
            }
        }

        public void LockInput(float durationSeconds = 0f)
        {
            m_State = InputState.Locked;
            if (durationSeconds > 0f)
            {
                m_LockUntilTime = Time.time + durationSeconds;
            }
            else
            {
                m_LockUntilTime = float.MaxValue;
            }
        }

        public void UnlockInput()
        {
            m_State = m_SelectedPos.Index >= 0 && HasSelection ? InputState.BallSelected : InputState.Idle;
            m_LockUntilTime = 0f;
        }

        public void ReleaseAt(float timeStamp)
        {
            m_LockUntilTime = timeStamp;
        }

        public void Tick(float dt)
        {
            if (m_State == InputState.Locked && m_LockUntilTime < float.MaxValue && Time.time >= m_LockUntilTime)
            {
                m_State = InputState.Idle;
            }

            ProcessPointerInput();
        }

        private void ProcessPointerInput()
        {
            Vector2 screenPos = Vector2.zero;
            bool pressed = false;

            var pointer = Pointer.current;
            if (pointer != null)
            {
                if (pointer.press.wasPressedThisFrame)
                {
                    screenPos = pointer.position.ReadValue();
                    pressed = true;
                }
            }

            if (!pressed) return;

            if (pointer != null && UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(pointer.deviceId))
            {
                return;
            }

            if (IsLocked)
            {
                // Player tapped during locked animation tail -> request 3x fast forward!
                OnFastForwardRequested?.Invoke();
                return;
            }

            if (m_Camera == null || m_BoardView == null || m_Session == null) return;

            // Mathematical raycast on horizontal board plane (y = m_PlaneOrigin.y)
            Plane boardPlane = new Plane(Vector3.up, m_PlaneOrigin);
            Ray ray = m_Camera.ScreenPointToRay(screenPos);

            if (boardPlane.Raycast(ray, out float enterDist))
            {
                Vector3 worldHit = ray.GetPoint(enterDist);
                if (m_BoardView.WorldToGrid(worldHit, out GridPos tappedPos))
                {
                    HandleCellTapped(tappedPos);
                }
            }
        }

        public void HandleCellTapped(GridPos tappedPos)
        {
            if (m_Session == null) return;

            SynchronizeSelectionFromSession();

            bool cellOccupied = !m_Session.Board.IsEmpty(tappedPos);

            if (m_State == InputState.Idle)
            {
                if (cellOccupied)
                {
                    SelectBall(tappedPos);
                }
            }
            else if (m_State == InputState.BallSelected)
            {
                if (tappedPos == m_SelectedPos)
                {
                    // Re-tap deselects
                    Deselect();
                }
                else if (cellOccupied)
                {
                    // Tapped a different ball -> switch selection
                    SelectBall(tappedPos);
                }
                else
                {
                    // Tapped empty cell -> attempt move
                    GridPos from = m_SelectedPos;
                    GridPos to = tappedPos;

                    bool moveStarted = m_Session.ExecuteMove(to);
                    if (moveStarted)
                    {
                        Deselect();
                        LockInput();
                    }
                    else
                    {
                        // Path is blocked or move is invalid
                        OnInvalidMoveAttempted?.Invoke(from);
                        // Short refusal lock: 240 ms per specification
                        LockInput(0.24f);
                    }
                }
            }
        }

        private void SynchronizeSelectionFromSession()
        {
            if (m_State != InputState.Idle || !m_Session.HasSelection)
            {
                return;
            }

            // Hint selection is initiated by the HUD directly on the authoritative session.
            // Mirror it before routing the next board tap so any legal destination remains valid.
            m_SelectedPos = m_Session.SelectedPos;
            m_State = InputState.BallSelected;
        }

        public void SelectBall(GridPos pos)
        {
            m_SelectedPos = pos;
            m_State = InputState.BallSelected;
            m_Session?.TrySelect(pos);
            OnBallSelected?.Invoke(pos);
        }

        public void Deselect()
        {
            if (m_State == InputState.BallSelected)
            {
                GridPos prev = m_SelectedPos;
                m_State = InputState.Idle;
                m_Session?.Deselect();
                OnBallDeselected?.Invoke(prev);
            }
        }
    }
}

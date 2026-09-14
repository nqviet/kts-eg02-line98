using System;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Designer-selectable navigation intent for buttons used by future menu screens.</summary>
    [RequireComponent(typeof(UiActionButton))]
    [DisallowMultipleComponent]
    public sealed class UiNavigationButton : MonoBehaviour
    {
        public enum UiDestination
        {
            None,
            Menu,
            Game,
            Settings,
            Statistics
        }

        [SerializeField] private UiDestination m_Destination;
        [SerializeField] private bool m_PlayClickSound = true;
        [SerializeField] private UiActionButton m_ActionButton;

        public event Action<UiDestination> OnNavigate;

        private void Awake()
        {
            if (m_ActionButton == null) m_ActionButton = GetComponent<UiActionButton>();
        }

        private void OnEnable()
        {
            if (m_ActionButton != null) m_ActionButton.OnPressed += HandlePressed;
        }

        private void OnDisable()
        {
            if (m_ActionButton != null) m_ActionButton.OnPressed -= HandlePressed;
        }

        private void HandlePressed()
        {
            if (m_Destination != UiDestination.None)
            {
                OnNavigate?.Invoke(m_Destination);
            }
        }
    }
}

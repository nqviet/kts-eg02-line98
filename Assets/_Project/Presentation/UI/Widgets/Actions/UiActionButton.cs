using System;
using UnityEngine;
using UnityEngine.Events;

namespace Line98.Presentation
{
    /// <summary>
    /// Semantic action control for a UI prefab. It preserves designer UnityEvents and exposes
    /// a C# event for screen binders without allowing either side to clear the other's listeners.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    [DisallowMultipleComponent]
    public sealed class UiActionButton : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button m_Button;
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private UiButtonFx m_PressFx;
        [SerializeField] private UnityEvent m_OnPressed;

        private event UnityAction m_Pressed;

        public UnityEngine.UI.Button Button => m_Button;
        public event UnityAction OnPressed
        {
            add => m_Pressed += value;
            remove => m_Pressed -= value;
        }

        private void Awake()
        {
            if (m_Button == null) m_Button = GetComponent<UnityEngine.UI.Button>();
            if (m_CanvasGroup == null) m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_PressFx == null) m_PressFx = GetComponent<UiButtonFx>();
        }

        private void OnEnable()
        {
            if (m_Button != null)
            {
                m_Button.onClick.AddListener(HandlePressed);
            }
        }

        private void OnDisable()
        {
            if (m_Button != null)
            {
                m_Button.onClick.RemoveListener(HandlePressed);
            }
        }

        public void Initialize(UiServices services)
        {
            if (services == null)
            {
                return;
            }

            m_PressFx?.Initialize(services.TweenRunner, services.AudioService);
        }

        public void SetInteractable(bool interactable)
        {
            if (m_Button != null)
            {
                m_Button.interactable = interactable;
            }

            UiDimState.Apply(m_CanvasGroup, interactable);
        }

        private void HandlePressed()
        {
            m_OnPressed?.Invoke();
            m_Pressed?.Invoke();
        }
    }
}

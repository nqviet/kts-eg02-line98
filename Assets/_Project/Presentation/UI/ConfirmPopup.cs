using System;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>
    /// Confirmation dialog for actions like starting a New Game mid-session.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ConfirmPopup : UiPopupBase
    {
        [SerializeField] private Button m_ConfirmButton;
        [SerializeField] private Button m_CancelButton;

        public Button ConfirmButton => m_ConfirmButton;
        public Button CancelButton => m_CancelButton;

        protected override void Awake()
        {
            base.Awake();
            if (m_CancelButton != null)
            {
                m_CancelButton.onClick.AddListener(() => Hide());
            }
        }
    }
}

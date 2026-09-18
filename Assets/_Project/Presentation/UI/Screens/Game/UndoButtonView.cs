using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Line98.Presentation
{
    /// <summary>
    /// Governs the Undo action card presentation:
    /// Displays remaining free undos on the top-right badge, grays out once they are exhausted,
    /// and manages interactable / dimmed visual state per game mode rules.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UndoButtonView : MonoBehaviour
    {
        [SerializeField] private Button m_Button;
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private Image m_BadgeImage;
        [SerializeField] private TMP_Text m_BadgeText;

        public Button Button => m_Button;

        private void Awake()
        {
            if (m_Button == null) m_Button = GetComponent<Button>();
            if (m_CanvasGroup == null) m_CanvasGroup = GetComponent<CanvasGroup>();
        }

        public void BindComponents(Button btn, CanvasGroup canvasGroup, Image badgeImage, TMP_Text badgeText)
        {
            m_Button = btn;
            m_CanvasGroup = canvasGroup;
            m_BadgeImage = badgeImage;
            m_BadgeText = badgeText;
        }

        public void SetUndoState(int freeUndosRemaining, bool canUndo, bool isAdAvailable, bool isModeAllowed)
        {
            if (!isModeAllowed)
            {
                SetInteractable(false);
                if (m_BadgeImage != null) m_BadgeImage.gameObject.SetActive(false);
                return;
            }

            if (m_BadgeImage != null)
            {
                m_BadgeImage.gameObject.SetActive(true);
            }

            if (freeUndosRemaining > 0)
            {
                if (m_BadgeText != null)
                {
                    m_BadgeText.text = freeUndosRemaining.ToString();
                }
                SetInteractable(canUndo);
            }
            else
            {
                // Exhausted free undos. TODO(P4.3): restore the "AD" badge and the rewarded path
                // once a rewarded undo can actually be served; advertising an ad that never plays
                // reads as a broken button, so the exhausted state shows a plain "0" and grays out.
                if (m_BadgeText != null)
                {
                    m_BadgeText.text = "0";
                }
                SetInteractable(canUndo && isAdAvailable);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (m_Button != null)
            {
                m_Button.interactable = interactable;
            }

            UiDimState.Apply(m_CanvasGroup, interactable);
        }
    }
}

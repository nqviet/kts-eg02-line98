using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Base modal popup view implementing the 320 ms show / 180 ms hide curve specified in Animation plan #28.
    /// Operates with an internal CanvasGroup and RectTransform scale animation with AlwaysAnimate discipline.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class PopupView : MonoBehaviour
    {
        [Header("Popup Elements")]
        [SerializeField] private RectTransform m_ModalContainer;
        [SerializeField] private CanvasGroup m_CanvasGroup;
        [SerializeField] private TMP_Text m_TitleText;
        [SerializeField] private TMP_Text m_BodyText;
        [SerializeField] private Button m_PrimaryButton;
        [SerializeField] private Button m_SecondaryButton;
        [SerializeField] private Button m_CloseButton;

        private TweenRunner m_TweenRunner;
        private bool m_IsOpen;

        public bool IsOpen => m_IsOpen;
        public CanvasGroup CanvasGroup => m_CanvasGroup;
        public RectTransform ModalContainer => m_ModalContainer;
        public Button PrimaryButton => m_PrimaryButton;
        public Button SecondaryButton => m_SecondaryButton;
        public Button CloseButton => m_CloseButton;

        protected virtual void Awake()
        {
            if (m_CanvasGroup == null) m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_ModalContainer == null) m_ModalContainer = transform as RectTransform;

            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.AddListener(() => Hide());
            }
        }

        public void Initialize(TweenRunner tweenRunner)
        {
            m_TweenRunner = tweenRunner;
        }

        public void SetContent(string title, string body = "")
        {
            if (m_TitleText != null) m_TitleText.text = title;
            if (m_BodyText != null) m_BodyText.text = body;
        }

        public virtual void Show(Action onComplete = null)
        {
            gameObject.SetActive(true);
            m_IsOpen = true;

            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.blocksRaycasts = true;
                m_CanvasGroup.interactable = true;
            }

            if (m_TweenRunner != null)
            {
                m_TweenRunner.CancelByOwner(this);

                // Alpha 0 -> 1 over 0.32s
                var alphaTween = new Tween
                {
                    From = m_CanvasGroup != null ? m_CanvasGroup.alpha : 0f,
                    To = 1f,
                    Duration = 0.32f,
                    Ease = Easing.OutCubic,
                    Owner = this,
                    OnUpdate = val => { if (m_CanvasGroup != null) m_CanvasGroup.alpha = val; },
                    OnComplete = onComplete
                };
                m_TweenRunner.Play(in alphaTween);

                // Scale 0.92 -> 1.0 over 0.32s with OutBack
                if (m_ModalContainer != null)
                {
                    var scaleTween = new Tween
                    {
                        From = 0.92f,
                        To = 1.0f,
                        Duration = 0.32f,
                        Ease = Easing.OutBack,
                        Owner = this,
                        OnUpdate = val => m_ModalContainer.localScale = new Vector3(val, val, 1.0f)
                    };
                    m_TweenRunner.Play(in scaleTween);
                }
            }
            else
            {
                if (m_CanvasGroup != null) m_CanvasGroup.alpha = 1.0f;
                if (m_ModalContainer != null) m_ModalContainer.localScale = Vector3.one;
                onComplete?.Invoke();
            }
        }

        public virtual void Hide(Action onComplete = null)
        {
            m_IsOpen = false;

            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.blocksRaycasts = false;
                m_CanvasGroup.interactable = false;
            }

            if (m_TweenRunner != null)
            {
                m_TweenRunner.CancelByOwner(this);

                var alphaTween = new Tween
                {
                    From = m_CanvasGroup != null ? m_CanvasGroup.alpha : 1f,
                    To = 0f,
                    Duration = 0.18f,
                    Ease = Easing.OutCubic,
                    Owner = this,
                    OnUpdate = val => { if (m_CanvasGroup != null) m_CanvasGroup.alpha = val; },
                    OnComplete = () =>
                    {
                        gameObject.SetActive(false);
                        onComplete?.Invoke();
                    }
                };
                m_TweenRunner.Play(in alphaTween);

                if (m_ModalContainer != null)
                {
                    var scaleTween = new Tween
                    {
                        From = 1.0f,
                        To = 0.92f,
                        Duration = 0.18f,
                        Ease = Easing.InExpo,
                        Owner = this,
                        OnUpdate = val => m_ModalContainer.localScale = new Vector3(val, val, 1.0f)
                    };
                    m_TweenRunner.Play(in scaleTween);
                }
            }
            else
            {
                if (m_CanvasGroup != null) m_CanvasGroup.alpha = 0f;
                gameObject.SetActive(false);
                onComplete?.Invoke();
            }
        }
    }
}

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
        private bool m_HasResponsiveBaseline;
        private Vector2 m_ModalSize;
        private Vector2 m_TitlePosition;
        private Vector2 m_TitleSize;
        private float m_TitleFontSize;
        private Vector2 m_BodyPosition;
        private Vector2 m_BodySize;
        private float m_BodyFontSize;
        private Vector2 m_PrimaryPosition;
        private Vector2 m_PrimarySize;
        private Vector2 m_SecondaryPosition;
        private Vector2 m_SecondarySize;
        private Vector2 m_ClosePosition;
        private Vector2 m_CloseSize;

        public bool IsOpen => m_IsOpen;
        public CanvasGroup CanvasGroup => m_CanvasGroup;
        public RectTransform ModalContainer => m_ModalContainer;
        public Button PrimaryButton => m_PrimaryButton;
        public Button SecondaryButton => m_SecondaryButton;
        public Button CloseButton => m_CloseButton;
        public virtual bool UsesFullLayoutHeight => false;
        public event Action<PopupView> OnCloseRequested;

        protected virtual void Awake()
        {
            if (m_CanvasGroup == null) m_CanvasGroup = GetComponent<CanvasGroup>();
            if (m_ModalContainer == null) m_ModalContainer = transform as RectTransform;
            CacheResponsiveBaseline();

            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.AddListener(HandleCloseRequested);
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_CloseButton != null)
            {
                m_CloseButton.onClick.RemoveListener(HandleCloseRequested);
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

        /// <summary>
        /// Constrains the modal to the responsive HUD column and keeps its content readable
        /// when a short window or a landscape editor view leaves little vertical space.
        /// </summary>
        public void ApplyResponsiveLayout(float layoutWidth, float middleHeight)
        {
            CacheResponsiveBaseline();
            if (!m_HasResponsiveBaseline || m_ModalContainer == null)
            {
                return;
            }

            float width = Mathf.Max(0f, Mathf.Min(m_ModalSize.x, layoutWidth - 96f));
            float height = Mathf.Max(0f, Mathf.Min(m_ModalSize.y, middleHeight - 96f));

            // Collapsing the modal to a zero extent inverts its masked children and removes the
            // popup from the screen entirely. Keep the authored size when the host has no room.
            if (width <= 0f || height <= 0f)
            {
                width = m_ModalSize.x;
                height = m_ModalSize.y;
            }

            float scale = Mathf.Min(
                1f,
                width / m_ModalSize.x,
                height / m_ModalSize.y);

            m_ModalContainer.anchorMin = new Vector2(0.5f, 0.5f);
            m_ModalContainer.anchorMax = new Vector2(0.5f, 0.5f);
            m_ModalContainer.pivot = new Vector2(0.5f, 0.5f);
            m_ModalContainer.sizeDelta = new Vector2(width, height);

            ApplyScaledRect(m_TitleText != null ? m_TitleText.rectTransform : null, m_TitlePosition, m_TitleSize, scale);
            ApplyScaledRect(m_BodyText != null ? m_BodyText.rectTransform : null, m_BodyPosition, m_BodySize, scale);
            ApplyScaledRect(m_PrimaryButton != null ? m_PrimaryButton.GetComponent<RectTransform>() : null, m_PrimaryPosition, m_PrimarySize, scale);
            ApplyScaledRect(m_SecondaryButton != null ? m_SecondaryButton.GetComponent<RectTransform>() : null, m_SecondaryPosition, m_SecondarySize, scale);
            ApplyScaledRect(m_CloseButton != null ? m_CloseButton.GetComponent<RectTransform>() : null, m_ClosePosition, m_CloseSize, scale);

            if (m_TitleText != null)
            {
                m_TitleText.fontSize = m_TitleFontSize * scale;
            }

            if (m_BodyText != null)
            {
                m_BodyText.enableAutoSizing = true;
                m_BodyText.fontSizeMin = Mathf.Min(12f, m_BodyFontSize * scale);
                m_BodyText.fontSizeMax = Mathf.Max(m_BodyText.fontSizeMin, m_BodyFontSize * scale);
                m_BodyText.fontSize = m_BodyFontSize * scale;
            }
        }

        private void CacheResponsiveBaseline()
        {
            if (m_HasResponsiveBaseline)
            {
                return;
            }

            if (m_ModalContainer == null) m_ModalContainer = transform as RectTransform;
            if (m_ModalContainer == null)
            {
                return;
            }

            m_ModalSize = ResolveModalSize(m_ModalContainer);
            if (m_ModalSize.x <= 0f || m_ModalSize.y <= 0f)
            {
                return;
            }

            CacheTextRect(m_TitleText, out m_TitlePosition, out m_TitleSize, out m_TitleFontSize);
            CacheTextRect(m_BodyText, out m_BodyPosition, out m_BodySize, out m_BodyFontSize);
            CacheButtonRect(m_PrimaryButton, out m_PrimaryPosition, out m_PrimarySize);
            CacheButtonRect(m_SecondaryButton, out m_SecondaryPosition, out m_SecondarySize);
            CacheButtonRect(m_CloseButton, out m_ClosePosition, out m_CloseSize);
            m_HasResponsiveBaseline = true;
        }

        /// <summary>
        /// Returns the authored modal footprint. A container stretched by its parent reports a
        /// zero or negative sizeDelta, so the laid-out rect is used instead of collapsing the modal.
        /// </summary>
        private static Vector2 ResolveModalSize(RectTransform container)
        {
            Vector2 size = container.sizeDelta;
            if (size.x <= 0f || size.y <= 0f)
            {
                size = container.rect.size;
            }

            return size;
        }

        private static void CacheTextRect(TMP_Text text, out Vector2 position, out Vector2 size, out float fontSize)
        {
            if (text == null)
            {
                position = Vector2.zero;
                size = Vector2.zero;
                fontSize = 0f;
                return;
            }

            position = text.rectTransform.anchoredPosition;
            size = text.rectTransform.sizeDelta;
            fontSize = text.fontSize;
        }

        private static void CacheButtonRect(Button button, out Vector2 position, out Vector2 size)
        {
            RectTransform rect = button != null ? button.GetComponent<RectTransform>() : null;
            if (rect == null)
            {
                position = Vector2.zero;
                size = Vector2.zero;
                return;
            }

            position = rect.anchoredPosition;
            size = rect.sizeDelta;
        }

        private static void ApplyScaledRect(RectTransform rect, Vector2 position, Vector2 size, float scale)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchoredPosition = position * scale;
            rect.sizeDelta = size * scale;
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

        private void HandleCloseRequested()
        {
            OnCloseRequested?.Invoke(this);
        }
    }
}

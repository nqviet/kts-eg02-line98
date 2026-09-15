using System;
using System.Collections;
using Line98.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Line98.Presentation
{
    /// <summary>Themeable spring toggle for tactile mobile settings rows.</summary>
    [DisallowMultipleComponent]
    public sealed class UiToggle : MonoBehaviour, IPointerClickHandler
    {
        [Header("Components")]
        [SerializeField] private UnityEngine.UI.Image m_TrackImage;
        [SerializeField] private UnityEngine.UI.Image m_ThumbImage;
        [SerializeField] private RectTransform m_ThumbTransform;

        [Header("Assets & Motion")]
        [SerializeField] private Sprite m_TrackOnSprite;
        [SerializeField] private Sprite m_TrackOffSprite;
        [SerializeField] private Sprite m_ThumbSprite;
        [SerializeField] private float m_ThumbTravelDistance = 44f;
        [SerializeField] private float m_TransitionDuration = 0.22f;

        [Header("State")]
        [SerializeField] private bool m_IsOn = true;

        public event Action<bool> OnToggled;
        public bool IsOn => m_IsOn;

        private void Awake()
        {
            UpdateVisuals(animate: false);
        }

        public void SetState(bool isOn, bool notify = true, bool animate = true)
        {
            if (m_IsOn == isOn)
            {
                UpdateVisuals(animate: false);
                return;
            }

            m_IsOn = isOn;
            UpdateVisuals(animate);
            if (notify) OnToggled?.Invoke(m_IsOn);
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null) return;

            if (theme.ToggleTrackOnSprite != null) m_TrackOnSprite = theme.ToggleTrackOnSprite;
            if (theme.ToggleTrackOffSprite != null) m_TrackOffSprite = theme.ToggleTrackOffSprite;
            if (theme.ToggleThumbSprite != null) m_ThumbSprite = theme.ToggleThumbSprite;
            UpdateVisuals(animate: false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SetState(!m_IsOn, notify: true, animate: true);
        }

        private void UpdateVisuals(bool animate)
        {
            if (m_TrackImage != null)
            {
                m_TrackImage.sprite = m_IsOn ? m_TrackOnSprite : m_TrackOffSprite;
            }

            if (m_ThumbImage != null && m_ThumbSprite != null)
            {
                m_ThumbImage.sprite = m_ThumbSprite;
            }

            if (m_ThumbTransform == null) return;

            Vector2 targetPosition = new Vector2(m_IsOn ? m_ThumbTravelDistance * 0.5f : -m_ThumbTravelDistance * 0.5f, 0f);
            if (!animate || !Application.isPlaying)
            {
                StopAllCoroutines();
                m_ThumbTransform.anchoredPosition = targetPosition;
                return;
            }

            StopAllCoroutines();
            StartCoroutine(AnimateThumbRoutine(targetPosition));
        }

        private IEnumerator AnimateThumbRoutine(Vector2 targetPosition)
        {
            Vector2 startPosition = m_ThumbTransform.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < m_TransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / m_TransitionDuration);
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float eased = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                m_ThumbTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, eased);
                yield return null;
            }

            m_ThumbTransform.anchoredPosition = targetPosition;
        }
    }
}

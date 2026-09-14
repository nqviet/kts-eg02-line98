using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Delivers tactile neumorphic button feedback matching Animation plan #31:
    /// 80 ms press (scale -> 0.96) and 140 ms OutBack release + click SFX.
    /// Integrated with TweenRunner and CancelByOwner for pool-safe execution.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiButtonFx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private float m_PressedScale = 0.96f;
        [SerializeField] private float m_PressDuration = 0.08f;
        [SerializeField] private float m_ReleaseDuration = 0.14f;
        [SerializeField] private AudioClip m_ClickSfx;

        private RectTransform m_RectTransform;
        private TweenRunner m_TweenRunner;
        private float m_CurrentScale = 1.0f;
        private AudioSource m_AudioSource;

        private static Line98.Presentation.Audio.AudioService s_AudioService;
        public static void SetAudioService(Line98.Presentation.Audio.AudioService audioService) => s_AudioService = audioService;
        public static Line98.Presentation.Audio.AudioService AudioService => s_AudioService;

        public event Action OnClicked;

        private void Awake()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_AudioSource = GetComponent<AudioSource>();
        }

        public void Initialize(TweenRunner tweenRunner, AudioClip clickSfx = null)
        {
            m_TweenRunner = tweenRunner;
            if (clickSfx != null)
            {
                m_ClickSfx = clickSfx;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            CancelExistingTween();

            if (m_TweenRunner != null)
            {
                var tween = new Tween
                {
                    From = m_CurrentScale,
                    To = m_PressedScale,
                    Duration = m_PressDuration,
                    Ease = Easing.OutCubic,
                    Owner = this,
                    OnUpdate = val => ApplyScale(val)
                };
                m_TweenRunner.Play(in tween);
            }
            else
            {
                ApplyScale(m_PressedScale);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            CancelExistingTween();

            if (m_TweenRunner != null)
            {
                var tween = new Tween
                {
                    From = m_CurrentScale,
                    To = 1.0f,
                    Duration = m_ReleaseDuration,
                    Ease = Easing.OutBack,
                    Owner = this,
                    OnUpdate = val => ApplyScale(val)
                };
                m_TweenRunner.Play(in tween);
            }
            else
            {
                ApplyScale(1.0f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PlayClickSound();
            OnClicked?.Invoke();
        }

        private void ApplyScale(float scale)
        {
            m_CurrentScale = scale;
            if (m_RectTransform != null)
            {
                m_RectTransform.localScale = new Vector3(scale, scale, 1.0f);
            }
        }

        private void PlayClickSound()
        {
            if (s_AudioService != null)
            {
                if (m_ClickSfx != null)
                {
                    s_AudioService.PlaySfx(m_ClickSfx, Line98.Data.AudioBusType.UI);
                }
                else
                {
                    s_AudioService.PlaySfx("sfx_ui_button_click");
                }
                return;
            }

            if (m_ClickSfx != null)
            {
                if (m_AudioSource != null)
                {
                    m_AudioSource.PlayOneShot(m_ClickSfx);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(m_ClickSfx, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
                }
            }
        }

        private void CancelExistingTween()
        {
            if (m_TweenRunner != null)
            {
                m_TweenRunner.CancelByOwner(this);
            }
        }

        private void OnDisable()
        {
            CancelExistingTween();
            ApplyScale(1.0f);
        }
    }
}

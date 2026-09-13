using UnityEngine;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Applies procedural additive screen shake on a dedicated child transform of CameraRig.
    /// Never touches the camera root or distance transform managed by BoardFitSolver.
    /// Uses closed-form damped sine oscillation aligned to the camera yaw axis.
    /// </summary>
    public sealed class CamShake : MonoBehaviour, ITickable
    {
        private float m_Amplitude;
        private float m_Duration;
        private float m_Frequency = 24f;
        private float m_Damping = 8f;
        private float m_Elapsed;
        private bool m_IsActive;
        private Vector3 m_BaseLocalPosition;

        private void Awake()
        {
            m_BaseLocalPosition = transform.localPosition;
        }

        public void TriggerShake(float amplitude, float duration = 0.35f, float frequency = 24f, float damping = 8f)
        {
            if (amplitude <= 0f || duration <= 0f) return;

            m_Amplitude = Mathf.Max(m_Amplitude, amplitude);
            m_Duration = duration;
            m_Frequency = frequency;
            m_Damping = damping;
            m_Elapsed = 0f;
            m_IsActive = true;
        }

        public void Tick(float dt)
        {
            if (!m_IsActive) return;

            m_Elapsed += dt;
            if (m_Elapsed >= m_Duration)
            {
                m_IsActive = false;
                m_Amplitude = 0f;
                transform.localPosition = m_BaseLocalPosition;
                return;
            }

            // Closed form damped sine: y(t) = A * e^(-damping * t) * cos(frequency * t * 2PI)
            float t = m_Elapsed;
            float decay = Mathf.Exp(-m_Damping * t);
            float oscillation = Mathf.Cos(m_Frequency * t * Mathf.PI * 2f);
            float displacement = m_Amplitude * decay * oscillation;

            // Horizontal camera shake (along local X)
            transform.localPosition = m_BaseLocalPosition + new Vector3(displacement, displacement * 0.2f, 0f);
        }

        public void ResetShake()
        {
            m_IsActive = false;
            m_Amplitude = 0f;
            transform.localPosition = m_BaseLocalPosition;
        }
    }
}

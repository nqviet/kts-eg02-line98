using System;
using TMPro;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Displays a 5-digit zero-padded number (D5 format) with smooth animated roll.
    /// Operates completely allocation-free via a pre-allocated char buffer and TMP_Text.SetCharArray.
    /// Supports both count-up (scoring) and count-down (undo).
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class RollingNumber : MonoBehaviour
    {
        [SerializeField] private TMP_Text m_Text;
        [SerializeField] private float m_Duration = 0.5f;

        private readonly char[] m_CharBuffer = new char[5];
        private int m_CurrentValue;
        private int m_TargetValue;
        private float m_StartValue;
        private float m_Elapsed;
        private bool m_IsRolling;

        public int CurrentValue => m_CurrentValue;
        public int TargetValue => m_TargetValue;

        private void Awake()
        {
            if (m_Text == null)
            {
                m_Text = GetComponent<TMP_Text>();
            }
            FormatD5(m_CurrentValue);
        }

        public void SetValue(int targetValue, bool animate = true)
        {
            if (targetValue < 0) targetValue = 0;
            if (targetValue > 99999) targetValue = 99999;

            m_TargetValue = targetValue;

            if (!animate || m_Duration <= 0f)
            {
                m_CurrentValue = targetValue;
                m_IsRolling = false;
                FormatD5(m_CurrentValue);
                return;
            }

            m_StartValue = m_CurrentValue;
            m_Elapsed = 0f;
            m_IsRolling = true;
        }

        public void Tick(float dt)
        {
            if (!m_IsRolling) return;

            m_Elapsed += dt;
            float t = Mathf.Clamp01(m_Elapsed / m_Duration);

            // Smooth cubic out interpolation
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            int interpolated = Mathf.RoundToInt(Mathf.Lerp(m_StartValue, m_TargetValue, ease));

            if (interpolated != m_CurrentValue)
            {
                m_CurrentValue = interpolated;
                FormatD5(m_CurrentValue);
            }

            if (t >= 1f)
            {
                m_CurrentValue = m_TargetValue;
                FormatD5(m_CurrentValue);
                m_IsRolling = false;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void FormatD5(int val)
        {
            if (val < 0) val = 0;
            if (val > 99999) val = 99999;

            m_CharBuffer[0] = (char)('0' + (val / 10000) % 10);
            m_CharBuffer[1] = (char)('0' + (val / 1000) % 10);
            m_CharBuffer[2] = (char)('0' + (val / 100) % 10);
            m_CharBuffer[3] = (char)('0' + (val / 10) % 10);
            m_CharBuffer[4] = (char)('0' + val % 10);

            if (m_Text != null)
            {
                m_Text.SetCharArray(m_CharBuffer, 0, 5);
            }
        }
    }
}

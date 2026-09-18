using System;
using UnityEngine;

namespace Line98.Presentation.Animation
{
    /// <summary>
    /// Pure mathematical easing and damped-oscillation functions with zero heap allocation.
    /// Implements GDD §8 "Soft Bounce" and Animation §2 closed-form curves.
    /// </summary>
    public static class Easing
    {
        public static float OutQuad(float t)
        {
            float f = 1.0f - Mathf.Clamp01(t);
            return 1.0f - f * f;
        }

        public static float OutCubic(float t)
        {
            float f = 1.0f - Mathf.Clamp01(t);
            return 1.0f - f * f * f;
        }

        public static float OutBack(float t)
        {
            return OutBack(t, 1.70158f);
        }

        public static float OutBack(float t, float s)
        {
            float f = Mathf.Clamp01(t) - 1.0f;
            return 1.0f + (s + 1.0f) * f * f * f + s * f * f;
        }

        /// <summary>Mirror of <see cref="OutBack(float)"/>: undershoots before accelerating away.</summary>
        public static float InBack(float t)
        {
            return 1f - OutBack(1f - t);
        }

        public static float InExpo(float t)
        {
            float clamped = Mathf.Clamp01(t);
            return clamped <= 0f ? 0f : Mathf.Pow(2f, 10f * (clamped - 1f));
        }

        public static float InOutQuad(float t)
        {
            float clamped = Mathf.Clamp01(t);
            return clamped < 0.5f ? 2.0f * clamped * clamped : 1.0f - Mathf.Pow(-2.0f * clamped + 2.0f, 2.0f) / 2.0f;
        }

        public static float OutElastic(float t)
        {
            float clamped = Mathf.Clamp01(t);
            if (clamped <= 0f) return 0f;
            if (clamped >= 1f) return 1f;

            const float c4 = (2f * Mathf.PI) / 3f;
            return Mathf.Pow(2f, -10f * clamped) * Mathf.Sin((clamped * 10f - 0.75f) * c4) + 1f;
        }

        /// <summary>
        /// Closed-form damped sine wave for soft landing bounce: y(t) = A * e^(-lambda * t) * cos(omega * t).
        /// </summary>
        public static float DampedSine(float t, float amplitude, float lambda, float omega)
        {
            return amplitude * Mathf.Exp(-lambda * t) * Mathf.Cos(omega * t);
        }

        /// <summary>
        /// Calculates volume-preserving squash and stretch from vertical displacement y:
        /// sy = 1 - k * max(0, -y) / amplitude, sx = sz = 1 / sqrt(sy).
        /// </summary>
        public static Vector3 VolumePreservingSquash(float y, float amplitude, float k = 0.35f)
        {
            float compression = Mathf.Max(0f, -y);
            float sy = 1.0f - (amplitude > 0.0001f ? (k * compression / amplitude) : 0f);
            sy = Mathf.Clamp(sy, 0.4f, 1.6f);
            float sXZ = 1.0f / Mathf.Sqrt(sy);
            return new Vector3(sXZ, sy, sXZ);
        }
    }
}

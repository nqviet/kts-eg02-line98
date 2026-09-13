using System;
using UnityEngine;

namespace Line98.Presentation.Animation
{
    public enum TimeSource : byte
    {
        Scaled,
        Unscaled
    }

    public struct Tween
    {
        public float From;
        public float To;
        public float Duration;
        public float Elapsed;
        public AnimationCurve Curve;
        public Func<float, float> Ease;
        public TimeSource Source;
        public object Owner;
        public int ActionId;
        public ITweenTarget Target;
        public Action<float> OnUpdate;
        public Action OnComplete;
        public bool IsActive;

        public void Reset()
        {
            From = 0f;
            To = 0f;
            Duration = 0f;
            Elapsed = 0f;
            Curve = null;
            Ease = null;
            Source = TimeSource.Scaled;
            Owner = null;
            ActionId = 0;
            Target = null;
            OnUpdate = null;
            OnComplete = null;
            IsActive = false;
        }
    }

    /// <summary>
    /// Centralized high-performance procedural tween runner.
    /// Uses pre-allocated struct slots and zero allocations per frame.
    /// Features CancelByOwner for bulletproof pooled object cleanup.
    /// </summary>
    public sealed class TweenRunner : ITickable
    {
        private const int InitialCapacity = 64;

        private Tween[] m_Tweens;
        private int m_ActiveCount;
        private int m_NextHandle = 1;
        private int[] m_Handles;

        public int ActiveCount => m_ActiveCount;

        public TweenRunner(int capacity = InitialCapacity)
        {
            m_Tweens = new Tween[capacity];
            m_Handles = new int[capacity];
        }

        public int Play(in Tween tween)
        {
            int slot = FindFreeSlot();
            if (slot < 0)
            {
                Grow();
                slot = FindFreeSlot();
            }

            int handle = m_NextHandle++;
            if (m_NextHandle <= 0) m_NextHandle = 1;

            m_Tweens[slot] = tween;
            m_Tweens[slot].IsActive = true;
            m_Tweens[slot].Elapsed = 0f;
            m_Handles[slot] = handle;
            m_ActiveCount++;

            // Initial tick at t=0
            float initialNormalized = tween.Duration > 0.0001f ? 0f : 1f;
            float evaluatedValue = Evaluate(m_Tweens[slot], initialNormalized);
            NotifyUpdate(slot, evaluatedValue);

            if (tween.Duration <= 0.0001f)
            {
                NotifyComplete(slot);
                m_Tweens[slot].Reset();
                m_Handles[slot] = 0;
                m_ActiveCount--;
                return 0;
            }

            return handle;
        }

        public void Cancel(int handle)
        {
            if (handle <= 0) return;

            for (int i = 0; i < m_Tweens.Length; i++)
            {
                if (m_Handles[i] == handle && m_Tweens[i].IsActive)
                {
                    m_Tweens[i].Reset();
                    m_Handles[i] = 0;
                    m_ActiveCount--;
                    return;
                }
            }
        }

        /// <summary>
        /// Cancels all active tweens tagged with the specified owner.
        /// Essential for pool recycling to prevent lingering tweens.
        /// </summary>
        public void CancelByOwner(object owner)
        {
            if (owner == null) return;

            for (int i = 0; i < m_Tweens.Length; i++)
            {
                if (m_Tweens[i].IsActive && ReferenceEquals(m_Tweens[i].Owner, owner))
                {
                    m_Tweens[i].Reset();
                    m_Handles[i] = 0;
                    m_ActiveCount--;
                }
            }
        }

        public void CancelAll()
        {
            for (int i = 0; i < m_Tweens.Length; i++)
            {
                if (m_Tweens[i].IsActive)
                {
                    m_Tweens[i].Reset();
                    m_Handles[i] = 0;
                }
            }
            m_ActiveCount = 0;
        }

        public void Tick(float dt)
        {
            if (m_ActiveCount == 0) return;

            float unscaledDt = Time.unscaledDeltaTime;

            for (int i = 0; i < m_Tweens.Length; i++)
            {
                if (!m_Tweens[i].IsActive) continue;

                float stepDt = m_Tweens[i].Source == TimeSource.Scaled ? dt : unscaledDt;
                m_Tweens[i].Elapsed += stepDt;

                float duration = m_Tweens[i].Duration;
                bool isFinished = m_Tweens[i].Elapsed >= duration;
                float normalizedTime = duration > 0.0001f ? Mathf.Clamp01(m_Tweens[i].Elapsed / duration) : 1f;

                float evaluatedValue = Evaluate(m_Tweens[i], normalizedTime);
                NotifyUpdate(i, evaluatedValue);

                if (isFinished)
                {
                    NotifyComplete(i);
                    m_Tweens[i].Reset();
                    m_Handles[i] = 0;
                    m_ActiveCount--;
                }
            }
        }

        private static float Evaluate(in Tween tween, float normalizedTime)
        {
            float easedT = normalizedTime;

            if (tween.Curve != null)
            {
                easedT = tween.Curve.Evaluate(normalizedTime);
            }
            else if (tween.Ease != null)
            {
                easedT = tween.Ease(normalizedTime);
            }

            return Mathf.LerpUnclamped(tween.From, tween.To, easedT);
        }

        private void NotifyUpdate(int index, float value)
        {
            if (m_Tweens[index].Target != null)
            {
                m_Tweens[index].Target.OnTweenUpdate(m_Tweens[index].ActionId, value);
            }
            m_Tweens[index].OnUpdate?.Invoke(value);
        }

        private void NotifyComplete(int index)
        {
            if (m_Tweens[index].Target != null)
            {
                m_Tweens[index].Target.OnTweenComplete(m_Tweens[index].ActionId);
            }
            m_Tweens[index].OnComplete?.Invoke();
        }

        private int FindFreeSlot()
        {
            for (int i = 0; i < m_Tweens.Length; i++)
            {
                if (!m_Tweens[i].IsActive) return i;
            }
            return -1;
        }

        private void Grow()
        {
            int newCap = m_Tweens.Length * 2;
            Array.Resize(ref m_Tweens, newCap);
            Array.Resize(ref m_Handles, newCap);
        }
    }
}

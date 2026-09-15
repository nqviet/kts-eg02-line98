using System;
using UnityEngine;
using Line98.Core;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Visual presentation instance for an individual ball.
    /// Follows the rigid hierarchy:
    ///   Ball_Root (floor pivot y=0)
    ///     ├── Ball_Visual (local 0, restHeight, 0)
    ///     ├── GlowShell (local 0, restHeight, 0, scale 1.02, additive)
    ///     └── BlobShadow (quad on floor y=0.005)
    /// No colliders, no PhysX, no Animator.
    /// Implements ITweenTarget for 0-heap allocation procedural tweens.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BallView : MonoBehaviour, ITweenTarget
    {
        private static readonly Func<float, float> s_GlowPopEase = Easing.OutBack;
        private static readonly Func<float, float> s_GlowOutEase = Easing.OutCubic;

        public const int ActionHop = 1;
        public const int ActionSquash = 2;
        public const int ActionGlow = 3;
        public const int ActionFlight = 4;
        public const int ActionScaleIn = 5;
        public const int ActionScaleOut = 6;
        public const int ActionShake = 7;
        public const int ActionGlowBreathe = 8;
        public const int ActionGlowOut = 9;

        [SerializeField] private Transform m_Visual;
        [SerializeField] private Transform m_GlowShell;
        [SerializeField] private Transform m_BlobShadow;
        [SerializeField] private float m_GlowShellScale = 1.10f;
        [SerializeField] private float m_GlowCollapsedScale = 0.80f;
        [SerializeField] private float m_GlowPopInDuration = 0.12f;
        [SerializeField] private float m_GlowBreathePeriod = 0.90f;
        [SerializeField] private float m_GlowBreatheAmplitude = 0.05f;
        [SerializeField] private float m_GlowOutDuration = 0.09f;

        private MeshRenderer m_VisualRenderer;
        private MeshRenderer m_GlowRenderer;
        private MeshRenderer m_ShadowRenderer;

        private GridPos m_GridPos;
        private BallColor m_Color;
        private float m_Pitch = 1.0f;
        private float m_RestHeight = 0.30f;
        private bool m_IsSelected;
        private TweenRunner m_TweenRunner;
        private int m_GlowTweenHandle;

        public GridPos Position => m_GridPos;
        public BallColor Color => m_Color;
        public bool IsSelected => m_IsSelected;
        public float RestHeight => m_RestHeight;
        public Transform Visual => m_Visual;
        public float GlowShellScale
        {
            get => m_GlowShellScale;
            set
            {
                m_GlowShellScale = value;
                SetGlowShellScale(m_GlowShellScale);
            }
        }

        public void InitializeHierarchy(Mesh ballMesh, Mesh shadowMesh, Material glowMaterial, Material shadowMaterial)
        {
            if (m_Visual == null)
            {
                var visGo = new GameObject("Ball_Visual");
                visGo.transform.SetParent(transform, false);
                m_Visual = visGo.transform;
                var mf = visGo.AddComponent<MeshFilter>();
                mf.sharedMesh = ballMesh;
                m_VisualRenderer = visGo.AddComponent<MeshRenderer>();
            }
            else
            {
                m_VisualRenderer = m_Visual.GetComponent<MeshRenderer>();
            }

            if (m_GlowShell == null && glowMaterial != null)
            {
                var glowGo = new GameObject("GlowShell");
                glowGo.transform.SetParent(transform, false);
                m_GlowShell = glowGo.transform;
                SetGlowShellScale(m_GlowShellScale);
                var mf = glowGo.AddComponent<MeshFilter>();
                mf.sharedMesh = ballMesh;
                m_GlowRenderer = glowGo.AddComponent<MeshRenderer>();
                m_GlowRenderer.sharedMaterial = glowMaterial;
                glowGo.SetActive(false);
            }
            else if (m_GlowShell != null)
            {
                SetGlowShellScale(m_GlowShellScale);
                m_GlowRenderer = m_GlowShell.GetComponent<MeshRenderer>();
            }

            if (m_BlobShadow == null && shadowMaterial != null)
            {
                var shadowGo = new GameObject("BlobShadow");
                shadowGo.transform.SetParent(transform, false);
                shadowGo.transform.localPosition = new Vector3(0f, 0.005f, 0f);
                shadowGo.transform.localRotation = Quaternion.identity;
                m_BlobShadow = shadowGo.transform;
                var mf = shadowGo.AddComponent<MeshFilter>();
                mf.sharedMesh = shadowMesh;
                m_ShadowRenderer = shadowGo.AddComponent<MeshRenderer>();
                m_ShadowRenderer.sharedMaterial = shadowMaterial;
            }
            else if (m_BlobShadow != null)
            {
                m_ShadowRenderer = m_BlobShadow.GetComponent<MeshRenderer>();
            }

            UpdatePitch(1.0f);
        }

        public void UpdatePitch(float pitch)
        {
            m_Pitch = pitch;
            m_RestHeight = 0.30f * m_Pitch;
            if (m_Visual != null)
            {
                m_Visual.localPosition = new Vector3(0f, m_RestHeight, 0f);
            }
            if (m_GlowShell != null)
            {
                m_GlowShell.localPosition = new Vector3(0f, m_RestHeight, 0f);
            }
        }

        public void Setup(GridPos pos, BallColor color, Material ballMaterial, TweenRunner runner)
        {
            Setup(pos, color, ballMaterial, null, runner);
        }

        public void Setup(GridPos pos, BallColor color, Material ballMaterial, Material glowMaterial, TweenRunner runner)
        {
            m_GridPos = pos;
            m_Color = color;
            m_TweenRunner = runner;
            m_IsSelected = false;

            if (m_VisualRenderer != null && ballMaterial != null)
            {
                m_VisualRenderer.sharedMaterial = ballMaterial;
            }

            if (m_GlowRenderer == null && m_GlowShell != null)
            {
                m_GlowRenderer = m_GlowShell.GetComponent<MeshRenderer>();
            }

            if (m_GlowRenderer != null && glowMaterial != null)
            {
                m_GlowRenderer.sharedMaterial = glowMaterial;
            }

            ResetVisuals();
        }

        public void ApplyMaterials(Material ball, Material glow)
        {
            if (m_VisualRenderer == null && m_Visual != null)
            {
                m_VisualRenderer = m_Visual.GetComponent<MeshRenderer>();
            }
            if (m_VisualRenderer != null && ball != null)
            {
                m_VisualRenderer.sharedMaterial = ball;
            }

            if (m_GlowRenderer == null && m_GlowShell != null)
            {
                m_GlowRenderer = m_GlowShell.GetComponent<MeshRenderer>();
            }
            if (m_GlowRenderer != null && glow != null)
            {
                m_GlowRenderer.sharedMaterial = glow;
            }
        }

        public void ApplyMesh(Mesh mesh)
        {
            if (mesh == null) return;

            if (m_Visual != null)
            {
                var mf = m_Visual.GetComponent<MeshFilter>();
                if (mf != null) mf.sharedMesh = mesh;
            }

            if (m_GlowShell != null)
            {
                var mf = m_GlowShell.GetComponent<MeshFilter>();
                if (mf != null) mf.sharedMesh = mesh;
            }

            UpdatePitch(m_Pitch);
        }

        public void SetSelected(bool selected)
        {
            m_IsSelected = selected;
            if (selected)
            {
                ShowSelectionHalo();
            }
            else
            {
                CollapseSelectionHalo();
            }
        }

        public void SetGlow(bool active, float intensity = 1.0f)
        {
            CancelGlowTween();
            SetGlowShellScale(m_GlowShellScale * intensity);
            if (m_GlowShell != null)
            {
                m_GlowShell.gameObject.SetActive(active);
            }
        }

        private void ShowSelectionHalo()
        {
            if (m_GlowShell == null)
            {
                return;
            }

            m_GlowShell.gameObject.SetActive(true);
            CancelGlowTween();

            if (m_TweenRunner == null)
            {
                SetGlowShellScale(m_GlowShellScale);
                return;
            }

            PlayGlowPhase(ActionGlow, m_GlowCollapsedScale, 1f, m_GlowPopInDuration, s_GlowPopEase);
        }

        private void CollapseSelectionHalo()
        {
            if (m_GlowShell == null)
            {
                return;
            }

            CancelGlowTween();

            if (m_TweenRunner == null)
            {
                SetGlowShellScale(m_GlowShellScale);
                m_GlowShell.gameObject.SetActive(false);
                return;
            }

            float baseScale = Mathf.Max(0.001f, m_GlowShellScale);
            float currentMultiplier = m_GlowShell.localScale.x / baseScale;
            PlayGlowPhase(ActionGlowOut, currentMultiplier, m_GlowCollapsedScale, m_GlowOutDuration, s_GlowOutEase);
        }

        private void PlayGlowPhase(int actionId, float from, float to, float duration, Func<float, float> ease)
        {
            var tween = new Tween
            {
                From = from,
                To = to,
                Duration = duration,
                Ease = ease,
                Source = TimeSource.Scaled,
                Owner = this,
                ActionId = actionId,
                Target = this
            };

            int handle = m_TweenRunner.Play(in tween);
            if (handle > 0)
            {
                m_GlowTweenHandle = handle;
            }
        }

        private void CancelGlowTween()
        {
            if (m_GlowTweenHandle > 0 && m_TweenRunner != null)
            {
                m_TweenRunner.Cancel(m_GlowTweenHandle);
            }

            m_GlowTweenHandle = 0;
        }

        private void SetGlowShellScale(float scale)
        {
            if (m_GlowShell != null)
            {
                m_GlowShell.localScale = Vector3.one * scale;
            }
        }

        public void SetSquash(float sx, float sy, float sz)
        {
            transform.localScale = new Vector3(sx, sy, sz);
        }

        public void SetHeightLift(float liftY)
        {
            if (m_Visual != null)
            {
                m_Visual.localPosition = new Vector3(0f, m_RestHeight + liftY, 0f);
            }
            if (m_GlowShell != null)
            {
                m_GlowShell.localPosition = new Vector3(0f, m_RestHeight + liftY, 0f);
            }

            // Scale blob shadow inversely with lift
            if (m_BlobShadow != null)
            {
                float shadowScale = Mathf.Clamp(1f / (1f + liftY * 2f), 0.3f, 1f);
                m_BlobShadow.localScale = new Vector3(shadowScale, 1f, shadowScale);
            }
        }

        public void ResetVisuals()
        {
            CancelGlowTween();
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            SetHeightLift(0f);
            SetGlow(false);
            if (m_BlobShadow != null)
            {
                m_BlobShadow.localScale = Vector3.one;
                m_BlobShadow.gameObject.SetActive(true);
            }
        }

        public void Release()
        {
            if (m_TweenRunner != null)
            {
                m_TweenRunner.CancelByOwner(this);
            }
            ResetVisuals();
            gameObject.SetActive(false);
        }

        public void OnTweenUpdate(int actionId, float value)
        {
            switch (actionId)
            {
                case ActionHop:
                    SetHeightLift(value);
                    break;

                case ActionSquash:
                    // Volume-preserving squash: sy = value, sx = sz = 1 / sqrt(sy)
                    float sy = Mathf.Clamp(value, 0.4f, 1.8f);
                    float sHoriz = 1f / Mathf.Sqrt(sy);
                    SetSquash(sHoriz, sy, sHoriz);
                    break;

                case ActionScaleIn:
                case ActionScaleOut:
                    transform.localScale = Vector3.one * Mathf.Max(0.001f, value);
                    break;

                case ActionGlow:
                case ActionGlowOut:
                    SetGlowShellScale(m_GlowShellScale * value);
                    break;

                case ActionGlowBreathe:
                    float breathe = Mathf.Sin(value * Mathf.PI * 2f);
                    SetGlowShellScale(m_GlowShellScale * (1f + m_GlowBreatheAmplitude * breathe));
                    break;
            }
        }

        public void OnTweenComplete(int actionId)
        {
            switch (actionId)
            {
                case ActionHop:
                    SetHeightLift(0f);
                    break;

                case ActionSquash:
                    SetSquash(1f, 1f, 1f);
                    break;

                case ActionScaleOut:
                    Release();
                    break;

                case ActionGlow:
                    SetGlowShellScale(m_GlowShellScale);
                    if (m_IsSelected)
                    {
                        PlayGlowPhase(ActionGlowBreathe, 0f, 1f, m_GlowBreathePeriod, null);
                    }
                    break;

                case ActionGlowBreathe:
                    if (m_IsSelected)
                    {
                        PlayGlowPhase(ActionGlowBreathe, 0f, 1f, m_GlowBreathePeriod, null);
                    }
                    break;

                case ActionGlowOut:
                    m_GlowTweenHandle = 0;
                    SetGlowShellScale(m_GlowShellScale);
                    if (m_GlowShell != null)
                    {
                        m_GlowShell.gameObject.SetActive(false);
                    }
                    break;
            }
        }
    }
}

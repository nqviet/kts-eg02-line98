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
        public const int ActionHop = 1;
        public const int ActionSquash = 2;
        public const int ActionGlow = 3;
        public const int ActionFlight = 4;
        public const int ActionScaleIn = 5;
        public const int ActionScaleOut = 6;
        public const int ActionShake = 7;

        [SerializeField] private Transform m_Visual;
        [SerializeField] private Transform m_GlowShell;
        [SerializeField] private Transform m_BlobShadow;

        private MeshRenderer m_VisualRenderer;
        private MeshRenderer m_GlowRenderer;
        private MeshRenderer m_ShadowRenderer;

        private GridPos m_GridPos;
        private BallColor m_Color;
        private float m_Pitch = 1.0f;
        private float m_RestHeight = 0.30f;
        private bool m_IsSelected;
        private TweenRunner m_TweenRunner;

        public GridPos Position => m_GridPos;
        public BallColor Color => m_Color;
        public bool IsSelected => m_IsSelected;
        public float RestHeight => m_RestHeight;
        public Transform Visual => m_Visual;

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
                m_GlowShell.localScale = Vector3.one * 1.02f;
                var mf = glowGo.AddComponent<MeshFilter>();
                mf.sharedMesh = ballMesh;
                m_GlowRenderer = glowGo.AddComponent<MeshRenderer>();
                m_GlowRenderer.sharedMaterial = glowMaterial;
                glowGo.SetActive(false);
            }
            else if (m_GlowShell != null)
            {
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
            m_GridPos = pos;
            m_Color = color;
            m_TweenRunner = runner;
            m_IsSelected = false;

            if (m_VisualRenderer != null && ballMaterial != null)
            {
                m_VisualRenderer.sharedMaterial = ballMaterial;
            }

            ResetVisuals();
        }

        public void SetSelected(bool selected)
        {
            m_IsSelected = selected;
            SetGlow(selected);
        }

        public void SetGlow(bool active)
        {
            if (m_GlowShell != null)
            {
                m_GlowShell.gameObject.SetActive(active);
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
            }
        }
    }
}

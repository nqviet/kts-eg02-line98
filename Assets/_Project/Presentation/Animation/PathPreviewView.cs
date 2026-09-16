using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Core;

namespace Line98.Presentation.Animation
{
    /// <summary>
    /// Presentation component that renders a marching dotted path preview along BFS routes
    /// and displays a pulsing destination ripple ring.
    /// Shared by MovePacer (move preview), HintService (hint highlight), and Onboarding.
    /// Zero heap allocation during active playback.
    /// </summary>
    public sealed class PathPreviewView : ITickable
    {
        private readonly BoardView m_BoardView;
        private readonly Transform m_Root;
        private readonly GameObject m_LineObject;
        private readonly LineRenderer m_LineRenderer;
        private readonly GameObject m_DestRingObject;
        private readonly Transform m_DestRingTransform;
        private readonly Material m_DotMaterial;
        private readonly Material m_RingMaterial;
        private readonly Material m_CreatedMaterial;

        private bool m_IsVisible;
        private object m_CurrentOwner;
        private float m_HoldTimer;
        private float m_MarchOffset;
        private float m_RippleTimer;

        public bool IsVisible => m_IsVisible;
        public LineRenderer LineRenderer => m_LineRenderer;
        public GameObject DestRingObject => m_DestRingObject;

        public PathPreviewView(BoardView boardView, Material dotMaterial, Material ringMaterial, Transform root)
        {
            m_BoardView = boardView;
            m_Root = root != null ? root : (boardView != null ? boardView.transform : null);

            // Create fallback material if null
            if (dotMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader == null)
                {
                    shader = Shader.Find("Universal Render Pipeline/Unlit");
                }
                m_CreatedMaterial = shader != null ? new Material(shader) : null;
                if (m_CreatedMaterial != null)
                {
                    m_CreatedMaterial.color = new Color(0.15f, 0.85f, 1.0f, 0.85f);
                }
                m_DotMaterial = m_CreatedMaterial;
            }
            else
            {
                m_DotMaterial = dotMaterial;
            }

            m_RingMaterial = ringMaterial != null ? ringMaterial : m_DotMaterial;

            // Create LineRenderer object
            m_LineObject = new GameObject("PathPreview_Line");
            if (m_Root != null)
            {
                m_LineObject.transform.SetParent(m_Root, false);
            }
            m_LineRenderer = m_LineObject.AddComponent<LineRenderer>();
            m_LineRenderer.useWorldSpace = true;
            m_LineRenderer.startWidth = 0.10f;
            m_LineRenderer.endWidth = 0.10f;
            m_LineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_LineRenderer.receiveShadows = false;
            m_LineRenderer.textureMode = LineTextureMode.Tile;
            if (m_DotMaterial != null)
            {
                m_LineRenderer.sharedMaterial = m_DotMaterial;
            }
            m_LineRenderer.positionCount = 0;
            m_LineRenderer.enabled = false;

            // Create Destination Ring Quad
            m_DestRingObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            m_DestRingObject.name = "PathPreview_DestRing";
            // Remove collider immediately
            var col = m_DestRingObject.GetComponent<Collider>();
            if (col != null)
            {
                UnityEngine.Object.DestroyImmediate(col);
            }
            if (m_Root != null)
            {
                m_DestRingObject.transform.SetParent(m_Root, false);
            }
            m_DestRingTransform = m_DestRingObject.transform;
            m_DestRingTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            m_DestRingTransform.localScale = Vector3.one * 0.8f;

            var meshRenderer = m_DestRingObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null && m_RingMaterial != null)
            {
                meshRenderer.sharedMaterial = m_RingMaterial;
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }
            m_DestRingObject.SetActive(false);
        }

        public void ShowPath(List<GridPos> path, float holdSeconds, object owner)
        {
            if (path == null || path.Count < 2)
            {
                return;
            }

            m_CurrentOwner = owner;
            m_HoldTimer = holdSeconds;
            m_IsVisible = true;
            m_MarchOffset = 0f;
            m_RippleTimer = 0f;

            if (m_LineRenderer != null)
            {
                m_LineRenderer.positionCount = path.Count;
                for (int i = 0; i < path.Count; i++)
                {
                    Vector3 worldPos = m_BoardView != null
                        ? m_BoardView.GridToWorld(path[i]) + Vector3.up * 0.12f
                        : new Vector3(path[i].X, 0.12f, path[i].Y);
                    m_LineRenderer.SetPosition(i, worldPos);
                }
                m_LineRenderer.enabled = true;
            }

            if (m_DestRingObject != null)
            {
                GridPos target = path[path.Count - 1];
                Vector3 destPos = m_BoardView != null
                    ? m_BoardView.GridToWorld(target) + Vector3.up * 0.02f
                    : new Vector3(target.X, 0.02f, target.Y);
                m_DestRingTransform.position = destPos;
                m_DestRingTransform.localScale = Vector3.one * 0.8f;
                m_DestRingObject.SetActive(true);
            }
        }

        public void ShowHint(GridPos from, GridPos to, List<GridPos> path, float holdSeconds, object owner)
        {
            m_CurrentOwner = owner;
            m_HoldTimer = holdSeconds;
            m_IsVisible = true;
            m_MarchOffset = 0f;
            m_RippleTimer = 0f;

            if (path != null && path.Count >= 2)
            {
                ShowPath(path, holdSeconds, owner);
            }
            else
            {
                if (m_LineRenderer != null)
                {
                    m_LineRenderer.enabled = false;
                    m_LineRenderer.positionCount = 0;
                }

                if (m_DestRingObject != null)
                {
                    Vector3 destPos = m_BoardView != null
                        ? m_BoardView.GridToWorld(to) + Vector3.up * 0.02f
                        : new Vector3(to.X, 0.02f, to.Y);
                    m_DestRingTransform.position = destPos;
                    m_DestRingTransform.localScale = Vector3.one * 0.8f;
                    m_DestRingObject.SetActive(true);
                }
            }
        }

        public void Hide()
        {
            m_IsVisible = false;
            m_CurrentOwner = null;
            m_HoldTimer = 0f;

            if (m_LineRenderer != null)
            {
                m_LineRenderer.enabled = false;
                m_LineRenderer.positionCount = 0;
            }

            if (m_DestRingObject != null)
            {
                m_DestRingObject.SetActive(false);
            }
        }

        public void CancelByOwner(object owner)
        {
            if (m_CurrentOwner == owner)
            {
                Hide();
            }
        }

        public void Tick(float dt)
        {
            if (!m_IsVisible)
            {
                return;
            }

            // March dotted texture offset
            m_MarchOffset -= dt * 2.5f;
            if (m_DotMaterial != null && m_DotMaterial.HasProperty("_MainTex"))
            {
                m_DotMaterial.mainTextureOffset = new Vector2(m_MarchOffset, 0f);
            }

            // Pulse destination ring scale
            m_RippleTimer += dt * 6.0f;
            if (m_DestRingTransform != null)
            {
                float ripple = 0.8f + Mathf.Sin(m_RippleTimer) * 0.18f;
                m_DestRingTransform.localScale = new Vector3(ripple, ripple, 1f);
            }

            if (m_HoldTimer > 0f)
            {
                m_HoldTimer -= dt;
                if (m_HoldTimer <= 0f)
                {
                    Hide();
                }
            }
        }
    }
}

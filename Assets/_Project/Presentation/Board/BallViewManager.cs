using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Core;
using Line98.Presentation.Animation;

namespace Line98.Presentation
{
    /// <summary>
    /// Manages the pool and spatial registry of BallView instances.
    /// Pre-allocates up to 81 instances to guarantee 0 B heap allocation during gameplay.
    /// Maps the 7 canonical BallColors to shared materials without MaterialPropertyBlock.
    /// </summary>
    public sealed class BallViewManager
    {
        private readonly BallView[] m_ActiveGrid = new BallView[BoardModel.CellCount];
        private readonly List<BallView> m_Pool = new List<BallView>(BoardModel.CellCount);
        private readonly Material[] m_ColorMaterials = new Material[7];
        private Material m_GlowMaterial;
        private Material m_ShadowMaterial;
        private Mesh m_BallMesh;
        private Mesh m_ShadowMesh;
        private Transform m_Parent;
        private TweenRunner m_TweenRunner;
        private float m_Pitch = 1.0f;

        public float Pitch => m_Pitch;

        public void Initialize(
            Mesh ballMesh,
            Mesh shadowMesh,
            Material[] ballMaterials,
            Material glowMaterial,
            Material shadowMaterial,
            Transform parent,
            TweenRunner runner,
            float pitch = 1.0f)
        {
            m_BallMesh = ballMesh;
            m_ShadowMesh = shadowMesh;
            m_GlowMaterial = glowMaterial;
            m_ShadowMaterial = shadowMaterial;
            m_Parent = parent;
            m_TweenRunner = runner;
            m_Pitch = pitch;

            // Cache the 7 materials indexed by BallColor enum
            if (ballMaterials != null)
            {
                for (int i = 0; i < ballMaterials.Length && i < m_ColorMaterials.Length; i++)
                {
                    m_ColorMaterials[i] = ballMaterials[i];
                }
            }

            // Prewarm pool of 81 ball views
            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                var go = new GameObject($"Ball_Pooled_{i}");
                go.transform.SetParent(m_Parent, false);
                var view = go.AddComponent<BallView>();
                view.InitializeHierarchy(m_BallMesh, m_ShadowMesh, m_GlowMaterial, m_ShadowMaterial);
                view.UpdatePitch(m_Pitch);
                view.gameObject.SetActive(false);
                m_Pool.Add(view);
            }
        }

        public Material GetMaterial(BallColor color)
        {
            int index = (int)color;
            if (index >= 0 && index < m_ColorMaterials.Length)
            {
                return m_ColorMaterials[index];
            }
            return m_ColorMaterials[0];
        }

        public BallView GetBallAt(GridPos pos)
        {
            int idx = pos.Index;
            if (idx >= 0 && idx < m_ActiveGrid.Length)
            {
                return m_ActiveGrid[idx];
            }
            return null;
        }

        public BallView SpawnBall(GridPos pos, BallColor color, Vector3 worldFloorPos)
        {
            int idx = pos.Index;
            if (m_ActiveGrid[idx] != null)
            {
                DespawnBall(pos);
            }

            BallView view = null;
            if (m_Pool.Count > 0)
            {
                int lastIdx = m_Pool.Count - 1;
                view = m_Pool[lastIdx];
                m_Pool.RemoveAt(lastIdx);
            }
            else
            {
                var go = new GameObject($"Ball_Dynamic_{idx}");
                go.transform.SetParent(m_Parent, false);
                view = go.AddComponent<BallView>();
                view.InitializeHierarchy(m_BallMesh, m_ShadowMesh, m_GlowMaterial, m_ShadowMaterial);
                view.UpdatePitch(m_Pitch);
            }

            view.transform.position = worldFloorPos;
            view.gameObject.SetActive(true);
            view.Setup(pos, color, GetMaterial(color), m_TweenRunner);
            m_ActiveGrid[idx] = view;
            return view;
        }

        public void DespawnBall(GridPos pos)
        {
            int idx = pos.Index;
            BallView view = m_ActiveGrid[idx];
            if (view != null)
            {
                m_ActiveGrid[idx] = null;
                view.Release();
                m_Pool.Add(view);
            }
        }

        public void RelocateBall(GridPos from, GridPos to)
        {
            BallView view = m_ActiveGrid[from.Index];
            if (view != null)
            {
                m_ActiveGrid[from.Index] = null;
                m_ActiveGrid[to.Index] = view;
            }
        }

        public void SyncFromBoard(BoardModel board, BoardView boardView)
        {
            for (int i = 0; i < BoardModel.CellCount; i++)
            {
                GridPos pos = GridPos.FromIndex(i);
                if (!board.IsEmpty(pos))
                {
                    BallColor col = board.ColorAt(pos);
                    BallView current = m_ActiveGrid[i];
                    if (current == null)
                    {
                        SpawnBall(pos, col, boardView.GridToWorld(pos));
                    }
                    else if (current.Color != col)
                    {
                        current.Setup(pos, col, GetMaterial(col), m_TweenRunner);
                    }
                }
                else
                {
                    if (m_ActiveGrid[i] != null)
                    {
                        DespawnBall(pos);
                    }
                }
            }
        }

        public void ClearAll()
        {
            for (int i = 0; i < m_ActiveGrid.Length; i++)
            {
                if (m_ActiveGrid[i] != null)
                {
                    m_ActiveGrid[i].Release();
                    m_Pool.Add(m_ActiveGrid[i]);
                    m_ActiveGrid[i] = null;
                }
            }
        }
    }
}

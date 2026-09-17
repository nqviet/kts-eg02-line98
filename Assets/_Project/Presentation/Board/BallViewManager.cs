using System;
using System.Collections.Generic;
using UnityEngine;
using Line98.Core;
using Line98.Data;
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
        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_RimColorId = Shader.PropertyToID("_RimColor");

        private readonly BallView[] m_ActiveGrid = new BallView[BoardModel.CellCount];
        private readonly List<BallView> m_Pool = new List<BallView>(BoardModel.CellCount);
        private readonly Material[] m_ColorMaterials = new Material[7];
        private readonly Material[] m_GlowMaterials = new Material[7];
        private Material m_GlowMaterial;
        private Material m_ShadowMaterial;
        private Mesh m_BallMesh;
        private Mesh m_ShadowMesh;
        private Transform m_Parent;
        private TweenRunner m_TweenRunner;
        private float m_Pitch = 1.0f;
        private BallThemeSO m_BallTheme;

        public float Pitch => m_Pitch;
        public BallThemeSO BallTheme => m_BallTheme;

        public void Initialize(
            Mesh ballMesh,
            Mesh shadowMesh,
            Material[] ballMaterials,
            Material glowMaterial,
            Material shadowMaterial,
            Transform parent,
            TweenRunner runner,
            float pitch = 1.0f,
            BallThemeSO ballTheme = null)
        {
            m_BallMesh = ballMesh;
            m_ShadowMesh = shadowMesh;
            m_GlowMaterial = glowMaterial;
            m_ShadowMaterial = shadowMaterial;
            m_Parent = parent;
            m_TweenRunner = runner;
            m_Pitch = pitch;
            m_BallTheme = ballTheme;

            // Cache the 7 materials indexed by BallColor enum
            Array.Clear(m_ColorMaterials, 0, m_ColorMaterials.Length);
            if (ballMaterials != null)
            {
                for (int i = 0; i < ballMaterials.Length && i < m_ColorMaterials.Length; i++)
                {
                    m_ColorMaterials[i] = ballMaterials[i];
                }
            }

            // Derive 7 shared glow materials tinted from each ball's base color
            if (m_GlowMaterial != null)
            {
                for (int i = 0; i < m_ColorMaterials.Length; i++)
                {
                    var glowMat = new Material(m_GlowMaterial);
                    glowMat.name = $"{m_GlowMaterial.name}_Color_{i}";
                    if (m_ColorMaterials[i] != null && m_ColorMaterials[i].HasProperty(s_BaseColorId))
                    {
                        Color baseColor = m_ColorMaterials[i].GetColor(s_BaseColorId);
                        Color rimColor = Color.Lerp(baseColor, Color.white, 0.55f);
                        glowMat.SetColor(s_RimColorId, rimColor);
                    }
                    m_GlowMaterials[i] = glowMat;
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
            int index = (int)color - 1;
            if (index >= 0 && index < m_ColorMaterials.Length)
            {
                return m_ColorMaterials[index];
            }
            return m_ColorMaterials[0];
        }

        public Material GetGlowMaterial(BallColor color)
        {
            int index = (int)color - 1;
            if (index >= 0 && index < m_GlowMaterials.Length && m_GlowMaterials[index] != null)
            {
                return m_GlowMaterials[index];
            }
            return m_GlowMaterial;
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
            view.Setup(pos, color, GetMaterial(color), GetGlowMaterial(color), m_TweenRunner);
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
                        current.Setup(pos, col, GetMaterial(col), GetGlowMaterial(col), m_TweenRunner);
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

        public void ApplyTheme(BallThemeSO theme)
        {
            if (theme == null) return;
            if (theme == m_BallTheme && ThemeAssetsMatch(theme)) return;
            m_BallTheme = theme;

            if (theme.BallMesh != null)
            {
                m_BallMesh = theme.BallMesh;
            }

            // 1. Rebuild m_ColorMaterials
            Array.Clear(m_ColorMaterials, 0, m_ColorMaterials.Length);
            if (theme.BallMaterials != null)
            {
                for (int i = 0; i < theme.BallMaterials.Length && i < m_ColorMaterials.Length; i++)
                {
                    m_ColorMaterials[i] = theme.BallMaterials[i];
                }
            }

            // 2. Re-assert pattern strength on materials per D33
            float patternStrength = theme.PatternsOn ? 1.0f : 0.0f;
            for (int i = 0; i < m_ColorMaterials.Length; i++)
            {
                if (m_ColorMaterials[i] != null && m_ColorMaterials[i].HasProperty("_PatternStrength"))
                {
                    m_ColorMaterials[i].SetFloat("_PatternStrength", patternStrength);
                }
            }

            // 3. Destroy old derived glow materials before creating new ones to prevent leak
            for (int i = 0; i < m_GlowMaterials.Length; i++)
            {
                if (m_GlowMaterials[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(m_GlowMaterials[i]);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(m_GlowMaterials[i]);
                    }
                    m_GlowMaterials[i] = null;
                }
            }

            // 4. Create 7 new glow materials derived from new color materials
            if (m_GlowMaterial != null)
            {
                for (int i = 0; i < m_ColorMaterials.Length; i++)
                {
                    var glowMat = new Material(m_GlowMaterial);
                    glowMat.name = $"{m_GlowMaterial.name}_Color_{i}";
                    if (m_ColorMaterials[i] != null && m_ColorMaterials[i].HasProperty(s_BaseColorId))
                    {
                        Color baseColor = m_ColorMaterials[i].GetColor(s_BaseColorId);
                        Color rimColor = Color.Lerp(baseColor, Color.white, 0.55f);
                        glowMat.SetColor(s_RimColorId, rimColor);
                    }
                    m_GlowMaterials[i] = glowMat;
                }
            }

            // 5. Push new materials and mesh to all active balls
            for (int i = 0; i < m_ActiveGrid.Length; i++)
            {
                var ball = m_ActiveGrid[i];
                if (ball != null)
                {
                    if (theme.BallMesh != null)
                    {
                        ball.ApplyMesh(theme.BallMesh);
                    }
                    ball.ApplyMaterials(GetMaterial(ball.Color), GetGlowMaterial(ball.Color));
                }
            }

            // Also update pooled balls mesh
            if (theme.BallMesh != null)
            {
                for (int i = 0; i < m_Pool.Count; i++)
                {
                    if (m_Pool[i] != null)
                    {
                        m_Pool[i].ApplyMesh(theme.BallMesh);
                    }
                }
            }
        }

        private bool ThemeAssetsMatch(BallThemeSO theme)
        {
            if (theme.BallMesh != null && theme.BallMesh != m_BallMesh)
            {
                return false;
            }

            Material[] materials = theme.BallMaterials;
            if (materials == null)
            {
                return true;
            }

            for (int i = 0; i < m_ColorMaterials.Length; i++)
            {
                Material expected = i < materials.Length ? materials[i] : null;
                if (m_ColorMaterials[i] != expected)
                {
                    return false;
                }
            }

            return true;
        }

        public void Dispose()
        {
            for (int i = 0; i < m_GlowMaterials.Length; i++)
            {
                if (m_GlowMaterials[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(m_GlowMaterials[i]);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(m_GlowMaterials[i]);
                    }
                    m_GlowMaterials[i] = null;
                }
            }
        }
    }
}

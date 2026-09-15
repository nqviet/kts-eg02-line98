using System;
using UnityEngine;
using Line98.Core;
using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Visual projector for the 9x9 board. Does not hold gameplay state.
    /// Maps grid coordinates to world space and manages 81 cell visual instances
    /// and the board frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoardView : MonoBehaviour
    {
        public const int BoardDimension = 9;
        public const int TotalCells = 81;

        [SerializeField] private float m_CellPitch = 1.0f;
        [SerializeField] private BoardThemeSO m_Theme;
        [SerializeField] private Mesh m_CellMesh;
        [SerializeField] private Material m_CellMaterial;
        [SerializeField] private Mesh m_FrameMesh;
        [SerializeField] private Material m_FrameMaterial;

        private Transform m_CellsRoot;
        private Transform m_FrameRoot;
        private readonly Transform[] m_CellTransforms = new Transform[TotalCells];
        private bool m_IsInitialized;

        public float CellPitch => m_CellPitch;
        public float BoardExtent => BoardDimension * m_CellPitch;
        public float RowPitchScale => m_Theme != null ? m_Theme.RowPitchScale : 1f;
        public float BoardDepth => BoardExtent * RowPitchScale;
        public bool IsInitialized => m_IsInitialized;

        public void Initialize(BoardThemeSO theme = null, Mesh cellMesh = null, Material cellMat = null, Mesh frameMesh = null, Material frameMat = null)
        {
            if (theme != null) m_Theme = theme;
            if (cellMesh != null) m_CellMesh = cellMesh;
            if (cellMat != null) m_CellMaterial = cellMat;
            if (frameMesh != null) m_FrameMesh = frameMesh;
            if (frameMat != null) m_FrameMaterial = frameMat;

            BuildBoardVisuals();
            m_IsInitialized = true;
        }

        private void BuildBoardVisuals()
        {
            if (m_CellsRoot == null)
            {
                var go = new GameObject("Cells_Root");
                go.transform.SetParent(transform, false);
                m_CellsRoot = go.transform;
            }

            if (m_FrameRoot == null && m_FrameMesh != null)
            {
                var frameGo = new GameObject("Board_Frame");
                frameGo.transform.SetParent(transform, false);
                frameGo.transform.localPosition = Vector3.zero;
                frameGo.transform.localRotation = Quaternion.identity;

                var mf = frameGo.AddComponent<MeshFilter>();
                mf.sharedMesh = m_FrameMesh;
                var mr = frameGo.AddComponent<MeshRenderer>();
                mr.sharedMaterial = m_FrameMaterial;
                m_FrameRoot = frameGo.transform;
            }

            // Create 81 cell renderers
            for (int y = 0; y < BoardDimension; y++)
            {
                for (int x = 0; x < BoardDimension; x++)
                {
                    int index = y * BoardDimension + x;
                    GridPos pos = new GridPos(x, y);
                    Vector3 worldPos = GridToWorld(pos);

                    if (m_CellTransforms[index] == null)
                    {
                        GameObject cellGo = new GameObject($"Cell_{x}_{y}");
                        cellGo.transform.SetParent(m_CellsRoot, false);
                        cellGo.transform.position = worldPos;
                        cellGo.transform.rotation = Quaternion.identity;

                        if (m_CellMesh != null)
                        {
                            var mf = cellGo.AddComponent<MeshFilter>();
                            mf.sharedMesh = m_CellMesh;
                        }
                        if (m_CellMaterial != null)
                        {
                            var mr = cellGo.AddComponent<MeshRenderer>();
                            mr.sharedMaterial = m_CellMaterial;
                        }

                        m_CellTransforms[index] = cellGo.transform;
                    }
                    else
                    {
                        m_CellTransforms[index].position = worldPos;
                    }
                }
            }
        }

        public Vector3 GridToWorld(GridPos pos)
        {
            float worldX = (pos.X - 4) * m_CellPitch;
            float worldZ = (pos.Y - 4) * m_CellPitch * RowPitchScale;
            return transform.position + new Vector3(worldX, 0f, worldZ);
        }

        public bool WorldToGrid(Vector3 worldPos, out GridPos pos)
        {
            Vector3 localPos = worldPos - transform.position;
            float fx = localPos.x / m_CellPitch + 4f;
            float fz = localPos.z / (m_CellPitch * RowPitchScale) + 4f;

            int x = Mathf.RoundToInt(fx);
            int y = Mathf.RoundToInt(fz);

            if (x >= 0 && x < BoardDimension && y >= 0 && y < BoardDimension)
            {
                if (Mathf.Abs(fx - x) <= 0.5f && Mathf.Abs(fz - y) <= 0.5f)
                {
                    pos = new GridPos(x, y);
                    return true;
                }
            }

            pos = default;
            return false;
        }

        public Transform GetCellTransform(GridPos pos)
        {
            int idx = pos.Index;
            if (idx >= 0 && idx < TotalCells)
            {
                return m_CellTransforms[idx];
            }
            return null;
        }

        public void ApplyTheme(BoardThemeSO theme)
        {
            if (theme == null) return;
            m_Theme = theme;

            if (theme.BoardFrameMaterial != null)
            {
                m_FrameMaterial = theme.BoardFrameMaterial;
                if (m_FrameRoot != null)
                {
                    var mr = m_FrameRoot.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = m_FrameMaterial;
                }
            }

            if (theme.BoardCellMaterial != null)
            {
                m_CellMaterial = theme.BoardCellMaterial;
                for (int i = 0; i < m_CellTransforms.Length; i++)
                {
                    if (m_CellTransforms[i] != null)
                    {
                        var mr = m_CellTransforms[i].GetComponent<MeshRenderer>();
                        if (mr != null) mr.sharedMaterial = m_CellMaterial;
                    }
                }
            }
        }
    }
}

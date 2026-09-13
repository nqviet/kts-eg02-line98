using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "BoardTheme_Classic", menuName = "Line98/Definitions/Board Theme")]
    public class BoardThemeSO : ScriptableObject
    {
        [SerializeField] private GameObject m_BoardFramePrefab;
        [SerializeField] private GameObject m_BoardCellPrefab;
        [SerializeField] private Material m_BoardCellMaterial;
        [SerializeField] private Material m_BoardFrameMaterial;
        [SerializeField, Min(0.1f)] private float m_RowPitchScale = 1f;

        public GameObject BoardFramePrefab => m_BoardFramePrefab;
        public GameObject BoardCellPrefab => m_BoardCellPrefab;
        public Material BoardCellMaterial => m_BoardCellMaterial;
        public Material BoardFrameMaterial => m_BoardFrameMaterial;
        public float RowPitchScale => m_RowPitchScale;
    }
}

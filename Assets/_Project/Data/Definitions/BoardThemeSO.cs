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

        [Header("Theme Identity")]
        [SerializeField] private string m_ThemeId = "classic";
        [SerializeField] private string m_DisplayName = "Classic";
        [SerializeField] private Sprite m_Thumbnail;
        [SerializeField] private bool m_UnlockedByDefault = true;

        public GameObject BoardFramePrefab => m_BoardFramePrefab;
        public GameObject BoardCellPrefab => m_BoardCellPrefab;
        public Material BoardCellMaterial => m_BoardCellMaterial;
        public Material BoardFrameMaterial => m_BoardFrameMaterial;
        public float RowPitchScale => m_RowPitchScale;

        public string ThemeId => m_ThemeId;
        public string DisplayName => m_DisplayName;
        public Sprite Thumbnail => m_Thumbnail;
        public bool UnlockedByDefault => m_UnlockedByDefault;
    }
}

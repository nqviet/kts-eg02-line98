using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Line98/Data/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [SerializeField] private int m_BoardSize = 9;
        [SerializeField] private int m_SpawnCount = 3;
        [SerializeField] private int m_PreviewCount = 3;
        [SerializeField] private int m_FreeUndoCount = 3;
        [SerializeField] private int m_MaxRewardedUndos = 3;

        public int BoardSize => m_BoardSize;
        public int SpawnCount => m_SpawnCount;
        public int PreviewCount => m_PreviewCount;
        public int FreeUndoCount => m_FreeUndoCount;
        public int MaxRewardedUndos => m_MaxRewardedUndos;
    }
}

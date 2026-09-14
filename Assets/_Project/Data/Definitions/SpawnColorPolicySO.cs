using Line98.Core;
using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "SpawnColorPolicy", menuName = "Line98/Data/Spawn Color Policy")]
    public class SpawnColorPolicySO : ScriptableObject
    {
        [SerializeField] private int m_InitialActiveColors = 5;
        [SerializeField] private int m_LinesForSixColors = 10;
        [SerializeField] private int m_LinesForSevenColors = 25;
        [SerializeField] private int m_SpawnCount = 3;

        public int InitialActiveColors => m_InitialActiveColors;
        public int LinesForSixColors => m_LinesForSixColors;
        public int LinesForSevenColors => m_LinesForSevenColors;
        public int SpawnCount => m_SpawnCount;

        public SpawnRules ToRules()
        {
            return new SpawnRules(
                m_InitialActiveColors,
                m_LinesForSixColors,
                m_LinesForSevenColors,
                m_SpawnCount);
        }
    }
}

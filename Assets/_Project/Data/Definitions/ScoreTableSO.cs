using Line98.Core;
using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "ScoreTable", menuName = "Line98/Data/Score Table")]
    public class ScoreTableSO : ScriptableObject, IScoreConfig
    {
        [SerializeField] private int m_Score5 = 100;
        [SerializeField] private int m_Score6 = 180;
        [SerializeField] private int m_Score7 = 300;
        [SerializeField] private int m_Score8 = 500;
        [SerializeField] private int m_Score9 = 800;
        [SerializeField] private float m_ComboStep = 0.25f;
        [SerializeField] private float m_MaxComboMultiplier = 2.0f;

        public int Score5 => m_Score5;
        public int Score6 => m_Score6;
        public int Score7 => m_Score7;
        public int Score8 => m_Score8;
        public int Score9 => m_Score9;
        public float ComboStep => m_ComboStep;
        public float MaxComboMultiplier => m_MaxComboMultiplier;

        public ScoreRules ToRules()
        {
            return new ScoreRules(
                m_Score5,
                m_Score6,
                m_Score7,
                m_Score8,
                m_Score9,
                m_ComboStep,
                m_MaxComboMultiplier);
        }
    }
}

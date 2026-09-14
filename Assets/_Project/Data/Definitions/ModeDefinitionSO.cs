using UnityEngine;

namespace Line98.Data
{
    public enum GameModeType : byte
    {
        Classic,
        DailyChallenge,
        Zen
    }

    [CreateAssetMenu(fileName = "ModeDefinition", menuName = "Line98/Data/Mode Definition")]
    public class ModeDefinitionSO : ScriptableObject
    {
        [SerializeField] private GameModeType m_ModeType;
        [SerializeField] private string m_ModeId = "classic";
        [SerializeField] private string m_DisplayName = "Classic";
        [SerializeField] private bool m_ScoringEnabled = true;
        [SerializeField] private bool m_AdsEnabled = true;
        [SerializeField] private bool m_UndoAllowed = true;
        [SerializeField] private bool m_HintsAllowed = true;
        [SerializeField] private bool m_RecordsStatistics = true;

        public GameModeType ModeType => m_ModeType;
        public string ModeId => m_ModeId;
        public string DisplayName => m_DisplayName;
        public bool ScoringEnabled => m_ScoringEnabled;
        public bool AdsEnabled => m_AdsEnabled;
        public bool UndoAllowed => m_UndoAllowed;
        public bool HintsAllowed => m_HintsAllowed;
        public bool RecordsStatistics => m_RecordsStatistics;
    }
}

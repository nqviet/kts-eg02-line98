using UnityEngine;

namespace Line98.Data
{
    /// <summary>Named cosmetic theme: a bundle of per-category parts with optional single-level inheritance.</summary>
    [CreateAssetMenu(menuName = "Line98/Definitions/Theme", fileName = "Theme_Classic")]
    public sealed class ThemeDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string m_ThemeId = "classic";
        [SerializeField] private string m_DisplayName = "Classic";
        [SerializeField] private Sprite m_Thumbnail;
        [SerializeField] private bool m_UnlockedByDefault = true;

        [Header("Parts — null inherits from m_InheritsFrom")]
        [SerializeField] private ThemeDefinitionSO m_InheritsFrom;
        [SerializeField] private BallThemeSO m_BallTheme;
        [SerializeField] private BoardThemeSO m_BoardTheme;
        [SerializeField] private UiThemeSO m_UiTheme;
        [SerializeField] private ClearEffectSO m_ClearEffect;

        public string ThemeId => m_ThemeId;
        public string DisplayName => m_DisplayName;
        public Sprite Thumbnail => m_Thumbnail;
        public bool UnlockedByDefault => m_UnlockedByDefault;
        public ThemeDefinitionSO InheritsFrom => m_InheritsFrom;

        public BallThemeSO BallTheme => m_BallTheme != null ? m_BallTheme : m_InheritsFrom?.BallTheme;
        public BoardThemeSO BoardTheme => m_BoardTheme != null ? m_BoardTheme : m_InheritsFrom?.BoardTheme;
        public UiThemeSO UiTheme => m_UiTheme != null ? m_UiTheme : m_InheritsFrom?.UiTheme;
        public ClearEffectSO ClearEffect => m_ClearEffect != null ? m_ClearEffect : m_InheritsFrom?.ClearEffect;
    }
}

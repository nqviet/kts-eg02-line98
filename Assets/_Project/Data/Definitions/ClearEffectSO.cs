using UnityEngine;

namespace Line98.Data
{
    /// <summary>
    /// Configuration for board clear VFX ribbons and bursts per cosmetic theme.
    /// </summary>
    [CreateAssetMenu(menuName = "Line98/Definitions/Clear Effect", fileName = "ClearEffect_Classic")]
    public sealed class ClearEffectSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string m_ThemeId = "classic";
        [SerializeField] private string m_DisplayName = "Classic";
        [SerializeField] private Sprite m_Thumbnail;
        [SerializeField] private bool m_UnlockedByDefault = true;

        [Header("Visual Properties")]
        [SerializeField] private Color m_RibbonTint = new Color(1f, 1f, 1f, 0.85f); // BoardAnimator default
        [SerializeField] private Color m_RibbonTintBanner = new Color(1f, 0.84f, 0f, 0.95f); // Tier-4 gold
        [SerializeField] private Color m_BurstTint = Color.white;
        [SerializeField] private string m_ClearBurstVfxKey = "Vfx_ClearBurst";

        public string ThemeId => m_ThemeId;
        public string DisplayName => m_DisplayName;
        public Sprite Thumbnail => m_Thumbnail;
        public bool UnlockedByDefault => m_UnlockedByDefault;

        public Color RibbonTint => m_RibbonTint;
        public Color RibbonTintBanner => m_RibbonTintBanner;
        public Color BurstTint => m_BurstTint;
        public string ClearBurstVfxKey => m_ClearBurstVfxKey;
    }
}

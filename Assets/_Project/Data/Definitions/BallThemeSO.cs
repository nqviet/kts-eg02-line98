using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "BallTheme_Crystal", menuName = "Line98/Definitions/Ball Theme")]
    public class BallThemeSO : ScriptableObject
    {
        [SerializeField] private Material[] m_BallMaterials;
        [SerializeField] private Texture2D m_AccessibilityPatterns;
        [SerializeField] private Texture2D m_InnerRefractionMask;
        [SerializeField] private Material m_BlobShadowMaterial;
        [SerializeField] private bool m_PatternsOn = false;

        public Material[] BallMaterials => m_BallMaterials;
        public Texture2D AccessibilityPatterns => m_AccessibilityPatterns;
        public Texture2D InnerRefractionMask => m_InnerRefractionMask;
        public Material BlobShadowMaterial => m_BlobShadowMaterial;
        public bool PatternsOn
        {
            get => m_PatternsOn;
            set => m_PatternsOn = value;
        }
    }
}

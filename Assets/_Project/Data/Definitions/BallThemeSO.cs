using System;
using UnityEngine;

namespace Line98.Data
{
    [CreateAssetMenu(fileName = "BallTheme_Classic", menuName = "Line98/Definitions/Ball Theme")]
    public class BallThemeSO : ScriptableObject
    {
        [SerializeField] private Material[] m_BallMaterials;
        [SerializeField] private Texture2D m_AccessibilityPatterns;
        [SerializeField] private Texture2D m_InnerRefractionMask;
        [SerializeField] private Material m_BlobShadowMaterial;
        [SerializeField] private bool m_PatternsOn = false;

        [Header("Theme Identity & Assets")]
        [SerializeField] private string m_ThemeId = "classic";
        [SerializeField] private string m_DisplayName = "Classic";
        [SerializeField] private Sprite m_Thumbnail;
        [SerializeField] private bool m_UnlockedByDefault = true;
        [SerializeField] private Mesh m_BallMesh;
        [SerializeField] private Color[] m_PreviewTints = Array.Empty<Color>();

        public Material[] BallMaterials => m_BallMaterials;
        public Texture2D AccessibilityPatterns => m_AccessibilityPatterns;
        public Texture2D InnerRefractionMask => m_InnerRefractionMask;
        public Material BlobShadowMaterial => m_BlobShadowMaterial;
        public bool PatternsOn
        {
            get => m_PatternsOn;
            set => m_PatternsOn = value;
        }

        public string ThemeId => m_ThemeId;
        public string DisplayName => m_DisplayName;
        public Sprite Thumbnail => m_Thumbnail;
        public bool UnlockedByDefault => m_UnlockedByDefault;
        public Mesh BallMesh => m_BallMesh;
        public Color[] PreviewTints => m_PreviewTints;
    }
}

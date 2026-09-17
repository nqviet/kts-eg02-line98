using UnityEngine;

namespace Line98.Data
{
    public interface IBrandConfig
    {
        string PrivacyUrl { get; }
        string SupportUrl { get; }
        string SupportEmail { get; }
        string VersionLabel { get; }
    }

    [CreateAssetMenu(fileName = "BrandConfig", menuName = "Line98/Data/Brand Config")]
    public class BrandConfigSO : ScriptableObject, IBrandConfig
    {
        [SerializeField] private string m_PrivacyUrl = "https://line98game.com/privacy";
        [SerializeField] private string m_SupportUrl = "https://line98game.com/support";
        [SerializeField] private string m_SupportEmail = "mailto:support@line98game.com";
        [SerializeField] private string m_VersionLabelOverride = "";

        public string PrivacyUrl => m_PrivacyUrl;
        public string SupportUrl => m_SupportUrl;
        public string SupportEmail => m_SupportEmail;
        public string VersionLabel => string.IsNullOrWhiteSpace(m_VersionLabelOverride) ? Application.version : m_VersionLabelOverride;

        public void InitializeRuntime(string privacyUrl, string supportUrl, string supportEmail, string versionLabel = null)
        {
            m_PrivacyUrl = privacyUrl;
            m_SupportUrl = supportUrl;
            m_SupportEmail = supportEmail;
            m_VersionLabelOverride = versionLabel ?? "";
        }
    }
}

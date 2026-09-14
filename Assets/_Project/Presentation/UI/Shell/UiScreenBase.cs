using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Minimal lifecycle seam shared by screen composition prefabs.</summary>
    public abstract class UiScreenBase : MonoBehaviour
    {
        [SerializeField] private string m_ScreenId;

        public virtual string ScreenId => m_ScreenId;

        public virtual void Initialize(UiServices services)
        {
        }
    }
}

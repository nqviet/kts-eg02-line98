using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Minimal lifecycle seam shared by screen composition prefabs.</summary>
    public abstract class UiScreenBase : MonoBehaviour
    {
        public virtual void Initialize(UiServices services)
        {
        }
    }
}

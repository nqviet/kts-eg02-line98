using Line98.Core;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>Prefab-facing preview tray adapter. The legacy view remains the queue renderer.</summary>
    [DisallowMultipleComponent]
    public sealed class UiPreviewTray : MonoBehaviour
    {
        [SerializeField] private PreviewQueueView m_View;

        private void Awake()
        {
            if (m_View == null) m_View = GetComponent<PreviewQueueView>();
        }

        public void SetQueue(PreviewQueue queue, int emptyCellCount)
        {
            m_View?.SetQueue(queue, emptyCellCount);
        }
    }
}

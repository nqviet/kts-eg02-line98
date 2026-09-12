using UnityEngine;
using Line98.Gameplay;

namespace Line98.Presentation
{
    /// <summary>
    /// Root presentation orchestrator component for Line 98 visual elements.
    /// Bridges the core game session to 3D board, ball views, VFX, and UI presentation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PresentationRoot : MonoBehaviour
    {
        private GameSession m_Session;
        private bool m_IsInitialized;

        public bool IsInitialized => m_IsInitialized;

        public void Initialize(GameSession session)
        {
            m_Session = session;
            m_IsInitialized = true;
        }
    }
}

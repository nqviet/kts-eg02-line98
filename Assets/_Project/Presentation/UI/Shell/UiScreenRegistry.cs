using System;
using System.Collections.Generic;
using UnityEngine;

namespace Line98.Presentation
{
    /// <summary>
    /// Maps scene names to screen roots and activates only the screen owned by the loaded scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiScreenRegistry : MonoBehaviour
    {
        [Serializable]
        private struct ScreenCatalogEntry
        {
            public string SceneName;
            public UiScreenBase Prefab;
        }

        [SerializeField] private ScreenCatalogEntry[] m_Catalog = Array.Empty<ScreenCatalogEntry>();

        private readonly Dictionary<string, UiScreenBase> m_Instances = new Dictionary<string, UiScreenBase>(StringComparer.Ordinal);
        private UiShell m_Shell;
        private UiServices m_Services;
        private UiScreenBase m_ActiveScreen;

        public UiScreenBase ActiveScreen => m_ActiveScreen;
        public int RegisteredCount => m_Instances.Count;

        public void Initialize(UiShell shell, UiServices services)
        {
            m_Shell = shell;
            m_Services = services;
            RegisterExistingScreens();
        }

        public void Register(string sceneName, UiScreenBase screen)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || screen == null)
            {
                return;
            }

            m_Instances[sceneName] = screen;
        }

        public bool ActivateForScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            UiScreenBase screen = Resolve(sceneName);
            if (screen == null)
            {
                return false;
            }

            if (m_ActiveScreen != null && m_ActiveScreen != screen)
            {
                m_ActiveScreen.gameObject.SetActive(false);
            }

            m_ActiveScreen = screen;
            m_ActiveScreen.gameObject.SetActive(true);
            m_ActiveScreen.Initialize(m_Services);
            return true;
        }

        public void DeactivateActive()
        {
            if (m_ActiveScreen == null)
            {
                return;
            }

            m_ActiveScreen.gameObject.SetActive(false);
            m_ActiveScreen = null;
        }

        private void RegisterExistingScreens()
        {
            UiScreenBase[] screens = GetComponentsInChildren<UiScreenBase>(true);
            for (int i = 0; i < screens.Length; i++)
            {
                string sceneName = screens[i].ScreenId;
                if (!string.IsNullOrWhiteSpace(sceneName))
                {
                    Register(sceneName, screens[i]);
                }
            }
        }

        private UiScreenBase Resolve(string sceneName)
        {
            if (m_Instances.TryGetValue(sceneName, out UiScreenBase screen) && screen != null)
            {
                return screen;
            }

            for (int i = 0; i < m_Catalog.Length; i++)
            {
                ScreenCatalogEntry entry = m_Catalog[i];
                if (!string.Equals(entry.SceneName, sceneName, StringComparison.Ordinal) || entry.Prefab == null)
                {
                    continue;
                }

                Transform parent = m_Shell != null ? m_Shell.ScreenParent : transform;
                UiScreenBase instance = Instantiate(entry.Prefab, parent);
                instance.name = entry.Prefab.name;
                instance.gameObject.SetActive(false);
                Register(sceneName, instance);
                return instance;
            }

            return null;
        }
    }
}

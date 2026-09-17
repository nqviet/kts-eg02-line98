using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Line98.Core;
using Line98.Data;
using Line98.Gameplay;
using Line98.Presentation.Animation;
using Line98.Presentation.Audio;

namespace Line98.Presentation
{
    /// <summary>
    /// Scene-agnostic UI shell. UIRouter remains as a compatibility facade for existing assets.
    /// </summary>
    [DisallowMultipleComponent]
    public class UiShell : MonoBehaviour, ITickable
    {
        [Header("Canvases")]
        [SerializeField] private Canvas m_StaticCanvas;
        [SerializeField] private Canvas m_DynamicCanvas;
        [SerializeField] private Canvas m_PopupCanvas;

        [Header("Modal Scrim")]
        [SerializeField] private UnityEngine.UI.Image m_ScrimImage;
        [SerializeField] private CanvasGroup m_ScrimGroup;

        [Header("Popups")]
        [SerializeField] private GameOverPopup m_GameOverPopup;
        [SerializeField] private ConfirmPopup m_ConfirmPopup;
        [SerializeField] private SettingsPopup m_SettingsPopup;
        [SerializeField] private StatisticsPopup m_StatisticsPopup;
        [SerializeField] private CosmeticsPopup m_CosmeticsPopup;
        [SerializeField] private UiThemeSO m_UiTheme;
        [SerializeField] private ThemeCatalogSO m_ThemeCatalog;

        [Header("Registries")]
        [SerializeField] private UiCanvasStack m_CanvasStack;
        [SerializeField] private UiPopupRegistry m_PopupRegistry;
        [SerializeField] private UiScreenRegistry m_ScreenRegistry;

        private TweenRunner m_TweenRunner;
        private readonly Stack<PopupView> m_PopupStack = new Stack<PopupView>();
        private UnityEngine.Events.UnityAction m_ConfirmAction;
        private UiServices m_Services;
        private IThemeSelector m_ThemeSelector;
        private bool m_IsInitialized;

        public Canvas StaticCanvas => m_StaticCanvas;
        public Canvas DynamicCanvas => m_DynamicCanvas;
        public Canvas PopupCanvas => m_PopupCanvas;
        public GameOverPopup GameOverPopup => m_GameOverPopup;
        public ConfirmPopup ConfirmPopup => m_ConfirmPopup;
        public SettingsPopup SettingsPopup => m_SettingsPopup;
        public StatisticsPopup StatisticsPopup => m_StatisticsPopup;
        public CosmeticsPopup CosmeticsPopup => m_CosmeticsPopup;
        public ThemeCatalogSO ThemeCatalog { get => m_ThemeCatalog; set => m_ThemeCatalog = value; }
        public IThemeSelector ThemeSelector { get => m_ThemeSelector; set => m_ThemeSelector = value; }
        public UiPopupRegistry PopupRegistry => m_PopupRegistry;
        public UiScreenRegistry ScreenRegistry => m_ScreenRegistry;
        public Transform ScreenParent => m_DynamicCanvas != null ? m_DynamicCanvas.transform : transform;
        public bool IsAnyPopupOpen => m_PopupStack.Count > 0;
        public int PopupStackCount => m_PopupStack.Count;
        public PopupView TopPopup => m_PopupStack.Count > 0 ? m_PopupStack.Peek() : null;
        public UiServices Services => m_Services;
        public bool IsInitialized => m_IsInitialized;

        protected virtual void Awake()
        {
            CacheExistingHierarchy();
            EnsureRuntimeComponents();
            Initialize(null, null, m_UiTheme, ResolveSession());
        }

        public void Initialize(TweenRunner tweenRunner)
        {
            Initialize(tweenRunner, null, null, ResolveSession());
        }

        public void Initialize(
            TweenRunner tweenRunner,
            AudioService audioService,
            UiThemeSO theme,
            GameSession session)
        {
            EnsureRuntimeComponents();
            m_TweenRunner = tweenRunner;

            if (theme != null)
            {
                m_UiTheme = theme;
            }

            m_CanvasStack.Configure(m_StaticCanvas, m_DynamicCanvas, m_PopupCanvas, m_ScrimImage, m_ScrimGroup);
            m_CanvasStack.Initialize(m_TweenRunner);

            SafeAreaFitter safeAreaFitter = GetComponent<SafeAreaFitter>();
            m_PopupRegistry.Initialize(this, safeAreaFitter);
            m_Services = new UiServices(m_TweenRunner, audioService, m_UiTheme, this, session);
            m_ScreenRegistry.Initialize(this, m_Services);
            m_ScreenRegistry.ActivateForScene(SceneManager.GetActiveScene().name);
            InitializeUiBehaviours();
            if (m_SettingsPopup != null)
            {
                m_SettingsPopup.OnCosmeticsClicked -= OpenCosmetics;
                m_SettingsPopup.OnCosmeticsClicked += OpenCosmetics;
            }
            m_IsInitialized = true;
        }

        public void SetThemeSelector(IThemeSelector selector)
        {
            m_ThemeSelector = selector;
        }

        public void SetThemeCatalog(ThemeCatalogSO catalog)
        {
            m_ThemeCatalog = catalog;
        }

        private string m_ActiveBundleThemeId = ThemeIds.Classic;

        public void ApplyTheme(UiThemeSO theme, string bundleThemeId = null)
        {
            if (!string.IsNullOrEmpty(bundleThemeId))
            {
                m_ActiveBundleThemeId = bundleThemeId;
            }

            if (theme != null)
            {
                m_UiTheme = theme;
                if (m_Services != null)
                {
                    m_Services = new UiServices(m_TweenRunner, m_Services.AudioService, m_UiTheme, this, m_Services.Session);
                }

                UiThemeApplier[] themeAppliers = FindObjectsByType<UiThemeApplier>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < themeAppliers.Length; i++)
                {
                    themeAppliers[i].Apply(m_UiTheme);
                }

                UiToggle[] toggles = FindObjectsByType<UiToggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < toggles.Length; i++)
                {
                    toggles[i].ApplyTheme(m_UiTheme);
                }

                m_SettingsPopup?.ApplyTheme(m_UiTheme);
                m_CosmeticsPopup?.ApplyTheme(m_UiTheme);

                if (m_PopupRegistry != null)
                {
                    if (m_PopupRegistry.TryGetRegistered(UiPopupId.Settings, out var sp) && sp is SettingsPopup settings)
                    {
                        settings.ApplyTheme(m_UiTheme);
                    }
                    if (m_PopupRegistry.TryGetRegistered(UiPopupId.Cosmetics, out var cp) && cp is CosmeticsPopup cosmetics)
                    {
                        cosmetics.ApplyTheme(m_UiTheme);
                    }
                }
            }

            if (m_CosmeticsPopup != null && m_CosmeticsPopup.IsOpen)
            {
                m_CosmeticsPopup.SetActiveTheme(m_ActiveBundleThemeId);
            }
        }

        public UiThemeSO UiTheme => m_UiTheme;

        public void EnsureCanvasSortingOrders()
        {
            m_CanvasStack?.EnsureSortingOrders();
        }

        public void OpenGameOver(in SessionSummary summary)
        {
            m_PopupRegistry?.Open(UiPopupId.GameOver, new GameOverPopupPayload(summary));
        }

        public void OpenGameOver(int score, int bestScore, int lines, bool canContinue)
        {
            OpenGameOver(new SessionSummary(score, bestScore, 0, lines, 0, canContinue));
        }

        public void OpenConfirm(string title, string body, Action onConfirm)
        {
            m_PopupRegistry?.Open(UiPopupId.Confirm, new ConfirmPopupPayload(title, body, onConfirm));
        }

        public void OpenSettings()
        {
            if (m_PopupRegistry == null || !m_PopupRegistry.Open(UiPopupId.Settings))
            {
                Debug.LogWarning("[UiShell] Cannot open Settings because its popup is not configured.");
            }
        }

        public void OpenCosmetics()
        {
            string activeId = !string.IsNullOrEmpty(m_ActiveBundleThemeId) ? m_ActiveBundleThemeId : ThemeIds.Classic;
            if (m_ThemeCatalog == null)
            {
                Debug.LogWarning("[UiShell] Cannot populate themes because no ThemeCatalogSO is configured.");
            }
            if (m_ThemeSelector == null)
            {
                Debug.LogWarning("[UiShell] Cannot change themes because no IThemeSelector is configured.");
            }

            if (m_PopupRegistry == null || !m_PopupRegistry.Open(UiPopupId.Cosmetics, new CosmeticsPopupPayload(m_ThemeCatalog, activeId, m_ThemeSelector)))
            {
                Debug.LogWarning("[UiShell] Cannot open Themes because its popup is not configured.");
            }
        }

        public void OpenStatistics(int gamesPlayed, int bestScore, int totalLines, int avgScore)
        {
            m_PopupRegistry?.Open(
                UiPopupId.Statistics,
                new StatisticsPopupPayload(gamesPlayed, bestScore, totalLines, avgScore));
        }

        internal void PrepareConfirm(ConfirmPopup popup, Action onConfirm)
        {
            if (popup == null || popup.ConfirmButton == null)
            {
                return;
            }

            if (m_ConfirmAction != null)
            {
                popup.ConfirmButton.onClick.RemoveListener(m_ConfirmAction);
            }

            m_ConfirmAction = () =>
            {
                ClosePopup(popup);
                onConfirm?.Invoke();
            };
            popup.ConfirmButton.onClick.AddListener(m_ConfirmAction);
        }

        internal void PushPopup(PopupView popup)
        {
            if (popup == null)
            {
                return;
            }

            if (m_PopupStack.Contains(popup))
            {
                popup.transform.SetAsLastSibling();
                m_CanvasStack?.SetModalVisible(true, popup.UsesDimScrim);
                if (!popup.IsOpen)
                {
                    popup.Show();
                }
                return;
            }

            popup.Initialize(m_TweenRunner);
            popup.OnCloseRequested -= HandlePopupCloseRequested;
            popup.OnCloseRequested += HandlePopupCloseRequested;
            if (m_UiTheme != null)
            {
                ApplyThemeToPopup(popup, m_UiTheme);
            }
            m_PopupStack.Push(popup);
            popup.transform.SetAsLastSibling();
            m_CanvasStack?.SetModalVisible(true, popup.UsesDimScrim);
            popup.Show();
        }

        public void ApplyThemeToPopup(PopupView popup, UiThemeSO theme)
        {
            if (popup == null || theme == null) return;

            UiThemeApplier[] appliers = popup.GetComponentsInChildren<UiThemeApplier>(true);
            for (int i = 0; i < appliers.Length; i++)
            {
                appliers[i].Apply(theme);
            }

            UiToggle[] toggles = popup.GetComponentsInChildren<UiToggle>(true);
            for (int i = 0; i < toggles.Length; i++)
            {
                toggles[i].ApplyTheme(theme);
            }

            if (popup is SettingsPopup settings)
            {
                m_SettingsPopup = settings;
                settings.ApplyTheme(theme);
            }
            else if (popup is CosmeticsPopup cosmetics)
            {
                m_CosmeticsPopup = cosmetics;
                cosmetics.ApplyTheme(theme);
            }
        }

        public void PopPopup()
        {
            if (m_PopupStack.Count == 0)
            {
                return;
            }

            PopupView popup = m_PopupStack.Pop();
            popup.Hide(() =>
            {
                if (m_PopupStack.Count == 0)
                {
                    m_CanvasStack?.SetModalVisible(false);
                }
                else
                {
                    m_CanvasStack?.SetModalVisible(true, m_PopupStack.Peek().UsesDimScrim);
                }
            });
        }

        /// <summary>
        /// Closes the popup with the given id when it is the top of the modal stack.
        /// Returns false when it is not on top, leaving the stack and the scrim untouched.
        /// </summary>
        public bool TryClosePopup(UiPopupId id)
        {
            if (m_PopupStack.Count == 0 || m_PopupRegistry == null)
            {
                return false;
            }

            if (!m_PopupRegistry.TryGetRegistered(id, out PopupView popup) || popup == null)
            {
                return false;
            }

            if (!ReferenceEquals(m_PopupStack.Peek(), popup))
            {
                return false;
            }

            PopPopup();
            return true;
        }

        private void HandlePopupCloseRequested(PopupView popup)
        {
            ClosePopup(popup);
        }

        private void ClosePopup(PopupView popup)
        {
            if (popup == null || m_PopupStack.Count == 0)
            {
                return;
            }

            if (ReferenceEquals(m_PopupStack.Peek(), popup))
            {
                PopPopup();
                return;
            }

            Debug.LogWarning("[UiShell] Ignored a close request from a popup that is not on top of the modal stack.");
        }

        public void CloseAllPopups()
        {
            while (m_PopupStack.Count > 0)
            {
                m_PopupStack.Pop().Hide();
            }

            m_CanvasStack?.SetModalVisible(false);
        }

        public void Tick(float dt)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame && m_PopupStack.Count > 0)
            {
                PopPopup();
            }
#endif
        }

        private void CacheExistingHierarchy()
        {
            m_StaticCanvas ??= FindCanvas("Canvas_StaticHUD");
            m_DynamicCanvas ??= FindCanvas("Canvas_DynamicHUD");
            m_PopupCanvas ??= FindCanvas("Canvas_Popups");
            m_ScrimImage ??= FindNamedComponent<UnityEngine.UI.Image>("ModalScrim");
            m_ScrimGroup ??= m_ScrimImage != null ? m_ScrimImage.GetComponent<CanvasGroup>() : FindNamedComponent<CanvasGroup>("ModalScrim");
            m_GameOverPopup ??= GetComponentInChildren<GameOverPopup>(true);
            m_ConfirmPopup ??= GetComponentInChildren<ConfirmPopup>(true);
            m_SettingsPopup ??= GetComponentInChildren<SettingsPopup>(true);
            m_StatisticsPopup ??= GetComponentInChildren<StatisticsPopup>(true);
            m_CosmeticsPopup ??= GetComponentInChildren<CosmeticsPopup>(true);
        }

        private void EnsureRuntimeComponents()
        {
            m_CanvasStack ??= GetComponent<UiCanvasStack>();
            if (m_CanvasStack == null) m_CanvasStack = gameObject.AddComponent<UiCanvasStack>();
            m_PopupRegistry ??= GetComponent<UiPopupRegistry>();
            if (m_PopupRegistry == null) m_PopupRegistry = gameObject.AddComponent<UiPopupRegistry>();
            m_ScreenRegistry ??= GetComponent<UiScreenRegistry>();
            if (m_ScreenRegistry == null) m_ScreenRegistry = gameObject.AddComponent<UiScreenRegistry>();
        }

        private void InitializeUiBehaviours()
        {
            UiButtonFx[] buttonEffects = FindObjectsByType<UiButtonFx>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttonEffects.Length; i++)
            {
                buttonEffects[i].Initialize(m_TweenRunner, m_Services.AudioService);
            }

            UiActionButton[] actionButtons = FindObjectsByType<UiActionButton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < actionButtons.Length; i++)
            {
                actionButtons[i].Initialize(m_Services);
            }

            UiThemeApplier[] themeAppliers = FindObjectsByType<UiThemeApplier>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < themeAppliers.Length; i++)
            {
                themeAppliers[i].Apply(m_Services.Theme);
            }
        }

        private static GameSession ResolveSession()
        {
            return null;
        }

        private Canvas FindCanvas(string objectName)
        {
            Transform child = transform.Find(objectName);
            return child != null ? child.GetComponent<Canvas>() : null;
        }

        private T FindNamedComponent<T>(string objectName) where T : Component
        {
            Transform child = FindChildRecursive(transform, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindChildRecursive(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildRecursive(root.GetChild(i), objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}

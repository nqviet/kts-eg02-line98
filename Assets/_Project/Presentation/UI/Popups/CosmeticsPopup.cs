using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Rebuilt Cosmetics / Theme Selection popup matching themes_selection_UI_crystal.png.
    /// Full-height responsive modal with TabStrip (BALLS / BOARD / EFFECTS),
    /// PreviewCard with 5x5 board mock and 7 diamond-flower ball slots,
    /// Item cards with swatch previews and DEFAULT / SELECT status badges.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CosmeticsPopup : UiPopupBase
    {
        private const string s_FullscreenBackdropName = "FullscreenBackdrop";
        private static readonly Vector2 s_ReferenceContentSize = new Vector2(960f, 1760f);

        [Header("Hierarchy & Layout")]
        [SerializeField] private RectTransform m_ContentRoot;
        [SerializeField] private UnityEngine.UI.Button m_BackButton;
        [SerializeField] private TMP_Text m_TitleLabel;

        [Header("Components")]
        [SerializeField] private UiTabStrip m_TabStrip;
        [SerializeField] private UiThemePreviewPanel m_PreviewPanel;
        [SerializeField] private RectTransform m_ItemsContainer;
        [SerializeField] private GameObject m_ItemPrefab;
        [SerializeField] private TMP_Text m_FooterNote;

        [Header("Mockup Styling")]
        [SerializeField] private Color m_FullscreenBackdropColor = Color.white;
        [SerializeField] private Color m_BrandNavy = new Color(0.075f, 0.157f, 0.31f, 1f); // #13284F
        [SerializeField, Min(0f)] private float m_HorizontalSafePadding = 24f;
        [SerializeField, Min(0f)] private float m_VerticalSafePadding = 24f;

        [SerializeField] private UnityEngine.UI.Graphic m_FullscreenBackdrop;

        private ThemeCatalogSO m_Catalog;
        private IThemeSelector m_Selector;
        private ThemeCategory m_ActiveTab = ThemeCategory.Ball;
        private string m_ActivePartId;    // The previewed part id; null on open
        private string m_SelectedPartId;  // The currently applied/persisted part id
        private readonly List<UiThemeListItem> m_SpawnedItems = new List<UiThemeListItem>();

        public event Action<string> OnThemeRequested;

        public override bool UsesFullLayoutHeight => true;
        public override bool UsesDimScrim => false;

        public RectTransform TilesContainer => m_ItemsContainer;
        public RectTransform ItemsContainer => m_ItemsContainer;
        public string ActiveThemeId => !string.IsNullOrEmpty(m_SelectedPartId) ? m_SelectedPartId : "crystal";
        public string PreviewedThemeId => !string.IsNullOrEmpty(m_ActivePartId) ? m_ActivePartId : ActiveThemeId;
        public string ActivePartId => m_ActivePartId;
        public string SelectedPartId => m_SelectedPartId;
        public ThemeCatalogSO Catalog => m_Catalog;
        public ThemeCategory ActiveTab => m_ActiveTab;
        public UiTabStrip TabStrip => m_TabStrip;
        public UiThemePreviewPanel PreviewPanel => m_PreviewPanel;

        protected override void Awake()
        {
            base.Awake();

            EnsureFullscreenPresentation();
            ApplyMockupTypography();

            // ThemeAuthoring wires BtnBack as PopupView.m_CloseButton. Do not register the
            // same handler twice or a single press can pop both Themes and Settings.
            if (m_BackButton != null && m_BackButton != CloseButton)
            {
                m_BackButton.onClick.AddListener(HandleCloseRequested);
            }

            if (m_TabStrip != null)
            {
                m_TabStrip.OnTabSelected += HandleTabChanged;
            }
        }

        protected override void OnDestroy()
        {
            if (m_BackButton != null && m_BackButton != CloseButton)
            {
                m_BackButton.onClick.RemoveListener(HandleCloseRequested);
            }

            if (m_TabStrip != null)
            {
                m_TabStrip.OnTabSelected -= HandleTabChanged;
            }

            base.OnDestroy();
        }

        /// <summary>
        /// The Themes surface occupies the complete popup canvas while its 960 x 1620 authored
        /// composition scales uniformly inside the safe layout. This preserves the mockup's
        /// proportions on phones, tablets, and short landscape/editor views.
        /// </summary>
        public override void ApplyResponsiveLayout(float layoutWidth, float layoutHeight)
        {
            EnsureFullscreenPresentation();

            if (m_ContentRoot == null)
            {
                return;
            }

            // UiResponsiveModal can run once before the canvas has reported its safe layout.
            // Falling back to the actual popup parent avoids caching a near-zero rest scale.
            RectTransform host = m_ContentRoot.parent as RectTransform;
            Canvas canvas = GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
            UnityEngine.UI.CanvasScaler canvasScaler = canvas != null
                ? canvas.rootCanvas.GetComponent<UnityEngine.UI.CanvasScaler>()
                : null;
            if (canvasScaler != null &&
                canvasScaler.uiScaleMode == UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                layoutWidth = Mathf.Max(layoutWidth, canvasScaler.referenceResolution.x);
                layoutHeight = Mathf.Max(layoutHeight, canvasScaler.referenceResolution.y);
            }

            if (canvasRect != null && canvasRect.rect.width > 1f && canvasRect.rect.height > 1f)
            {
                // CanvasScaler already converts physical pixels into this reference-space rect.
                // The responsive host can pass physical Game View pixels, so always author the
                // composition against the root canvas to avoid applying the scale twice.
                layoutWidth = Mathf.Max(layoutWidth, canvasRect.rect.width);
                layoutHeight = Mathf.Max(layoutHeight, canvasRect.rect.height);
            }
            else if (host != null && host.rect.width > 1f && host.rect.height > 1f)
            {
                layoutWidth = host.rect.width;
                layoutHeight = host.rect.height;
            }

            if (layoutWidth <= 1f || layoutHeight <= 1f)
            {
                if (host != null && host.rect.width > 1f && host.rect.height > 1f)
                {
                    layoutWidth = host.rect.width;
                    layoutHeight = host.rect.height;
                }
                else
                {
                    return;
                }
            }

            float availableWidth = Mathf.Max(1f, layoutWidth - m_HorizontalSafePadding * 2f);
            float availableHeight = Mathf.Max(1f, layoutHeight - m_VerticalSafePadding * 2f);
            float contentScale = Mathf.Min(
                1f,
                availableWidth / s_ReferenceContentSize.x,
                availableHeight / s_ReferenceContentSize.y);

            m_ContentRoot.anchorMin = new Vector2(0.5f, 1f);
            m_ContentRoot.anchorMax = new Vector2(0.5f, 1f);
            m_ContentRoot.pivot = new Vector2(0.5f, 1f);
            m_ContentRoot.anchoredPosition = new Vector2(0f, -m_VerticalSafePadding);
            m_ContentRoot.sizeDelta = s_ReferenceContentSize;
            SetModalRestScale(new Vector3(contentScale, contentScale, 1f));
        }

        public override void Show(Action onComplete = null)
        {
            // Re-evaluate after CanvasScaler has established its reference-space rect. The
            // initial responsive callback can occur one frame earlier at physical resolution.
            RectTransform host = m_ContentRoot != null ? m_ContentRoot.parent as RectTransform : null;
            if (host != null)
            {
                ApplyResponsiveLayout(host.rect.width, host.rect.height);
            }

            base.Show(onComplete);
        }

        public void Populate(
            ThemeCatalogSO catalog,
            string activeThemeId,
            IThemeSelector selector,
            ThemeCategory initialTab = ThemeCategory.Ball)
        {
            m_Catalog = catalog;
            m_Selector = selector;
            m_ActiveTab = initialTab;

            // F2: on open, ActivePartId is null, nothing is highlighted
            m_ActivePartId = null;

            if (m_TabStrip != null)
            {
                m_TabStrip.SetActive(m_ActiveTab, notify: false);
            }

            ResolveSelectedPartId(activeThemeId);
            RebuildItems();
            UpdatePreview(animate: false);
        }

        public void Populate(
            ThemeCatalogSO catalog,
            IThemeSelector selector,
            ThemeCategory initialTab = ThemeCategory.Ball)
        {
            Populate(catalog, selector?.ActiveBallThemeId, selector, initialTab);
        }

        public void SetActiveTheme(string themeId)
        {
            ResolveSelectedPartId(themeId);
            RefreshItems();
            UpdatePreview(animate: false);
        }

        private void ResolveSelectedPartId(string fallbackId = null)
        {
            if (m_Selector != null)
            {
                m_SelectedPartId = m_ActiveTab switch
                {
                    ThemeCategory.Ball => m_Selector.ActiveBallThemeId,
                    ThemeCategory.Board => m_Selector.ActiveBoardThemeId,
                    ThemeCategory.ClearEffect => m_Selector.ActiveClearEffectThemeId,
                    _ => m_Selector.ActiveBallThemeId
                };
            }

            if (string.IsNullOrEmpty(m_SelectedPartId))
            {
                m_SelectedPartId = !string.IsNullOrEmpty(fallbackId) ? fallbackId : (m_Catalog?.DefaultThemeId ?? "crystal");
            }
        }

        private void HandleTabChanged(ThemeCategory newCategory)
        {
            m_ActiveTab = newCategory;
            m_ActivePartId = null; // Reset previewed item on tab switch per F2

            ResolveSelectedPartId();

            if (m_ActiveTab == ThemeCategory.Ball)
            {
                if (m_ItemsContainer != null) m_ItemsContainer.gameObject.SetActive(true);
                RebuildItems();
                UpdatePreview(animate: true);
            }
            else
            {
                // Out of scope: BOARD and EFFECTS tab contents render shared empty state
                if (m_ItemsContainer != null) m_ItemsContainer.gameObject.SetActive(false);
                if (m_PreviewPanel != null) m_PreviewPanel.ShowEmpty(m_ActiveTab);
            }
        }

        private void RebuildItems()
        {
            if (m_ItemsContainer == null || m_Catalog == null)
            {
                return;
            }

            // Clear old items
            for (int i = 0; i < m_SpawnedItems.Count; i++)
            {
                if (m_SpawnedItems[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(m_SpawnedItems[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(m_SpawnedItems[i].gameObject);
                    }
                }
            }
            m_SpawnedItems.Clear();

            var items = ThemePartResolver.ResolveItems(m_Catalog, m_ActiveTab);
            // The mockup leads with the currently applied theme. Keep the remaining catalog
            // order stable so this is presentation-only and does not affect theme identity.
            items.Sort((left, right) =>
            {
                bool leftSelected = string.Equals(left.PartId, m_SelectedPartId, StringComparison.OrdinalIgnoreCase);
                bool rightSelected = string.Equals(right.PartId, m_SelectedPartId, StringComparison.OrdinalIgnoreCase);
                return leftSelected == rightSelected ? 0 : leftSelected ? -1 : 1;
            });
            for (int i = 0; i < items.Count; i++)
            {
                var model = items[i];
                GameObject itemObj;
                if (m_ItemPrefab != null)
                {
                    itemObj = Instantiate(m_ItemPrefab, m_ItemsContainer);
                }
                else
                {
                    itemObj = CreateDefaultItemObject(m_ItemsContainer);
                }

                itemObj.name = $"Item_{model.PartId}";
                itemObj.SetActive(true);

                // Add Tile_ alias child for test compatibility
                if (itemObj.transform.Find($"Tile_{model.PartId}") == null)
                {
                    var alias = new GameObject($"Tile_{model.PartId}");
                    alias.transform.SetParent(itemObj.transform, false);
                }

                var listItem = itemObj.GetComponent<UiThemeListItem>();
                if (listItem == null)
                {
                    listItem = itemObj.AddComponent<UiThemeListItem>();
                }

                bool isActive = string.Equals(model.PartId, m_ActivePartId, StringComparison.OrdinalIgnoreCase);
                bool isApplied = string.Equals(model.PartId, m_SelectedPartId, StringComparison.OrdinalIgnoreCase);

                listItem.Bind(model, isActive, isApplied);
                listItem.OnCardClicked += HandleCardClicked;
                listItem.OnSelectClicked += HandleSelectClicked;

                m_SpawnedItems.Add(listItem);
            }
        }

        private void RefreshItems()
        {
            for (int i = 0; i < m_SpawnedItems.Count; i++)
            {
                var item = m_SpawnedItems[i];
                if (item == null) continue;

                bool isActive = string.Equals(item.PartId, m_ActivePartId, StringComparison.OrdinalIgnoreCase);
                bool isApplied = string.Equals(item.PartId, m_SelectedPartId, StringComparison.OrdinalIgnoreCase);

                item.SetIsActive(isActive);
                item.SetIsApplied(isApplied, punch: false);
            }
        }

        private void UpdatePreview(bool animate = false)
        {
            if (m_PreviewPanel == null || m_Catalog == null)
            {
                return;
            }

            if (m_ActiveTab != ThemeCategory.Ball)
            {
                m_PreviewPanel.ShowEmpty(m_ActiveTab);
                return;
            }

            // Preview item: active item if tapped, otherwise currently selected item
            string previewId = !string.IsNullOrEmpty(m_ActivePartId) ? m_ActivePartId : m_SelectedPartId;
            var bundle = ThemePartResolver.ResolveBundle(m_Catalog, m_ActiveTab, previewId);
            string themeName = bundle != null ? bundle.DisplayName : previewId;

            var previewSprites = ThemePartResolver.ResolvePreviewSprites(m_Catalog, m_ActiveTab, previewId, bundle);
            bool showSelectedBadge = !string.IsNullOrEmpty(m_ActivePartId) &&
                                     string.Equals(m_ActivePartId, m_SelectedPartId, StringComparison.OrdinalIgnoreCase);

            m_PreviewPanel.Show(themeName, previewSprites, showSelectedBadge, animate);
        }

        private void HandleCardClicked(string partId)
        {
            // F3: Tap an item card -> preview only, NO service call
            m_ActivePartId = partId;
            RefreshItems();
            UpdatePreview(animate: true);
        }

        private void HandleSelectClicked(string partId)
        {
            // F4: Tap SELECT -> activate and apply theme
            m_ActivePartId = partId;
            m_SelectedPartId = partId;

            // Apply via selector
            if (m_Selector != null)
            {
                m_Selector.RequestTheme(m_ActiveTab, partId);
            }

            OnThemeRequested?.Invoke(partId);

            // Update items with punch animation on status badge
            for (int i = 0; i < m_SpawnedItems.Count; i++)
            {
                var item = m_SpawnedItems[i];
                if (item == null) continue;

                bool isActive = string.Equals(item.PartId, m_ActivePartId, StringComparison.OrdinalIgnoreCase);
                bool isApplied = string.Equals(item.PartId, m_SelectedPartId, StringComparison.OrdinalIgnoreCase);

                item.SetIsActive(isActive);
                item.SetIsApplied(isApplied, punch: isApplied);
            }

            UpdatePreview(animate: false);
        }

        private void EnsureFullscreenPresentation()
        {
            var root = transform as RectTransform;
            if (root != null)
            {
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
                root.pivot = new Vector2(0.5f, 0.5f);
            }

            if (m_FullscreenBackdrop == null)
            {
                Transform existingBackdrop = transform.Find(s_FullscreenBackdropName);
                if (existingBackdrop != null)
                {
                    m_FullscreenBackdrop = existingBackdrop.GetComponent<UnityEngine.UI.Graphic>();
                }
            }

            if (m_FullscreenBackdrop == null)
            {
                var backdropObject = new GameObject(
                    s_FullscreenBackdropName,
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(UnityEngine.UI.RawImage));
                backdropObject.transform.SetParent(transform, false);
                m_FullscreenBackdrop = backdropObject.GetComponent<UnityEngine.UI.RawImage>();
            }

            RectTransform backdropRect = m_FullscreenBackdrop.rectTransform;
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            backdropRect.SetAsFirstSibling();

            m_FullscreenBackdrop.color = m_FullscreenBackdropColor;
            // This is also the modal input shield: the fullscreen surface must prevent
            // taps in its outer margins from reaching the game or Settings underneath.
            m_FullscreenBackdrop.raycastTarget = true;
        }

        private void ApplyMockupTypography()
        {
            if (m_TitleLabel != null)
            {
                m_TitleLabel.text = "THEMES";
                // The title carries an authored navy vertex gradient; only tint flat titles.
                if (!m_TitleLabel.enableVertexGradient)
                {
                    m_TitleLabel.color = m_BrandNavy;
                }
                m_TitleLabel.fontStyle |= FontStyles.Bold;
                m_TitleLabel.alignment = TextAlignmentOptions.Center;
                m_TitleLabel.raycastTarget = false;
            }

            if (m_FooterNote != null)
            {
                // The flanking rules are separate Images in the prefab.
                m_FooterNote.text = "THEMES CHANGE VISUALS ONLY";
                m_FooterNote.color = m_BrandNavy;
                m_FooterNote.alignment = TextAlignmentOptions.Center;
                m_FooterNote.raycastTarget = false;
            }
        }

        private static GameObject CreateDefaultItemObject(RectTransform parent)
        {
            var go = new GameObject(
                "Item",
                typeof(RectTransform),
                typeof(UnityEngine.UI.Image),
                typeof(UnityEngine.UI.Button),
                typeof(UiThemeListItem));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(920, 220);
            return go;
        }
    }
}

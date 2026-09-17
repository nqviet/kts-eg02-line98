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
        [SerializeField] private RectTransform m_PlaceholderRoot;
        [SerializeField] private TMP_Text m_PlaceholderText;

        [Header("Mockup Styling")]
        [SerializeField] private Color m_FullscreenBackdropColor = Color.white;
        [SerializeField] private Color m_BrandNavy = new Color(0.075f, 0.157f, 0.31f, 1f); // #13284F
        [SerializeField, Min(0f)] private float m_HorizontalSafePadding = 24f;
        [SerializeField, Min(0f)] private float m_VerticalSafePadding = 24f;

        [SerializeField] private UnityEngine.UI.Graphic m_FullscreenBackdrop;
        [SerializeField] private Texture2D m_CrystalBackdropTexture;

        private UiThemeSO m_CurrentTheme;
        private ThemeCatalogSO m_Catalog;
        private IThemeSelector m_Selector;
        private ThemeCategory m_ActiveTab = ThemeCategory.Ball;
        private string m_ActivePartId;    // The previewed part id; null on open
        private string m_SelectedPartId;  // The applied part id for the active tab
        // Applied ids when no selector is bound (editor previews/tests); the selector is authoritative otherwise.
        private readonly Dictionary<ThemeCategory, string> m_LocalAppliedIds = new Dictionary<ThemeCategory, string>();
        private readonly List<UiThemeListItem> m_SpawnedItems = new List<UiThemeListItem>();

        /// <summary>Raised after SELECT with the tab's category and the requested part id.</summary>
        public event Action<ThemeCategory, string> OnThemeRequested;

        public override bool UsesFullLayoutHeight => true;
        public override bool UsesDimScrim => false;

        public RectTransform TilesContainer => m_ItemsContainer;
        public RectTransform ItemsContainer => m_ItemsContainer;
        public RectTransform PlaceholderRoot => m_PlaceholderRoot;
        public TMP_Text PlaceholderText => m_PlaceholderText;
        /// <summary>Applied part id for the active tab.</summary>
        public string AppliedPartId => !string.IsNullOrEmpty(m_SelectedPartId) ? m_SelectedPartId : ThemeIds.Classic;
        public string PreviewedThemeId => !string.IsNullOrEmpty(m_ActivePartId) ? m_ActivePartId : AppliedPartId;
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
            EnsurePlaceholder();
            if (m_FullscreenBackdrop is UnityEngine.UI.RawImage rawBackdrop && m_CrystalBackdropTexture == null && rawBackdrop.texture is Texture2D tex)
            {
                m_CrystalBackdropTexture = tex;
            }
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

        // The 960x1760 themes page (ContentRoot) is fitted uniformly into the padded safe area,
        // so short, wide, and notched screens never clip the tabs, preview, or item list.
        protected override Vector2 ReferenceLayoutSize => s_ReferenceContentSize;
        protected override float ReferenceMaxScale => 1f;
        protected override Vector2 ReferenceFitPadding => new Vector2(m_HorizontalSafePadding, m_VerticalSafePadding);

        public void Populate(
            ThemeCatalogSO catalog,
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
                m_TabStrip.OnTabSelected -= HandleTabChanged;
                m_TabStrip.OnTabSelected += HandleTabChanged;
                m_TabStrip.SetActive(m_ActiveTab, notify: false);
            }

            ResolveSelectedPartId();
            UpdateTabState(animate: false);
        }

        public void SelectTab(ThemeCategory category)
        {
            if (m_TabStrip != null)
            {
                m_TabStrip.SetActive(category, notify: true);
            }
            else
            {
                HandleTabChanged(category);
            }
        }

        /// <summary>Re-reads the applied part ids from the selector (e.g. after an external theme change).</summary>
        public void RefreshSelection()
        {
            ResolveSelectedPartId();
            if (m_ActiveTab != ThemeCategory.ClearEffect)
            {
                RefreshItems();
                UpdatePreview(animate: false);
            }
        }

        public void ApplyTheme(UiThemeSO theme)
        {
            if (theme == null) return;
            m_CurrentTheme = theme;

            m_BrandNavy = theme.BrandNavy;
            ApplyMockupTypography();

            if (m_FullscreenBackdrop != null)
            {
                var rawImg = m_FullscreenBackdrop as UnityEngine.UI.RawImage;
                if (theme.CardBackgroundSprite != null)
                {
                    if (rawImg != null)
                    {
                        rawImg.texture = m_CrystalBackdropTexture;
                    }
                    m_FullscreenBackdrop.color = Color.white;
                }
                else
                {
                    if (rawImg != null)
                    {
                        rawImg.texture = null;
                    }
                    m_FullscreenBackdrop.color = theme.LightScrim;
                }
            }

            if (m_TitleLabel != null)
            {
                Transform leaf = m_TitleLabel.transform.Find("LeafDecor");
                if (leaf != null)
                {
                    leaf.gameObject.SetActive(theme.CardBackgroundSprite != null);
                }
            }

            var appliers = GetComponentsInChildren<UiThemeApplier>(true);
            for (int i = 0; i < appliers.Length; i++)
            {
                appliers[i].Apply(theme);
            }

            m_TabStrip?.ApplyTheme(theme);

            if (m_PlaceholderText != null)
            {
                m_PlaceholderText.color = theme.BrandNavy;
            }

            for (int i = 0; i < m_SpawnedItems.Count; i++)
            {
                m_SpawnedItems[i]?.ApplyTheme(theme);
            }
        }

        private void ResolveSelectedPartId()
        {
            m_SelectedPartId = GetAppliedPartId(m_ActiveTab);
        }

        private string GetAppliedPartId(ThemeCategory category)
        {
            string id = null;
            if (m_Selector != null)
            {
                id = category switch
                {
                    ThemeCategory.Ball => m_Selector.ActiveBallThemeId,
                    ThemeCategory.Board => m_Selector.ActiveBoardThemeId,
                    ThemeCategory.ClearEffect => m_Selector.ActiveClearEffectThemeId,
                    ThemeCategory.Ui => m_Selector.ActiveUiThemeId,
                    _ => null
                };
            }
            else
            {
                m_LocalAppliedIds.TryGetValue(category, out id);
            }

            if (string.IsNullOrEmpty(id))
            {
                id = m_Catalog != null ? m_Catalog.DefaultPartId(category) : ThemeIds.Classic;
            }
            return id;
        }

        private void HandleTabChanged(ThemeCategory newCategory)
        {
            m_ActiveTab = newCategory;
            m_ActivePartId = null; // Reset previewed item on tab switch per F2

            ResolveSelectedPartId();

            if (m_FooterNote != null)
            {
                m_FooterNote.text = m_ActiveTab == ThemeCategory.Board ? "BOARD SETS THE UI STYLE" : "THEMES CHANGE VISUALS ONLY";
            }

            UpdateTabState(animate: true);
        }

        private void UpdateTabState(bool animate)
        {
            EnsurePlaceholder();
            bool isEffects = m_ActiveTab == ThemeCategory.ClearEffect;

            if (isEffects)
            {
                if (m_PreviewPanel != null) m_PreviewPanel.gameObject.SetActive(false);
                if (m_ItemsContainer != null) m_ItemsContainer.gameObject.SetActive(false);
                if (m_PlaceholderRoot != null) m_PlaceholderRoot.gameObject.SetActive(true);
            }
            else
            {
                if (m_PlaceholderRoot != null) m_PlaceholderRoot.gameObject.SetActive(false);
                if (m_PreviewPanel != null) m_PreviewPanel.gameObject.SetActive(true);
                if (m_ItemsContainer != null) m_ItemsContainer.gameObject.SetActive(true);
                RebuildItems();
                UpdatePreview(animate: animate);
            }
        }

        private void RebuildItems()
        {
            if (m_ItemsContainer == null || m_Catalog == null || m_ActiveTab == ThemeCategory.ClearEffect)
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
                bool leftSelected = ComponentIsApplied(left);
                bool rightSelected = ComponentIsApplied(right);
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
                bool isApplied = ComponentIsApplied(model);

                listItem.Bind(model, m_ActiveTab, isActive, isApplied);
                if (m_CurrentTheme != null)
                {
                    listItem.ApplyTheme(m_CurrentTheme);
                }
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
                bool isApplied = ComponentIsApplied(item);

                item.SetIsActive(isActive);
                item.SetIsApplied(isApplied, punch: false);
            }
        }

        private void UpdatePreview(bool animate = false)
        {
            if (m_PreviewPanel == null || m_Catalog == null || m_ActiveTab == ThemeCategory.ClearEffect)
            {
                return;
            }

            // Preview item: active item if tapped, otherwise the applied item for this tab
            string previewId = !string.IsNullOrEmpty(m_ActivePartId) ? m_ActivePartId : m_SelectedPartId;
            bool onBoard = m_ActiveTab == ThemeCategory.Board;

            m_Catalog.TryGetPackForPart(m_ActiveTab, previewId, out ThemeDefinitionSO pack);
            m_Catalog.TryGetBoardTheme(onBoard ? previewId : GetAppliedPartId(ThemeCategory.Board), out BoardThemeSO board);
            m_Catalog.TryGetBallTheme(onBoard ? GetAppliedPartId(ThemeCategory.Ball) : previewId, out BallThemeSO ball);

            string themeName;
            if (onBoard)
            {
                themeName = board != null && !string.IsNullOrEmpty(board.DisplayName)
                    ? board.DisplayName
                    : (pack != null ? pack.DisplayName : previewId);
            }
            else
            {
                themeName = ball != null && !string.IsNullOrEmpty(ball.DisplayName)
                    ? ball.DisplayName
                    : (pack != null ? pack.DisplayName : previewId);
            }

            var previewSprites = ThemePartResolver.ResolvePreviewSprites(m_Catalog, ball);
            bool showSelectedBadge = !string.IsNullOrEmpty(m_ActivePartId) && IsAppliedPartId(m_ActivePartId);

            var request = new ThemePreviewRequest(
                displayName: themeName,
                board: board,
                ball: ball,
                gemSprites: previewSprites,
                emphasis: onBoard ? PreviewEmphasis.Board : PreviewEmphasis.Gems,
                isApplied: showSelectedBadge,
                animate: animate);

            m_PreviewPanel.Show(in request);
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
            // F4: Tap SELECT -> apply this part only. On the BOARD tab the service pairs the UI theme.
            m_ActivePartId = partId;

            if (m_Selector != null)
            {
                m_Selector.RequestTheme(m_ActiveTab, partId);
            }
            else
            {
                m_LocalAppliedIds[m_ActiveTab] = partId;
            }
            ResolveSelectedPartId();

            OnThemeRequested?.Invoke(m_ActiveTab, partId);

            if (m_ActiveTab == ThemeCategory.Board)
            {
                PlayBoardSelectionCue();
            }

            // Update items with punch animation on status badge
            for (int i = 0; i < m_SpawnedItems.Count; i++)
            {
                var item = m_SpawnedItems[i];
                if (item == null) continue;

                bool isActive = string.Equals(item.PartId, m_ActivePartId, StringComparison.OrdinalIgnoreCase);
                bool isApplied = ComponentIsApplied(item);

                item.SetIsActive(isActive);
                item.SetIsApplied(isApplied, punch: isApplied);
            }

            UpdatePreview(animate: false);
        }

        private void PlayBoardSelectionCue()
        {
            if (m_ContentRoot == null) return;

            var cg = m_ContentRoot.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = m_ContentRoot.gameObject.AddComponent<CanvasGroup>();
            }

            var tr = TweenRunner ?? FindAnyObjectByType<PresentationRoot>()?.TweenRunner;
            if (tr != null)
            {
                tr.CancelByOwner(cg);
                cg.alpha = 1.0f;

                Animation.Tween dipIn = new Animation.Tween
                {
                    From = 1.0f,
                    To = 0.72f,
                    Duration = 0.12f,
                    Ease = Animation.Easing.OutCubic,
                    Owner = cg,
                    OnUpdate = val =>
                    {
                        if (cg != null) cg.alpha = val;
                    },
                    OnComplete = () =>
                    {
                        if (cg == null) return;
                        Animation.Tween dipOut = new Animation.Tween
                        {
                            From = 0.72f,
                            To = 1.0f,
                            Duration = 0.12f,
                            Ease = Animation.Easing.OutCubic,
                            Owner = cg,
                            OnUpdate = v =>
                            {
                                if (cg != null) cg.alpha = v;
                            }
                        };
                        tr.Play(in dipOut);
                    }
                };
                tr.Play(in dipIn);
            }
        }

        private bool IsAppliedPartId(string partId)
        {
            return string.Equals(partId, m_SelectedPartId, StringComparison.OrdinalIgnoreCase);
        }

        private bool ComponentIsApplied(ThemeItemModel model)
        {
            return IsAppliedPartId(model.PartId);
        }

        private bool ComponentIsApplied(UiThemeListItem item)
        {
            return item != null && IsAppliedPartId(item.PartId);
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
                if (m_CurrentTheme != null && m_CurrentTheme.CardBackgroundSprite == null)
                {
                    m_TitleLabel.enableVertexGradient = false;
                    m_TitleLabel.color = m_CurrentTheme.BrandNavy;
                }
                else if (m_CurrentTheme != null && m_CurrentTheme.CardBackgroundSprite != null)
                {
                    m_TitleLabel.enableVertexGradient = true;
                    m_TitleLabel.colorGradient = new VertexGradient(new Color(0.118f, 0.290f, 0.549f, 1f), new Color(0.118f, 0.290f, 0.549f, 1f), m_BrandNavy, m_BrandNavy);
                }
                else if (!m_TitleLabel.enableVertexGradient)
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
                m_FooterNote.text = m_ActiveTab == ThemeCategory.Board ? "BOARD SETS THE UI STYLE" : "THEMES CHANGE VISUALS ONLY";
                m_FooterNote.color = m_CurrentTheme != null ? m_CurrentTheme.InkLabel : m_BrandNavy;
                m_FooterNote.alignment = TextAlignmentOptions.Center;
                m_FooterNote.raycastTarget = false;
            }

            if (m_PlaceholderText != null)
            {
                m_PlaceholderText.color = m_CurrentTheme != null ? m_CurrentTheme.BrandNavy : m_BrandNavy;
                m_PlaceholderText.fontStyle = FontStyles.Bold;
                m_PlaceholderText.alignment = TextAlignmentOptions.Center;
                m_PlaceholderText.raycastTarget = false;
            }
        }

        private void EnsurePlaceholder()
        {
            if (m_PlaceholderRoot != null)
            {
                if (m_PlaceholderText == null)
                {
                    m_PlaceholderText = m_PlaceholderRoot.GetComponentInChildren<TMP_Text>(true);
                }
                return;
            }

            Transform existing = m_ContentRoot != null
                ? m_ContentRoot.Find("PlaceholderRoot")
                : transform.Find("PlaceholderRoot");

            if (existing != null)
            {
                m_PlaceholderRoot = existing as RectTransform;
                m_PlaceholderText = existing.GetComponentInChildren<TMP_Text>(true);
                return;
            }

            if (m_ContentRoot == null) return;

            var go = new GameObject("PlaceholderRoot", typeof(RectTransform));
            go.transform.SetParent(m_ContentRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, 60f);
            rt.offsetMax = new Vector2(0f, -220f);

            var textGo = new GameObject("Label_Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(rt, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "Coming Soon";
            tmp.fontSize = 40f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = m_CurrentTheme != null ? m_CurrentTheme.BrandNavy : m_BrandNavy;
            tmp.characterSpacing = 2f;
            tmp.raycastTarget = false;

            m_PlaceholderRoot = rt;
            m_PlaceholderText = tmp;
            go.SetActive(false);
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

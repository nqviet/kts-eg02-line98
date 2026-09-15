using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Line98.Data;

namespace Line98.Presentation
{
    /// <summary>
    /// Cosmetics picker popup displaying available theme bundles as selectable thumbnail tiles.
    /// Strictly no shop, no currency, no prices per GDD §16.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CosmeticsPopup : PopupView
    {
        [Header("Cosmetics Elements")]
        [SerializeField] private RectTransform m_TilesContainer;
        [SerializeField] private GameObject m_TilePrefab;
        [SerializeField] private Color m_ActiveHighlightColor = new Color(0f, 0.282f, 0.941f, 1f); // BrandBlue
        [SerializeField] private Color m_NormalBorderColor = new Color(0.761f, 0.816f, 0.886f, 0.88f); // PanelCell

        private readonly List<GameObject> m_SpawnedTiles = new List<GameObject>();
        private ThemeCatalogSO m_Catalog;
        private string m_ActiveThemeId = "crystal";
        private IThemeSelector m_Selector;

        public event Action<string> OnThemeRequested;

        public RectTransform TilesContainer => m_TilesContainer;
        public string ActiveThemeId => m_ActiveThemeId;
        public ThemeCatalogSO Catalog => m_Catalog;

        public void Populate(ThemeCatalogSO catalog, string activeThemeId, IThemeSelector selector)
        {
            bool catalogChanged = m_Catalog != catalog;
            m_Catalog = catalog;
            m_ActiveThemeId = !string.IsNullOrEmpty(activeThemeId) ? activeThemeId : "crystal";
            m_Selector = selector;

            if (m_Catalog == null)
            {
                Debug.LogWarning("[CosmeticsPopup] Cannot show themes because no ThemeCatalogSO was provided.");
            }
            else if (m_Selector == null)
            {
                Debug.LogWarning("[CosmeticsPopup] Theme selection is unavailable because no IThemeSelector was provided.");
            }

            if (catalogChanged)
            {
                RebuildTiles();
            }
            else
            {
                RefreshTiles();
            }
        }

        public void SetActiveTheme(string themeId)
        {
            m_ActiveThemeId = themeId;
            RefreshTiles();
        }

        private void RebuildTiles()
        {
            if (m_TilesContainer == null || m_Catalog == null)
            {
                return;
            }

            // Clear old tiles
            for (int i = 0; i < m_SpawnedTiles.Count; i++)
            {
                if (m_SpawnedTiles[i] != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(m_SpawnedTiles[i]);
                    }
                    else
                    {
                        DestroyImmediate(m_SpawnedTiles[i]);
                    }
                }
            }
            m_SpawnedTiles.Clear();

            // Build tiles for all themes in catalog
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                var theme = m_Catalog.ThemeAt(i);
                if (theme == null) continue;

                GameObject tileObj;
                if (m_TilePrefab != null)
                {
                    tileObj = Instantiate(m_TilePrefab, m_TilesContainer);
                }
                else
                {
                    tileObj = CreateDefaultTileObject(m_TilesContainer);
                }

                tileObj.name = $"Tile_{theme.ThemeId}";
                tileObj.SetActive(true);

                ConfigureTile(tileObj, theme, bindClickHandler: true);
                m_SpawnedTiles.Add(tileObj);
            }
        }

        private void RefreshTiles()
        {
            if (m_TilesContainer == null || m_Catalog == null)
            {
                return;
            }

            int tileIndex = 0;
            for (int i = 0; i < m_Catalog.Count; i++)
            {
                ThemeDefinitionSO theme = m_Catalog.ThemeAt(i);
                if (theme == null)
                {
                    continue;
                }

                if (tileIndex >= m_SpawnedTiles.Count || m_SpawnedTiles[tileIndex] == null)
                {
                    RebuildTiles();
                    return;
                }

                ConfigureTile(m_SpawnedTiles[tileIndex], theme, bindClickHandler: false);
                tileIndex++;
            }

            if (tileIndex != m_SpawnedTiles.Count)
            {
                RebuildTiles();
            }
        }

        private void ConfigureTile(GameObject tileObj, ThemeDefinitionSO theme, bool bindClickHandler)
        {
            bool isActive = string.Equals(theme.ThemeId, m_ActiveThemeId, StringComparison.OrdinalIgnoreCase);
            bool isUnlocked = theme.UnlockedByDefault;
            bool canSelectTheme = m_Selector != null;

            var button = tileObj.GetComponent<Button>() ?? tileObj.GetComponentInChildren<Button>();
            var image = tileObj.transform.Find("Thumbnail")?.GetComponent<Image>() ?? tileObj.GetComponent<Image>();
            var nameText = tileObj.transform.Find("Label_Name")?.GetComponent<TMP_Text>() ?? tileObj.GetComponentInChildren<TMP_Text>();
            var highlight = tileObj.transform.Find("Highlight")?.gameObject;

            if (nameText != null)
            {
                nameText.text = theme.DisplayName;
            }

            if (image != null && theme.Thumbnail != null)
            {
                image.sprite = theme.Thumbnail;
            }

            if (highlight != null)
            {
                highlight.SetActive(isActive);
            }

            var canvasGroup = tileObj.GetComponent<CanvasGroup>() ?? tileObj.AddComponent<CanvasGroup>();
            if (!isUnlocked || !canSelectTheme)
            {
                canvasGroup.alpha = isUnlocked ? 0.6f : 0.45f;
                if (button != null) button.interactable = false;
            }
            else
            {
                canvasGroup.alpha = 1.0f;
                if (button != null)
                {
                    button.interactable = true;
                    if (bindClickHandler)
                    {
                        button.onClick.RemoveAllListeners();
                        string capturedId = theme.ThemeId;
                        button.onClick.AddListener(() => RequestTheme(capturedId));
                    }
                }
            }
        }

        private void RequestTheme(string themeId)
        {
            OnThemeRequested?.Invoke(themeId);

            if (m_Selector == null)
            {
                Debug.LogWarning("[CosmeticsPopup] Cannot change themes because no IThemeSelector is configured.");
                return;
            }

            m_Selector.RequestTheme(themeId);
        }

        private static GameObject CreateDefaultTileObject(RectTransform parent)
        {
            var go = new GameObject("Tile", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 160);

            var bgImage = go.GetComponent<Image>();
            bgImage.color = new Color(0.918f, 0.949f, 1f, 0.65f);

            var thumbGo = new GameObject("Thumbnail", typeof(RectTransform), typeof(Image));
            thumbGo.transform.SetParent(go.transform, false);
            var thumbRt = thumbGo.GetComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0.5f, 0.5f);
            thumbRt.anchorMax = new Vector2(0.5f, 0.5f);
            thumbRt.sizeDelta = new Vector2(100, 100);
            thumbRt.anchoredPosition = new Vector2(0, 15);

            var labelGo = new GameObject("Label_Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 0.3f);
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var text = labelGo.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 20;
            text.color = new Color(0.039f, 0.063f, 0.200f, 1f);

            var highlightGo = new GameObject("Highlight", typeof(RectTransform), typeof(Image));
            highlightGo.transform.SetParent(go.transform, false);
            var hRt = highlightGo.GetComponent<RectTransform>();
            hRt.anchorMin = Vector2.zero;
            hRt.anchorMax = Vector2.one;
            hRt.offsetMin = new Vector2(-4, -4);
            hRt.offsetMax = new Vector2(4, 4);
            var hImg = highlightGo.GetComponent<Image>();
            hImg.color = new Color(0f, 0.282f, 0.941f, 1f);
            hImg.raycastTarget = false;
            highlightGo.transform.SetAsFirstSibling();

            return go;
        }
    }
}

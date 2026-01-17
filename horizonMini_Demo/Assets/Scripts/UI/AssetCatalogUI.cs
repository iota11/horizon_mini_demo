using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using HorizonMini.Build;
using HorizonMini.Controllers;
using System.Collections.Generic;

namespace HorizonMini.UI
{
    /// <summary>
    /// Bottom drawer UI showing categorized placeable assets
    /// Supports drag-and-drop to 3D scene
    /// </summary>
    public class AssetCatalogUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildController buildController;
        [SerializeField] private AssetCatalog assetCatalog;

        [Header("UI Elements")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform categoryTabContainer;
        [SerializeField] private Transform assetGridContainer;
        [SerializeField] private GameObject categoryTabPrefab;
        [SerializeField] private GameObject assetItemPrefab;

        [Header("Settings")]
        [SerializeField] private bool startCollapsed = false;

        private Dictionary<AssetCategory, GameObject> categoryTabs = new Dictionary<AssetCategory, GameObject>();
        private List<GameObject> currentAssetItems = new List<GameObject>();
        private AssetCategory currentCategory = AssetCategory.Furniture;

        private void Start()
        {
            if (assetCatalog != null)
            {
                InitializeCatalog();
            }

            if (startCollapsed)
            {
                Collapse();
            }
        }

        private void InitializeCatalog()
        {
            CreateCategoryTabs();
            ShowCategory(currentCategory);
        }

        private void CreateCategoryTabs()
        {
            if (categoryTabContainer == null || categoryTabPrefab == null)
                return;

            var categories = assetCatalog.GetAvailableCategories();

            foreach (var category in categories)
            {
                GameObject tabObj = Instantiate(categoryTabPrefab, categoryTabContainer);

                TextMeshProUGUI tabText = tabObj.GetComponentInChildren<TextMeshProUGUI>();
                if (tabText != null)
                {
                    tabText.text = category.ToString();
                }

                Button tabButton = tabObj.GetComponent<Button>();
                if (tabButton != null)
                {
                    AssetCategory cat = category; // Capture for lambda
                    tabButton.onClick.AddListener(() => ShowCategory(cat));
                }

                categoryTabs[category] = tabObj;
            }
        }

        public void ShowCategory(AssetCategory category)
        {
            currentCategory = category;

            // Clear current items
            foreach (var item in currentAssetItems)
            {
                Destroy(item);
            }
            currentAssetItems.Clear();

            // Get assets for category
            var assets = assetCatalog.GetAssetsByCategory(category);

            // Create asset items
            foreach (var asset in assets)
            {
                CreateAssetItem(asset);
            }

            // Update tab visuals
            UpdateTabHighlight();
        }

        private void CreateAssetItem(PlaceableAsset asset)
        {
            if (assetGridContainer == null || assetItemPrefab == null)
                return;

            GameObject itemObj = Instantiate(assetItemPrefab, assetGridContainer);

            // Set icon
            Image iconImage = itemObj.GetComponentInChildren<Image>();
            if (iconImage != null && asset.icon != null)
            {
                iconImage.sprite = asset.icon;
            }

            // Set label
            TextMeshProUGUI labelText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null)
            {
                labelText.text = asset.displayName;
            }

            // Add drag handler
            AssetItemDragHandler dragHandler = itemObj.GetComponent<AssetItemDragHandler>();
            if (dragHandler == null)
            {
                dragHandler = itemObj.AddComponent<AssetItemDragHandler>();
            }
            dragHandler.Initialize(asset, this);

            currentAssetItems.Add(itemObj);
        }

        private void UpdateTabHighlight()
        {
            foreach (var kvp in categoryTabs)
            {
                Button button = kvp.Value.GetComponent<Button>();
                if (button != null)
                {
                    ColorBlock colors = button.colors;
                    colors.normalColor = (kvp.Key == currentCategory) ? Color.yellow : Color.white;
                    button.colors = colors;
                }
            }
        }

        public void OnAssetDragStart(PlaceableAsset asset)
        {
            if (buildController != null)
            {
                var placementSystem = buildController.GetComponent<PlacementSystem>();
                if (placementSystem != null)
                {
                    placementSystem.StartDragging(asset);
                }
            }
        }

        public void OnAssetDragUpdate(PlaceableAsset asset, Vector2 screenPosition)
        {
            if (buildController != null)
            {
                var placementSystem = buildController.GetComponent<PlacementSystem>();
                if (placementSystem != null)
                {
                    placementSystem.UpdateDragging(screenPosition);
                }
            }
        }

        public void OnAssetDragEnd(PlaceableAsset asset, Vector2 screenPosition)
        {
            if (buildController != null)
            {
                var placementSystem = buildController.GetComponent<PlacementSystem>();
                if (placementSystem != null)
                {
                    placementSystem.EndDragging(screenPosition);
                }
            }
        }

        public void Show()
        {
            if (panel != null)
            {
                panel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        public void Collapse()
        {
            // TODO: Animate panel to collapsed state
            if (assetGridContainer != null)
            {
                assetGridContainer.gameObject.SetActive(false);
            }
        }

        public void Expand()
        {
            // TODO: Animate panel to expanded state
            if (assetGridContainer != null)
            {
                assetGridContainer.gameObject.SetActive(true);
            }
        }

        public void SetCatalog(AssetCatalog catalog)
        {
            assetCatalog = catalog;
            InitializeCatalog();
        }

        public void SetBuildController(BuildController controller)
        {
            buildController = controller;
        }
    }

    /// <summary>
    /// Handles drag events for individual asset items
    /// </summary>
    public class AssetItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private PlaceableAsset asset;
        private AssetCatalogUI catalogUI;

        public void Initialize(PlaceableAsset placeableAsset, AssetCatalogUI ui)
        {
            asset = placeableAsset;
            catalogUI = ui;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (catalogUI != null && asset != null)
            {
                catalogUI.OnAssetDragStart(asset);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (catalogUI != null && asset != null)
            {
                catalogUI.OnAssetDragUpdate(asset, eventData.position);
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (catalogUI != null && asset != null)
            {
                catalogUI.OnAssetDragEnd(asset, eventData.position);
            }
        }
    }
}

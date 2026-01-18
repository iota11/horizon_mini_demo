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
        [SerializeField] private Camera buildCamera;

        [Header("UI Elements")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Transform categoryTabContainer;
        [SerializeField] private Transform assetGridContainer;
        [SerializeField] private GameObject categoryTabPrefab;
        [SerializeField] private GameObject assetItemPrefab;
        [SerializeField] private UnityEngine.UI.Button toggleButton;

        [Header("Settings")]
        [SerializeField] private bool startCollapsed = false;
        [SerializeField] private float animationDuration = 0.3f; // Duration of expand/collapse animation
        [SerializeField] private float toggleButtonHeight = 60f; // Height of toggle button

        private Dictionary<AssetCategory, GameObject> categoryTabs = new Dictionary<AssetCategory, GameObject>();
        private List<GameObject> currentAssetItems = new List<GameObject>();
        private AssetCategory currentCategory = AssetCategory.Furniture;
        private bool isExpanded = true;
        private RectTransform panelRect;
        private float expandedYPosition; // Expanded position (anchoredPosition.y)
        private float collapsedYPosition; // Collapsed position (moved down to hide content)
        private Coroutine animationCoroutine;

        private void Start()
        {
            Debug.Log("AssetCatalogUI Start() called");
            Debug.Log($"AssetCatalog: {(assetCatalog != null ? "assigned" : "NULL")}");
            Debug.Log($"CategoryTabContainer: {(categoryTabContainer != null ? "assigned" : "NULL")}");
            Debug.Log($"AssetGridContainer: {(assetGridContainer != null ? "assigned" : "NULL")}");
            Debug.Log($"CategoryTabPrefab: {(categoryTabPrefab != null ? "assigned" : "NULL")}");
            Debug.Log($"AssetItemPrefab: {(assetItemPrefab != null ? "assigned" : "NULL")}");

            // Get panel RectTransform and calculate positions
            if (panel != null)
            {
                panelRect = panel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    // Store expanded position (default position, y=0)
                    expandedYPosition = 0f;

                    // Calculate collapsed position (move panel down so only toggle button shows)
                    // Panel height = anchorMax.y * screen height (0.4 = 40% of screen)
                    // We want to move down by (panel height - toggle button height)
                    float panelHeightRatio = panelRect.anchorMax.y - panelRect.anchorMin.y; // Should be 0.4
                    Canvas canvas = panelRect.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                        float screenHeight = canvasRect.rect.height;
                        float panelHeight = screenHeight * panelHeightRatio;

                        // Move down by (panel height - toggle button height) to hide content
                        collapsedYPosition = -(panelHeight - toggleButtonHeight);

                        Debug.Log($"Panel setup: screenHeight={screenHeight}, panelHeight={panelHeight}, collapsedY={collapsedYPosition}");
                    }
                }
            }

            // Setup toggle button
            if (toggleButton != null)
            {
                toggleButton.onClick.AddListener(ToggleExpandCollapse);
            }

            if (assetCatalog != null)
            {
                InitializeCatalog();
            }
            else
            {
                Debug.LogError("AssetCatalog is null in Start()! Cannot initialize catalog.");
            }

            // Note: startCollapsed check removed - use Expand/Collapse methods manually if needed
            // InitializeCatalog already calls Expand() at the end
        }

        private void InitializeCatalog()
        {
            CreateCategoryTabs();
            ShowCategory(currentCategory);

            // Force expand after initialization
            Expand();
        }

        private void CreateCategoryTabs()
        {
            if (categoryTabContainer == null)
            {
                Debug.LogError("Category Tab Container is null! Please assign it in the Inspector.");
                return;
            }

            if (categoryTabPrefab == null)
            {
                Debug.LogError("Category Tab Prefab is null! Please assign it in the Inspector.");
                return;
            }

            if (assetCatalog == null)
            {
                Debug.LogError("Asset Catalog is null! Please assign it in the Inspector.");
                return;
            }

            var categories = assetCatalog.GetAvailableCategories();
            Debug.Log($"Creating tabs for {categories.Count} categories");

            foreach (var category in categories)
            {
                GameObject tabObj = Instantiate(categoryTabPrefab, categoryTabContainer);
                Debug.Log($"Created tab for category: {category}");

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
            Debug.Log($"ShowCategory called for: {category}");
            currentCategory = category;

            // Clear current items
            foreach (var item in currentAssetItems)
            {
                Destroy(item);
            }
            currentAssetItems.Clear();

            if (assetCatalog == null)
            {
                Debug.LogError("AssetCatalog is null! Please assign it in the Inspector.");
                return;
            }

            // Get assets for category
            var assets = assetCatalog.GetAssetsByCategory(category);
            Debug.Log($"Found {assets.Count} assets in category {category}");

            if (assets.Count == 0)
            {
                Debug.LogWarning($"No assets found in category {category}. Check if assets are added to the catalog.");
                return;
            }

            // Create asset items
            foreach (var asset in assets)
            {
                CreateAssetItem(asset);
            }

            Debug.Log($"Created {currentAssetItems.Count} asset items");

            // Update tab visuals
            UpdateTabHighlight();
        }

        private void CreateAssetItem(PlaceableAsset asset)
        {
            if (assetGridContainer == null || assetItemPrefab == null)
                return;

            GameObject itemObj = Instantiate(assetItemPrefab, assetGridContainer);

            // Set icon - find the Icon child object specifically
            Transform iconTransform = itemObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                Image iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null && asset.icon != null)
                {
                    iconImage.sprite = asset.icon;
                }
            }

            // Set label
            Transform labelTransform = itemObj.transform.Find("Label");
            if (labelTransform != null)
            {
                TextMeshProUGUI labelText = labelTransform.GetComponent<TextMeshProUGUI>();
                if (labelText != null)
                {
                    labelText.text = asset.displayName;
                }
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

        public void ToggleExpandCollapse()
        {
            if (isExpanded)
            {
                Collapse();
            }
            else
            {
                Expand();
            }
        }

        public void Collapse()
        {
            if (panelRect == null)
                return;

            // Stop any existing animation
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            // Start collapse animation - move panel down
            animationCoroutine = StartCoroutine(AnimatePanelPosition(collapsedYPosition, false));

            // Update button text
            UpdateToggleButtonText("▲ Expand");

            isExpanded = false;
            Debug.Log("Asset Catalog collapsing");
        }

        public void Expand()
        {
            if (panelRect == null)
                return;

            // Stop any existing animation
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            // Start expand animation - move panel up
            animationCoroutine = StartCoroutine(AnimatePanelPosition(expandedYPosition, true));

            // Update button text
            UpdateToggleButtonText("▼ Collapse");

            isExpanded = true;
            Debug.Log("Asset Catalog expanding");
        }

        private System.Collections.IEnumerator AnimatePanelPosition(float targetYPosition, bool showContent)
        {
            Vector2 startPos = panelRect.anchoredPosition;
            Vector2 targetPos = new Vector2(startPos.x, targetYPosition);
            float elapsed = 0f;

            // If collapsing, hide content immediately
            if (!showContent)
            {
                if (assetGridContainer != null)
                {
                    assetGridContainer.gameObject.SetActive(false);
                }

                if (categoryTabContainer != null)
                {
                    categoryTabContainer.gameObject.SetActive(false);
                }
            }
            else
            {
                // If expanding, show content immediately
                if (assetGridContainer != null)
                {
                    assetGridContainer.gameObject.SetActive(true);
                }

                if (categoryTabContainer != null)
                {
                    categoryTabContainer.gameObject.SetActive(true);
                }
            }

            // Animate the panel position (move up/down)
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);

                // Smooth easing
                t = t * t * (3f - 2f * t); // Smoothstep

                panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                yield return null;
            }

            // Ensure final position is exact
            panelRect.anchoredPosition = targetPos;
            animationCoroutine = null;
        }

        private void UpdateToggleButtonText(string text)
        {
            if (toggleButton != null)
            {
                TextMeshProUGUI buttonText = toggleButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = text;
                }
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

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Creates a properly structured Asset Catalog UI
    /// Left sidebar: Category tabs (vertical scroll)
    /// Right panel: Asset grid (5 per row, vertical scroll)
    /// </summary>
    public class CreateAssetCatalogUI : EditorWindow
    {
        [MenuItem("Tools/Create Asset Catalog UI")]
        public static void ShowWindow()
        {
            GetWindow<CreateAssetCatalogUI>("Create Asset Catalog UI");
        }

        private Canvas targetCanvas;

        private void OnGUI()
        {
            GUILayout.Label("Create Asset Catalog UI", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This will create a new Asset Catalog UI with:\n" +
                "- Left sidebar with vertical category tabs\n" +
                "- Right panel with asset grid (5 per row)\n" +
                "- Both areas scrollable",
                MessageType.Info
            );

            EditorGUILayout.Space();

            targetCanvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas:", targetCanvas, typeof(Canvas), true);

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(targetCanvas == null);
            {
                if (GUILayout.Button("Create UI", GUILayout.Height(40)))
                {
                    CreateUI();
                }
            }
            EditorGUI.EndDisabledGroup();

            if (targetCanvas == null)
            {
                EditorGUILayout.HelpBox("Please select a Canvas first.", MessageType.Warning);
            }
        }

        private void CreateUI()
        {
            // Create main panel
            GameObject panel = new GameObject("AssetCatalogPanel");
            panel.transform.SetParent(targetCanvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();

            // Anchor to bottom of screen, take 40% height
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.4f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            // Add vertical layout to stack toggle button and content
            VerticalLayoutGroup vLayout = panel.AddComponent<VerticalLayoutGroup>();
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = true; // Need to control height for flexible layout to work
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.spacing = 0;
            vLayout.padding = new RectOffset(0, 0, 0, 0);

            // TOP: Toggle button
            GameObject toggleButton = CreateToggleButton(panel.transform);

            // BOTTOM: Content area (category + assets)
            GameObject contentArea = CreateContentArea(panel.transform);

            // LEFT SIDEBAR: Category tabs
            GameObject categoryPanel = CreateCategoryPanel(contentArea.transform);

            // RIGHT PANEL: Asset grid
            GameObject assetPanel = CreateAssetPanel(contentArea.transform);

            // Find prefabs and other components
            string categoryTabPrefabPath = "Assets/Prefabs/UI/CategoryTabPrefab.prefab";
            string assetItemPrefabPath = "Assets/Prefabs/UI/AssetItemPrefab.prefab";
            GameObject categoryTabPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(categoryTabPrefabPath);
            GameObject assetItemPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetItemPrefabPath);

            // Find CategoryTabContainer and AssetGridContainer
            Transform categoryTabContainer = FindChildRecursive(panel.transform, "CategoryTabContainer");
            Transform assetGridContainer = FindChildRecursive(panel.transform, "AssetGridContainer");

            // Try to find BuildController and AssetCatalog in scene
            HorizonMini.Controllers.BuildController buildController = GameObject.FindFirstObjectByType<HorizonMini.Controllers.BuildController>();
            HorizonMini.Build.AssetCatalog assetCatalog = AssetDatabase.FindAssets("t:AssetCatalog")
                .Select(guid => AssetDatabase.LoadAssetAtPath<HorizonMini.Build.AssetCatalog>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();

            // Add AssetCatalogUI component
            var catalogUI = panel.AddComponent<HorizonMini.UI.AssetCatalogUI>();

            // Auto-assign all references
            SerializedObject so = new SerializedObject(catalogUI);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("toggleButton").objectReferenceValue = toggleButton.GetComponent<Button>();
            so.FindProperty("categoryTabContainer").objectReferenceValue = categoryTabContainer;
            so.FindProperty("assetGridContainer").objectReferenceValue = assetGridContainer;
            so.FindProperty("categoryTabPrefab").objectReferenceValue = categoryTabPrefabAsset;
            so.FindProperty("assetItemPrefab").objectReferenceValue = assetItemPrefabAsset;

            if (buildController != null)
            {
                so.FindProperty("buildController").objectReferenceValue = buildController;
            }

            if (assetCatalog != null)
            {
                so.FindProperty("assetCatalog").objectReferenceValue = assetCatalog;
            }

            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(panel);

            // Build success message
            string message = "Asset Catalog UI created successfully!\n\nAuto-assigned references:\n";
            message += "✓ Panel\n";
            message += "✓ Toggle Button\n";
            message += categoryTabContainer != null ? "✓ Category Tab Container\n" : "✗ Category Tab Container (not found)\n";
            message += assetGridContainer != null ? "✓ Asset Grid Container\n" : "✗ Asset Grid Container (not found)\n";
            message += categoryTabPrefabAsset != null ? "✓ Category Tab Prefab\n" : "✗ Category Tab Prefab (not found)\n";
            message += assetItemPrefabAsset != null ? "✓ Asset Item Prefab\n" : "✗ Asset Item Prefab (not found)\n";
            message += buildController != null ? "✓ Build Controller\n" : "⚠ Build Controller (not found in scene)\n";
            message += assetCatalog != null ? "✓ Asset Catalog\n" : "⚠ Asset Catalog (not found in project)\n";

            EditorUtility.DisplayDialog("Success", message, "OK");
            Selection.activeGameObject = panel;
        }

        private GameObject CreateToggleButton(Transform parent)
        {
            GameObject button = CreateUIObject("ToggleButton", parent);
            RectTransform btnRect = button.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(0, 60); // Full width, 60px height (larger for easier touch)

            LayoutElement layout = button.AddComponent<LayoutElement>();
            layout.preferredHeight = 60;
            layout.flexibleHeight = 0;

            Image btnBg = button.AddComponent<Image>();
            btnBg.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            Button btnComp = button.AddComponent<Button>();
            ColorBlock colors = btnComp.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            btnComp.colors = colors;

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(button.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "▼ Collapse";
            text.fontSize = 20; // Larger font for bigger button
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return button;
        }

        private GameObject CreateContentArea(Transform parent)
        {
            GameObject contentArea = CreateUIObject("ContentArea", parent);
            RectTransform contentRect = contentArea.GetComponent<RectTransform>();

            LayoutElement layout = contentArea.AddComponent<LayoutElement>();
            layout.preferredHeight = 500; // Explicit height for content area
            layout.flexibleHeight = 1; // Also allow it to grow if space available

            // Horizontal layout for category + assets
            HorizontalLayoutGroup hLayout = contentArea.AddComponent<HorizontalLayoutGroup>();
            hLayout.childControlWidth = true; // Control children width for flexible width to work
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = true;
            hLayout.spacing = 10;
            hLayout.padding = new RectOffset(10, 10, 10, 10);

            return contentArea;
        }

        private GameObject CreateCategoryPanel(Transform parent)
        {
            // Container
            GameObject container = CreateUIObject("CategoryPanel", parent);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(150, 0); // Fixed width 150

            LayoutElement layout = container.AddComponent<LayoutElement>();
            layout.preferredWidth = 150;
            layout.flexibleWidth = 0;
            layout.flexibleHeight = 1; // Fill available vertical space

            // Background
            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Scroll view
            GameObject scrollView = CreateUIObject("CategoryScrollView", container.transform);
            RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            scroll.viewport = viewportRect;

            // Content (category tab container)
            GameObject content = CreateUIObject("CategoryTabContainer", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 300);

            VerticalLayoutGroup vLayout = content.AddComponent<VerticalLayoutGroup>();
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;
            vLayout.spacing = 5;
            vLayout.padding = new RectOffset(5, 5, 5, 5);

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;

            // Create category tab prefab
            CreateCategoryTabPrefab(content.transform);

            return container;
        }

        private void CreateCategoryTabPrefab(Transform parent)
        {
            GameObject tab = CreateUIObject("CategoryTabPrefab", parent);
            RectTransform tabRect = tab.GetComponent<RectTransform>();
            tabRect.sizeDelta = new Vector2(0, 50);

            LayoutElement layout = tab.AddComponent<LayoutElement>();
            layout.preferredHeight = 50;

            Button button = tab.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f, 1f);
            colors.pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            colors.selectedColor = new Color(1f, 0.8f, 0f, 1f);
            button.colors = colors;

            Image bg = tab.AddComponent<Image>();
            bg.color = Color.white;
            button.targetGraphic = bg;

            // Text
            GameObject textObj = CreateUIObject("Text", tab.transform);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(5, 5);
            textRect.offsetMax = new Vector2(-5, -5);

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "Category";
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            // Save as prefab
            string prefabPath = "Assets/Prefabs/UI/CategoryTabPrefab.prefab";
            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(tab, prefabPath);

            Debug.Log($"Created category tab prefab at: {prefabPath}");
        }

        private GameObject CreateAssetPanel(Transform parent)
        {
            // Container
            GameObject container = CreateUIObject("AssetPanel", parent);
            RectTransform containerRect = container.GetComponent<RectTransform>();

            LayoutElement layout = container.AddComponent<LayoutElement>();
            layout.flexibleWidth = 1;
            layout.flexibleHeight = 1; // Fill available vertical space

            // Background
            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            // Scroll view
            GameObject scrollView = CreateUIObject("AssetScrollView", container.transform);
            RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            scroll.viewport = viewportRect;

            // Content (asset grid container)
            GameObject content = CreateUIObject("AssetGridContainer", viewport.transform);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 500);

            // Grid layout: 5 items per row
            GridLayoutGroup gridLayout = content.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(100, 120);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.padding = new RectOffset(10, 10, 10, 10);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5; // 5 per row
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;

            // Create asset item prefab
            CreateAssetItemPrefab(content.transform);

            return container;
        }

        private void CreateAssetItemPrefab(Transform parent)
        {
            GameObject item = CreateUIObject("AssetItemPrefab", parent);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(100, 120);

            // Background
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.35f, 0.35f, 0.35f, 1f);

            // Icon
            GameObject iconObj = CreateUIObject("Icon", item.transform);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0.3f);
            iconRect.anchorMax = new Vector2(1, 1);
            iconRect.offsetMin = new Vector2(5, 5);
            iconRect.offsetMax = new Vector2(-5, -5);

            Image icon = iconObj.AddComponent<Image>();
            icon.color = Color.white;
            icon.preserveAspect = true;

            // Label
            GameObject labelObj = CreateUIObject("Label", item.transform);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0.3f);
            labelRect.offsetMin = new Vector2(2, 2);
            labelRect.offsetMax = new Vector2(-2, -2);

            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "Asset Name";
            label.fontSize = 10;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Ellipsis;

            // Save as prefab
            string prefabPath = "Assets/Prefabs/UI/AssetItemPrefab.prefab";
            System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(item, prefabPath);

            Debug.Log($"Created asset item prefab at: {prefabPath}");
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();

            // Set default anchors to stretch
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);

            return obj;
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
                return parent;

            foreach (Transform child in parent)
            {
                Transform found = FindChildRecursive(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}

using UnityEngine;
using UnityEditor;
using HorizonMini.Build;
using System.Linq;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Batch add PlaceableAssets to AssetCatalog
    /// </summary>
    public class AddAssetsToLibrary : EditorWindow
    {
        [MenuItem("Tools/Add Assets to Library")]
        public static void ShowWindow()
        {
            GetWindow<AddAssetsToLibrary>("Add Assets to Library");
        }

        private AssetCatalog catalog;
        private string[] searchFolders = new string[] { "Assets/ScriptableObjects/PlaceableAssets" };
        private int addedCount = 0;

        private void OnGUI()
        {
            GUILayout.Label("Add Assets to Library", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool scans specified folders for PlaceableAssets and adds them to the AssetCatalog.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            catalog = (AssetCatalog)EditorGUILayout.ObjectField("Asset Catalog:", catalog, typeof(AssetCatalog), false);

            EditorGUILayout.Space();
            GUILayout.Label("Search Folders:", EditorStyles.boldLabel);

            for (int i = 0; i < searchFolders.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                searchFolders[i] = EditorGUILayout.TextField(searchFolders[i]);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        searchFolders[i] = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(catalog == null);
            {
                if (GUILayout.Button("Scan and Add Assets", GUILayout.Height(40)))
                {
                    ScanAndAddAssets();
                }
            }
            EditorGUI.EndDisabledGroup();

            if (catalog == null)
            {
                EditorGUILayout.HelpBox("Please assign an AssetCatalog first.", MessageType.Warning);
            }

            if (addedCount > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox($"Added {addedCount} new assets to catalog!", MessageType.Info);
            }
        }

        private void ScanAndAddAssets()
        {
            addedCount = 0;

            // Get all existing assets in catalog to avoid duplicates
            var existingAssets = catalog.GetAllAssets();

            // Find all PlaceableAssets in specified folders
            string[] assetGuids = AssetDatabase.FindAssets("t:PlaceableAsset", searchFolders);

            foreach (string guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PlaceableAsset asset = AssetDatabase.LoadAssetAtPath<PlaceableAsset>(path);

                if (asset != null && !existingAssets.Contains(asset))
                {
                    catalog.AddAsset(asset);
                    addedCount++;
                    Debug.Log($"Added: {asset.displayName} ({path})");
                }
            }

            if (addedCount > 0)
            {
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=green>Successfully added {addedCount} assets to catalog!</color>");
            }
            else
            {
                Debug.Log("No new assets found to add.");
            }
        }
    }
}

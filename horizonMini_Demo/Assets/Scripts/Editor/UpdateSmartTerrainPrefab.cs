using UnityEngine;
using UnityEditor;
using HorizonMini.Build;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to update existing SmartTerrain prefab with BoxCollider
    /// </summary>
    public class UpdateSmartTerrainPrefab
    {
        [MenuItem("HorizonMini/Update SmartTerrain Prefab (Add Collider)")]
        public static void UpdatePrefab()
        {
            string prefabPath = "Assets/Prefabs/SmartTerrain/SmartTerrain.prefab";

            // Load the prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"SmartTerrain prefab not found at: {prefabPath}");
                EditorUtility.DisplayDialog("Error", "SmartTerrain prefab not found!\n\nPlease create it first using:\nHorizonMini > Create SmartTerrain Asset", "OK");
                return;
            }

            // Load prefab contents
            GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

            // Add BoxCollider if not exists
            BoxCollider boxCollider = prefabInstance.GetComponent<BoxCollider>();
            if (boxCollider == null)
            {
                boxCollider = prefabInstance.AddComponent<BoxCollider>();
                Debug.Log("Added BoxCollider to SmartTerrain prefab");
            }
            else
            {
                Debug.Log("BoxCollider already exists on SmartTerrain prefab");
            }

            // Update SmartTerrain component reference
            SmartTerrain smartTerrain = prefabInstance.GetComponent<SmartTerrain>();
            if (smartTerrain != null)
            {
                SerializedObject so = new SerializedObject(smartTerrain);
                so.FindProperty("boxCollider").objectReferenceValue = boxCollider;
                so.ApplyModifiedProperties();
                Debug.Log("Updated SmartTerrain boxCollider reference");
            }

            // Save the updated prefab
            PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            PrefabUtility.UnloadPrefabContents(prefabInstance);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green>SmartTerrain prefab updated successfully!</color>");
            EditorUtility.DisplayDialog("Success", "SmartTerrain prefab updated!\n\nBoxCollider has been added.", "OK");
        }
    }
}

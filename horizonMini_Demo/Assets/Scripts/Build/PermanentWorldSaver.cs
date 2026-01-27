using UnityEngine;
using HorizonMini.Data;
using HorizonMini.Core;
using System.Collections.Generic;

namespace HorizonMini.Build
{
    /// <summary>
    /// Helper class to save a world as permanent (non-deletable) from Build mode
    /// </summary>
    public class PermanentWorldSaver
    {
        /// <summary>
        /// Save current world as a permanent WorldData asset
        /// </summary>
        public static void SaveAsPermanent(string worldId, string worldTitle, string worldAuthor, SaveService saveService)
        {
            if (string.IsNullOrEmpty(worldId))
            {
                Debug.LogError("[PermanentWorldSaver] World ID is null or empty");
                return;
            }

            // Load the current world data
            WorldData worldData = saveService.LoadCreatedWorld(worldId);
            if (worldData == null)
            {
                Debug.LogError($"[PermanentWorldSaver] Failed to load world {worldId}");
                return;
            }

#if UNITY_EDITOR
            // Create asset path
            string savePath = "Assets/Data/ManualWorlds";
            if (!UnityEditor.AssetDatabase.IsValidFolder(savePath))
            {
                // Create folders
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Data"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Data");
                }
                UnityEditor.AssetDatabase.CreateFolder("Assets/Data", "ManualWorlds");
            }

            // Create new WorldData asset (duplicate from runtime data)
            WorldData permanentWorld = ScriptableObject.CreateInstance<WorldData>();

            // Copy all data
            permanentWorld.worldId = worldData.worldId;
            permanentWorld.worldTitle = !string.IsNullOrEmpty(worldTitle) ? worldTitle : worldData.worldTitle;
            permanentWorld.worldAuthor = !string.IsNullOrEmpty(worldAuthor) ? worldAuthor : worldData.worldAuthor;
            permanentWorld.isDraft = false;
            permanentWorld.gridDimensions = worldData.gridDimensions;
            permanentWorld.skyColor = worldData.skyColor;
            permanentWorld.gravity = worldData.gravity;

            // Deep copy volumes
            permanentWorld.volumes = new List<VolumeCell>(worldData.volumes);

            // Deep copy props
            permanentWorld.props = new List<PropData>();
            foreach (var prop in worldData.props)
            {
                PropData newProp = new PropData
                {
                    propId = prop.propId,
                    prefabName = prop.prefabName,
                    position = prop.position,
                    rotation = prop.rotation,
                    scale = prop.scale,
                    smartTerrainControlPoint = prop.smartTerrainControlPoint,
                    smartWallControlPoints = prop.smartWallControlPoints != null ?
                        new List<Vector3>(prop.smartWallControlPoints) : new List<Vector3>(),
                    smartWallHeight = prop.smartWallHeight
                };
                permanentWorld.props.Add(newProp);
            }

            // Save asset
            string fileName = $"World_{permanentWorld.worldTitle.Replace(" ", "_")}_{System.DateTime.Now:yyyyMMdd_HHmmss}.asset";
            string assetPath = $"{savePath}/{fileName}";

            UnityEditor.AssetDatabase.CreateAsset(permanentWorld, assetPath);

            // Add to permanent registry
            AddToPermanentRegistry(permanentWorld.worldId, assetPath);

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"<color=green>✓ World '{permanentWorld.worldTitle}' saved as PERMANENT at {assetPath}</color>");

            // Ping asset in Project window
            UnityEditor.Selection.activeObject = permanentWorld;
            UnityEditor.EditorGUIUtility.PingObject(permanentWorld);
#else
            Debug.LogWarning("[PermanentWorldSaver] Cannot save as permanent in build mode - only available in Editor");
#endif
        }

#if UNITY_EDITOR
        private static void AddToPermanentRegistry(string worldId, string assetPath)
        {
            string registryPath = "Assets/Data/PermanentWorldsRegistry.asset";

            PermanentWorldsRegistry registry = UnityEditor.AssetDatabase.LoadAssetAtPath<PermanentWorldsRegistry>(registryPath);

            if (registry == null)
            {
                // Create new registry
                registry = ScriptableObject.CreateInstance<PermanentWorldsRegistry>();

                // Ensure Data folder exists
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Data"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Data");
                }

                UnityEditor.AssetDatabase.CreateAsset(registry, registryPath);
            }

            // Add to registry
            if (!registry.permanentWorldIds.Contains(worldId))
            {
                registry.permanentWorldIds.Add(worldId);
                registry.permanentWorldPaths.Add(assetPath);
                UnityEditor.EditorUtility.SetDirty(registry);
                UnityEditor.AssetDatabase.SaveAssets();

                Debug.Log($"<color=cyan>✓ Added world {worldId} to permanent registry</color>");
            }
            else
            {
                Debug.Log($"<color=yellow>World {worldId} already in permanent registry</color>");
            }
        }
#endif
    }
}

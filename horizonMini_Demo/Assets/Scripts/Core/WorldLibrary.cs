using UnityEngine;
using System.Collections.Generic;
using HorizonMini.Data;
using HorizonMini.Build;

namespace HorizonMini.Core
{

    /// <summary>
    /// Manages the library of available worlds (both built-in and user-created)
    /// </summary>
    /// 
    ///
    
    public class WorldLibrary : MonoBehaviour
    {
        [Header("Built-in Worlds")]
        [SerializeField] private List<WorldData> builtInWorlds = new List<WorldData>();

        [Header("Prefabs")]
        [SerializeField] private GameObject volumePrefab;
        [SerializeField] private GridSettings gridSettings;

        [Header("Asset Catalog")]
        [SerializeField] private AssetCatalog assetCatalog;

        private List<WorldMeta> allWorldMetas = new List<WorldMeta>();
        private SaveService saveService;

        public void Initialize(SaveService save)
        {
            saveService = save;
            RefreshLibrary();
        }

        public void RefreshLibrary()
        {
            allWorldMetas.Clear();

            // Skip built-in worlds - only show user-created worlds
            // foreach (var worldData in builtInWorlds)
            // {
            //     if (worldData != null)
            //     {
            //         worldData.Initialize();
            //         allWorldMetas.Add(worldData.ToMeta());
            //     }
            // }

            // Add user-created worlds only
            if (saveService != null)
            {
                foreach (string worldId in saveService.GetCreatedWorldIds())
                {
                    WorldData userData = saveService.LoadCreatedWorld(worldId);
                    if (userData != null)
                    {
                        allWorldMetas.Add(userData.ToMeta());
                    }
                }
            }
        }

        public List<WorldMeta> GetAllWorlds()
        {
            return new List<WorldMeta>(allWorldMetas);
        }

        public WorldMeta GetWorldMeta(string worldId)
        {
            return allWorldMetas.Find(w => w.id == worldId);
        }

        public WorldData GetWorldData(string worldId)
        {
            // First check built-in
            WorldData builtIn = builtInWorlds.Find(w => w.worldId == worldId);
            if (builtIn != null)
            {
                return builtIn;
            }

            // Then check user-created
            if (saveService != null)
            {
                return saveService.LoadCreatedWorld(worldId);
            }

            return null;
        }

        public WorldInstance InstantiateWorld(string worldId, Transform parent = null)
        {
            WorldData data = GetWorldData(worldId);
            if (data == null)
            {
                Debug.LogError($"World {worldId} not found");
                return null;
            }

            return InstantiateWorld(data, parent);
        }

        public WorldInstance InstantiateWorld(WorldData data, Transform parent = null)
        {
            // Create container
            GameObject container = new GameObject($"World_{data.worldTitle}");
            if (parent != null)
            {
                container.transform.SetParent(parent);
            }
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;

            // Add WorldInstance component
            WorldInstance instance = container.AddComponent<WorldInstance>();
            instance.Initialize(data, gridSettings);

            // Instantiate volumes
            foreach (var volumeCell in data.volumes)
            {
                Vector3 worldPos = gridSettings.GridToWorldPosition(volumeCell.gridPosition);
                GameObject volumeObj = Instantiate(volumePrefab, container.transform);
                volumeObj.transform.localPosition = worldPos;
                volumeObj.transform.localRotation = Quaternion.Euler(0, volumeCell.rotationY, 0);
                volumeObj.name = $"Volume_{volumeCell.gridPosition.x}_{volumeCell.gridPosition.y}_{volumeCell.gridPosition.z}";
            }

            // Instantiate props (placed objects)
            if (data.props != null && data.props.Count > 0)
            {
                foreach (var propData in data.props)
                {
                    InstantiateProp(propData, container.transform);
                }
            }

            return instance;
        }

        private void InstantiateProp(PropData propData, Transform parent)
        {
            if (assetCatalog == null)
            {
                Debug.LogWarning("AssetCatalog not assigned to WorldLibrary! Cannot load props.");
                return;
            }

            // Find asset by ID
            PlaceableAsset asset = assetCatalog.GetAssetById(propData.prefabName);
            if (asset == null || asset.prefab == null)
            {
                Debug.LogWarning($"Asset not found for ID: {propData.prefabName}");
                return;
            }

            // Instantiate prefab
            GameObject obj = Instantiate(asset.prefab, parent);

            // Check if it's a SmartTerrain - restore control point position BEFORE setting transform
            // This ensures the mesh is regenerated with correct size
            HorizonMini.Build.SmartTerrain terrain = obj.GetComponent<HorizonMini.Build.SmartTerrain>();
            if (terrain != null)
            {
                Debug.Log($"[BROWSE LOAD] Found SmartTerrain: {obj.name}");
                Debug.Log($"[BROWSE LOAD] Saved control point data: {propData.smartTerrainControlPoint}");
                Debug.Log($"[BROWSE LOAD] Current control point exists: {terrain.controlPoint != null}");
                if (terrain.controlPoint != null)
                {
                    Debug.Log($"[BROWSE LOAD] Current control point position BEFORE restore: {terrain.controlPoint.localPosition}");
                }

                // Check if we have saved control point data (will be zero if not saved or default)
                if (propData.smartTerrainControlPoint != Vector3.zero)
                {
                    terrain.SetControlPointPosition(propData.smartTerrainControlPoint, forceImmediate: true);
                    Debug.Log($"[BROWSE LOAD] ✓ Called SetControlPointPosition with: {propData.smartTerrainControlPoint}");
                    if (terrain.controlPoint != null)
                    {
                        Debug.Log($"[BROWSE LOAD] Control point position AFTER restore: {terrain.controlPoint.localPosition}");
                    }
                    Debug.Log($"[BROWSE LOAD] Resulting terrain size: {terrain.GetSize()}");
                }
                else
                {
                    Debug.LogWarning($"[BROWSE LOAD] SmartTerrain loaded without saved control point data (was zero) - using default");
                    Debug.LogWarning($"[BROWSE LOAD] Using default control point position");
                }
            }

            obj.transform.position = propData.position;
            obj.transform.rotation = propData.rotation;
            obj.transform.localScale = propData.scale;
            obj.name = asset.displayName;

            // Fix materials to URP
            FixMaterialsToURP(obj);

            // Add PlacedObject component
            PlacedObject placedObj = obj.AddComponent<PlacedObject>();
            placedObj.assetId = propData.prefabName;
            placedObj.sourceAsset = asset;
            placedObj.UpdateSavedTransform();
        }

        private void FixMaterialsToURP(GameObject obj)
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogWarning("URP Lit shader not found!");
                return;
            }

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                    continue;

                Material[] newMaterials = new Material[renderer.sharedMaterials.Length];

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    Material oldMat = renderer.sharedMaterials[i];

                    if (oldMat == null)
                    {
                        newMaterials[i] = oldMat;
                        continue;
                    }

                    bool needsFix = oldMat.shader == null ||
                                   oldMat.shader.name.Contains("Standard") ||
                                   oldMat.shader.name.Contains("Legacy") ||
                                   oldMat.shader.name == "Hidden/InternalErrorShader";

                    if (needsFix)
                    {
                        Material newMat = new Material(urpLitShader);

                        if (oldMat.HasProperty("_Color"))
                        {
                            newMat.SetColor("_BaseColor", oldMat.color);
                        }
                        else if (oldMat.HasProperty("_BaseColor"))
                        {
                            newMat.SetColor("_BaseColor", oldMat.GetColor("_BaseColor"));
                        }

                        if (oldMat.HasProperty("_MainTex") && oldMat.mainTexture != null)
                        {
                            newMat.SetTexture("_MainTex", oldMat.mainTexture);
                        }

                        newMat.name = oldMat.name + "_URP";
                        newMaterials[i] = newMat;
                    }
                    else
                    {
                        newMaterials[i] = oldMat;
                    }
                }

                renderer.sharedMaterials = newMaterials;
            }
        }

        public GameObject GetVolumePrefab()
        {
            return volumePrefab;
        }

        public GridSettings GetGridSettings()
        {
            return gridSettings;
        }
    }
}

using UnityEngine;
using System.Collections.Generic;
using HorizonMini.Data;

namespace HorizonMini.Core
{
    /// <summary>
    /// Manages the library of available worlds (both built-in and user-created)
    /// </summary>
    public class WorldLibrary : MonoBehaviour
    {
        [Header("Built-in Worlds")]
        [SerializeField] private List<WorldData> builtInWorlds = new List<WorldData>();

        [Header("Prefabs")]
        [SerializeField] private GameObject volumePrefab;
        [SerializeField] private GridSettings gridSettings;

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

            // Add built-in worlds
            foreach (var worldData in builtInWorlds)
            {
                if (worldData != null)
                {
                    worldData.Initialize();
                    allWorldMetas.Add(worldData.ToMeta());
                }
            }

            // Add user-created worlds
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

            return instance;
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

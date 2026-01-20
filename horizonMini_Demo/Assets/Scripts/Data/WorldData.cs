using UnityEngine;
using System.Collections.Generic;
using System;

namespace HorizonMini.Data
{
    /// <summary>
    /// Complete data for a world including layout and content
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "WorldData", menuName = "HorizonMini/WorldData")]
    public class WorldData : ScriptableObject
    {
        [Header("World Identity")]
        public string worldId;
        public string worldTitle = "New World";
        public string worldAuthor = "Creator";

        [Header("Grid Layout")]
        public Vector3Int gridDimensions = new Vector3Int(2, 1, 2); // Width, Height, Depth in volumes
        public List<VolumeCell> volumes = new List<VolumeCell>();

        [Header("Props & Objects")]
        public List<PropData> props = new List<PropData>();

        [Header("World Settings")]
        public Color skyColor = new Color(0.5f, 0.7f, 1f);
        public float gravity = -9.81f;

        public void Initialize()
        {
            if (string.IsNullOrEmpty(worldId))
            {
                worldId = Guid.NewGuid().ToString();
            }
        }

        public bool HasVolumeAt(Vector3Int gridPos)
        {
            return volumes.Exists(v => v.gridPosition == gridPos);
        }

        public void AddVolume(VolumeCell cell)
        {
            if (!HasVolumeAt(cell.gridPosition))
            {
                volumes.Add(cell);
            }
        }

        public void RemoveVolume(Vector3Int gridPos)
        {
            volumes.RemoveAll(v => v.gridPosition == gridPos);
        }

        public WorldMeta ToMeta()
        {
            return new WorldMeta
            {
                id = worldId,
                title = worldTitle,
                author = worldAuthor,
                worldDataPath = name // ScriptableObject asset name
            };
        }
    }

    /// <summary>
    /// Data for props/objects placed in the world
    /// </summary>
    [Serializable]
    public class PropData
    {
        public string propId;
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale = Vector3.one;

        // SmartTerrain specific data
        public Vector3 smartTerrainControlPoint = Vector3.zero; // Control point local position for SmartTerrain

        public PropData()
        {
            propId = Guid.NewGuid().ToString();
        }
    }
}

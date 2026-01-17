using UnityEngine;
using System.Collections.Generic;
using System.IO;
using HorizonMini.Data;

namespace HorizonMini.Core
{
    /// <summary>
    /// Handles local persistence for worlds and user data
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        private const string SAVE_FILE = "horizonmini_save.json";
        private SaveData currentSave;

        private void Awake()
        {
            LoadSave();
        }

        public void LoadSave()
        {
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE);

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    currentSave = JsonUtility.FromJson<SaveData>(json);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load save: {e.Message}");
                    currentSave = new SaveData();
                }
            }
            else
            {
                currentSave = new SaveData();
            }
        }

        public void SaveToFile()
        {
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE);

            try
            {
                string json = JsonUtility.ToJson(currentSave, true);
                File.WriteAllText(path, json);
                Debug.Log($"Save successful: {path}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save: {e.Message}");
            }
        }

        // Collected worlds
        public void CollectWorld(string worldId)
        {
            if (!currentSave.collectedWorldIds.Contains(worldId))
            {
                currentSave.collectedWorldIds.Add(worldId);
                SaveToFile();
            }
        }

        public void UncollectWorld(string worldId)
        {
            if (currentSave.collectedWorldIds.Remove(worldId))
            {
                SaveToFile();
            }
        }

        public bool IsWorldCollected(string worldId)
        {
            return currentSave.collectedWorldIds.Contains(worldId);
        }

        public List<string> GetCollectedWorldIds()
        {
            return new List<string>(currentSave.collectedWorldIds);
        }

        // Created worlds
        public void SaveCreatedWorld(WorldData worldData)
        {
            // Save as JSON in persistent data
            string worldPath = Path.Combine(Application.persistentDataPath, $"world_{worldData.worldId}.json");

            try
            {
                WorldDataSerializable serializable = new WorldDataSerializable(worldData);
                string json = JsonUtility.ToJson(serializable, true);
                File.WriteAllText(worldPath, json);

                if (!currentSave.createdWorldIds.Contains(worldData.worldId))
                {
                    currentSave.createdWorldIds.Add(worldData.worldId);
                }

                SaveToFile();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save world: {e.Message}");
            }
        }

        public WorldData LoadCreatedWorld(string worldId)
        {
            string worldPath = Path.Combine(Application.persistentDataPath, $"world_{worldId}.json");

            if (File.Exists(worldPath))
            {
                try
                {
                    string json = File.ReadAllText(worldPath);
                    WorldDataSerializable serializable = JsonUtility.FromJson<WorldDataSerializable>(json);
                    return serializable.ToWorldData();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load world {worldId}: {e.Message}");
                }
            }

            return null;
        }

        public List<string> GetCreatedWorldIds()
        {
            return new List<string>(currentSave.createdWorldIds);
        }

        // Likes
        public void ToggleLike(string worldId)
        {
            if (currentSave.likedWorldIds.Contains(worldId))
            {
                currentSave.likedWorldIds.Remove(worldId);
            }
            else
            {
                currentSave.likedWorldIds.Add(worldId);
            }
            SaveToFile();
        }

        public bool IsWorldLiked(string worldId)
        {
            return currentSave.likedWorldIds.Contains(worldId);
        }

        public int GetLikeCount(string worldId)
        {
            // In prototype, just return 1 if liked, 0 if not
            return IsWorldLiked(worldId) ? 1 : 0;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                SaveToFile();
            }
        }

        private void OnApplicationQuit()
        {
            SaveToFile();
        }
    }

    [System.Serializable]
    public class SaveData
    {
        public List<string> collectedWorldIds = new List<string>();
        public List<string> createdWorldIds = new List<string>();
        public List<string> likedWorldIds = new List<string>();
    }

    [System.Serializable]
    public class WorldDataSerializable
    {
        public string worldId;
        public string worldTitle;
        public string worldAuthor;
        public Vector3IntSerializable gridDimensions;
        public List<VolumeCellSerializable> volumes = new List<VolumeCellSerializable>();
        public ColorSerializable skyColor;
        public float gravity;

        public WorldDataSerializable() { }

        public WorldDataSerializable(WorldData data)
        {
            worldId = data.worldId;
            worldTitle = data.worldTitle;
            worldAuthor = data.worldAuthor;
            gridDimensions = new Vector3IntSerializable(data.gridDimensions);

            foreach (var vol in data.volumes)
            {
                volumes.Add(new VolumeCellSerializable(vol));
            }

            skyColor = new ColorSerializable(data.skyColor);
            gravity = data.gravity;
        }

        public WorldData ToWorldData()
        {
            WorldData data = ScriptableObject.CreateInstance<WorldData>();
            data.worldId = worldId;
            data.worldTitle = worldTitle;
            data.worldAuthor = worldAuthor;
            data.gridDimensions = gridDimensions.ToVector3Int();

            data.volumes = new List<VolumeCell>();
            foreach (var vol in volumes)
            {
                data.volumes.Add(vol.ToVolumeCell());
            }

            data.skyColor = skyColor.ToColor();
            data.gravity = gravity;

            return data;
        }
    }

    [System.Serializable]
    public class VolumeCellSerializable
    {
        public Vector3IntSerializable gridPosition;
        public string volumeType;
        public int rotationY;

        public VolumeCellSerializable() { }

        public VolumeCellSerializable(VolumeCell cell)
        {
            gridPosition = new Vector3IntSerializable(cell.gridPosition);
            volumeType = cell.volumeType;
            rotationY = cell.rotationY;
        }

        public VolumeCell ToVolumeCell()
        {
            return new VolumeCell(gridPosition.ToVector3Int(), volumeType, rotationY);
        }
    }

    [System.Serializable]
    public class Vector3IntSerializable
    {
        public int x, y, z;

        public Vector3IntSerializable() { }

        public Vector3IntSerializable(Vector3Int v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3Int ToVector3Int()
        {
            return new Vector3Int(x, y, z);
        }
    }

    [System.Serializable]
    public class ColorSerializable
    {
        public float r, g, b, a;

        public ColorSerializable() { }

        public ColorSerializable(Color c)
        {
            r = c.r;
            g = c.g;
            b = c.b;
            a = c.a;
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }
}

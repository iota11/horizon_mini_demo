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
            // Priority 1: Try StreamingAssets (git-tracked save)
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Worlds/Published", SAVE_FILE);
            if (File.Exists(streamingPath))
            {
                try
                {
                    string json = File.ReadAllText(streamingPath);
                    currentSave = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"Loaded save from StreamingAssets (git-tracked)");
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load save from StreamingAssets: {e.Message}");
                }
            }

            // Priority 2: Fallback to persistentDataPath (local save)
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    currentSave = JsonUtility.FromJson<SaveData>(json);
                    Debug.Log($"Loaded save from persistentDataPath (local)");
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

        public void SaveToFile(bool silent = false)
        {
            string path = Path.Combine(Application.persistentDataPath, SAVE_FILE);

            try
            {
                string json = JsonUtility.ToJson(currentSave, true);
                File.WriteAllText(path, json);
                if (!silent)
                {
                    Debug.Log($"Save successful: {path}");
                }
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
            // Priority 1: Try StreamingAssets/Worlds/Published (git-tracked worlds)
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "Worlds/Published", $"world_{worldId}.json");
            if (File.Exists(streamingPath))
            {
                try
                {
                    string json = File.ReadAllText(streamingPath);
                    WorldDataSerializable serializable = JsonUtility.FromJson<WorldDataSerializable>(json);
                    return serializable.ToWorldData();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to load world from StreamingAssets {worldId}: {e.Message}");
                }
            }

            // Priority 2: Fallback to persistentDataPath (local drafts)
            string worldPath = Path.Combine(Application.persistentDataPath, $"world_{worldId}.json");
            if (File.Exists(worldPath))
            {
                try
                {
                    string json = File.ReadAllText(worldPath);
                    WorldDataSerializable serializable = JsonUtility.FromJson<WorldDataSerializable>(json);
                    Debug.Log($"Loaded world {worldId} from persistentDataPath (local draft)");
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

        public void DeleteCreatedWorld(string worldId)
        {
            // Check if world is permanent (cannot be deleted)
            // This requires WorldLibrary reference, so we check via AppRoot
            if (AppRoot.Instance != null && AppRoot.Instance.WorldLibrary != null)
            {
                if (AppRoot.Instance.WorldLibrary.IsPermanentWorld(worldId))
                {
                    Debug.LogWarning($"Cannot delete permanent world: {worldId}");
                    return;
                }
            }

            // Delete world file
            string worldPath = Path.Combine(Application.persistentDataPath, $"world_{worldId}.json");

            try
            {
                if (File.Exists(worldPath))
                {
                    File.Delete(worldPath);
                    Debug.Log($"Deleted world file: {worldPath}");
                }

                // Remove from created worlds list
                if (currentSave.createdWorldIds.Remove(worldId))
                {
                    Debug.Log($"Removed world ID from created list: {worldId}");
                }

                SaveToFile();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete world {worldId}: {e.Message}");
            }
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
                SaveToFile(silent: true); // Don't log during auto-save
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
        public bool isDraft;
        public Vector3IntSerializable gridDimensions;
        public List<VolumeCellSerializable> volumes = new List<VolumeCellSerializable>();
        public List<PropDataSerializable> props = new List<PropDataSerializable>();
        public List<MiniGameDataSerializable> miniGames = new List<MiniGameDataSerializable>();
        public ColorSerializable skyColor;
        public float gravity;

        public WorldDataSerializable() { }

        public WorldDataSerializable(WorldData data)
        {
            worldId = data.worldId;
            worldTitle = data.worldTitle;
            worldAuthor = data.worldAuthor;
            isDraft = data.isDraft;
            gridDimensions = new Vector3IntSerializable(data.gridDimensions);

            foreach (var vol in data.volumes)
            {
                volumes.Add(new VolumeCellSerializable(vol));
            }

            foreach (var prop in data.props)
            {
                props.Add(new PropDataSerializable(prop));
            }

            if (data.miniGames != null)
            {
                foreach (var miniGame in data.miniGames)
                {
                    miniGames.Add(new MiniGameDataSerializable(miniGame));
                }
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
            data.isDraft = isDraft;
            data.gridDimensions = gridDimensions.ToVector3Int();

            data.volumes = new List<VolumeCell>();
            foreach (var vol in volumes)
            {
                data.volumes.Add(vol.ToVolumeCell());
            }

            data.props = new List<PropData>();
            foreach (var prop in props)
            {
                data.props.Add(prop.ToPropData());
            }

            data.miniGames = new List<MiniGameData>();
            if (miniGames != null)
            {
                foreach (var miniGame in miniGames)
                {
                    data.miniGames.Add(miniGame.ToMiniGameData());
                }
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
    public class PropDataSerializable
    {
        public string propId;
        public string prefabName;
        public Vector3Serializable position;
        public QuaternionSerializable rotation;
        public Vector3Serializable scale;

        // SmartTerrain specific data
        public Vector3Serializable smartTerrainControlPoint;

        // SmartWall specific data
        public List<Vector3Serializable> smartWallControlPoints;
        public float smartWallHeight;

        public PropDataSerializable() { }

        public PropDataSerializable(PropData data)
        {
            propId = data.propId;
            prefabName = data.prefabName;
            position = new Vector3Serializable(data.position);
            rotation = new QuaternionSerializable(data.rotation);
            scale = new Vector3Serializable(data.scale);

            // Save SmartTerrain control point if it exists (not zero)
            if (data.smartTerrainControlPoint != Vector3.zero)
            {
                smartTerrainControlPoint = new Vector3Serializable(data.smartTerrainControlPoint);
            }

            // Save SmartWall control points if they exist
            if (data.smartWallControlPoints != null && data.smartWallControlPoints.Count > 0)
            {
                smartWallControlPoints = new List<Vector3Serializable>();
                foreach (var cp in data.smartWallControlPoints)
                {
                    smartWallControlPoints.Add(new Vector3Serializable(cp));
                }

                // Save unified wall height
                smartWallHeight = data.smartWallHeight;
            }
        }

        public PropData ToPropData()
        {
            PropData propData = new PropData
            {
                propId = propId,
                prefabName = prefabName,
                position = position.ToVector3(),
                rotation = rotation.ToQuaternion(),
                scale = scale.ToVector3()
            };

            // Restore SmartTerrain control point if it was saved
            if (smartTerrainControlPoint != null)
            {
                propData.smartTerrainControlPoint = smartTerrainControlPoint.ToVector3();
            }

            // Restore SmartWall control points if they exist
            if (smartWallControlPoints != null && smartWallControlPoints.Count > 0)
            {
                propData.smartWallControlPoints = new List<Vector3>();
                foreach (var cp in smartWallControlPoints)
                {
                    propData.smartWallControlPoints.Add(cp.ToVector3());
                }

                // Restore unified wall height
                propData.smartWallHeight = smartWallHeight;
            }

            return propData;
        }
    }

    [System.Serializable]
    public class Vector3Serializable
    {
        public float x, y, z;

        public Vector3Serializable() { }

        public Vector3Serializable(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [System.Serializable]
    public class QuaternionSerializable
    {
        public float x, y, z, w;

        public QuaternionSerializable() { }

        public QuaternionSerializable(Quaternion q)
        {
            x = q.x;
            y = q.y;
            z = q.z;
            w = q.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(x, y, z, w);
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

    [System.Serializable]
    public class MiniGameDataSerializable
    {
        public string gameId;
        public string gameType;
        public string gameName;
        public Vector3Serializable position;

        public MiniGameDataSerializable() { }

        public MiniGameDataSerializable(MiniGameData data)
        {
            gameId = data.gameId;
            gameType = data.gameType;
            gameName = data.gameName;
            position = new Vector3Serializable(data.position);
        }

        public MiniGameData ToMiniGameData()
        {
            return new MiniGameData
            {
                gameId = gameId,
                gameType = gameType,
                gameName = gameName,
                position = position.ToVector3()
            };
        }
    }
}

using UnityEngine;
using UnityEditor;
using HorizonMini.Core;
using HorizonMini.Data;
using HorizonMini.Build;
using System.Collections.Generic;
using System.IO;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Load and browse saved worlds
    /// </summary>
    public class LoadSavedWorld : EditorWindow
    {
        [MenuItem("Tools/Load Saved World")]
        public static void ShowWindow()
        {
            GetWindow<LoadSavedWorld>("Load Saved World");
        }

        private List<string> worldIds = new List<string>();
        private Vector2 scrollPos;
        private string selectedWorldId = null;
        private WorldData selectedWorldData = null;

        private void OnEnable()
        {
            RefreshWorldList();
        }

        private void OnGUI()
        {
            GUILayout.Label("Load Saved World", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Browse and load saved worlds from persistent storage.\n" +
                "Saved worlds are stored in: " + Application.persistentDataPath,
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh List", GUILayout.Height(30)))
            {
                RefreshWorldList();
            }

            EditorGUILayout.Space();

            if (worldIds.Count == 0)
            {
                EditorGUILayout.HelpBox("No saved worlds found.", MessageType.Warning);
                return;
            }

            GUILayout.Label($"Found {worldIds.Count} saved worlds:", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            {
                foreach (string worldId in worldIds)
                {
                    EditorGUILayout.BeginVertical("box");
                    {
                        EditorGUILayout.LabelField("World ID:", worldId);

                        // Try to load world data to show details
                        string worldPath = Path.Combine(Application.persistentDataPath, $"world_{worldId}.json");
                        if (File.Exists(worldPath))
                        {
                            try
                            {
                                string json = File.ReadAllText(worldPath);
                                WorldDataSerializable serializable = JsonUtility.FromJson<WorldDataSerializable>(json);

                                EditorGUILayout.LabelField("Title:", serializable.worldTitle);
                                EditorGUILayout.LabelField("Author:", serializable.worldAuthor);
                                EditorGUILayout.LabelField("Grid Size:",
                                    $"{serializable.gridDimensions.x} x {serializable.gridDimensions.y} x {serializable.gridDimensions.z}");
                                EditorGUILayout.LabelField("Props Count:", serializable.props.Count.ToString());
                            }
                            catch (System.Exception e)
                            {
                                EditorGUILayout.LabelField("Error:", e.Message);
                            }
                        }

                        EditorGUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("Load Details", GUILayout.Width(100)))
                            {
                                LoadWorldDetails(worldId);
                            }

                            if (GUILayout.Button("Delete", GUILayout.Width(60)))
                            {
                                if (EditorUtility.DisplayDialog("Confirm Delete",
                                    $"Delete world {worldId}?",
                                    "Yes", "No"))
                                {
                                    DeleteWorld(worldId);
                                }
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.EndScrollView();

            // Show selected world details
            if (selectedWorldData != null)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Selected World Details:", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical("box");
                {
                    EditorGUILayout.LabelField("World ID:", selectedWorldData.worldId);
                    EditorGUILayout.LabelField("Title:", selectedWorldData.worldTitle);
                    EditorGUILayout.LabelField("Author:", selectedWorldData.worldAuthor);
                    EditorGUILayout.LabelField("Grid Dimensions:", selectedWorldData.gridDimensions.ToString());
                    EditorGUILayout.LabelField("Props Count:", selectedWorldData.props.Count.ToString());

                    if (selectedWorldData.props.Count > 0)
                    {
                        EditorGUILayout.Space();
                        GUILayout.Label("Props:", EditorStyles.boldLabel);
                        foreach (var prop in selectedWorldData.props)
                        {
                            EditorGUILayout.LabelField($"  - {prop.prefabName} at {prop.position}");
                        }
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void RefreshWorldList()
        {
            worldIds.Clear();

            // Check if save file exists
            string savePath = Path.Combine(Application.persistentDataPath, "horizonmini_save.json");
            if (!File.Exists(savePath))
            {
                Debug.Log("No save file found.");
                return;
            }

            try
            {
                string json = File.ReadAllText(savePath);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                worldIds.AddRange(saveData.createdWorldIds);
                Debug.Log($"Found {worldIds.Count} saved worlds");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to read save file: {e.Message}");
            }
        }

        private void LoadWorldDetails(string worldId)
        {
            string worldPath = Path.Combine(Application.persistentDataPath, $"world_{worldId}.json");

            if (!File.Exists(worldPath))
            {
                Debug.LogError($"World file not found: {worldPath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(worldPath);
                WorldDataSerializable serializable = JsonUtility.FromJson<WorldDataSerializable>(json);
                selectedWorldData = serializable.ToWorldData();
                selectedWorldId = worldId;

                Debug.Log($"Loaded world details: {selectedWorldData.worldTitle}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load world {worldId}: {e.Message}");
            }
        }

        private void DeleteWorld(string worldId)
        {
            // Delete world file
            string worldPath = Path.Combine(Application.persistentDataPath, $"world_{worldId}.json");
            if (File.Exists(worldPath))
            {
                File.Delete(worldPath);
            }

            // Remove from save file
            string savePath = Path.Combine(Application.persistentDataPath, "horizonmini_save.json");
            if (File.Exists(savePath))
            {
                try
                {
                    string json = File.ReadAllText(savePath);
                    SaveData saveData = JsonUtility.FromJson<SaveData>(json);
                    saveData.createdWorldIds.Remove(worldId);

                    json = JsonUtility.ToJson(saveData, true);
                    File.WriteAllText(savePath, json);

                    Debug.Log($"Deleted world {worldId}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to update save file: {e.Message}");
                }
            }

            RefreshWorldList();
        }
    }
}

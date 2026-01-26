using UnityEditor;
using UnityEngine;
using HorizonMini.Core;
using HorizonMini.Data;
using System.IO;
using System.Collections.Generic;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to clear all saved worlds (except permanent ones)
    /// </summary>
    public class ClearAllWorlds
    {
        [MenuItem("HorizonMini/Clear All Worlds")]
        public static void ClearWorlds()
        {
            // Load permanent world IDs
            HashSet<string> permanentWorldIds = LoadPermanentWorldIds();

            // Confirm with user
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear All Worlds",
                "⚠️ WARNING ⚠️\n\n" +
                "This will permanently delete ALL non-permanent saved worlds!\n\n" +
                $"Permanent worlds ({permanentWorldIds.Count}) will be protected.\n\n" +
                "This action CANNOT be undone.\n\n" +
                "Are you sure you want to continue?",
                "Yes, Delete Non-Permanent Worlds",
                "Cancel"
            );

            if (!confirmed)
            {
                Debug.Log("[ClearAllWorlds] Operation cancelled by user");
                return;
            }

            // Double confirmation
            bool doubleConfirmed = EditorUtility.DisplayDialog(
                "Final Confirmation",
                "This is your last chance!\n\n" +
                "All non-permanent world data will be permanently deleted.\n\n" +
                "Continue?",
                "DELETE ALL",
                "Cancel"
            );

            if (!doubleConfirmed)
            {
                Debug.Log("[ClearAllWorlds] Operation cancelled by user");
                return;
            }

            int deletedCount = 0;
            int skippedCount = 0;
            List<string> skippedWorlds = new List<string>();

            // Get the save path - worlds are saved directly in persistentDataPath
            string savePath = Application.persistentDataPath;

            if (Directory.Exists(savePath))
            {
                try
                {
                    // Get all world files (pattern: world_*.json)
                    string[] worldFiles = Directory.GetFiles(savePath, "world_*.json");

                    if (worldFiles.Length == 0)
                    {
                        EditorUtility.DisplayDialog(
                            "Info",
                            "No world files found.\n\n" +
                            "Either no worlds have been created yet,\n" +
                            "or they have already been deleted.",
                            "OK"
                        );
                        Debug.Log($"[ClearAllWorlds] No world files found at: {savePath}");
                        return;
                    }

                    // Delete each world file (except permanent ones)
                    foreach (string filePath in worldFiles)
                    {
                        // Extract world ID from filename (world_{id}.json)
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        string worldId = fileName.Replace("world_", "");

                        // Check if world is permanent
                        if (permanentWorldIds.Contains(worldId))
                        {
                            skippedCount++;
                            skippedWorlds.Add(fileName);
                            Debug.Log($"[ClearAllWorlds] Skipped permanent world: {fileName}");
                            continue;
                        }

                        // Delete non-permanent world
                        File.Delete(filePath);
                        deletedCount++;
                        Debug.Log($"[ClearAllWorlds] Deleted: {Path.GetFileName(filePath)}");
                    }

                    string message = $"✅ Successfully deleted {deletedCount} world(s)\n\n";
                    if (skippedCount > 0)
                    {
                        message += $"🔒 Protected {skippedCount} permanent world(s):\n";
                        foreach (string worldName in skippedWorlds)
                        {
                            message += $"  • {worldName}\n";
                        }
                    }

                    EditorUtility.DisplayDialog(
                        "Success",
                        message,
                        "OK"
                    );

                    Debug.Log($"[ClearAllWorlds] Successfully deleted {deletedCount} world(s), protected {skippedCount} permanent world(s)");
                }
                catch (System.Exception e)
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        $"Failed to delete worlds:\n\n{e.Message}",
                        "OK"
                    );
                    Debug.LogError($"[ClearAllWorlds] Error: {e.Message}\n{e.StackTrace}");
                }
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Save directory not found.",
                    "OK"
                );
                Debug.LogError($"[ClearAllWorlds] Save directory not found: {savePath}");
            }
        }

        private static HashSet<string> LoadPermanentWorldIds()
        {
            HashSet<string> permanentIds = new HashSet<string>();

            string registryPath = "Assets/Data/PermanentWorldsRegistry.asset";
            PermanentWorldsRegistry registry = AssetDatabase.LoadAssetAtPath<PermanentWorldsRegistry>(registryPath);

            if (registry != null && registry.permanentWorldIds != null)
            {
                foreach (string id in registry.permanentWorldIds)
                {
                    permanentIds.Add(id);
                }
                Debug.Log($"[ClearAllWorlds] Loaded {permanentIds.Count} permanent world IDs from registry");
            }
            else
            {
                Debug.Log("[ClearAllWorlds] No permanent worlds registry found");
            }

            return permanentIds;
        }

        [MenuItem("HorizonMini/Show Worlds Save Location")]
        public static void ShowSaveLocation()
        {
            string savePath = Application.persistentDataPath;

            // Open in file explorer
            EditorUtility.RevealInFinder(savePath);

            // Count world files
            string[] worldFiles = Directory.GetFiles(savePath, "world_*.json");

            // Also show in dialog
            EditorUtility.DisplayDialog(
                "Worlds Save Location",
                $"Worlds are saved to:\n\n{savePath}\n\n" +
                $"Found {worldFiles.Length} world file(s)\n\n" +
                "The folder has been opened in your file explorer.",
                "OK"
            );

            Debug.Log($"[ClearAllWorlds] Worlds save location: {savePath}");
        }

        [MenuItem("HorizonMini/List All Worlds")]
        public static void ListAllWorlds()
        {
            string savePath = Application.persistentDataPath;

            // Get all world files (pattern: world_*.json)
            string[] worldFiles = Directory.GetFiles(savePath, "world_*.json");

            if (worldFiles.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Worlds",
                    "No world files found.\n\n" +
                    "No worlds have been created yet.",
                    "OK"
                );
                return;
            }

            string worldList = $"Found {worldFiles.Length} world(s):\n\n";

            foreach (string filePath in worldFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                FileInfo fileInfo = new FileInfo(filePath);
                worldList += $"• {fileName}\n";
                worldList += $"  Size: {fileInfo.Length / 1024f:F1} KB\n";
                worldList += $"  Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n\n";
            }

            worldList += $"\nSave location:\n{savePath}";

            EditorUtility.DisplayDialog(
                "All Worlds",
                worldList,
                "OK"
            );

            Debug.Log($"[ClearAllWorlds] {worldList}");
        }
    }
}

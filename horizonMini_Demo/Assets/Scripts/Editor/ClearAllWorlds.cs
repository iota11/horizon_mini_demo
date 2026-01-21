using UnityEditor;
using UnityEngine;
using HorizonMini.Core;
using System.IO;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to clear all saved worlds
    /// </summary>
    public class ClearAllWorlds
    {
        [MenuItem("HorizonMini/Clear All Worlds")]
        public static void ClearWorlds()
        {
            // Confirm with user
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear All Worlds",
                "⚠️ WARNING ⚠️\n\n" +
                "This will permanently delete ALL saved worlds!\n\n" +
                "This action CANNOT be undone.\n\n" +
                "Are you sure you want to continue?",
                "Yes, Delete All Worlds",
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
                "All world data will be permanently deleted.\n\n" +
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

            // Get the save path - worlds are saved directly in persistentDataPath
            string savePath = Application.persistentDataPath;

            if (Directory.Exists(savePath))
            {
                try
                {
                    // Get all world files (pattern: world_*.json)
                    string[] worldFiles = Directory.GetFiles(savePath, "world_*.json");
                    deletedCount = worldFiles.Length;

                    if (deletedCount == 0)
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

                    // Delete each world file
                    foreach (string filePath in worldFiles)
                    {
                        File.Delete(filePath);
                        Debug.Log($"[ClearAllWorlds] Deleted: {Path.GetFileName(filePath)}");
                    }

                    EditorUtility.DisplayDialog(
                        "Success",
                        $"✅ Successfully deleted {deletedCount} world(s)\n\n" +
                        "All saved worlds have been cleared.",
                        "OK"
                    );

                    Debug.Log($"[ClearAllWorlds] Successfully deleted {deletedCount} world file(s)");
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

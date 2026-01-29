using UnityEditor;
using UnityEngine;
using HorizonMini.Core;
using HorizonMini.Data;
using System.IO;
using System.Collections.Generic;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to clear all saved worlds
    /// </summary>
    public class ClearAllWorlds
    {
        [MenuItem("HorizonMini/Clear All Worlds (Non-Permanent Only)")]
        public static void ClearNonPermanentWorlds()
        {
            ClearWorlds(includePermanent: false);
        }

        [MenuItem("HorizonMini/Clear All Worlds (Including Permanent)")]
        public static void ClearAllWorldsIncludingPermanent()
        {
            ClearWorlds(includePermanent: true);
        }

        private static void ClearWorlds(bool includePermanent)
        {
            // Load permanent world IDs
            HashSet<string> permanentWorldIds = LoadPermanentWorldIds();

            string warningMessage = includePermanent
                ? "⚠️ DANGER ⚠️\n\n" +
                  "This will permanently delete ALL saved worlds including PERMANENT ones!\n\n" +
                  $"Total permanent worlds: {permanentWorldIds.Count}\n\n" +
                  "This action CANNOT be undone.\n\n" +
                  "Are you ABSOLUTELY SURE?"
                : "⚠️ WARNING ⚠️\n\n" +
                  "This will permanently delete ALL non-permanent saved worlds!\n\n" +
                  $"Permanent worlds ({permanentWorldIds.Count}) will be protected.\n\n" +
                  "This action CANNOT be undone.\n\n" +
                  "Are you sure you want to continue?";

            // Confirm with user
            bool confirmed = EditorUtility.DisplayDialog(
                "Clear All Worlds",
                warningMessage,
                includePermanent ? "DELETE EVERYTHING" : "Yes, Delete Non-Permanent Worlds",
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
                (includePermanent
                    ? "ALL worlds (including permanent) will be permanently deleted.\n\n"
                    : "All non-permanent world data will be permanently deleted.\n\n") +
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

            try
            {
                // 1. Clear from persistentDataPath (drafts)
                string draftPath = Application.persistentDataPath;
                if (Directory.Exists(draftPath))
                {
                    string[] draftFiles = Directory.GetFiles(draftPath, "world_*.json");
                    foreach (string filePath in draftFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        string worldId = fileName.Replace("world_", "");

                        // Check if world is permanent and should be protected
                        if (!includePermanent && permanentWorldIds.Contains(worldId))
                        {
                            skippedCount++;
                            skippedWorlds.Add(fileName);
                            Debug.Log($"[ClearAllWorlds] Skipped permanent world (draft): {fileName}");
                            continue;
                        }

                        File.Delete(filePath);
                        deletedCount++;
                        Debug.Log($"[ClearAllWorlds] Deleted draft: {Path.GetFileName(filePath)}");
                    }
                }

                // 2. Clear from StreamingAssets/Worlds/Published (published worlds)
                string publishedPath = Path.Combine(Application.streamingAssetsPath, "Worlds/Published");
                if (Directory.Exists(publishedPath))
                {
                    string[] publishedFiles = Directory.GetFiles(publishedPath, "world_*.json");
                    foreach (string filePath in publishedFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        string worldId = fileName.Replace("world_", "");

                        // Check if world is permanent and should be protected
                        if (!includePermanent && permanentWorldIds.Contains(worldId))
                        {
                            skippedCount++;
                            skippedWorlds.Add(fileName);
                            Debug.Log($"[ClearAllWorlds] Skipped permanent world (published): {fileName}");
                            continue;
                        }

                        File.Delete(filePath);
                        deletedCount++;
                        Debug.Log($"[ClearAllWorlds] Deleted published: {Path.GetFileName(filePath)}");
                    }
                }

                if (deletedCount == 0 && skippedCount == 0)
                {
                    EditorUtility.DisplayDialog(
                        "Info",
                        "No world files found.\n\n" +
                        "Either no worlds have been created yet,\n" +
                        "or they have already been deleted.",
                        "OK"
                    );
                    Debug.Log("[ClearAllWorlds] No world files found");
                    return;
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
            string draftPath = Application.persistentDataPath;
            string publishedPath = Path.Combine(Application.streamingAssetsPath, "Worlds/Published");

            // Count world files
            string[] draftFiles = Directory.Exists(draftPath) ? Directory.GetFiles(draftPath, "world_*.json") : new string[0];
            string[] publishedFiles = Directory.Exists(publishedPath) ? Directory.GetFiles(publishedPath, "world_*.json") : new string[0];

            // Show dialog
            EditorUtility.DisplayDialog(
                "Worlds Save Locations",
                $"📁 Drafts (persistentDataPath):\n{draftPath}\nFound {draftFiles.Length} draft(s)\n\n" +
                $"📁 Published (StreamingAssets/git):\n{publishedPath}\nFound {publishedFiles.Length} published world(s)\n\n" +
                "Click OK to open draft folder in file explorer.",
                "OK"
            );

            // Open draft folder in file explorer
            EditorUtility.RevealInFinder(draftPath);

            Debug.Log($"[ClearAllWorlds] Draft path: {draftPath}");
            Debug.Log($"[ClearAllWorlds] Published path: {publishedPath}");
        }

        [MenuItem("HorizonMini/List All Worlds")]
        public static void ListAllWorlds()
        {
            string draftPath = Application.persistentDataPath;
            string publishedPath = Path.Combine(Application.streamingAssetsPath, "Worlds/Published");

            // Get all world files
            string[] draftFiles = Directory.Exists(draftPath) ? Directory.GetFiles(draftPath, "world_*.json") : new string[0];
            string[] publishedFiles = Directory.Exists(publishedPath) ? Directory.GetFiles(publishedPath, "world_*.json") : new string[0];

            if (draftFiles.Length == 0 && publishedFiles.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Worlds",
                    "No world files found.\n\n" +
                    "No worlds have been created yet.",
                    "OK"
                );
                return;
            }

            string worldList = "";

            // List drafts
            if (draftFiles.Length > 0)
            {
                worldList += $"📝 DRAFTS ({draftFiles.Length}):\n\n";
                foreach (string filePath in draftFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    FileInfo fileInfo = new FileInfo(filePath);
                    worldList += $"• {fileName}\n";
                    worldList += $"  Size: {fileInfo.Length / 1024f:F1} KB\n";
                    worldList += $"  Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n\n";
                }
            }

            // List published
            if (publishedFiles.Length > 0)
            {
                worldList += $"\n📦 PUBLISHED ({publishedFiles.Length}):\n\n";
                foreach (string filePath in publishedFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    FileInfo fileInfo = new FileInfo(filePath);
                    worldList += $"• {fileName}\n";
                    worldList += $"  Size: {fileInfo.Length / 1024f:F1} KB\n";
                    worldList += $"  Modified: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}\n\n";
                }
            }

            worldList += $"\nTotal: {draftFiles.Length + publishedFiles.Length} world(s)";

            EditorUtility.DisplayDialog(
                "All Worlds",
                worldList,
                "OK"
            );

            Debug.Log($"[ClearAllWorlds] {worldList}");
        }
    }
}

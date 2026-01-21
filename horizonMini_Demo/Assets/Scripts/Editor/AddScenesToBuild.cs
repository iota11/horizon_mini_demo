using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to add Main, Build, Play scenes to Build Settings
    /// </summary>
    public class AddScenesToBuild
    {
        [MenuItem("HorizonMini/Add Scenes to Build Settings")]
        public static void AddScenes()
        {
            // Get current scenes in build settings
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

            // Scenes to add
            string[] requiredScenes = new string[]
            {
                "Assets/Scenes/Main.unity",
                "Assets/Scenes/Build.unity",
                "Assets/Scenes/Play.unity"
            };

            int addedCount = 0;

            foreach (string scenePath in requiredScenes)
            {
                // Check if scene already exists in build settings
                bool exists = scenes.Any(s => s.path == scenePath);

                if (!exists)
                {
                    // Check if scene file exists
                    if (System.IO.File.Exists(scenePath))
                    {
                        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                        addedCount++;
                        Debug.Log($"[AddScenesToBuild] Added {scenePath} to Build Settings");
                    }
                    else
                    {
                        Debug.LogWarning($"[AddScenesToBuild] Scene file not found: {scenePath}");
                    }
                }
                else
                {
                    Debug.Log($"[AddScenesToBuild] Scene already in Build Settings: {scenePath}");
                }
            }

            // Update build settings
            EditorBuildSettings.scenes = scenes.ToArray();

            if (addedCount > 0)
            {
                EditorUtility.DisplayDialog("Success",
                    $"Added {addedCount} scene(s) to Build Settings!\n\n" +
                    "Scenes in Build Settings:\n" +
                    "• Main (index 0)\n" +
                    "• Build (index 1)\n" +
                    "• Play (index 2)\n\n" +
                    "You can now use SceneManager.LoadScene() to switch between scenes.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Info",
                    "All required scenes are already in Build Settings.",
                    "OK");
            }
        }
    }
}

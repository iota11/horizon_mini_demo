using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to enable Read/Write on all meshes in the project
    /// </summary>
    public class FixMeshReadWrite : EditorWindow
    {
        [MenuItem("HorizonMini/Fix Mesh Read/Write")]
        public static void ShowWindow()
        {
            GetWindow<FixMeshReadWrite>("Fix Mesh Read/Write");
        }

        private Vector2 scrollPosition;
        private List<string> foundMeshes = new List<string>();

        private void OnGUI()
        {
            GUILayout.Label("Fix Mesh Read/Write Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool will enable Read/Write on all mesh assets in your project.\n" +
                "This is needed for NavMesh baking but will increase memory usage.",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Find All Meshes Without Read/Write", GUILayout.Height(30)))
            {
                FindMeshesWithoutReadWrite();
            }

            EditorGUILayout.Space();

            if (foundMeshes.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {foundMeshes.Count} meshes without Read/Write:");

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                foreach (string meshPath in foundMeshes)
                {
                    EditorGUILayout.LabelField(meshPath);
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                if (GUILayout.Button("Enable Read/Write on All Found Meshes", GUILayout.Height(30)))
                {
                    EnableReadWriteOnFoundMeshes();
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Enable Read/Write on ALL Project Meshes", GUILayout.Height(30)))
            {
                EnableReadWriteOnAllMeshes();
            }
        }

        private void FindMeshesWithoutReadWrite()
        {
            foundMeshes.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Model t:Mesh");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

                if (importer != null && !importer.isReadable)
                {
                    foundMeshes.Add(path);
                }
            }

            UnityEngine.Debug.Log($"[FixMeshReadWrite] Found {foundMeshes.Count} meshes without Read/Write enabled");
        }

        private void EnableReadWriteOnFoundMeshes()
        {
            int count = 0;

            foreach (string path in foundMeshes)
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    count++;
                }
            }

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log($"[FixMeshReadWrite] ✓ Enabled Read/Write on {count} meshes");
            foundMeshes.Clear();
        }

        private void EnableReadWriteOnAllMeshes()
        {
            if (!EditorUtility.DisplayDialog(
                "Enable Read/Write on All Meshes",
                "This will enable Read/Write on ALL mesh assets in your project. This may take a while and increase memory usage. Continue?",
                "Yes", "Cancel"))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Model t:Mesh");
            int count = 0;

            EditorUtility.DisplayProgressBar("Fixing Meshes", "Enabling Read/Write...", 0);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

                if (importer != null && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    count++;
                }

                EditorUtility.DisplayProgressBar("Fixing Meshes", $"Processing {i + 1}/{guids.Length}", (float)i / guids.Length);
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Complete",
                $"Enabled Read/Write on {count} meshes",
                "OK");

            UnityEngine.Debug.Log($"[FixMeshReadWrite] ✓ Enabled Read/Write on {count} meshes");
        }
    }
}

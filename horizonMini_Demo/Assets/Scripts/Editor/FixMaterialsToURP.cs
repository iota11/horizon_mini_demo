using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Utility to convert all materials to URP Lit shader
    /// </summary>
    public class FixMaterialsToURP : EditorWindow
    {
        [MenuItem("Tools/Fix Materials to URP")]
        public static void ShowWindow()
        {
            GetWindow<FixMaterialsToURP>("Fix Materials to URP");
        }

        private void OnGUI()
        {
            GUILayout.Label("Convert Materials to URP", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This will convert all materials in the project to use Universal Render Pipeline/Lit shader.\n\n" +
                "This fixes pink/purple materials caused by Built-in RP shaders.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Fix All Materials", GUILayout.Height(40)))
            {
                FixAllMaterials();
            }
        }

        private static void FixAllMaterials()
        {
            // Find all materials in the project
            string[] guids = AssetDatabase.FindAssets("t:Material");

            List<Material> fixedMaterials = new List<Material>();
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");

            if (urpLitShader == null)
            {
                Debug.LogError("URP Lit shader not found! Make sure URP is installed.");
                return;
            }

            int progressTotal = guids.Length;
            int progressCurrent = 0;

            foreach (string guid in guids)
            {
                progressCurrent++;
                string path = AssetDatabase.GUIDToAssetPath(guid);

                EditorUtility.DisplayProgressBar(
                    "Fixing Materials",
                    $"Processing {progressCurrent}/{progressTotal}: {path}",
                    (float)progressCurrent / progressTotal
                );

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (mat != null && mat.shader != null)
                {
                    string shaderName = mat.shader.name;

                    // Check if shader is missing (pink material) or using Built-in RP
                    if (shaderName.Contains("Standard") ||
                        shaderName.Contains("Legacy") ||
                        mat.shader.name == "Hidden/InternalErrorShader")
                    {
                        // Store old color
                        Color oldColor = Color.white;
                        if (mat.HasProperty("_Color"))
                        {
                            oldColor = mat.color;
                        }
                        else if (mat.HasProperty("_BaseColor"))
                        {
                            oldColor = mat.GetColor("_BaseColor");
                        }

                        // Convert to URP Lit
                        mat.shader = urpLitShader;
                        mat.SetColor("_BaseColor", oldColor);

                        EditorUtility.SetDirty(mat);
                        fixedMaterials.Add(mat);

                        Debug.Log($"Fixed material: {path} (was using {shaderName})");
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>Fixed {fixedMaterials.Count} materials to use URP Lit shader!</color>");
            EditorUtility.DisplayDialog(
                "Materials Fixed",
                $"Successfully converted {fixedMaterials.Count} materials to URP Lit shader.",
                "OK"
            );
        }
    }
}

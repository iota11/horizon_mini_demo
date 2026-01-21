using UnityEngine;
using UnityEditor;
using HorizonMini.Build;

namespace HorizonMini.Editor
{
    /// <summary>
    /// Editor utility to create SpawnPoint prefab
    /// </summary>
    public class CreateSpawnPointPrefab
    {
        [MenuItem("HorizonMini/Create SpawnPoint Prefab")]
        public static void CreatePrefab()
        {
            // Create root GameObject
            GameObject spawnPointObj = new GameObject("SpawnPoint_Player");

            // Add SpawnPoint component
            SpawnPoint spawnPoint = spawnPointObj.AddComponent<SpawnPoint>();

            // Create visual model - simple arrow made of primitives
            CreateArrowVisual(spawnPointObj);

            // Create cursor as child
            GameObject cursorObj = new GameObject("SpawnPointCursor");
            cursorObj.transform.SetParent(spawnPointObj.transform);
            cursorObj.transform.localPosition = Vector3.zero;

            SpawnPointCursor cursor = cursorObj.AddComponent<SpawnPointCursor>();

            // Setup references using SerializedObject
            SerializedObject spawnPointSO = new SerializedObject(spawnPoint);
            spawnPointSO.FindProperty("spawnType").enumValueIndex = (int)SpawnType.Player;
            spawnPointSO.ApplyModifiedProperties();

            SerializedObject cursorSO = new SerializedObject(cursor);
            cursorSO.FindProperty("spawnPoint").objectReferenceValue = spawnPoint;
            cursorSO.FindProperty("uiLayer").intValue = LayerMask.GetMask("UI");
            cursorSO.ApplyModifiedProperties();

            // Create prefab directory if needed
            string prefabPath = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Save as prefab
            string fullPath = $"{prefabPath}/SpawnPoint_Player.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(spawnPointObj, fullPath);

            // Clean up scene instance
            Object.DestroyImmediate(spawnPointObj);

            Debug.Log($"<color=green>Created SpawnPoint prefab at: {fullPath}</color>");

            // Select the prefab in Project window
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            EditorUtility.DisplayDialog("Success",
                "SpawnPoint prefab created successfully!\n\n" +
                "Path: " + fullPath + "\n\n" +
                "Next steps:\n" +
                "1. Create a PlaceableAsset (Right-click → Create → HorizonMini → PlaceableAsset)\n" +
                "2. Set Category to 'SpawnPoint'\n" +
                "3. Assign this prefab to the PlaceableAsset\n" +
                "4. Add the PlaceableAsset to your AssetCatalog",
                "OK");
        }

        /// <summary>
        /// Create a simple arrow visual made of cubes
        /// </summary>
        private static void CreateArrowVisual(GameObject parent)
        {
            GameObject visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(parent.transform);
            visualRoot.transform.localPosition = Vector3.zero;

            // Arrow shaft (vertical stick)
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "Shaft";
            shaft.transform.SetParent(visualRoot.transform);
            shaft.transform.localPosition = new Vector3(0, 0.5f, 0);
            shaft.transform.localScale = new Vector3(0.1f, 1f, 0.1f);
            SetColor(shaft, new Color(0.2f, 0.8f, 1f)); // Cyan

            // Arrow head (cone-like shape made of cube)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(visualRoot.transform);
            head.transform.localPosition = new Vector3(0, 1.2f, 0);
            head.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            head.transform.localRotation = Quaternion.Euler(45, 45, 0);
            SetColor(head, new Color(1f, 0.5f, 0f)); // Orange

            // Base circle (to show ground position)
            GameObject baseCircle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseCircle.name = "Base";
            baseCircle.transform.SetParent(visualRoot.transform);
            baseCircle.transform.localPosition = new Vector3(0, 0.02f, 0);
            baseCircle.transform.localScale = new Vector3(0.6f, 0.02f, 0.6f);
            SetColor(baseCircle, new Color(0.2f, 0.8f, 1f, 0.5f)); // Semi-transparent cyan

            // Remove colliders from visual elements (SpawnPoint has its own collider)
            Object.DestroyImmediate(shaft.GetComponent<Collider>());
            Object.DestroyImmediate(head.GetComponent<Collider>());
            Object.DestroyImmediate(baseCircle.GetComponent<Collider>());
        }

        /// <summary>
        /// Set color of GameObject's renderer
        /// </summary>
        private static void SetColor(GameObject obj, Color color)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                // Create URP material
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader != null)
                {
                    Material mat = new Material(urpShader);
                    mat.color = color;

                    // Set transparency if alpha < 1
                    if (color.a < 1f)
                    {
                        mat.SetFloat("_Surface", 1); // Transparent
                        mat.SetFloat("_Blend", 0); // Alpha
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.renderQueue = 3000;
                    }

                    renderer.material = mat;
                }
            }
        }
    }
}

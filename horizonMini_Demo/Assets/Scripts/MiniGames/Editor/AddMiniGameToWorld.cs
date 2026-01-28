using UnityEngine;
using UnityEditor;
using HorizonMini.Data;

namespace HorizonMini.MiniGames.Editor
{
    /// <summary>
    /// Editor tool to add MiniGame to WorldData asset
    /// </summary>
    public class AddMiniGameToWorld : EditorWindow
    {
        private WorldData targetWorld;
        private string gameName = "Stack Game";
        private string gameType = "CubeStack";

        [MenuItem("Tools/MiniGames/Add to World Asset")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddMiniGameToWorld>("Add MiniGame to World");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            GUILayout.Label("Add MiniGame to World", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This will add a MiniGame entry to a WorldData asset.\n\n" +
                "When users browse to this World in Browse mode, they will see a button to play the game.",
                MessageType.Info
            );

            EditorGUILayout.Space();

            // World selection
            targetWorld = (WorldData)EditorGUILayout.ObjectField(
                "Target World",
                targetWorld,
                typeof(WorldData),
                false
            );

            EditorGUILayout.Space();
            GUILayout.Label("Game Settings:", EditorStyles.boldLabel);

            gameType = EditorGUILayout.TextField("Game Type", gameType);
            gameName = EditorGUILayout.TextField("Game Name", gameName);

            EditorGUILayout.Space();

            GUI.enabled = targetWorld != null;
            if (GUILayout.Button("Add MiniGame to World", GUILayout.Height(40)))
            {
                AddGameToWorld();
            }
            GUI.enabled = true;

            if (targetWorld == null)
            {
                EditorGUILayout.HelpBox("Please select a WorldData asset!", MessageType.Warning);
            }

            EditorGUILayout.Space();

            GUILayout.Label("How to Use:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Select a WorldData asset from your project\n" +
                "2. Click 'Add MiniGame to World' above\n" +
                "3. The World will now have a mini game button in Browse mode",
                MessageType.Info
            );
        }

        private void AddGameToWorld()
        {
            Debug.Log("<color=cyan>[AddMiniGame] Adding mini game to World...</color>");

            // Create MiniGameData
            MiniGameData gameData = new MiniGameData
            {
                gameType = gameType,
                gameName = gameName,
                position = Vector3.zero
            };

            // Add to WorldData
            if (targetWorld.miniGames == null)
            {
                targetWorld.miniGames = new System.Collections.Generic.List<MiniGameData>();
            }

            targetWorld.miniGames.Add(gameData);

            // Mark asset as dirty to save changes
            EditorUtility.SetDirty(targetWorld);
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[AddMiniGame] ✓ MiniGame added!</color>");

            EditorUtility.DisplayDialog(
                "Success!",
                $"MiniGame '{gameName}' has been added to {targetWorld.worldTitle}!\n\n" +
                "The game button will appear when browsing to this World in Browse mode.",
                "OK"
            );
        }
    }
}

using UnityEngine;
using UnityEditor;
using HorizonMini.Data;
using HorizonMini.Controllers;
using HorizonMini.Build;

namespace HorizonMini.MiniGames.Editor
{
    /// <summary>
    /// Editor tool to add MiniGame to current Build mode World
    /// </summary>
    public class AddMiniGameToBuildWorld : EditorWindow
    {
        private string gameName = "Stack Game";
        private string gameType = "CubeStack";

        [MenuItem("Tools/MiniGames/Add to Current Build World")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddMiniGameToBuildWorld>("Add MiniGame");
            window.minSize = new Vector2(400, 250);
        }

        private void OnGUI()
        {
            GUILayout.Label("Add MiniGame to Build World", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This will add a MiniGame to the current World in Build mode.\n\n" +
                "When users browse to this World, they will see a button to play the game.",
                MessageType.Info
            );

            EditorGUILayout.Space();
            GUILayout.Label("Game Settings:", EditorStyles.boldLabel);

            gameType = EditorGUILayout.TextField("Game Type", gameType);
            gameName = EditorGUILayout.TextField("Game Name", gameName);

            EditorGUILayout.Space();

            if (GUILayout.Button("Add MiniGame to Current World", GUILayout.Height(40)))
            {
                AddGameToWorld();
            }

            EditorGUILayout.Space();

            GUILayout.Label("How to Use:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Make sure you're in Build mode with a World loaded\n" +
                "2. Click 'Add MiniGame to Current World' above\n" +
                "3. Save your World (BuildController → Save)\n" +
                "4. Browse to this World to see the game button",
                MessageType.Info
            );
        }

        private void AddGameToWorld()
        {
            Debug.Log("<color=cyan>[AddMiniGame] Adding mini game marker to scene...</color>");

            // Find BuildController
            BuildController buildController = Object.FindObjectOfType<BuildController>();
            if (buildController == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "BuildController not found! Make sure you're in Build mode.",
                    "OK"
                );
                return;
            }

            // Find VolumeGrid in scene
            VolumeGrid volumeGrid = Object.FindObjectOfType<VolumeGrid>();
            if (volumeGrid == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "No VolumeGrid found! Please create a World first.",
                    "OK"
                );
                return;
            }

            // Create a marker object that will be saved with the world
            GameObject marker = new GameObject("MiniGameMarker");
            marker.transform.SetParent(volumeGrid.transform);
            marker.transform.localPosition = Vector3.zero;

            // Add a component to store the mini game data
            MiniGameMarker markerComponent = marker.AddComponent<MiniGameMarker>();
            markerComponent.gameType = gameType;
            markerComponent.gameName = gameName;

            UnityEditor.Selection.activeGameObject = marker;

            Debug.Log("<color=green>[AddMiniGame] ✓ MiniGame marker created!</color>");

            EditorUtility.DisplayDialog(
                "Success!",
                $"MiniGame marker '{gameName}' has been added to the scene!\n\n" +
                "IMPORTANT: You MUST click SAVE in BuildController to save this to the World.\n\n" +
                "The marker object has been selected in the Hierarchy.",
                "OK"
            );
        }
    }
}

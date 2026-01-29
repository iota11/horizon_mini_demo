using UnityEngine;
using UnityEditor;
using HorizonMini.Data;
using HorizonMini.Controllers;
using HorizonMini.Build;

namespace HorizonMini.MiniGames.Editor
{
    /// <summary>
    /// Editor tool to add KotobaMatch game to current Build mode World
    /// </summary>
    public class AddKotobaMatchToBuildWorld : EditorWindow
    {
        private string gameName = "Word Match";

        [MenuItem("Tools/MiniGames/Add KotobaMatch to Build World")]
        public static void ShowWindow()
        {
            var window = GetWindow<AddKotobaMatchToBuildWorld>("Add KotobaMatch");
            window.minSize = new Vector2(400, 280);
        }

        private void OnGUI()
        {
            GUILayout.Label("Add KotobaMatch to Build World", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This will add a KotobaMatch (Japanese-English word matching game) to the current World in Build mode.\n\n" +
                "When users browse to this World, they will see a button to play the game.",
                MessageType.Info
            );

            EditorGUILayout.Space();
            GUILayout.Label("Game Settings:", EditorStyles.boldLabel);

            gameName = EditorGUILayout.TextField("Game Name", gameName);

            EditorGUILayout.Space();

            // Preview info
            EditorGUILayout.HelpBox(
                "Game Type: KotobaMatch\n" +
                "Description: Match Japanese words with their English translations\n" +
                "Duration: 60 seconds\n" +
                "Pairs per round: 6",
                MessageType.None
            );

            EditorGUILayout.Space();

            if (GUILayout.Button("Add KotobaMatch to Current World", GUILayout.Height(40)))
            {
                AddGameToWorld();
            }

            EditorGUILayout.Space();

            GUILayout.Label("How to Use:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "1. Make sure you're in Build mode with a World loaded\n" +
                "2. Click 'Add KotobaMatch to Current World' above\n" +
                "3. Save your World (click 'Save' in Build mode)\n" +
                "4. Browse to this World to see the game button",
                MessageType.Info
            );
        }

        private void AddGameToWorld()
        {
            Debug.Log("<color=cyan>[AddKotobaMatch] Adding KotobaMatch marker to scene...</color>");

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
            GameObject marker = new GameObject("MiniGameMarker_KotobaMatch");
            marker.transform.SetParent(volumeGrid.transform);
            marker.transform.localPosition = Vector3.zero;

            // Add a component to store the mini game data
            MiniGameMarker markerComponent = marker.AddComponent<MiniGameMarker>();
            markerComponent.gameType = "KotobaMatch";
            markerComponent.gameName = gameName;

            UnityEditor.Selection.activeGameObject = marker;

            Debug.Log("<color=green>[AddKotobaMatch] ✓ KotobaMatch marker created!</color>");
            Debug.Log($"<color=green>  Game Name: {gameName}</color>");
            Debug.Log($"<color=green>  Game Type: KotobaMatch</color>");

            EditorUtility.DisplayDialog(
                "Success!",
                $"KotobaMatch game '{gameName}' has been added to the scene!\n\n" +
                "IMPORTANT: You MUST click SAVE in Build mode to save this to the World.\n\n" +
                "The marker object has been selected in the Hierarchy.",
                "OK"
            );
        }
    }
}

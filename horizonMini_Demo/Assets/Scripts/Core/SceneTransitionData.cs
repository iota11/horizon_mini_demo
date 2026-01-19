using UnityEngine;

namespace HorizonMini.Core
{
    /// <summary>
    /// Static class to pass data between scenes
    /// </summary>
    public static class SceneTransitionData
    {
        public static string WorldIdToEdit { get; set; }

        public static void SetWorldToEdit(string worldId)
        {
            WorldIdToEdit = worldId;
            Debug.Log($"SceneTransitionData: Set world to edit: {worldId}");
        }

        public static string GetAndClearWorldToEdit()
        {
            string worldId = WorldIdToEdit;
            WorldIdToEdit = null;
            Debug.Log($"SceneTransitionData: Retrieved world to edit: {worldId}");
            return worldId;
        }

        public static bool HasWorldToEdit()
        {
            return !string.IsNullOrEmpty(WorldIdToEdit);
        }
    }
}

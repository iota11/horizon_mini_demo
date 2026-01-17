using UnityEngine;
using HorizonMini.Data;

namespace HorizonMini.Core
{
    /// <summary>
    /// Wrapper component for a loaded world instance with lifecycle management
    /// </summary>
    public class WorldInstance : MonoBehaviour
    {
        private WorldData worldData;
        private GridSettings gridSettings;
        private bool isActive = false;

        public WorldData WorldData => worldData;
        public string WorldId => worldData?.worldId;
        public bool IsActive => isActive;

        public void Initialize(WorldData data, GridSettings settings)
        {
            worldData = data;
            gridSettings = settings;
        }

        public void SetActive(bool active)
        {
            isActive = active;
            gameObject.SetActive(active);
        }

        public void SetActivationLevel(ActivationLevel level)
        {
            switch (level)
            {
                case ActivationLevel.FullyActive:
                    isActive = true;
                    gameObject.SetActive(true);
                    Time.timeScale = 1f;
                    break;

                case ActivationLevel.Preloaded:
                    // Active but may run at reduced frequency
                    isActive = true;
                    gameObject.SetActive(true);
                    break;

                case ActivationLevel.Inactive:
                    isActive = false;
                    gameObject.SetActive(false);
                    break;
            }
        }

        public Bounds GetWorldBounds()
        {
            if (worldData == null)
                return new Bounds(transform.position, Vector3.one * 8f);

            Vector3 size = new Vector3(
                worldData.gridDimensions.x * gridSettings.cellSize,
                worldData.gridDimensions.y * gridSettings.cellSize,
                worldData.gridDimensions.z * gridSettings.cellSize
            );

            Vector3 center = transform.position + size * 0.5f;
            return new Bounds(center, size);
        }

        public void OnWorldEnter()
        {
            // Called when player enters this world in first-person mode
            Debug.Log($"Entering world: {worldData?.worldTitle}");
        }

        public void OnWorldExit()
        {
            // Called when player exits this world
            Debug.Log($"Exiting world: {worldData?.worldTitle}");
        }

        private void OnDestroy()
        {
            // Cleanup when world is destroyed
        }
    }

    public enum ActivationLevel
    {
        Inactive,      // Completely disabled
        Preloaded,     // Active but simplified
        FullyActive    // Full functionality
    }
}

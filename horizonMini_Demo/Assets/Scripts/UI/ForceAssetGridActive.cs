using UnityEngine;

namespace HorizonMini.UI
{
    /// <summary>
    /// Forces AssetGridContainer to stay active
    /// Temporary debug script to fix activation issues
    /// </summary>
    public class ForceAssetGridActive : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log($"ForceAssetGridActive: Activating {gameObject.name}");
            gameObject.SetActive(true);
        }

        private void Update()
        {
            // Force active every frame
            if (!gameObject.activeSelf)
            {
                Debug.LogWarning($"ForceAssetGridActive: {gameObject.name} was disabled! Re-activating...");
                gameObject.SetActive(true);
            }
        }
    }
}

using UnityEngine;

namespace HorizonMini.MiniGames
{
    /// <summary>
    /// Fixes CubeStack game to work with world positioning
    /// CubeStack uses absolute world coordinates, so we need to offset all containers
    /// </summary>
    public class CubeStackPositionFix : MonoBehaviour
    {
        private Vector3 _gameOffset;
        private bool _isFixed = false;

        public void SetGameOffset(Vector3 offset)
        {
            _gameOffset = offset;
            Debug.Log($"[CubeStackPositionFix] Game offset set to: {offset}");
        }

        private void Start()
        {
            // Wait one frame for BlockManager to create containers
            StartCoroutine(FixPositionNextFrame());
        }

        private System.Collections.IEnumerator FixPositionNextFrame()
        {
            yield return null; // Wait one frame

            if (!_isFixed)
            {
                FixContainerPositions();
                _isFixed = true;
            }
        }

        private void FixContainerPositions()
        {
            Debug.Log($"<color=yellow>[CubeStackPositionFix] Starting position fix...</color>");
            Debug.Log($"[CubeStackPositionFix] Game offset: {_gameOffset}");
            Debug.Log($"[CubeStackPositionFix] This GameObject position: {transform.position}");
            Debug.Log($"[CubeStackPositionFix] This GameObject localPosition: {transform.localPosition}");

            // Find all containers that need to be offset
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            Debug.Log($"[CubeStackPositionFix] Found {allChildren.Length} total children");

            int movedCount = 0;
            foreach (Transform child in allChildren)
            {
                // Find TowerContainer, DebrisContainer, etc
                if (child.name.Contains("Container") || child.name.Contains("Manager"))
                {
                    Vector3 oldPos = child.position;
                    child.position += _gameOffset;
                    Debug.Log($"[CubeStackPositionFix] Moved {child.name}: {oldPos} → {child.position}");
                    movedCount++;
                }
            }

            Debug.Log($"<color=green>[CubeStackPositionFix] ✓ Moved {movedCount} containers!</color>");
        }
    }
}

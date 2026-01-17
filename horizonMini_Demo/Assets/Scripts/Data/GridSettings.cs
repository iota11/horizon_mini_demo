using UnityEngine;

namespace HorizonMini.Data
{
    /// <summary>
    /// Global settings for the volume grid system
    /// </summary>
    [CreateAssetMenu(fileName = "GridSettings", menuName = "HorizonMini/GridSettings")]
    public class GridSettings : ScriptableObject
    {
        [Header("Volume Configuration")]
        [Tooltip("Size of each volume cell in Unity units")]
        public float cellSize = 8f;

        [Header("Grid Constraints")]
        public int maxGridWidth = 10;
        public int maxGridHeight = 10;
        public int maxGridDepth = 10;

        public Vector3 GetCellSize() => new Vector3(cellSize, cellSize, cellSize);

        public Vector3 GridToWorldPosition(Vector3Int gridPos)
        {
            return new Vector3(
                gridPos.x * cellSize,
                gridPos.y * cellSize,
                gridPos.z * cellSize
            );
        }

        public Vector3Int WorldToGridPosition(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x / cellSize),
                Mathf.RoundToInt(worldPos.y / cellSize),
                Mathf.RoundToInt(worldPos.z / cellSize)
            );
        }
    }
}

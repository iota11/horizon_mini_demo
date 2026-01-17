using UnityEngine;
using System;

namespace HorizonMini.Data
{
    /// <summary>
    /// Represents a single volume cell in the grid
    /// </summary>
    [Serializable]
    public struct VolumeCell
    {
        public Vector3Int gridPosition;
        public string volumeType; // For future: different volume styles/materials
        public int rotationY; // 0, 90, 180, 270

        public VolumeCell(Vector3Int gridPos, string type = "default", int rotation = 0)
        {
            gridPosition = gridPos;
            volumeType = type;
            rotationY = rotation;
        }

        public VolumeCell(int x, int y, int z, string type = "default", int rotation = 0)
        {
            gridPosition = new Vector3Int(x, y, z);
            volumeType = type;
            rotationY = rotation;
        }

        public override bool Equals(object obj)
        {
            if (obj is VolumeCell other)
            {
                return gridPosition == other.gridPosition;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return gridPosition.GetHashCode();
        }

        public static bool operator ==(VolumeCell a, VolumeCell b)
        {
            return a.gridPosition == b.gridPosition;
        }

        public static bool operator !=(VolumeCell a, VolumeCell b)
        {
            return a.gridPosition != b.gridPosition;
        }
    }
}

using UnityEngine;
using System;

namespace HorizonMini.Build
{
    /// <summary>
    /// Component attached to every placed object in the scene
    /// </summary>
    public class PlacedObject : MonoBehaviour
    {
        public string objectId;
        public string assetId;
        public PlaceableAsset sourceAsset;

        [Header("Serialization")]
        public Vector3 savedPosition;
        public Quaternion savedRotation;
        public Vector3 savedScale;

        [Header("Procedural Object Data")]
        public Vector3 savedControlPoint; // For SmartTerrain, SmartTerrainChunk (XYZ control point)

        private void Awake()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                objectId = Guid.NewGuid().ToString();
            }
        }

        public void UpdateSavedTransform()
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
            savedScale = transform.localScale;

            // Save control point data for procedural objects
            SmartTerrainChunk chunk = GetComponent<SmartTerrainChunk>();
            if (chunk != null && chunk.xyzControlPoint != null)
            {
                savedControlPoint = chunk.xyzControlPoint.localPosition;
            }

            SmartTerrain terrain = GetComponent<SmartTerrain>();
            if (terrain != null && terrain.controlPoint != null)
            {
                savedControlPoint = terrain.controlPoint.localPosition;
            }
        }

        public PlacedObjectData ToData()
        {
            UpdateSavedTransform();
            return new PlacedObjectData
            {
                objectId = objectId,
                assetId = assetId,
                position = savedPosition,
                rotation = savedRotation,
                scale = savedScale,
                controlPoint = savedControlPoint
            };
        }

        public void LoadFromData(PlacedObjectData data)
        {
            // Restore control point for procedural objects
            SmartTerrainChunk chunk = GetComponent<SmartTerrainChunk>();
            if (chunk != null && chunk.xyzControlPoint != null && data.controlPoint != Vector3.zero)
            {
                chunk.SetControlPointPosition(data.controlPoint, forceImmediate: true);
            }

            SmartTerrain terrain = GetComponent<SmartTerrain>();
            if (terrain != null && terrain.controlPoint != null && data.controlPoint != Vector3.zero)
            {
                terrain.SetControlPointPosition(data.controlPoint, forceImmediate: true);
            }
        }
    }

    [Serializable]
    public class PlacedObjectData
    {
        public string objectId;
        public string assetId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Vector3 controlPoint; // For procedural objects (SmartTerrain, SmartTerrainChunk)
    }
}

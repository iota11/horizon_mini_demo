using UnityEngine;
using System.Collections.Generic;

namespace HorizonMini.Build
{
    /// <summary>
    /// Manages all SpawnPoint instances and their cursors
    /// Ensures only active spawn point shows cursor
    /// </summary>
    public class SpawnPointManager : MonoBehaviour
    {
        private static SpawnPointManager instance;
        private static bool isQuitting = false;

        public static SpawnPointManager Instance
        {
            get
            {
                if (isQuitting)
                {
                    return null;
                }

                if (instance == null)
                {
                    GameObject obj = new GameObject("SpawnPointManager");
                    instance = obj.AddComponent<SpawnPointManager>();
                }
                return instance;
            }
        }

        private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
        private List<SpawnPointCursor> cursors = new List<SpawnPointCursor>();
        private SpawnPoint activeSpawnPoint = null;

        private void Awake()
        {
            // Reset quitting flag when new instance is created (scene loaded)
            isQuitting = false;

            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            // Clear singleton reference when destroyed
            if (instance == this)
            {
                instance = null;
                isQuitting = true;
            }
        }

        private void OnApplicationQuit()
        {
            isQuitting = true;
        }

        /// <summary>
        /// Register a SpawnPoint
        /// </summary>
        public void RegisterSpawnPoint(SpawnPoint spawnPoint)
        {
            if (!spawnPoints.Contains(spawnPoint))
            {
                spawnPoints.Add(spawnPoint);
            }
        }

        /// <summary>
        /// Unregister a SpawnPoint
        /// </summary>
        public void UnregisterSpawnPoint(SpawnPoint spawnPoint)
        {
            spawnPoints.Remove(spawnPoint);
            if (activeSpawnPoint == spawnPoint)
            {
                activeSpawnPoint = null;
                HideAllCursors();
            }
        }

        /// <summary>
        /// Register a SpawnPointCursor
        /// NOTE: SpawnPointCursor is no longer used. SpawnPoint now uses ObjectCursor.
        /// </summary>
        public void RegisterCursor(SpawnPointCursor cursor)
        {
            // SpawnPointCursor no longer used - do nothing
        }

        /// <summary>
        /// Unregister a SpawnPointCursor
        /// NOTE: SpawnPointCursor is no longer used. SpawnPoint now uses ObjectCursor.
        /// </summary>
        public void UnregisterCursor(SpawnPointCursor cursor)
        {
            // SpawnPointCursor no longer used - do nothing
        }

        /// <summary>
        /// Set active spawn point (only this spawn point's cursor will be visible)
        /// </summary>
        public void SetActiveSpawnPoint(SpawnPoint spawnPoint)
        {
            if (activeSpawnPoint == spawnPoint)
                return;

            activeSpawnPoint = spawnPoint;
            UpdateCursorVisibility();
        }

        /// <summary>
        /// Get the currently active spawn point
        /// </summary>
        public SpawnPoint GetActiveSpawnPoint()
        {
            return activeSpawnPoint;
        }

        /// <summary>
        /// Check if any cursor is currently being dragged
        /// NOTE: SpawnPointCursor is no longer used. SpawnPoint now uses ObjectCursor.
        /// </summary>
        public bool IsAnySpawnPointCursorDragging()
        {
            // SpawnPointCursor no longer used - always return false
            return false;
        }

        /// <summary>
        /// Update cursor visibility based on active spawn point
        /// </summary>
        private void UpdateCursorVisibility()
        {
            foreach (var cursor in cursors)
            {
                if (cursor == null)
                    continue;

                SpawnPoint cursorSpawnPoint = cursor.GetSpawnPoint();
                bool shouldBeVisible = (cursorSpawnPoint == activeSpawnPoint);
                cursor.SetVisible(shouldBeVisible);
            }
        }

        /// <summary>
        /// Hide all cursors
        /// </summary>
        public void HideAllCursors()
        {
            foreach (var cursor in cursors)
            {
                if (cursor != null)
                    cursor.SetVisible(false);
            }
        }

        /// <summary>
        /// Show cursor for specific spawn point
        /// </summary>
        public void ShowCursorForSpawnPoint(SpawnPoint spawnPoint)
        {
            SetActiveSpawnPoint(spawnPoint);
        }

        /// <summary>
        /// Enter View mode - hide all cursors
        /// </summary>
        public void EnterViewMode()
        {
            activeSpawnPoint = null;
            HideAllCursors();
        }

        /// <summary>
        /// Enter Edit mode - show cursor for active spawn point
        /// </summary>
        public void EnterEditMode()
        {
            if (activeSpawnPoint != null)
            {
                UpdateCursorVisibility();
            }
        }

        /// <summary>
        /// Clear active spawn point (deselect)
        /// </summary>
        public void ClearActiveSpawnPoint()
        {
            activeSpawnPoint = null;
            HideAllCursors();
        }

        /// <summary>
        /// Get all spawn points
        /// </summary>
        public List<SpawnPoint> GetAllSpawnPoints()
        {
            return new List<SpawnPoint>(spawnPoints);
        }
    }
}

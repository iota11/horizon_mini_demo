using UnityEngine;
using System.Collections.Generic;

namespace HorizonMini.Build
{
    /// <summary>
    /// Manages all SmartWall instances and their cursors
    /// Ensures only active wall shows cursors
    /// </summary>
    public class SmartWallManager : MonoBehaviour
    {
        private static SmartWallManager instance;
        private static bool isQuitting = false;

        public static SmartWallManager Instance
        {
            get
            {
                if (isQuitting)
                {
                    return null;
                }

                if (instance == null)
                {
                    GameObject obj = new GameObject("SmartWallManager");
                    instance = obj.AddComponent<SmartWallManager>();
                }
                return instance;
            }
        }

        private List<SmartWall> walls = new List<SmartWall>();
        private List<SmartWallCursor> cursors = new List<SmartWallCursor>();
        private SmartWall activeWall = null;

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
        /// Register a SmartWall
        /// </summary>
        public void RegisterWall(SmartWall wall)
        {
            if (!walls.Contains(wall))
            {
                walls.Add(wall);
            }
        }

        /// <summary>
        /// Unregister a SmartWall
        /// </summary>
        public void UnregisterWall(SmartWall wall)
        {
            walls.Remove(wall);
            if (activeWall == wall)
            {
                activeWall = null;
                HideAllCursors();
            }
        }

        /// <summary>
        /// Register a SmartWallCursor
        /// </summary>
        public void RegisterCursor(SmartWallCursor cursor)
        {
            if (!cursors.Contains(cursor))
            {
                cursors.Add(cursor);
            }
        }

        /// <summary>
        /// Unregister a SmartWallCursor
        /// </summary>
        public void UnregisterCursor(SmartWallCursor cursor)
        {
            cursors.Remove(cursor);
        }

        /// <summary>
        /// Set active wall (only this wall's cursors will be visible)
        /// </summary>
        public void SetActiveWall(SmartWall wall)
        {
            if (activeWall == wall)
                return;

            activeWall = wall;
            UpdateCursorVisibility();
        }

        /// <summary>
        /// Get the currently active wall
        /// </summary>
        public SmartWall GetActiveWall()
        {
            return activeWall;
        }

        /// <summary>
        /// Check if any cursor is currently being dragged
        /// </summary>
        public bool IsAnyWallCursorDragging()
        {
            foreach (var cursor in cursors)
            {
                if (cursor != null && cursor.IsDragging())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Update cursor visibility based on active wall
        /// </summary>
        private void UpdateCursorVisibility()
        {
            foreach (var cursor in cursors)
            {
                if (cursor == null)
                    continue;

                SmartWall cursorWall = cursor.GetSmartWall();
                bool shouldBeVisible = (cursorWall == activeWall);
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
        /// Show cursors for specific wall
        /// </summary>
        public void ShowCursorsForWall(SmartWall wall)
        {
            SetActiveWall(wall);
        }

        /// <summary>
        /// Enter View mode - hide all cursors
        /// </summary>
        public void EnterViewMode()
        {
            activeWall = null;
            HideAllCursors();
        }

        /// <summary>
        /// Enter Edit mode - show cursors for active wall
        /// </summary>
        public void EnterEditMode()
        {
            if (activeWall != null)
            {
                UpdateCursorVisibility();
            }
        }

        /// <summary>
        /// Clear active wall (deselect)
        /// </summary>
        public void ClearActiveWall()
        {
            activeWall = null;
            HideAllCursors();
        }
    }
}

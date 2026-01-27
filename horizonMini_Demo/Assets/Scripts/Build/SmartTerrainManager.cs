using UnityEngine;
using System.Collections.Generic;

namespace HorizonMini.Build
{
    /// <summary>
    /// Manages visibility of all SmartTerrain control points and cursors
    /// Handles Edit/View mode switching
    /// </summary>
    public class SmartTerrainManager : MonoBehaviour
    {
        private static SmartTerrainManager instance;
        private static bool isQuitting = false;

        public static SmartTerrainManager Instance
        {
            get
            {
                if (isQuitting)
                {
                    return null;
                }

                if (instance == null)
                {
                    GameObject managerObj = new GameObject("SmartTerrainManager");
                    instance = managerObj.AddComponent<SmartTerrainManager>();
                    // Don't use DontDestroyOnLoad - let each scene have its own manager
                }
                return instance;
            }
        }

        [Header("Settings")]
        [SerializeField] private bool showControlsInEditMode = true;
        [SerializeField] private bool showControlsInViewMode = false;

        // Track all SmartTerrain instances
        private HashSet<SmartTerrain> registeredTerrains = new HashSet<SmartTerrain>();
        private HashSet<SmartTerrainCursor> registeredCursors = new HashSet<SmartTerrainCursor>();

        private bool isEditMode = false;
        private SmartTerrain activeTerrain = null; // Currently active/selected terrain

        private void Awake()
        {
            // Reset quitting flag when new instance is created (scene loaded)
            isQuitting = false;

            if (instance == null)
            {
                instance = this;
                // Don't use DontDestroyOnLoad - let each scene have its own manager
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
        /// Register a SmartTerrain for management
        /// </summary>
        public void RegisterTerrain(SmartTerrain terrain)
        {
            if (terrain != null && !registeredTerrains.Contains(terrain))
            {
                registeredTerrains.Add(terrain);

                // Apply current mode visibility
                terrain.SetControlPointVisible(isEditMode ? showControlsInEditMode : showControlsInViewMode);
            }
        }

        /// <summary>
        /// Unregister a SmartTerrain
        /// </summary>
        public void UnregisterTerrain(SmartTerrain terrain)
        {
            if (terrain != null)
            {
                registeredTerrains.Remove(terrain);
            }
        }

        /// <summary>
        /// Register a SmartTerrainCursor for management
        /// </summary>
        public void RegisterCursor(SmartTerrainCursor cursor)
        {
            if (cursor != null && !registeredCursors.Contains(cursor))
            {
                registeredCursors.Add(cursor);

                // Apply current mode visibility
                cursor.SetVisible(isEditMode ? showControlsInEditMode : showControlsInViewMode);
            }
        }

        /// <summary>
        /// Unregister a SmartTerrainCursor
        /// </summary>
        public void UnregisterCursor(SmartTerrainCursor cursor)
        {
            if (cursor != null)
            {
                registeredCursors.Remove(cursor);
            }
        }

        /// <summary>
        /// Switch to Edit Mode - show all control points and cursors
        /// </summary>
        public void EnterEditMode()
        {
            isEditMode = true;
            UpdateAllVisibility();
        }

        /// <summary>
        /// Switch to View Mode - hide all control points and cursors
        /// </summary>
        public void EnterViewMode()
        {
            isEditMode = false;
            UpdateAllVisibility();
        }

        /// <summary>
        /// Toggle between Edit and View modes
        /// </summary>
        public void ToggleMode()
        {
            if (isEditMode)
                EnterViewMode();
            else
                EnterEditMode();
        }

        /// <summary>
        /// Get current mode
        /// </summary>
        public bool IsEditMode()
        {
            return isEditMode;
        }

        /// <summary>
        /// Update visibility of all registered SmartTerrains and cursors
        /// </summary>
        private void UpdateAllVisibility()
        {
            bool shouldShow = isEditMode ? showControlsInEditMode : showControlsInViewMode;

            // Update all terrains and cursors
            foreach (var terrain in registeredTerrains)
            {
                if (terrain != null)
                {
                    // Only show controls if this is the active terrain (or no terrain is active)
                    bool showForThisTerrain = shouldShow && (activeTerrain == null || activeTerrain == terrain);
                    terrain.SetControlPointVisible(showForThisTerrain);
                }
            }

            // Update cursors - only show for active terrain's cursor
            foreach (var cursor in registeredCursors)
            {
                if (cursor != null)
                {
                    SmartTerrain cursorTerrain = cursor.GetSmartTerrain();
                    bool showForThisCursor = shouldShow && (activeTerrain == null || activeTerrain == cursorTerrain);
                    cursor.SetVisible(showForThisCursor);
                }
            }

            Debug.Log($"SmartTerrain controls visibility: {(shouldShow ? "VISIBLE" : "HIDDEN")} (Mode: {(isEditMode ? "EDIT" : "VIEW")}), Active: {(activeTerrain != null ? activeTerrain.name : "None")}");
        }

        /// <summary>
        /// Clean up null references
        /// </summary>
        private void Update()
        {
            // Clean up destroyed terrains
            registeredTerrains.RemoveWhere(t => t == null);
            registeredCursors.RemoveWhere(c => c == null);
        }

        /// <summary>
        /// Show controls for a specific SmartTerrain (override mode)
        /// </summary>
        public void ShowControlsForTerrain(SmartTerrain terrain)
        {
            if (terrain != null)
            {
                terrain.SetControlPointVisible(true);
            }
        }

        /// <summary>
        /// Hide controls for a specific SmartTerrain (override mode)
        /// </summary>
        public void HideControlsForTerrain(SmartTerrain terrain)
        {
            if (terrain != null)
            {
                terrain.SetControlPointVisible(false);
            }
        }

        /// <summary>
        /// Get count of registered terrains
        /// </summary>
        public int GetTerrainCount()
        {
            return registeredTerrains.Count;
        }

        /// <summary>
        /// Get count of registered cursors
        /// </summary>
        public int GetCursorCount()
        {
            return registeredCursors.Count;
        }

        /// <summary>
        /// Check if any SmartTerrainCursor is currently being dragged
        /// </summary>
        public bool IsAnyTerrainCursorDragging()
        {
            foreach (var cursor in registeredCursors)
            {
                if (cursor != null && cursor.IsDragging())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Set the active terrain (only this terrain will show controls)
        /// </summary>
        public void SetActiveTerrain(SmartTerrain terrain)
        {
            activeTerrain = terrain;
            UpdateAllVisibility();
            Debug.Log($"Active terrain set to: {(terrain != null ? terrain.name : "None")}");
        }

        /// <summary>
        /// Clear active terrain (all terrains will show controls based on mode)
        /// </summary>
        public void ClearActiveTerrain()
        {
            activeTerrain = null;
            UpdateAllVisibility();
            Debug.Log("Active terrain cleared");
        }

        /// <summary>
        /// Get the currently active terrain
        /// </summary>
        public SmartTerrain GetActiveTerrain()
        {
            return activeTerrain;
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            // Debug UI in editor
            GUILayout.BeginArea(new Rect(10, 200, 300, 150));
            GUILayout.Label($"SmartTerrain Manager", new GUIStyle { fontSize = 14, fontStyle = FontStyle.Bold });
            GUILayout.Label($"Mode: {(isEditMode ? "EDIT" : "VIEW")}");
            GUILayout.Label($"Registered Terrains: {registeredTerrains.Count}");
            GUILayout.Label($"Registered Cursors: {registeredCursors.Count}");

            if (GUILayout.Button("Toggle Edit/View Mode"))
            {
                ToggleMode();
            }

            GUILayout.EndArea();
        }
#endif
    }
}

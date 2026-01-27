using UnityEngine;

namespace HorizonMini.Build.Cursors
{
    /// <summary>
    /// Interface for all cursor types
    /// </summary>
    public interface ICursor
    {
        /// <summary>
        /// Show or hide the cursor
        /// </summary>
        void SetVisible(bool visible);

        /// <summary>
        /// Check if cursor is currently visible
        /// </summary>
        bool IsVisible();

        /// <summary>
        /// Check if cursor is currently being dragged
        /// </summary>
        bool IsDragging();

        /// <summary>
        /// Update cursor position (called every frame when active)
        /// </summary>
        void UpdatePosition(Vector3 position);
    }
}

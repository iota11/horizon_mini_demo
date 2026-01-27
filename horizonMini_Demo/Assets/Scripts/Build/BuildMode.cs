using UnityEngine;

namespace HorizonMini.Build
{
    /// <summary>
    /// Build mode states
    /// </summary>
    public enum BuildMode
    {
        SizePicker,    // Choosing volume grid size (LEGACY - now uses VolumeDrawing)
        VolumeDrawing, // Drawing volume grid with 3D cursor
        View,          // Navigation and selection
        Edit,          // Editing selected object with cursor (move/rotate/delete)
        Play           // FPS test mode
    }
}

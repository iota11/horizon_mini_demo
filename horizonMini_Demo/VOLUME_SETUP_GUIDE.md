# Volume Setup Guide

## Problem 1: UI Blocking Volume View

### Solution: Reposition Size Picker UI

In Unity Editor:

1. **Open your scene** (BuildMode.unity)

2. **Find VolumeSizePickerPanel** in Hierarchy:
   ```
   BuildModeCanvas
   └─ VolumeSizePickerPanel
   ```

3. **Adjust the RectTransform**:
   - Select `VolumeSizePickerPanel`
   - In Inspector → Rect Transform

   **Option A: Move to Bottom**
   ```
   Anchors: Bottom-Center
   Pos Y: 100 (or adjust to preference)
   Width: 400
   Height: 200
   ```

   **Option B: Move to Side**
   ```
   Anchors: Left-Middle
   Pos X: 200
   Pos Y: 0
   Width: 300
   Height: 400
   ```

4. **Alternative: Make UI Compact**
   - Reduce the panel size
   - Stack controls vertically
   - Use smaller fonts

### Quick Fix in Code

If you want the UI to auto-position at bottom, add this to `VolumeSizePickerUI.cs`:

```csharp
private void Start()
{
    // Auto-position at bottom
    RectTransform rt = panel.GetComponent<RectTransform>();
    if (rt != null)
    {
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 100);
    }

    // ... existing code
}
```

---

## Problem 2: How to Assign Material to Volume

### Method 1: Assign Material in Inspector (Recommended)

**Step 1: Create Your Material**
1. Project window → Right-click → Create → Material
2. Name it "VolumeGridMaterial"
3. Shader: Select `HorizonMini/VolumeGrid_URP` (or `HorizonMini/VolumeGrid`)
4. Assign your grid texture to `Grid Texture` slot
5. Adjust `Grid Scale` (e.g., 1.0 for 1 grid per meter)

**Step 2: Assign to VolumeGrid Component**

Since VolumeGrid is created at runtime, you have two options:

**Option A: Via BuildController**
1. Create a VolumeGrid prefab:
   - GameObject → 3D Object → Cube
   - Add `VolumeGrid` component
   - Delete the MeshRenderer
   - Assign your material to `Bounds Material` field
   - Save as prefab in `Assets/Prefabs/VolumeGridPrefab.prefab`

2. Assign to BuildController:
   - Select `BuildController` in scene
   - Drag `VolumeGridPrefab` to `Volume Grid Prefab` field

**Option B: Direct Assignment After Creation**
1. Run the game once
2. Find the created `VolumeGrid` GameObject in Hierarchy
3. In Inspector → VolumeGrid component → `Bounds Material`
4. Drag your material here
5. Stop and restart the game (material won't persist)

**Option C: Set Material in Code**

Modify `VolumeGrid.cs` to use a serialized material reference:

```csharp
[Header("Visualization")]
[SerializeField] private Material boundsMaterialPrefab; // Assign in prefab
public Material boundsMaterial; // Runtime material
```

### Method 2: Assign Texture Directly

If you just want to use a texture without creating a material:

1. **Select VolumeGrid in Hierarchy** (when game is running)
2. **In Inspector**:
   ```
   Grid Texture: Drag your grid texture here
   Grid Scale: 1.0 (adjust as needed)
   ```

The code will auto-create a material with the texture.

### Method 3: Set via BuildController Reference

Add to `BuildController.cs`:

```csharp
[Header("Volume Settings")]
[SerializeField] private Material volumeMaterial;
[SerializeField] private Texture2D volumeTexture;
[SerializeField] private float volumeGridScale = 1.0f;
```

Then in `UpdateVolumePreview()`:

```csharp
currentVolumeGrid = gridObj.AddComponent<VolumeGrid>();
if (volumeMaterial != null)
{
    currentVolumeGrid.boundsMaterial = volumeMaterial;
}
if (volumeTexture != null)
{
    currentVolumeGrid.SetGridTexture(volumeTexture);
}
currentVolumeGrid.SetGridScale(volumeGridScale);
currentVolumeGrid.Initialize(dimensions);
```

---

## Testing the Setup

1. **Run the game**
2. **Check Console** for: `"BuildController started in standalone mode"`
3. **Verify**:
   - Volume appears with your grid material
   - UI is positioned correctly (not blocking view)
   - Sliders update volume size in real-time
   - Grid maintains consistent scale as volume changes

---

## Common Issues

### Grid Texture Not Showing
- Check Wrap Mode is set to **Repeat**
- Verify texture is assigned before `Initialize()` is called
- Check shader is `HorizonMini/VolumeGrid_URP` or `HorizonMini/VolumeGrid`

### Grid Scale Inconsistent
- Ensure `Grid Scale` matches your texture (1.0 = 1 grid per meter)
- Check texture tiling in material settings

### Material Shows as Pink
- Shader not found - verify shader files exist
- URP project needs `VolumeGrid_URP.shader`
- Built-in project needs `VolumeGrid.shader`

---

## Recommended Workflow

1. **Create Material Asset**:
   - `Assets/Materials/VolumeGridMat.mat`
   - Shader: `HorizonMini/VolumeGrid_URP`
   - Texture: Your grid texture (512x512, Repeat mode)
   - Grid Scale: 1.0

2. **Create VolumeGrid Prefab**:
   - Empty GameObject with VolumeGrid component
   - Assign material to `Bounds Material`
   - Save as `Assets/Prefabs/VolumeGridPrefab.prefab`

3. **Assign to BuildController**:
   - Select BuildController
   - Drag prefab to `Volume Grid Prefab` slot

4. **Run and Test**:
   - Volume appears with correct material
   - Grid scales properly
   - Lighting works (after shader update below)

---

## Next: Adding Lighting to Shader

See updated shaders below with full lighting support.

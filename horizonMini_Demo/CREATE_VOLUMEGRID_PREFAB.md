# How to Create VolumeGrid Prefab

## Step-by-Step Guide

### Step 1: Create the Material First

1. **In Project window**, navigate to `Assets/Materials/` (create folder if it doesn't exist)

2. **Right-click** → **Create** → **Material**

3. **Name it**: `VolumeGridMaterial`

4. **In Inspector**, configure the material:
   ```
   Shader: HorizonMini/VolumeGridLit_URP

   Grid Texture: [Drag your grid texture here]
   Tint Color: White (1, 1, 1, 1)
   Grid Scale: 1.0
   Smoothness: 0.5
   Metallic: 0.0
   AO Strength: 1.0
   ```

5. **Important**: Set your grid texture settings:
   - Select your texture in Project
   - In Inspector:
     - **Wrap Mode**: Repeat
     - **Filter Mode**: Bilinear or Trilinear
     - Click **Apply**

---

### Step 2: Create the VolumeGrid Prefab

#### Method A: Create from Empty GameObject (Recommended)

1. **In Hierarchy**, right-click → **Create Empty**

2. **Name it**: `VolumeGridPrefab`

3. **Add Component** → Search for `Volume Grid`
   - If VolumeGrid script is attached correctly, you'll see it in Inspector

4. **Configure VolumeGrid Component**:
   ```
   Grid Configuration:
   ├─ Volume Dimensions: (2, 1, 2)  [Default, will be overridden at runtime]
   └─ Volume Size: 8

   Visualization:
   ├─ Bounds Material: [Drag VolumeGridMaterial here]
   ├─ Show Bounds: ✓ Checked
   ├─ Grid Texture: [Optional - can also set via material]
   └─ Grid Scale: 1.0
   ```

5. **Create the Prefab**:
   - Drag `VolumeGridPrefab` from Hierarchy to `Assets/Prefabs/` folder
   - Unity will ask "Create Original Prefab" - Click **OK**

6. **Delete from Hierarchy** (the prefab asset is saved, you don't need the instance)

---

#### Method B: Create from Scene Instance (Alternative)

1. **Run the game once** to let BuildController create a VolumeGrid

2. **While game is running**:
   - Find `VolumeGrid` GameObject in Hierarchy
   - Assign your material to `Bounds Material`
   - Adjust settings

3. **Stop the game**

4. **Create again manually** following Method A with the settings you tested

---

### Step 3: Assign Prefab to BuildController

1. **In Scene Hierarchy**, find `BuildController` (or `BuildSystem`)

2. **In Inspector** → **BuildController component**:
   ```
   Prefabs:
   ├─ Volume Grid Prefab: [Drag VolumeGridPrefab here]
   ├─ Move Gizmo Prefab: (Leave empty for now)
   └─ Rotate Gizmo Prefab: (Leave empty for now)
   ```

3. **Save the scene**: Ctrl+S (Cmd+S on Mac)

---

### Step 4: Test the Prefab

1. **Run the game**

2. **Check**:
   - ✅ Volume appears with your grid material
   - ✅ Grid texture is visible
   - ✅ Lighting and shadows work
   - ✅ Adjusting sliders updates volume size
   - ✅ Material persists (not pink/purple)

3. **If volume is pink/purple**:
   - Shader not found
   - Check `Assets/Shaders/` has `VolumeGridLit_URP.shader`
   - Reimport shader: Right-click → Reimport

---

## Alternative: Minimal Prefab (Let Code Handle Material)

If you want the code to auto-create the material:

### Minimal Setup:

1. **Create Empty GameObject** → Name: `VolumeGridPrefab`

2. **Add Component**: `Volume Grid`

3. **Set only these**:
   ```
   Grid Texture: [Your grid texture]
   Grid Scale: 1.0
   ```

4. **Leave `Bounds Material` empty** - code will auto-create

5. **Save as Prefab**

The code will automatically:
- Find `VolumeGridLit_URP` shader
- Create material with your texture
- Apply proper settings

---

## Prefab Structure (Final Result)

```
VolumeGridPrefab (Prefab Asset)
└─ VolumeGrid (Script)
   ├─ Grid Configuration
   │  ├─ Volume Dimensions: (2, 1, 2)
   │  └─ Volume Size: 8
   └─ Visualization
      ├─ Bounds Material: VolumeGridMaterial
      ├─ Show Bounds: ✓
      ├─ Grid Texture: YourGridTexture
      └─ Grid Scale: 1.0
```

**Runtime behavior**:
- BuildController instantiates this prefab
- Calls `Initialize(dimensions)` with user-selected size
- Prefab's material settings are preserved

---

## Common Issues & Solutions

### Issue: "VolumeGrid component not found"

**Solution**:
- Script might not be compiled
- Check Console for errors
- Try: Assets → Reimport All

### Issue: "Material turns pink when running"

**Solution**:
- Shader not in build
- Add shader to "Always Included Shaders":
  - Edit → Project Settings → Graphics
  - Under "Always Included Shaders", add your shader

### Issue: "Grid texture doesn't tile correctly"

**Solution**:
- Texture import settings:
  - Wrap Mode: **Repeat** (not Clamp)
  - Compression: **None** or **Normal Quality**

### Issue: "Grid scale wrong after resize"

**Solution**:
- This is correct! Grid scale is in world-space
- `Grid Scale = 1.0` means 1 grid cell per meter
- Volume size changes, but grid density stays consistent

### Issue: "Prefab changes don't apply to scene"

**Solution**:
- Make sure you're editing the **prefab asset**, not instance
- Or: Right-click prefab → Apply changes

---

## Advanced: Multiple Material Variants

You can create multiple VolumeGrid prefabs with different materials:

### Setup:

1. **Create materials**:
   - `VolumeGridMaterial_Blue` (blue grid)
   - `VolumeGridMaterial_Red` (red grid)
   - `VolumeGridMaterial_Wireframe` (wireframe style)

2. **Create prefab variants**:
   - `VolumeGridPrefab_Blue`
   - `VolumeGridPrefab_Red`
   - `VolumeGridPrefab_Wireframe`

3. **Switch at runtime** (optional):
   ```csharp
   // In BuildController
   [SerializeField] private GameObject[] volumeGridPrefabs;
   [SerializeField] private int currentPrefabIndex = 0;

   // Use volumeGridPrefabs[currentPrefabIndex] when instantiating
   ```

---

## Quick Reference Card

### ✅ Checklist before running:

- [ ] Grid texture imported with Wrap Mode = Repeat
- [ ] Material created with VolumeGridLit_URP shader
- [ ] Material assigned to prefab's Bounds Material
- [ ] Prefab has VolumeGrid component
- [ ] Prefab assigned to BuildController's Volume Grid Prefab slot
- [ ] Scene saved

### 🎯 Expected result:

When you run the game:
1. Volume appears immediately in Size Picker mode
2. Grid texture visible and tiling correctly
3. Lighting and shadows work
4. Sliders update volume in real-time
5. Click Create → switches to View mode with volume intact

---

## Next Steps

After creating the prefab, you can:

1. **Adjust material in prefab** - changes apply to all future volumes
2. **Create prefab variants** - different grid styles
3. **Add custom properties** - like grid colors, transparency
4. **Test different grid scales** - find the perfect density

Your VolumeGrid prefab is now reusable and customizable! 🎉
